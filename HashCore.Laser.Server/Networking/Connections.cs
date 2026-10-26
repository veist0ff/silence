namespace HashCore.Laser.Server.Networking
{
    using Masuda.Net;
    using HashCore.Laser.Logic.Message.Account;
    using HashCore.Laser.Logic.Message.Team.Stream;
    using HashCore.Laser.Logic.Team.Stream;
    using HashCore.Laser.Server.Networking.Session;

    public static class Connections
    {
        public static int Count
        {
            get { lock (ActiveConnectionsLock) return ActiveConnections.Count; }
        }
        public static MasudaBot bot;
        private static List<Connection> ActiveConnections;
        private static readonly object ActiveConnectionsLock = new object();
        private static Thread Thread;
        private static long SecondsGone;

        public static void Init()
        {
            ActiveConnections = new List<Connection>();
            bot = new MasudaBot(102038674, "ElazeGW3722wbRMI9StXcSJbvsRLitBm", "ElazeGW3722wbRMI9StXcSJbvsRLitBm", BotType.Public).LogTo(null);
            Thread = new Thread(Update);
            Thread.Start();
            SecondsGone = 0;
        }

        private static void Update()
        {
            while (true)
            {
                try
                {
                Connection[] snapshot;
                lock (ActiveConnectionsLock) snapshot = ActiveConnections.ToArray();

                foreach (Connection connection in snapshot)
                {
                    if (connection.Avatar != null && !Sessions.IsSessionActive(connection.Avatar.AccountId))
                    {
                        connection.Close();
                        RemoveConnection(connection);
                    }
                    if (!connection.MessageManager.IsAlive())
                    {
                        if (connection.MessageManager.HomeMode != null)
                        {
                            Sessions.Remove(connection.Avatar.AccountId);
                        }
                        connection.Close();
                        RemoveConnection(connection);
                    }
                }
                if (SecondsGone % 30 == 0) bot.ModifyChannelAsync("216185176", "服务器在线人数：" + Count, 0, 4, "141954264");
                //if (SecondsGone % 5 == 0) Sessions.SendGlobalMessage(-1, "System", "test");
                SecondsGone++;
                }
                catch (Exception exception)
                {
                    Logger.Error($"[Connections::Update] {exception}");
                }
                Thread.Sleep(1000);
            }
        }

        public static void AddConnection(Connection connection)
        {
            lock (ActiveConnectionsLock) ActiveConnections.Add(connection);
        }

        public static void RemoveConnection(Connection connection)
        {
            lock (ActiveConnectionsLock) ActiveConnections.Remove(connection);
        }
    }
}
