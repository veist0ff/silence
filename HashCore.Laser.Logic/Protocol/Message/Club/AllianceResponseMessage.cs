namespace HashCore.Laser.Logic.Message.Club
{
    public class AllianceResponseMessage : GameMessage
    {
        public int ResponseType;

        public int RemainingSeconds;

        public override void Encode()
        {
            Stream.WriteVInt(ResponseType);
            Stream.WriteVInt(RemainingSeconds);
        }

        public override int GetMessageType()
        {
            return 24333;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
