namespace HashCore.Laser.Logic.Message.Account.Auth
{
    /// <summary>
    /// 10636 — клиент просит привязать текущий игровой аккаунт к Supercell ID.
    /// Формат снят с клиента (encode 0x1004DBED8): две сжатые строки — токен SCID
    /// и bindToken из login.confirm. Без ответа 25165 окно SCID висит намертво.
    /// </summary>
    public class ScidBindAccountMessage : GameMessage
    {
        public string ScidToken;
        public string BindToken;

        public override void Decode()
        {
            ScidToken = ReadCompressed();
            BindToken = ReadCompressed();
        }

        private string ReadCompressed()
        {
            int n = Stream.ReadInt();
            if (n <= 4) return null;
            byte[] raw = Stream.ReadBytes(n, 900000);
            using var ms = new System.IO.MemoryStream(raw, 4, raw.Length - 4);
            using var z = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionMode.Decompress);
            using var r = new System.IO.StreamReader(z, System.Text.Encoding.UTF8);
            return r.ReadToEnd();
        }

        public override int GetMessageType()
        {
            return 10636;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }

    /// <summary>
    /// 25165 SCIDAccountBound — ответ на привязку (decode 0x1004DBD74): Int код
    /// (0 — успех) и сжатая строка с токеном, который игра передаёт SDK.
    /// </summary>
    public class ScidAccountBoundMessage : GameMessage
    {
        public int Result;
        public string Token = "";

        public override void Encode()
        {
            Stream.WriteInt(Result);
            Stream.WriteCompressedString(Token ?? "");
        }

        public override int GetMessageType()
        {
            return 25165;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
