namespace HashCore.Laser.Logic.Listener
{
    using HashCore.Laser.Logic.Message;

    public abstract class LogicGameListener
    {
        public int HandledInputs;

        public abstract void SendMessage(GameMessage message);
        public abstract void SendTCPMessage(GameMessage message);
    }
}
