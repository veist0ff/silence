#!/usr/bin/env python3
"""Сборка патча контента: серверные Assets против fingerprint.json клиента.

Собираются два патча:
  small — для чистого APK SilenceBrawl (fingerprint оригинала). Файлы, которые уже
          лежат в APK (patcher/client_files.json), не качаются: клиент берёт их из APK.
  full  — для клиентов, у которых уже стоял какой-то патч. В нём все файлы с их
          настоящими хешами: скачанное раньше совпадёт и не перекачается, а в small
          такие клиенты увидели бы оригинальные хеши и полезли бы за файлами (404).
Сервер выбирает патч по ResourceSha клиента (current.json).

Модели, текстуры, .sc, звуки — так же, как CSV: положить файл в Assets/ по тому же
пути, что в клиенте (например Assets/sc3d/<имя>.glb), и запустить скрипт.
"""
import hashlib, json, os, shutil

ROOT = "/root/v64"
BASE = ROOT + "/patcher/base_fingerprint.json"
SRC = ROOT + "/HashCore.Laser.Server/bin/Debug/net10.0/Assets"
# Папки клиента, которые можно патчить. Остального (MapPrint.json и т.п.) клиент не знает.
DIRS = ["csv_logic", "csv_client", "localization", "sc", "sc3d", "sfx", "music",
        "json", "image", "badge", "font", "shader"]
WWW = "/var/www/patch"
CLIENT = ROOT + "/patcher/client_files.json"

base = json.load(open(BASE, encoding="utf-8"))
client_has = json.load(open(CLIENT)) if os.path.exists(CLIENT) else {}

# Все файлы сервера один раз: путь -> sha.
server = {}
for d in DIRS:
    top = os.path.join(SRC, d)
    if not os.path.isdir(top):
        continue
    for dirpath, _, names in os.walk(top):
        for name in sorted(names):
            full = os.path.join(dirpath, name)
            path = os.path.relpath(full, SRC).replace(os.sep, "/")
            server[path] = hashlib.sha1(open(full, "rb").read()).hexdigest()


def build(skip_client_files):
    files = {f["file"]: f["sha"] for f in base["files"]}
    order = [f["file"] for f in base["files"]]
    changed, added, skipped = [], [], 0
    for path in sorted(server):
        sha = server[path]
        if skip_client_files and client_has.get(path) == sha:
            skipped += 1
            # такой же файл уже есть в APK: в fingerprint остаётся оригинальный хеш.
            # Хеш APK тут ставить нельзя: тогда чистый APK качает весь патч и падает.
            continue
        if path not in files:
            files[path] = sha
            order.append(path)
            added.append(path)
        elif sha != files[path]:
            files[path] = sha
            changed.append(path)

    new_files = [{"file": p, "sha": files[p]} for p in order]
    root_sha = hashlib.sha1("".join(f["file"] + f["sha"] for f in new_files).encode()).hexdigest()
    text = json.dumps({"files": new_files, "sha": root_sha, "version": base["version"]},
                      separators=(",", ":"))

    out = os.path.join(WWW, root_sha)
    # Полный патч раздаёт ВСЕ файлы сервера: клиент после отката может запросить и те,
    # что совпадают с базой (например, вернувшийся оригинал).
    # Раздаём и файлы, пропущенные как «уже в APK»: у клиента с другой сборкой APK
    # их нет, он их запросит, и 404 роняет клиент.
    for path in sorted(server):
        dst = os.path.join(out, path)
        if not os.path.exists(dst):
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            try: os.link(os.path.join(SRC, path), dst)  # жёсткая ссылка: диск почти полон
            except OSError: shutil.copyfile(os.path.join(SRC, path), dst)
    os.makedirs(out, exist_ok=True)
    open(os.path.join(out, "fingerprint.json"), "w").write(text)
    return root_sha, text, changed, added, skipped


small_sha, small_text, changed, added, skipped = build(True)
full_sha, full_text, f_changed, f_added, _ = build(False)
os.system("python3 " + ROOT + "/patcher/backfill.py")
os.system("chown -R www-data:www-data " + WWW)

json.dump({
    "base_sha": base["sha"],
    "sha": small_sha, "fingerprint": small_text,
    "full_sha": full_sha, "full_fingerprint": full_text,
}, open(ROOT + "/patcher/current.json", "w"))

print("малый патч (чистый APK):", small_sha)
print("  изменено:", len(changed), " новых:", len(added), " уже в APK:", skipped)
for p in changed: print("   ", p)
for p in added: print("   +", p)
print("полный патч (у кого уже был патч):", full_sha)
print("  изменено:", len(f_changed), " новых:", len(f_added))
