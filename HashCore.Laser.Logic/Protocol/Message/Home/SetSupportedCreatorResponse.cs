namespace HashCore.Laser.Logic.Message.Home
{
    public class SetSupportedCreatorResponse : GameMessage
    {
        public string Name;

        public override void Encode()
        {
            Stream.WriteVInt(1);
            Stream.WriteString(Name);
        }

        public override int GetMessageType()
        {
            return 28686;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
