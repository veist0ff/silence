namespace HashCore.Laser.Logic.Message.Home
{
    public class StopHomeLogicMessage: GameMessage
    {
        public override int GetMessageType()
        {
            return 24106;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
