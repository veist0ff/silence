namespace HashCore.Laser.Logic.Message.Home
{
    public class GetPlayerMapsMessage : GameMessage
    {
        public override void Decode()
        {
            ;

        }

        public override int GetMessageType()
        {
            return 12102;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
