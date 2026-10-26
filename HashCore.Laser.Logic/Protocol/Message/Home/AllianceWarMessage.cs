namespace HashCore.Laser.Logic.Message.Home
{
    public class AllianceWarMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteInt(0);
            Stream.WriteInt(1);
            Stream.WriteVInt(1);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);

        }

        public override int GetMessageType()
        {
            return 24776;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
