namespace HashCore.Laser.Logic.Message.Home
{
    public class SetSupportedCreatorMessage : GameMessage
    {
        public string Creator { get; set; }

        public override void Decode()
        {
            Creator = Stream.ReadString();
        }

        public override int GetMessageType()
        {
            return 18686;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
