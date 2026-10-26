namespace HashCore.Laser.Logic.Message.Club
{
    public class AskForAllianceDataMessage : GameMessage
    {
        public long AllianceId;
        public long? ContextAccountId;

        public override void Decode()
        {
            AllianceId = Stream.ReadLong();
            ContextAccountId = Stream.ReadBoolean() ? Stream.ReadLong() : null;
        }

        public override int GetMessageType()
        {
            return 14302;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
