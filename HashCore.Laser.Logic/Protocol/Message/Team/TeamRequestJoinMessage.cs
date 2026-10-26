namespace HashCore.Laser.Logic.Message.Team
{
    public class TeamRequestJoinMessage : GameMessage
    {
        public long TeamId;
        public long unk;

        public override void Decode()
        {
            unk = Stream.ReadLong();
            TeamId = Stream.ReadLong();
        }

        public override int GetMessageType()
        {
            return 14881;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
