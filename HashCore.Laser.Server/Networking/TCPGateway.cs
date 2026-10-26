namespace HashCore.Laser.Server.Networking
{
    using HashCore.Laser.Logic.Battle;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Message.Team;
    using HashCore.Laser.Logic.Team;
    using HashCore.Laser.Server.Logic.Game;
    using HashCore.Laser.Server.Networking.Session;
    using System.Net;
    using System.Net.Sockets;

    public static class ContentServer
    {
        private static HttpListener HttpListener;
        public static void Init(string host, int port)
        {
            HttpListener = new HttpListener();
            //HttpListener.
        }
    }
    public static class TCPGateway
    {
        private static List<Connection> ActiveConnections;
        private static readonly object ActiveConnectionsLock = new object();

        private static Socket Socket;
        private static Thread Thread;

        private static ManualResetEvent AcceptEvent;
        public static void Init(string host, int port)
        {
            ActiveConnections = new List<Connection>();

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
            Socket.Listen(100);

            AcceptEvent = new ManualResetEvent(false);

            Thread = new Thread(TCPGateway.Update);
            Thread.Start();

            Logger.Print($"[TcpConnectionInfoMessage::Init] start at {host}:{port}");
        }

        private static void Update()
        {
            while (true)
            {
                try
                {
                AcceptEvent.Reset();
                Socket.BeginAccept(new AsyncCallback(OnAccept), null);
                AcceptEvent.WaitOne();
                }
                catch (Exception exception)
                {
                    Logger.Error($"[TCPGateway::Accept] {exception}");
                    Thread.Sleep(1000);
                }
            }
        }

        private static void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket client = Socket.EndAccept(ar);
                Connection connection = new Connection(client);
                lock (ActiveConnectionsLock) ActiveConnections.Add(connection);
                Logger.Print("[ServerConnection::connect] new connection");
                Connections.AddConnection(connection);
                client.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(OnReceive), connection);
            } 
            catch (Exception exception)
            {
                Logger.Error($"[TCPGateway::OnAccept] {exception}");
            }

            AcceptEvent.Set();
        }

        private static void OnReceive(IAsyncResult ar)
        {
            try
            {
                Connection connection = (Connection)ar.AsyncState;
                if (connection == null) return;

                try
                {
                    int r = connection.Socket.EndReceive(ar);
                    if (r <= 0)
                    {
                        Logger.Print("[ServerConnection::disconnect] client disconnected.");
                        RemoveActiveConnection(connection);
                        //TeamEntry team = Teams.Get(connection.MessageManager.HomeMode.Avatar.TeamId);

                        //TeamMember entry = team.GetMember(connection.MessageManager.HomeMode.Avatar.AccountId);

                        //team.Members.Remove(entry);

                        if (connection.MessageManager.HomeMode != null)
                        {
                            Sessions.Remove(connection.Avatar.AccountId);
                            BattleMode battle = Battles.Get(connection.Home.HomeMode.Avatar.BattleId);
                            if (battle != null)
                            {
                                BattlePlayer player = battle.GetPlayerBySessionId(connection.UdpSessionId);
                                if (player != null) battle.RemovePlayer(player);
                            }
                        }

                        connection.Close();
                        return;
                    }

                    connection.Memory.Write(connection.ReadBuffer, 0, r);
                    if (connection.Messaging.OnReceive() != 0)
                    {
                        RemoveActiveConnection(connection);
                        if (connection.MessageManager.HomeMode != null)
                        {
                            Sessions.Remove(connection.Avatar.AccountId);
                        }
                        if (connection.Home != null)
                        {
                            BattleMode battle = Battles.Get(connection.Home.HomeMode.Avatar.BattleId);
                            if (battle != null)
                            {
                                BattlePlayer player = battle.GetPlayerBySessionId(connection.UdpSessionId);
                                if (player != null) battle.RemovePlayer(player);
                            }
                        }
                        connection.Close();
                        Logger.Print("[ServerConnection::disconnect] client disconnected.");
                        return;
                    }
                    connection.Socket.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(OnReceive), connection);
                }
                catch (SocketException)
                {
                    RemoveActiveConnection(connection);
                    if (connection.MessageManager?.HomeMode != null)
                    {
                        Sessions.Remove(connection.Avatar.AccountId);
                    }
                    BattleMode battle = connection.Home?.HomeMode == null ? null : Battles.Get(connection.Home.HomeMode.Avatar.BattleId);
                    if (battle != null)
                    {
                        BattlePlayer player = battle.GetPlayerBySessionId(connection.UdpSessionId);
                        if (player != null) battle.RemovePlayer(player);
                    }
                    connection.Close();
                    Logger.Print("[ServerConnection::disconnect] client disconnected.");
                }
                catch (Exception exception)
                {
                    Logger.Error($"[TCPGateway::OnReceive] {exception}");
                    connection.Close();
                    //Logger.Print("Unhandled exception: " + exception + ", trace: " + exception.StackTrace);
                }
            }
            catch (Exception exception)
            {
                Logger.Print("Unhandled exception: " + exception + ", trace: " + exception.StackTrace);
            }
        }

        private static void RemoveActiveConnection(Connection connection)
        {
            lock (ActiveConnectionsLock) ActiveConnections.Remove(connection);
            Connections.RemoveConnection(connection);
        }

        public static void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndSend(ar);
            }
            catch (Exception)
            {
                ;
            }
        }
    }
}
