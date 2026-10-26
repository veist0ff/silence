namespace HashCore.Laser.Logic.Home.Items
{
    // LogicPlayerBrawlerEarlyAccessEventData::decode (0x1003b5b0c).
    // Unk suffixes are decimal client object offsets; field meanings are unconfirmed.
    public class PlayerBrawlerEarlyAccessEventData
    {
        public int Unk8; // Event slot, compared to EventData +0 (0x100387f44).
        public int Unk12; // Available tickets (0x10012ed78).
        public int Unk16; // Wins; UI takes modulo event +32 (0x10033294c).
        public int Unk20;
        public int Unk24;
        public int Unk28; // Elapsed seconds since ticket batch at snapshot (0x100388194).
        public int Unk32;
        public int Unk36;
        public int Unk40;

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Unk8);
            // The client consumes no further fields when the first value is non-positive.
            if (Unk8 <= 0) return;

            stream.WriteVInt(Unk12);
            stream.WriteVInt(Unk16);
            stream.WriteVInt(Unk20);
            stream.WriteVInt(Unk24);
            stream.WriteVInt(Unk28);
            stream.WriteVInt(Unk32);
            stream.WriteVInt(Unk36);
            stream.WriteVInt(Unk40);
        }
    }
}
