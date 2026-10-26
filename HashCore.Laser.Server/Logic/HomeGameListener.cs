namespace HashCore.Laser.Server.Logic
{
    using HashCore.Laser.Logic.Listener;
    using HashCore.Laser.Server.Networking;
    using HashCore.Laser.Logic.Message;

    public class HomeGameListener : LogicGameListener
    {
        private Connection Connection;

        public HomeGameListener(Connection connection)
        {
            Connection = connection;
        }

        public override void SendMessage(GameMessage message)
        {
            Connection.Send(message);
        }

        public override void SendTCPMessage(GameMessage message)
        {
            Connection.Send(message);
        }
    }
}
