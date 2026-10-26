namespace HashCore.Laser.Server.Networking
{
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Server.Networking.UDP.Game;

    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;

    public static class UDPGateway
    {
        private static Socket Socket;
        private static Thread Thread;

        private static long SessionCounter;
        private static Dictionary<long, UDPSocket> Sockets;

        private static ManualResetEvent ReceiveEvent;

        public static void Init(string host, int port)
        {
            Sockets = new Dictionary<long, UDPSocket>();

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));

            ReceiveEvent = new ManualResetEvent(false);

            Thread = new Thread(Update);
            Thread.Start();

            Logger.Print($"[UdpConnectionInfoMessage::Init] start at {host}:{port}");
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            ReceiveEvent.Set();
            State state = (State)result.AsyncState;

            byte[] buffer = state.Buffer;
            EndPoint remoteEndPoint = state.RemoteEndPoint;
            int r = 0;
            try
            {
                r = Socket.EndReceiveFrom(result, ref remoteEndPoint);
            }
            catch (Exception e)
            {
            }

            if (r <= 0) return;
            Logger.Print($"[UDP::in] {r} байт от {remoteEndPoint}"); // ТЕСТ: каждая датаграмма

            try
            {
                ByteStream stream = new ByteStream(buffer, r);
                long sessionId = stream.ReadLong();
                stream.ReadShort();

                if (!Sockets.ContainsKey(sessionId))
                {
                    Logger.Warning($"[UDPGateway::receive] unknown udp session {sessionId} from {remoteEndPoint} ({r} bytes)");
                    return;
                }

                UDPSocket client = Sockets[sessionId];
                if (!client.IsConnected)
                {
                    client.SetEndPoint(remoteEndPoint);
                    Logger.Print($"[UDPGateway::receive] udp session {sessionId} connected from {remoteEndPoint}");

                    if (client.Battle == null)
                    {
                        Logger.Error($"[UDPGateway::receive] udp session {sessionId} has no battle attached");
                        return;
                    }

                    if (!client.IsSpectator)
                    {
                        BattlePlayer player = client.Battle.GetPlayerBySessionId(client.SessionId);
                        if (player == null)
                        {
                            Logger.Error($"[UDPGateway::receive] no battle player for udp session {client.SessionId} in battle {client.Battle.Id}");
                            return;
                        }

                        player.GameListener = new UDPGameListener(client, client.TCPConnection);
                        Logger.Print($"[UDPGateway::receive] listener attached: battle {client.Battle.Id} player {player.PlayerIndex} team {player.TeamIndex}");
                    }
                }

                client.ProcessReceive(stream);
            }
            catch (Exception e)
            {
                Logger.Error($"EXCEPTION: {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
            }
        }

        private class State
        {
            public byte[] Buffer;
            public EndPoint RemoteEndPoint;
        }

        private static void Update()
        {
            while (true)
            {
                try
                {
                    ReceiveEvent.Reset();
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                    byte[] buffer = new byte[1500];

                    State state = new State()
                    {
                        RemoteEndPoint = remoteEndPoint,
                        Buffer = buffer
                    };

                    Socket.BeginReceiveFrom(buffer, 0, 1500, SocketFlags.None, ref remoteEndPoint, new AsyncCallback(ReceiveCallback), state);
                    ReceiveEvent.WaitOne();
                }
                catch (Exception e)
                {
                    Console.WriteLine("BEGIN RECEIVE EXCEPTION: " + e.GetType().Name);
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>Сессия по id, или null. Нужна TCP-пути инпутов, когда клиент не поднял UDP.</summary>
        public static UDPSocket GetSocket(long sessionId)
        {
            return Sockets.TryGetValue(sessionId, out UDPSocket socket) ? socket : null;
        }

        public static UDPSocket CreateSocket()
        {
            UDPSocket socket = new UDPSocket(++SessionCounter);
            Sockets.Add(socket.SessionId, socket);
            return socket;
        }

        public static void SendTo(byte[] data, int startIndex, int count, EndPoint remoteEndPoint)
        {

            if (remoteEndPoint == null) return;
            Socket.SendTo(data, startIndex, count, SocketFlags.None, remoteEndPoint);

        }
    }
}
