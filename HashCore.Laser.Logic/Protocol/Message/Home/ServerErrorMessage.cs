namespace HashCore.Laser.Logic.Message.Home
{
    public class ServerErrorMessage: GameMessage
    {
        public override int GetMessageType()
        {
            return 24115;
        }
        public override void Encode()
        {
            Stream.WriteInt(43);//49 50
        }
        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
