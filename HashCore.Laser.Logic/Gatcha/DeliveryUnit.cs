namespace HashCore.Laser.Logic.Home.Gatcha
{

    public class DeliveryUnit
    {
        public int Type;
        private List<GatchaDrop> Drops;

        public DeliveryUnit(int type)
        {
            Type = type;
            Drops = new List<GatchaDrop>();
        }

        public void AddDrop(GatchaDrop drop)
        {
            if (drop != null)
            {
                Drops.Add(drop);
            }
        }

        public GatchaDrop[] GetDrops()
        {
            return Drops.ToArray();
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Type);
            stream.WriteVInt(Drops.Count);
            foreach (GatchaDrop drop in Drops)
            {
                drop.Encode(stream);
            }
        }

        public void Decode(ByteStream stream)
        {
            Type = stream.ReadVInt();

            Drops.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                GatchaDrop drop = new GatchaDrop(0);
                drop.Decode(stream);
                Drops.Add(drop);
            }
        }
    }
}
