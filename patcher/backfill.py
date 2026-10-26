# Дозаполняет каждую папку патча в /var/www/patch всеми файлами её fingerprint.json:
# клиенты со старым патчем запрашивают файлы по старому sha, 404 роняет загрузку.
import os, json, hashlib, shutil
WWW = "/var/www/patch"
SRC = ["/var/www/patch", "/root/v64/Assets", "/root/v64/patcher/base", "/root/v64/patcher/base.disabled"]
index = {}
for root in SRC:
    for dp, _, names in os.walk(root):
        for n in names:
            f = os.path.join(dp, n)
            if n == "fingerprint.json": continue
            try: index.setdefault(hashlib.sha1(open(f, "rb").read()).hexdigest(), f)
            except OSError: pass
added = missing = 0
for d in os.listdir(WWW):
    fp = os.path.join(WWW, d, "fingerprint.json")
    if not os.path.exists(fp): continue
    for e in json.load(open(fp))["files"]:
        dst = os.path.join(WWW, d, e["file"])
        if os.path.exists(dst): continue
        src = index.get(e["sha"])
        if not src: missing += 1; continue
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        try: os.link(src, dst)
        except OSError: shutil.copyfile(src, dst)
        added += 1
print("backfill: добавлено", added, "не найдено (берутся из APK)", missing)
