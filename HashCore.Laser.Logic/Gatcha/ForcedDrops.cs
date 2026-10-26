namespace HashCore.Laser.Logic.Home.Gatcha
{
    public class ForcedDrops
    {
        public int Unk1;
        public int Unk2;
        public readonly List<int> Drops = new List<int>();

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Unk1);
            stream.WriteVInt(Unk2);

            stream.WriteVInt(Drops.Count);
            foreach (int drop in Drops)
            {
                stream.WriteVInt(drop);
            }
        }

        public void Decode(ByteStream stream)
        {
            Unk1 = stream.ReadVInt();
            Unk2 = stream.ReadVInt();

            Drops.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                Drops.Add(stream.ReadVInt());
            }
        }
    }
}
