namespace HashCore.Laser.Logic.Message.Team
{
    public class TeamToggleMemberSideMessage : GameMessage
    {
        public List<long> Longs = new List<long>();
        public int Unk1;

        public override void Decode()
        {
            base.Decode();

            for(int i = Stream.ReadVInt(); i > 0; i--)
            {
                Longs.Add(Stream.ReadLong());
            }
            Unk1 = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 14357;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
