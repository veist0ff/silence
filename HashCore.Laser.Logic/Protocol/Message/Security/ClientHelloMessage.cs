namespace HashCore.Laser.Logic.Message.Security
{
    public class ClientHelloMessage : GameMessage
    {
        public int KeyVersion { get; set; }
        public int ClientSeed { get; set; }
        public int MajorVersion;

        public override int GetMessageType()
        {
            return 10100;
        }

        public override void Decode()
        {
            Stream.ReadInt();
            KeyVersion = Stream.ReadInt();
            MajorVersion= Stream.ReadInt();
            ClientSeed = Stream.ReadInt();

        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
