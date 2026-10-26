namespace HashCore.Laser.Logic.Message.Home
{
    public class RankedMatchTerminatedMessage : GameMessage
    {
        public string Name;
        public override void Encode()
        {
            Stream.WriteVInt(1);
            Stream.WriteVInt(1);
            Stream.WriteString(Name);
        }

        public override int GetMessageType()
        {
            return 0x568F;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
