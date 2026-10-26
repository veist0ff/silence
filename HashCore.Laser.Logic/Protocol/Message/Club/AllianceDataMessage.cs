namespace HashCore.Laser.Logic.Message.Club
{
    using HashCore.Laser.Logic.Club;

    public class AllianceDataMessage : GameMessage
    {
        public Alliance Alliance;
        public bool IsMyAlliance;

        public override void Encode()
        {
            Stream.WriteBoolean(IsMyAlliance);
            Alliance.Encode(Stream);
        }

        public override int GetMessageType()
        {
            return 24301;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
