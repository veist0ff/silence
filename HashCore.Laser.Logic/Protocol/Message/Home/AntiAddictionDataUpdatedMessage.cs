namespace HashCore.Laser.Logic.Message.Home
{
    public class AntiAddictionDataUpdatedMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteInt(1);
            Stream.WriteInt(0);
        }

        public override int GetMessageType()
        {
            return 20931;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
