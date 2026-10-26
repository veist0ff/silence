namespace HashCore.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationFailedMessage : GameMessage
    {
        public int ErrorCode;
        public string FingerprintSha;
        public string Message;
        public string UpdateUrl;
        public DateTime dateTime;
        public string ContentUrl;
        public override void Encode()
        {
            Stream.WriteInt(ErrorCode);
            Stream.WriteString(FingerprintSha);
            Stream.WriteString(null); // Redirect
            Stream.WriteString(ContentUrl); // content url
            Stream.WriteString(UpdateUrl); // update url
            Stream.WriteString(Message);
            
            Stream.WriteInt((int)(dateTime-DateTime.Now).TotalSeconds);
            Stream.WriteBoolean(false);
            // Сжатый fingerprint (+216): длина блока, внутри 4 байта исходной длины
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
            // Список адресов контента (LoginFailedMessage::decode, +240).
            if (ContentUrl != null) { Stream.WriteInt(1); Stream.WriteString(ContentUrl); }
            else Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(1);
            Stream.WriteString(null);
            Stream.WriteInt(0);
            Stream.WriteBoolean(true);
            Stream.WriteBoolean(true);
            Stream.WriteString(null);
            Stream.WriteVInt(0);
            Stream.WriteString(null);
            Stream.WriteBoolean(false);
            //HashCore.Laser.Titan.Debug.Debugger.Print("e");
        }

        public override int GetMessageType()
        {
            return 20103;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
