namespace HashCore.Laser.Logic.Home.Items
{
    using System;
    using System.Text;

    public sealed class ChronosFileEntry
    {
        public string Hash { get; }
        public string Filename { get; }

        public ChronosFileEntry(string hash, string filename)
        {
            if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("Chronos requires both a nonempty hash and filename.");
            if (Encoding.UTF8.GetByteCount(hash) > 1024 || Encoding.UTF8.GetByteCount(filename) > 1024)
                throw new ArgumentException("Chronos hash and filename must each fit in 1024 UTF-8 bytes.");
            Hash = hash;
            Filename = filename;
        }

        public void Encode(ByteStream stream)
        {
            // Native +16 (hash), then +0 (filename), not object memory order.
            stream.WriteStringReference(Hash);
            stream.WriteStringReference(Filename);
        }
    }
}
