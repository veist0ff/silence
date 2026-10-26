p = "HashCore.Laser.Logic/Protocol/Message/Account/Auth/AuthenticationFailedMessage.cs"
s = open(p, encoding="utf-8").read()
old = """            Stream.WriteInt(0); // сжатые данные fingerprint: пусто
"""
new = """            // Сжатый fingerprint (+216): длина блока, внутри 4 байта исходной длины
            // (little-endian) и zlib. Клиент читает его как «uncompressed fingerprint data».
            if (FingerprintSha != null)
            {
                byte[] raw = System.Text.Encoding.UTF8.GetBytes(FingerprintSha);
                using var ms = new System.IO.MemoryStream();
                ms.Write(System.BitConverter.GetBytes(raw.Length), 0, 4);
                using (var z = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Optimal, true))
                    z.Write(raw, 0, raw.Length);
                byte[] packed = ms.ToArray();
                Stream.WriteInt(packed.Length);
                Stream.WriteBytesWithoutLength(packed, packed.Length);
            }
            else Stream.WriteInt(0);
"""
assert s.count(old) == 1
s = s.replace(old, new)
open(p, "w", encoding="utf-8").write(s)
print("ok")
