namespace HashCore.Laser.Logic.Message.Home
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Home;

    public class OwnHomeDataMessage : GameMessage
    {
        public OwnHomeDataMessage() : base()
        {
            ;
        }

        public ClientHome Home;
        public ClientAvatar Avatar;

        public override void Encode()
        {
        Home.Encode(Stream);
        Avatar.Encode(Stream); 
        }

        public override int GetMessageType()
        {
            return 24101;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
