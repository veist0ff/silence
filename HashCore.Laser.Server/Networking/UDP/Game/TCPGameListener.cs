namespace HashCore.Laser.Server.Networking.UDP.Game
{
    using HashCore.Laser.Logic.Listener;
    using HashCore.Laser.Logic.Message;

    /// <summary>
    /// Слушатель боя для клиентов, которые не подняли UDP-сессию и шлют
    /// ClientInputMessage по TCP. Весь боевой трафик уходит тем же TCP-каналом.
    /// </summary>
    public class TCPGameListener : LogicGameListener
    {
        private readonly Connection TCPConnection;

        public TCPGameListener(Connection connection)
        {
            TCPConnection = connection;
        }

        public override void SendMessage(GameMessage message)
        {
            SendTCPMessage(message);
        }

        public override void SendTCPMessage(GameMessage message)
        {
            try
            {
                TCPConnection.Send(message);
            }
            catch (Exception)
            {
            }
        }
    }
}
