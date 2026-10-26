p = "HashCore.Laser.Server/Networking/Message/MessageManager.cs"
s = open(p, encoding="utf-8").read()
start = s.index("            // ТЕСТ: всем код 7")
end = s.index("            return;\n", start) + len("            return;\n")
new = '''            // Патчер контента: клиент с другим fingerprint получает код 7 и качает
            // изменённые файлы по ContentUrl/<sha>/<файл> (сборка: patcher/build.py).
            try
            {
                var patch = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("/root/v64/patcher/current.json"));
                string patchSha = (string)patch["sha"];
                if (message.ResourceSha != patchSha)
                {
                    Logger.Print($"[Patcher] клиент {message.ResourceSha} -> патч {patchSha}");
                    Connection.Send(new AuthenticationFailedMessage()
                    {
                        ErrorCode = 7,
                        FingerprintSha = (string)patch["fingerprint"],
                        ContentUrl = "http://2.26.48.213:8395",
                    });
                    return;
                }
            }
            catch (Exception e) { Logger.Error($"[Patcher] {e.Message}"); }
'''
s = s[:start] + new + s[end:]
open(p, "w", encoding="utf-8").write(s)

p = "HashCore.Laser.Logic/Protocol/Message/Account/Auth/AuthenticationFailedMessage.cs"
s = open(p, encoding="utf-8").read()
old = """            Stream.WriteBoolean(false);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(1);"""
new = """            Stream.WriteBoolean(false);
            Stream.WriteInt(0); // сжатые данные fingerprint: пусто
            // Список адресов контента (LoginFailedMessage::decode, +240).
            if (ContentUrl != null) { Stream.WriteInt(1); Stream.WriteString(ContentUrl); }
            else Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(1);"""
assert s.count(old) == 1
open(p, "w", encoding="utf-8").write(s.replace(old, new))
print("ok")
