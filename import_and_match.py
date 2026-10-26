# import_and_match.py (fixed propagation)
# Запускать в IDA на целевой версии (БЕЗ debug-символов)
#
# ИСПРАВЛЕНО: called-набор у локальных функций строится ДИНАМИЧЕСКИ
# по EA колбэков, а не по статичным именам на момент сбора фич. Раньше
# почти все called-имена отфильтровывались как "sub_XXXX" и пропагация
# не находила ничего (0 совпадений). Теперь по мере роста числа
# совпадений called-набор для ещё несматченных функций тоже растёт.

import ida_funcs
import ida_name
import ida_ua
import idautils
import idc
import json
import hashlib
import time
import math
import os
import traceback
import re
from collections import defaultdict, Counter
import heapq

try:
    import orjson
except ImportError:
    orjson = None

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else os.getcwd()
DEFAULT_SOURCE = os.path.join(SCRIPT_DIR, "v67.jsonl")
LEGACY_SOURCE = r"C:\Users\daniel\Downloads\Telegram Desktop\v67.jsonl"
SRC_JSON = os.environ.get("LASER_MATCH_SOURCE", DEFAULT_SOURCE)
if not os.path.isfile(SRC_JSON) and os.path.isfile(LEGACY_SOURCE):
    SRC_JSON = LEGACY_SOURCE
REPORT_JSON = os.path.join(SCRIPT_DIR, "match.json")
LOG_PATH = os.path.join(SCRIPT_DIR, "import_and_match.log")
CHECKPOINT_JSON = os.path.join(SCRIPT_DIR, "match_checkpoint.json")
PROGRESS_EVERY = 250
MAX_FUZZY_CANDIDATES = 128
NUM_BUCKET = 8
SIZE_BUCKET = 64
MAX_INDEX_POSTINGS = 64
MAX_GRAPH_POSTINGS = 256
MIN_STRING_LENGTH = 4


_AUTO_NAME_RE = re.compile(r"^(?:sub|loc|unk|off|byte|word|dword|qword|stru|nullsub|j)_", re.I)
_HEX_RE = re.compile(r"\b(?:0x)?[0-9a-f]{8,}\b", re.I)
_NUMBER_RE = re.compile(r"\d+")
_SPACE_RE = re.compile(r"\s+")


def is_auto_name(name):
    return not name or bool(_AUTO_NAME_RE.match(name.lstrip("_")))


def symbol_key(name):
    """Canonical key for Mach-O names (IDA may add/remove one leading underscore)."""
    if not name:
        return ""
    while name.startswith("j_"):
        name = name[2:]
    return name.lstrip("_")


def cpp_owner_key(name):
    """Return the Itanium C++ namespace/class owner of a mangled method."""
    key = symbol_key(name)
    if not key.startswith("ZN"):
        return ""
    pos = 2
    if pos < len(key) and key[pos] == "K":
        pos += 1
    parts = []
    while pos < len(key) and key[pos].isdigit():
        digits_end = pos
        while digits_end < len(key) and key[digits_end].isdigit():
            digits_end += 1
        length = int(key[pos:digits_end])
        value = key[digits_end:digits_end + length]
        if len(value) != length:
            break
        parts.append(value)
        pos = digits_end + length
    # The last length-prefixed component is normally the method. Constructors
    # and destructors use C1/C2/D0 and leave only the owner components here.
    if len(parts) >= 2:
        parts.pop()
    return "::".join(parts)


def stable_string_key(value):
    """Normalize build/version-specific parts while retaining semantic text."""
    if not value or len(value) < 8:
        return ""
    value = value.lower().replace("\\", "/")
    value = _HEX_RE.sub("#", value)
    value = _NUMBER_RE.sub("#", value)
    value = _SPACE_RE.sub(" ", value).strip()
    if len(value) < 6 or value in ("#", "/#"):
        return ""
    return value


def log(message):
    line = "[%s] %s" % (time.strftime("%H:%M:%S"), message)
    print(line, flush=True)
    try:
        with open(LOG_PATH, "a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass


def save_json_atomic(path, data, indent=2):
    temp_path = path + ".tmp"
    with open(temp_path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=indent, separators=None if indent else (",", ":"))
        f.flush()
        os.fsync(f.fileno())
    os.replace(temp_path, path)


def save_checkpoint(report, matched_local_ea, matched_src_name, stage):
    save_json_atomic(CHECKPOINT_JSON, {
        "stage": stage,
        "saved_at": time.strftime("%Y-%m-%d %H:%M:%S"),
        "report": report,
        "matched_local_ea": {hex(ea): name for ea, name in matched_local_ea.items()},
        "matched_src_name": {name: hex(ea) for name, ea in matched_src_name.items()},
    }, indent=None)
    log("Checkpoint saved: %s (%d matches)" % (stage, len(matched_src_name)))


def load_source_features(path):
    """Load JSONL using orjson when available, with a fast stdlib fallback."""
    result = []
    started = time.time()
    append = result.append
    prepare = prepare_feature
    if orjson is not None:
        with open(path, "rb", buffering=4 * 1024 * 1024) as f:
            for line in f:
                if not line.isspace():
                    append(prepare(orjson.loads(line)))
    else:
        loads = json.loads
        with open(path, "r", encoding="utf-8", buffering=4 * 1024 * 1024) as f:
            for line in f:
                if not line.isspace():
                    append(prepare(loads(line)))
    log("Loaded %d source functions in %.1fs (%s)" % (
        len(result), time.time() - started, "orjson" if orjson is not None else "json"))
    return result


def build_string_index():
    log("Building string index...")
    t0 = time.time()
    func_strings = defaultdict(set)
    rare_postings = {}
    common_strings = set()
    normalized_postings = {}
    common_normalized = set()
    string_count = 0
    reference_count = 0

    for s in idautils.Strings():
        try:
            text = str(s)
        except Exception:
            continue
        if not text:
            continue

        string_count += 1
        for ref_ea in idautils.DataRefsTo(s.ea):
            f = ida_funcs.get_func(ref_ea)
            if f:
                func_ea = f.start_ea
                func_strings[func_ea].add(text)
                reference_count += 1

                if len(text) < MIN_STRING_LENGTH or text in common_strings:
                    continue
                postings = rare_postings.setdefault(text, set())
                postings.add(func_ea)
                if len(postings) > MAX_INDEX_POSTINGS:
                    del rare_postings[text]
                    common_strings.add(text)

                normalized = stable_string_key(text)
                if normalized and normalized not in common_normalized:
                    norm_hits = normalized_postings.setdefault(normalized, set())
                    norm_hits.add(func_ea)
                    if len(norm_hits) > MAX_INDEX_POSTINGS:
                        del normalized_postings[normalized]
                        common_normalized.add(normalized)

    indexed_strings = {
        func_ea: frozenset(values)
        for func_ea, values in func_strings.items()
    }
    indexed_postings = {
        value: tuple(functions)
        for value, functions in rare_postings.items()
    }
    indexed_normalized = {
        value: tuple(functions)
        for value, functions in normalized_postings.items()
    }
    log("String index ready: %d strings, %d references, %d functions, %d rare keys, %d normalized keys (%.1fs)" % (
        string_count, reference_count, len(indexed_strings), len(indexed_postings),
        len(indexed_normalized), time.time() - t0))
    return indexed_strings, indexed_postings, indexed_normalized


def normalize_insn_fast(insn):
    parts = [str(insn.itype)]
    for op in insn.ops:
        if op.type == ida_ua.o_void:
            break
        parts.append(str(op.type))
    return ",".join(parts)


def is_call(insn):
    return idc.print_insn_mnem(insn.ea).lower() in ("call", "bl", "blx", "blr")


def prepare_feature(feat):
    # Keep immutable, precomputed values on every feature.  The matcher calls
    # these helpers millions of times; rebuilding sets in the hot loop was a
    # measurable part of the old runtime.
    strings = feat.get("strings") or ()
    feat["_strings_set"] = strings if isinstance(strings, frozenset) else frozenset(filter(None, strings))
    feat["_string_keys"] = frozenset(filter(None, (stable_string_key(value) for value in strings)))
    feat["_called_set"] = frozenset(feat.get("called") or ())
    hist = feat.get("opcode_hist", {})
    feat["_opcode_norm"] = math.sqrt(sum(v * v for v in hist.values()))
    feat["_opcode_items"] = tuple(hist.items())
    feat["_num_insns"] = int(feat.get("num_insns", 0) or 0)
    feat["_size"] = int(feat.get("size", 0) or 0)
    return feat


def intersection_size(left, right):
    """Count a set intersection without allocating a temporary set."""
    if len(left) > len(right):
        left, right = right, left
    count = 0
    for value in left:
        if value in right:
            count += 1
    return count


def feature_similarity(src, dst):
    """Weighted similarity tolerant of compiler/build differences."""
    score = 0.0
    if src.get("norm_hash") and src["norm_hash"] == dst.get("norm_hash"):
        score += 0.62
    if src.get("mnemonic_hash") and src["mnemonic_hash"] == dst.get("mnemonic_hash"):
        score += 0.24
    sh, dh = src.get("opcode_hist", {}), dst.get("opcode_hist", {})
    if sh and dh:
        # Iterate over the smaller histogram instead of allocating a union for
        # every candidate pair.
        if len(sh) <= len(dh):
            dot = sum(value * dh.get(key, 0) for key, value in src.get("_opcode_items", sh.items()))
        else:
            dot = sum(value * sh.get(key, 0) for key, value in dst.get("_opcode_items", dh.items()))
        sl = src.get("_opcode_norm") or math.sqrt(sum(v * v for v in sh.values()))
        dl = dst.get("_opcode_norm") or math.sqrt(sum(v * v for v in dh.values()))
        if sl and dl:
            score += 0.12 * dot / (sl * dl)
    ss = src.get("_strings_set", frozenset())
    ds = dst.get("_strings_set", frozenset())
    if ss or ds:
        shared = intersection_size(ss, ds)
        score += 0.22 * shared / max(1, len(ss) + len(ds) - shared)
    sk = src.get("_string_keys", frozenset())
    dk = dst.get("_string_keys", frozenset())
    if sk or dk:
        shared_keys = intersection_size(sk, dk)
        score += 0.10 * shared_keys / max(1, len(sk) + len(dk) - shared_keys)
    sn, dn = src.get("_num_insns", src.get("num_insns", 0)), dst.get("_num_insns", dst.get("num_insns", 0))
    if sn and dn:
        score += 0.10 * math.exp(-abs(sn - dn) / max(sn, dn, 1))
    sz, dz = src.get("_size", src.get("size", 0)), dst.get("_size", dst.get("size", 0))
    if sz and dz:
        score += 0.08 * math.exp(-abs(sz - dz) / max(sz, dz, 1))
    return score


def candidate_score(src, dst, called_names=None):
    score = feature_similarity(src, dst)
    if called_names:
        expected = src.get("_called_set", frozenset())
        if expected:
            score += 0.24 * len(expected & called_names) / len(expected)
    return score


def cheap_similarity(src, dst):
    """A fast upper-level ranking score used before full similarity."""
    sn, dn = src.get("_num_insns", 0), dst.get("_num_insns", 0)
    sz, dz = src.get("_size", 0), dst.get("_size", 0)
    score = 0.0
    if src.get("mnemonic_hash") and src.get("mnemonic_hash") == dst.get("mnemonic_hash"):
        score += 4.0
    if src.get("norm_hash") and src.get("norm_hash") == dst.get("norm_hash"):
        score += 8.0
    ss, ds = src.get("_strings_set", frozenset()), dst.get("_strings_set", frozenset())
    if ss and ds:
        shared = intersection_size(ss, ds)
        score += 3.0 * shared / (len(ss) + len(ds) - shared)
    sk, dk = src.get("_string_keys", frozenset()), dst.get("_string_keys", frozenset())
    if sk and dk:
        shared_keys = intersection_size(sk, dk)
        score += 1.2 * shared_keys / (len(sk) + len(dk) - shared_keys)
    if sn and dn:
        score += math.exp(-abs(sn - dn) / max(sn, dn, 1))
    if sz and dz:
        score += math.exp(-abs(sz - dz) / max(sz, dz, 1))
    return score


def second_score(ranked):
    return ranked[1][1] if len(ranked) > 1 else 0.0


def find_mutual_matches(src_list, local, matched_src_name, matched_local_ea,
                        candidate_map, min_score, min_margin, use_calls=False):
    """Streaming mutual-best matching with O(function count) memory."""
    src_top = {}
    local_top = {}
    total = len(src_list)
    started = time.time()
    scored_pairs = 0

    def update_top(table, key, item, score):
        best_item, best_score, second = table.get(key, (None, -1.0, -1.0))
        if score > best_score:
            table[key] = (item, score, best_score)
        elif score > second:
            table[key] = (best_item, best_score, score)

    for index, src in enumerate(src_list, 1):
        name = src["name"]
        if name in matched_src_name:
            continue
        for ea in candidate_map(src):
            if ea in matched_local_ea:
                continue
            called = resolve_called_names(local[ea]["called_eas"], matched_local_ea, local) if use_calls else None
            score = candidate_score(src, local[ea], called)
            scored_pairs += 1
            update_top(src_top, name, ea, score)
            update_top(local_top, ea, name, score)
        if index % PROGRESS_EVERY == 0 or index == total:
            elapsed = time.time() - started
            rate = index / elapsed if elapsed else 0.0
            eta = (total - index) / rate if rate else 0.0
            log("Fuzzy scoring: %d/%d, %.1f sources/s, %d pairs, ETA %.1f min" %
                (index, total, rate, scored_pairs, eta / 60.0))

    accepted = []
    for src in src_list:
        name = src["name"]
        top = src_top.get(name)
        if not top:
            continue
        ea, score, src_second = top
        reverse = local_top.get(ea)
        if not reverse or reverse[0] != name:
            continue
        src_margin = score - max(0.0, src_second)
        dst_margin = score - max(0.0, reverse[2])
        if score >= min_score and src_margin >= min_margin and dst_margin >= min_margin:
            accepted.append((src, ea, score, min(src_margin, dst_margin)))
    log("Fuzzy scoring complete: %d pairs, %d mutual candidates (%.1fs)" %
        (scored_pairs, len(accepted), time.time() - started))
    return accepted


def collect_local_features():
    """called_eas — сырые адреса колбэков (не имена!). Имена будем
    резолвить динамически на каждом раунде пропагации."""
    func_strings_idx, rare_string_postings, normalized_string_postings = build_string_index()

    local = {}
    all_funcs = list(idautils.Functions())
    log(f"Collecting features for {len(all_funcs)} functions...")

    t0 = time.time()
    insn = ida_ua.insn_t()

    for i, func_ea in enumerate(all_funcs):
        func = ida_funcs.get_func(func_ea)
        if not func:
            continue
        start_ea, end_ea = func.start_ea, func.end_ea

        norm_parts = []
        mnemonic_parts = []
        opcode_hist = Counter()
        called_eas = set()
        num_insns = 0

        for item_ea in idautils.FuncItems(func_ea):
            length = ida_ua.decode_insn(insn, item_ea)
            if length == 0:
                continue
            num_insns += 1
            norm_parts.append(normalize_insn_fast(insn))
            mnemonic_parts.append(idc.print_insn_mnem(item_ea).lower())
            opcode_hist[str(insn.itype)] += 1

            if is_call(insn):
                for xref in idautils.CodeRefsFrom(item_ea, 0):
                    tf = ida_funcs.get_func(xref)
                    called_eas.add(tf.start_ea if tf else xref)

        norm_hash = hashlib.sha256("|".join(norm_parts).encode()).hexdigest()
        mnemonic_hash = hashlib.sha256("|".join(mnemonic_parts).encode()).hexdigest()
        strings = func_strings_idx.get(start_ea, frozenset())
        cur_name = ida_funcs.get_func_name(func_ea)

        local[func_ea] = prepare_feature({
            "ea": func_ea,
            "name": cur_name,
            "size": end_ea - start_ea,
            "num_insns": num_insns,
            "norm_hash": norm_hash,
            "mnemonic_hash": mnemonic_hash,
            "opcode_hist": dict(opcode_hist),
            "strings": strings,
            "called_eas": called_eas,
        })

        if (i + 1) % 2000 == 0:
            elapsed = time.time() - t0
            rate = (i + 1) / elapsed if elapsed > 0 else 0
            remaining = len(all_funcs) - i - 1
            eta_min = (remaining / rate / 60) if rate > 0 else 0
            log(f"Features: {i+1}/{len(all_funcs)}, {rate:.1f} functions/s, ETA {eta_min:.1f} min")

    log(f"Feature collection complete: {len(local)} functions ({time.time()-t0:.1f}s)")
    return local, rare_string_postings, normalized_string_postings


def resolve_called_names(called_eas, matched_local_ea, local):
    """Адреса колбэков -> имена, ДИНАМИЧЕСКИ: если колбэк уже сматчен -
    берём перенесённое имя; если это настоящий (не sub_/loc_) символ,
    уже присутствующий в целевой базе - берём его. Набор растёт по
    мере накопления совпадений на предыдущих раундах."""
    names = set()
    for ea in called_eas:
        if ea in matched_local_ea:
            names.add(matched_local_ea[ea])
            continue
        feat = local.get(ea)
        # All function EAs are collected in `local`; avoid an IDA name lookup
        # in the propagation hot loop whenever possible.
        nm = feat["name"] if feat else (ida_funcs.get_func_name(ea) or idc.get_name(ea))
        if nm and not nm.startswith(("sub_", "loc_")):
            names.add(nm)
    return names


def apply_name(ea, name, report_bucket, method, extra=None, current_name=None):
    cur = current_name if current_name is not None else ida_funcs.get_func_name(ea)
    if cur == name:
        return True
    if ida_name.set_name(ea, name, ida_name.SN_FORCE | ida_name.SN_NOWARN):
        entry = {"ea": hex(ea), "old_name": cur, "new_name": name, "method": method}
        if extra:
            entry.update(extra)
        report_bucket.append(entry)
        return True
    return False


def main():
    started_total = time.time()
    log("import_and_match started")
    src_list = load_source_features(SRC_JSON)
    local, rare_string_postings, normalized_string_postings = collect_local_features()
    log(f"Found {len(local)} target functions")

    report = {"existing_name": [], "exact_hash": [], "string_match": [], "fuzzy_match": [],
              "propagated": [], "ordered_windows": [], "class_windows": [],
              "ambiguous": [], "unmatched_count": 0}
    matched_local_ea = {}
    matched_src_name = {}

    # Round 0: reuse trustworthy names already present in both databases.  This
    # is especially important when resuming a previous Laser run: every reused
    # symbol immediately becomes a call-graph anchor instead of being ignored.
    source_by_key = defaultdict(list)
    local_by_key = defaultdict(list)
    for src in src_list:
        if not is_auto_name(src.get("name")):
            source_by_key[symbol_key(src["name"])].append(src)
    for ea, feat in local.items():
        if not is_auto_name(feat.get("name")):
            local_by_key[symbol_key(feat["name"])].append(ea)
    for key, sources in source_by_key.items():
        targets = local_by_key.get(key, ())
        if not key or len(sources) != 1 or len(targets) != 1:
            continue
        src, ea = sources[0], targets[0]
        if apply_name(ea, src["name"], report["existing_name"], "existing_name",
                      current_name=local[ea]["name"]):
            matched_local_ea[ea] = src["name"]
            matched_src_name[src["name"]] = ea
    log("Round 0, existing names: %d anchors" % len(matched_src_name))

    # Индексы строятся один раз.  В старой версии часть из них создавалась
    # только для одного раунда, а затем каждый источник снова сканировал
    # большие диапазоны функций.
    hash_buckets_local = defaultdict(list)
    mnemonic_buckets_local = defaultdict(list)
    for ea, feat in local.items():
        hash_buckets_local[feat["norm_hash"]].append(ea)
        if feat.get("mnemonic_hash"):
            mnemonic_buckets_local[feat["mnemonic_hash"]].append(ea)

    # ---------- Раунд 1: точный хеш ----------
    # При коллизии norm_hash выбираем только структурно лучший вариант и
    # требуем заметный отрыв. Это возвращает совпадения, которые раньше
    # без причины оставались пропущенными, не принимая случайный дубль.

    source_hash_counts = Counter(src["norm_hash"] for src in src_list)
    for src in src_list:
        # Duplicate source hashes can never pass this round's uniqueness rule.
        if source_hash_counts[src["norm_hash"]] != 1:
            continue
        candidates = [ea for ea in hash_buckets_local.get(src["norm_hash"], ())
                      if ea not in matched_local_ea]
        if not candidates:
            continue
        ranked = heapq.nlargest(2, ((cheap_similarity(src, local[ea]), ea) for ea in candidates))
        best_score, ea = ranked[0]
        second = ranked[1][0] if len(ranked) > 1 else -1.0
        # A unique exact hash is safe. A collision is accepted only if the
        # remaining shape features clearly agree as well.
        if len(ranked) == 1 or best_score - second >= 0.75:
            if apply_name(ea, src["name"], report["exact_hash"], "exact_hash",
                          current_name=local[ea]["name"]):
                matched_local_ea[ea] = src["name"]
                matched_src_name[src["name"]] = ea

    log(f"Round 1, exact hash: {len(report['exact_hash'])} matches")
    save_checkpoint(report, matched_local_ea, matched_src_name, "exact_hash")

    # ---------- Раунд 2: редкая строка + форма функции ----------
    for src in src_list:
        if src["name"] in matched_src_name or not src["strings"]:
            continue
        candidates = set()
        for value in src.get("_strings_set", ()):
            if len(value) >= MIN_STRING_LENGTH:
                candidates.update(rare_string_postings.get(value, ()))
        for value in src.get("_string_keys", ()):
            candidates.update(normalized_string_postings.get(value, ()))
        if not candidates:
            continue

        # One rare string is useful only when the function shape is compatible.
        ranked = heapq.nlargest(2, (
            (cheap_similarity(src, local[ea]), ea)
            for ea in candidates if ea not in matched_local_ea
        ))
        if not ranked:
            continue
        best_score, ea = ranked[0]
        second = ranked[1][0] if len(ranked) > 1 else -1.0
        shared = intersection_size(src["_strings_set"], local[ea]["_strings_set"])
        shared_normalized = intersection_size(src["_string_keys"], local[ea]["_string_keys"])
        required_score = 1.25 if shared else 1.65
        if (shared or shared_normalized) and best_score >= required_score and (len(ranked) == 1 or best_score - second >= 0.35):
            if apply_name(ea, src["name"], report["string_match"], "string_match",
                          {"shared_strings": shared, "shared_normalized_strings": shared_normalized},
                          current_name=local[ea]["name"]):
                matched_local_ea[ea] = src["name"]
                matched_src_name[src["name"]] = ea

    log(f"Round 2, string match: {len(report['string_match'])} matches")
    save_checkpoint(report, matched_local_ea, matched_src_name, "string_match")

    # ---------- Раунд 3: взаимное многосигнальное ранжирование ----------
    # Индексы сокращают число пар для scoring. Сначала используются сильные
    # признаки (хеш/мнемоника/редкая строка), а диапазоны формы служат
    # запасным вариантом для функций без таких признаков.
    # Keep both one-dimensional indexes. Candidate generation walks only the
    # smaller posting range and checks the other dimension in-place, instead
    # of materializing two large sets and intersecting them.
    local_num_buckets = defaultdict(list)
    local_size_buckets = defaultdict(list)
    for ea, feat in local.items():
        if ea in matched_local_ea:
            continue
        sn = feat.get("_num_insns", 0)
        sz = feat.get("_size", 0)
        local_num_buckets[sn // NUM_BUCKET].append(ea)
        local_size_buckets[sz // SIZE_BUCKET].append(ea)

    candidate_stats = {
        "sources": 0, "candidates": 0,
        "strong_pool": 0, "shape_fallback": 0, "shape_intersection": 0,
        "no_evidence": 0,
    }

    def fuzzy_candidates(src):
        candidate_stats["sources"] += 1
        ss = src.get("_strings_set", frozenset())
        sn, sz = src.get("_num_insns", 0), src.get("_size", 0)
        pool = set()
        same_norm = hash_buckets_local.get(src.get("norm_hash"), ())
        if same_norm and len(same_norm) <= MAX_FUZZY_CANDIDATES * 2:
            pool.update(same_norm)
        same_mnemonic = mnemonic_buckets_local.get(src.get("mnemonic_hash"), ())
        if same_mnemonic and len(same_mnemonic) <= MAX_INDEX_POSTINGS:
            pool.update(same_mnemonic)
        for value in ss:
            postings = rare_string_postings.get(value)
            if postings:
                pool.update(postings)

        def build_shape_pool():
            # This fallback is expensive on large binaries, so build it only
            # when strong hash/string indexes did not already give a bounded
            # pool. The resulting set is identical to the previous eager one.
            nb = sn // NUM_BUCKET if sn else 0
            sb = sz // SIZE_BUCKET if sz else 0
            num_radius = max(2, int(max(12, sn * 0.65) / NUM_BUCKET)) if sn else 4
            size_radius = max(2, int(max(96, sz * 0.75) / SIZE_BUCKET)) if sz else 4
            num_keys = range(nb - num_radius, nb + num_radius + 1)
            size_keys = range(sb - size_radius, sb + size_radius + 1)
            num_postings = sum(len(local_num_buckets.get(bucket, ())) for bucket in num_keys)
            size_postings = sum(len(local_size_buckets.get(bucket, ())) for bucket in size_keys)
            if num_postings <= size_postings:
                size_low, size_high = sb - size_radius, sb + size_radius
                return {
                    ea for bucket in num_keys for ea in local_num_buckets.get(bucket, ())
                    if size_low <= local[ea]["_size"] // SIZE_BUCKET <= size_high
                }
            num_low, num_high = nb - num_radius, nb + num_radius
            return {
                ea for bucket in size_keys for ea in local_size_buckets.get(bucket, ())
                if num_low <= local[ea]["_num_insns"] // NUM_BUCKET <= num_high
            }

        if not pool:
            # v67 contains roughly 100k Tencent/GCloud/Puerh/SDK functions
            # absent from v64. A global shape fallback compares all of them to
            # arbitrary Laser functions and dominates runtime. Functions with
            # no concrete evidence are handled later inside anchored C++
            # translation-unit/class windows instead.
            candidate_stats["no_evidence"] += 1
            return ()
        elif len(pool) > MAX_FUZZY_CANDIDATES * 20:
            # A common mnemonic/string can still produce a huge posting list.
            # Intersecting with shape bounds keeps recall while preventing a
            # full-binary scan.
            candidate_stats["shape_intersection"] += 1
            pool &= build_shape_pool()
        else:
            candidate_stats["strong_pool"] += 1

        def ranked_pool():
            for ea in pool:
                if ea in matched_local_ea:
                    continue
                dst = local[ea]
                dn, dz = dst.get("_num_insns", 0), dst.get("_size", 0)
                if sn and dn and abs(sn - dn) > max(12, int(max(sn, dn) * 0.65)):
                    continue
                if sz and dz and abs(sz - dz) > max(96, int(max(sz, dz) * 0.75)):
                    continue
                ds = dst.get("_strings_set", frozenset())
                source_keys = src.get("_string_keys", frozenset())
                target_keys = dst.get("_string_keys", frozenset())
                if (ss and ds and not any(value in ds for value in ss) and
                        not any(value in target_keys for value in source_keys) and
                        src.get("mnemonic_hash") != dst.get("mnemonic_hash")):
                    continue
                yield cheap_similarity(src, dst), ea

        # Stream candidates directly into the bounded heap. This keeps peak
        # memory stable even for unusually large buckets/posting lists.
        selected = [ea for _, ea in heapq.nlargest(MAX_FUZZY_CANDIDATES, ranked_pool())]
        candidate_stats["candidates"] += len(selected)
        return selected

    def has_global_fuzzy_evidence(src):
        norm_hits = hash_buckets_local.get(src.get("norm_hash"), ())
        if norm_hits and len(norm_hits) <= MAX_FUZZY_CANDIDATES * 2:
            return True
        mnemonic_hits = mnemonic_buckets_local.get(src.get("mnemonic_hash"), ())
        if mnemonic_hits and len(mnemonic_hits) <= MAX_INDEX_POSTINGS:
            return True
        return any(rare_string_postings.get(value) for value in src.get("_strings_set", ()))

    fuzzy_unmatched = [src for src in src_list if src["name"] not in matched_src_name]
    fuzzy_src_list = [src for src in fuzzy_unmatched if has_global_fuzzy_evidence(src)]
    candidate_stats["no_evidence"] = len(fuzzy_unmatched) - len(fuzzy_src_list)
    log("Global fuzzy input pruned: %d -> %d functions; %d absent/unsupported v67 functions deferred" %
        (len(fuzzy_unmatched), len(fuzzy_src_list), candidate_stats["no_evidence"]))
    fuzzy_matches = find_mutual_matches(fuzzy_src_list, local, matched_src_name, matched_local_ea,
                                        fuzzy_candidates, min_score=0.48, min_margin=0.09)
    for src, ea, score, margin in fuzzy_matches:
        if ea in matched_local_ea or src["name"] in matched_src_name:
            continue
        if apply_name(ea, src["name"], report["fuzzy_match"], "fuzzy_similarity",
                      {"score": round(score, 4), "margin": round(margin, 4)}):
            matched_local_ea[ea] = src["name"]
            matched_src_name[src["name"]] = ea
    log(f"Round 3, fuzzy match: {len(report['fuzzy_match'])} matches")
    if candidate_stats["sources"]:
        log("Candidate index: %d sources, %d candidates, %.1f average" % (
            candidate_stats["sources"], candidate_stats["candidates"],
            candidate_stats["candidates"] / float(candidate_stats["sources"])))
        log("Candidate paths: %d strong, %d shape fallback, %d large intersections" % (
            candidate_stats["strong_pool"], candidate_stats["shape_fallback"],
            candidate_stats["shape_intersection"]))
        log("Candidate pruning: %d source functions skipped without cross-version evidence" %
            candidate_stats["no_evidence"])
    log(f"Propagation anchors: {len(matched_local_ea)}")
    save_checkpoint(report, matched_local_ea, matched_src_name, "fuzzy_match")

    # ---------- Раунд 3+: propagation, ДИНАМИЧЕСКИЙ called-набор ----------
    # Build the source call inverse once. Filtering matched names is cheaper
    # than reconstructing the entire inverse map on every propagation pass.
    src_inv_tmp = defaultdict(list)
    for s in src_list:
        for cname in s.get("_called_set", frozenset(s.get("called", []))):
            src_inv_tmp[cname].append(s["name"])
    src_inv_all = {key: tuple(value) for key, value in src_inv_tmp.items()}
    source_by_name = {src["name"]: src for src in src_list}

    # Reverse target edges let us use callers as well as callees.  This is a
    # strong version-independent signal for Laser's many tiny C++ wrappers.
    local_callers = defaultdict(set)
    for caller_ea, feat in local.items():
        for callee_ea in feat.get("called_eas", ()):
            if callee_ea in local:
                local_callers[callee_ea].add(caller_ea)

    def graph_frontier():
        frontier = set()
        for anchor_ea in matched_local_ea:
            frontier.update(local_callers.get(anchor_ea, ()))
            for callee_ea in local[anchor_ea].get("called_eas", ()):
                if callee_ea in local:
                    frontier.add(callee_ea)
        frontier.difference_update(matched_local_ea)
        return frontier

    for min_signals in (2, 1):
        pending_eas = graph_frontier()
        for iteration in range(8):
            proposals = {}
            reverse_best = {}

            scanned = 0
            for ea in pending_eas:
                if ea in matched_local_ea:
                    continue
                feat = local.get(ea)
                if not feat:
                    continue
                scanned += 1

                called_now = resolve_called_names(feat["called_eas"], matched_local_ea, local)
                callers_now = resolve_called_names(local_callers.get(ea, ()), matched_local_ea, local)

                score_counter = Counter()
                for cname in called_now:
                    postings = src_inv_all.get(cname, ())
                    if len(postings) > MAX_GRAPH_POSTINGS:
                        continue
                    for src_name in postings:
                        if src_name not in matched_src_name:
                            score_counter[src_name] += 1

                # If a matched caller calls this local function, its source
                # counterpart must call the corresponding source candidate.
                for caller_name in callers_now:
                    caller_src = source_by_name.get(caller_name)
                    if not caller_src:
                        continue
                    for src_name in caller_src.get("_called_set", ()):
                        if src_name in source_by_name and src_name not in matched_src_name:
                            score_counter[src_name] += 1

                if not score_counter:
                    continue

                ranked = []
                for src_name, signals in score_counter.items():
                    src = source_by_name.get(src_name)
                    if not src or signals < min_signals:
                        continue
                    similarity = feature_similarity(src, feat)
                    if similarity < (0.18 if signals >= 2 else 0.50):
                        continue
                    ranked.append((signals + similarity, signals, similarity, src_name))
                if not ranked:
                    continue
                ranked.sort(reverse=True)
                best = ranked[0]
                second = ranked[1][0] if len(ranked) > 1 else 0.0
                margin = best[0] - second
                if margin < (0.08 if best[1] >= 2 else 0.15):
                    continue
                proposals[ea] = (best[3], best[0], best[1], best[2], margin)
                previous = reverse_best.get(best[3])
                if previous is None or best[0] > previous[1]:
                    reverse_best[best[3]] = (ea, best[0])

            new_matches = 0
            new_match_eas = []
            for ea, (best_name, combined, signals, similarity, margin) in proposals.items():
                if reverse_best.get(best_name, (None,))[0] != ea:
                    continue
                if ea in matched_local_ea or best_name in matched_src_name:
                    continue
                if apply_name(ea, best_name, report["propagated"], "bidirectional_call_graph",
                              {"signals": signals, "similarity": round(similarity, 4),
                               "score": round(combined, 4), "margin": round(margin, 4)}):
                    matched_local_ea[ea] = best_name
                    matched_src_name[best_name] = ea
                    new_matches += 1
                    new_match_eas.append(ea)

            log("Propagation signals=%d, pass %d: scanned %d frontier functions, +%d" %
                (min_signals, iteration + 1, scanned, new_matches))
            save_checkpoint(report, matched_local_ea, matched_src_name,
                            "propagation_%d_%d" % (min_signals, iteration + 1))
            if new_matches == 0:
                break

            # Only neighbors of newly named functions gained graph evidence.
            # Rechecking all 76k functions made every old pass cost 2-3 min.
            next_pending = set()
            for matched_ea in new_match_eas:
                next_pending.update(local_callers.get(matched_ea, ()))
                for callee_ea in local[matched_ea].get("called_eas", ()):
                    if callee_ea in local:
                        next_pending.add(callee_ea)
            next_pending.difference_update(matched_local_ea)
            pending_eas = next_pending

    # ---------- Round 5: ordered C++ translation-unit windows ----------
    # Supercell's linker keeps functions from the same class/.cpp together and
    # in a stable order.  When two already matched anchors enclose the same
    # number of functions in both builds, align that small window ordinally.
    # Requiring compatible features for the whole window avoids crossing areas
    # where v67 inserted or removed methods.
    source_order = sorted(src_list, key=lambda item: int(item.get("ea", 0)))
    local_order = sorted(local)
    source_pos = {src["name"]: index for index, src in enumerate(source_order)}
    local_pos = {ea: index for index, ea in enumerate(local_order)}

    for window_pass in range(8):
        anchors = sorted(
            (source_pos[name], local_pos[ea], name, ea)
            for name, ea in matched_src_name.items()
            if name in source_pos and ea in local_pos
        )
        additions = []
        reserved_names = set()
        reserved_eas = set()
        for left, right in zip(anchors, anchors[1:]):
            src_left, local_left = left[0], left[1]
            src_right, local_right = right[0], right[1]
            src_gap = src_right - src_left - 1
            local_gap = local_right - local_left - 1
            if src_gap != local_gap or not (1 <= src_gap <= 32) or local_right <= local_left:
                continue

            window = []
            valid = True
            for offset in range(1, src_gap + 1):
                src = source_order[src_left + offset]
                ea = local_order[local_left + offset]
                name = src["name"]
                if (name in matched_src_name or ea in matched_local_ea or
                        name in reserved_names or ea in reserved_eas):
                    valid = False
                    break
                similarity = feature_similarity(src, local[ea])
                if similarity < 0.18:
                    valid = False
                    break
                window.append((src, ea, similarity))
            if not valid or not window:
                continue
            average = sum(item[2] for item in window) / len(window)
            if average < 0.38:
                continue
            additions.extend(window)
            reserved_names.update(item[0]["name"] for item in window)
            reserved_eas.update(item[1] for item in window)

        new_matches = 0
        for src, ea, similarity in additions:
            name = src["name"]
            if name in matched_src_name or ea in matched_local_ea:
                continue
            if apply_name(ea, name, report["ordered_windows"], "ordered_cpp_window",
                          {"similarity": round(similarity, 4), "pass": window_pass + 1}):
                matched_local_ea[ea] = name
                matched_src_name[name] = ea
                new_matches += 1
        log("Ordered C++ windows, pass %d: +%d" % (window_pass + 1, new_matches))
        save_checkpoint(report, matched_local_ea, matched_src_name,
                        "ordered_windows_%d" % (window_pass + 1))
        if new_matches == 0:
            break

    # ---------- Round 6: class-local fuzzy matching ----------
    # Debug names expose the Itanium owner (for example
    # LogicClaimRandomRewardCommand). Two matched methods fence a very small
    # target range; inside it we can safely use a lower fuzzy threshold than a
    # 76k-function global search, even if v67 inserted extra methods.
    source_by_owner = defaultdict(list)
    for src in source_order:
        owner = cpp_owner_key(src.get("name", ""))
        if owner:
            source_by_owner[owner].append(src)

    for class_pass in range(6):
        additions = []
        reserved_names = set()
        reserved_eas = set()
        for owner, members in source_by_owner.items():
            if len(members) < 3:
                continue
            member_pos = {src["name"]: index for index, src in enumerate(members)}
            anchors = sorted(
                (member_pos[src["name"]], local_pos[matched_src_name[src["name"]]])
                for src in members
                if src["name"] in matched_src_name and matched_src_name[src["name"]] in local_pos
            )
            if len(anchors) < 2:
                continue
            for left, right in zip(anchors, anchors[1:]):
                src_left, local_left = left
                src_right, local_right = right
                if src_right <= src_left + 1 or local_right <= local_left + 1:
                    continue
                src_candidates = [
                    members[index] for index in range(src_left + 1, src_right)
                    if members[index]["name"] not in matched_src_name
                    and members[index]["name"] not in reserved_names
                ]
                local_candidates = [
                    local_order[index] for index in range(local_left + 1, local_right)
                    if local_order[index] not in matched_local_ea
                    and local_order[index] not in reserved_eas
                ]
                if not src_candidates or not local_candidates:
                    continue
                if len(src_candidates) > 32 or len(local_candidates) > 96:
                    continue

                src_best = {}
                local_best = {}
                for src_index, src in enumerate(src_candidates):
                    for local_index, ea in enumerate(local_candidates):
                        similarity = feature_similarity(src, local[ea])
                        if similarity < 0.32:
                            continue
                        src_relative = (src_index + 1.0) / (len(src_candidates) + 1.0)
                        local_relative = (local_index + 1.0) / (len(local_candidates) + 1.0)
                        score = similarity + 0.12 * math.exp(-4.0 * abs(src_relative - local_relative))
                        current = src_best.get(src["name"])
                        if current is None or score > current[1]:
                            src_best[src["name"]] = (ea, score, similarity)
                        current = local_best.get(ea)
                        if current is None or score > current[1]:
                            local_best[ea] = (src["name"], score)

                for src in src_candidates:
                    name = src["name"]
                    best = src_best.get(name)
                    if not best:
                        continue
                    ea, score, similarity = best
                    if local_best.get(ea, (None,))[0] != name:
                        continue
                    alternatives = sorted(
                        (feature_similarity(src, local[other]) for other in local_candidates if other != ea),
                        reverse=True
                    )
                    margin = similarity - (alternatives[0] if alternatives else 0.0)
                    if margin < 0.07:
                        continue
                    additions.append((src, ea, similarity, margin, owner))
                    reserved_names.add(name)
                    reserved_eas.add(ea)

        new_matches = 0
        for src, ea, similarity, margin, owner in additions:
            name = src["name"]
            if name in matched_src_name or ea in matched_local_ea:
                continue
            if apply_name(ea, name, report["class_windows"], "cpp_class_window",
                          {"owner": owner, "similarity": round(similarity, 4),
                           "margin": round(margin, 4), "pass": class_pass + 1}):
                matched_local_ea[ea] = name
                matched_src_name[name] = ea
                new_matches += 1
        log("C++ class windows, pass %d: +%d" % (class_pass + 1, new_matches))
        save_checkpoint(report, matched_local_ea, matched_src_name,
                        "class_windows_%d" % (class_pass + 1))
        if new_matches == 0:
            break

    report["unmatched_count"] = len(src_list) - len(matched_src_name)

    save_json_atomic(REPORT_JSON, report)

    total_applied = sum(len(report[key]) for key in
                        ("existing_name", "exact_hash", "string_match", "fuzzy_match",
                         "propagated", "ordered_windows", "class_windows"))
    log("Match summary")
    log(f"Applied names: {total_applied}")
    log(f"Existing-name anchors: {len(report['existing_name'])}")
    log(f"Exact hash: {len(report['exact_hash'])}")
    log(f"String match: {len(report['string_match'])}")
    log(f"Fuzzy match: {len(report['fuzzy_match'])}")
    log(f"Call graph: {len(report['propagated'])}")
    log(f"Ordered C++ windows: {len(report['ordered_windows'])}")
    log(f"C++ class windows: {len(report['class_windows'])}")
    log(f"Unmatched: {report['unmatched_count']}")
    log(f"Report: {REPORT_JSON} ({(time.time()-started_total)/60:.1f} min)")


try:
    main()
except Exception:
    log("Fatal error:\n" + traceback.format_exc())
    raise
