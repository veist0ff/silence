namespace HashCore.Laser.Logic.Message.Home
{
    public class GoHomeFromMapEditorMessage : GameMessage
    {
        public override int GetMessageType()
        {
            return 12108;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
