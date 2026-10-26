namespace HashCore.Laser.Logic.Message.Club
{
    public class LeaveAllianceMessage : GameMessage
    {
        public long? AllianceId;

        public override void Decode()
        {
            AllianceId = Stream.ReadBoolean() ? Stream.ReadLong() : null;
        }

        public override int GetMessageType()
        {
            return 14308;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
