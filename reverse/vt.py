#!/usr/bin/env python3
"""
Поиск encode/decode сообщения в клиентском бинаре по его message id.

Метод (arm64, как в tutor64.md):
  1. getMessageType компилируется в `movz w0, #id ; ret` -> ищем этот паттерн;
  2. адрес геттера лежит в vtable, слот 5 (0x28) -> ищем 8-байтовую ссылку;
  3. раскладка vtable: 0 ~D2, 1 ~D0, 2 encode, 3 decode, 4 getServiceNodeType,
     5 getMessageType, 6 getMessageTypeName, 7 destruct.

Работает и на стрипнутом бинаре с зашифрованными строками.
Для ELF, где первый LOAD имеет offset == vaddr == 0, файловое смещение равно vaddr.

  python3 vt.py <libg.so> 24109 10555        # адреса
  python3 vt.py <libg.so> 24109 --disasm     # + дизасм encode/decode через r2
  python3 vt.py <libg.so> --list             # все id, похожие на сообщения
"""
import struct
import subprocess
import sys

RET = 0xD65F03C0
MOVZ_W0 = 0x52800000
SLOT = {0: "~dtor D2", 1: "~dtor D0", 2: "encode", 3: "decode",
        4: "getServiceNodeType", 5: "getMessageType", 6: "getMessageTypeName", 7: "destruct"}


def load(path):
    with open(path, "rb") as f:
        return f.read()


def find_getters(data, msg_id):
    want = MOVZ_W0 | (msg_id << 5)
    out = []
    for off in range(0, len(data) - 8, 4):
        if struct.unpack_from("<I", data, off)[0] != want:
            continue
        if struct.unpack_from("<I", data, off + 4)[0] == RET:
            out.append(off)
    return out


def all_getter_ids(data):
    ids = {}
    for off in range(0, len(data) - 8, 4):
        w1 = struct.unpack_from("<I", data, off)[0]
        if (w1 & 0xFFE0001F) != MOVZ_W0:
            continue
        if struct.unpack_from("<I", data, off + 4)[0] != RET:
            continue
        ids.setdefault((w1 >> 5) & 0xFFFF, []).append(off)
    return ids


def find_vtables(data, getter_va):
    """Ссылки на геттер, выровненные по 8 -> начало vtable = ссылка - 0x28."""
    pat = struct.pack("<Q", getter_va)
    out, i = [], 0
    while True:
        i = data.find(pat, i)
        if i < 0:
            break
        if i % 8 == 0 and i >= 0x28:
            base = i - 0x28
            slots = [struct.unpack_from("<Q", data, base + 8 * k)[0] for k in range(8)]
            # у настоящей vtable сообщения все слоты - валидные адреса кода
            if all(0x1000 < s < len(data) for s in slots):
                out.append((base, slots))
        i += 1
    return out


def disasm(path, addr, n=120):
    try:
        return subprocess.run(["r2", "-q", "-c", f"s {hex(addr)}; pD {n * 4}", path],
                              capture_output=True, text=True, timeout=120).stdout
    except FileNotFoundError:
        return "(r2 не установлен: apt install radare2)"


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 1
    path = sys.argv[1]
    args = sys.argv[2:]
    data = load(path)

    if "--list" in args:
        ids = all_getter_ids(data)
        msg = sorted(i for i in ids if 8000 <= i <= 30000)
        print(f"геттеров всего: {len(ids)}; похожих на message id: {len(msg)}")
        print(msg)
        return 0

    want_disasm = "--disasm" in args
    for a in args:
        if a.startswith("--"):
            continue
        mid = int(a)
        getters = find_getters(data, mid)
        if not getters:
            print(f"{mid}: геттер не найден (другой id в этой версии, либо getMessageType заинлайнен)")
            continue
        for g in getters:
            print(f"{mid}: getMessageType @ {hex(g)}")
            for base, slots in find_vtables(data, g):
                print(f"  vtable @ {hex(base)}")
                for k, s in enumerate(slots):
                    print(f"    [{k}] {hex(s):>12}  {SLOT[k]}")
                if want_disasm:
                    for k in (2, 3):
                        print(f"\n--- {SLOT[k]} @ {hex(slots[k])} ---")
                        print(disasm(path, slots[k]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
