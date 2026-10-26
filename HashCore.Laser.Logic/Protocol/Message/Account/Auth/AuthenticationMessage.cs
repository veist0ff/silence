namespace HashCore.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationMessage : GameMessage
    {
        public AuthenticationMessage() : base()
        {
            AccountId = 0;
        }

        public long AccountId;
        public string PassToken;
        public int ClientMajor;
        public int ClientMinor;
        public int ClientBuild;
        public string ResourceSha;
        public string Device; // модель устройства («iPhone14,5», «SM-A226B»…)

        public override void Decode()
        {
            AccountId = Stream.ReadLong();
            PassToken = Stream.ReadString();
            ClientMajor = Stream.ReadInt();
            ClientMinor = Stream.ReadInt();
            ClientBuild = Stream.ReadInt();
            ResourceSha = Stream.ReadString();//c09e3d66f8aef616998026efcc5dd408fa8ba255
            try { Device = Stream.ReadString(); } catch { Device = null; }
        }

        public override int GetMessageType()
        {
            return 10101;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
