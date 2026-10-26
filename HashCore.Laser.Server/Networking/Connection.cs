namespace HashCore.Laser.Server.Networking
{
    using HashCore.Laser.Logic.Avatar;

    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Message;
    using HashCore.Laser.Server.Logic.Game;
    using HashCore.Laser.Server.Message;
    using System;
    using System.Net.Sockets;

    public class Connection
    {
        public Messaging Messaging { get; }
        public MessageManager MessageManager { get; }
        public byte[] ReadBuffer { get; }
        public Socket Socket { get; }

        public int Ping { get; private set; }

        public MemoryStream Memory { get; set; }
        public bool IsOpen;

        public int MatchmakeSlot;
        public MatchmakingEntry MatchmakingEntry;

        public long UdpSessionId;
        /// <summary>Сессионный JWT Supercell ID из блока DATA сообщения 10101 (iOS).</summary>
        public string ScidToken;
        public string Nonce;
        public ClientHome Home
        {
            get
            {
                if (MessageManager.HomeMode != null)
                {
                    return MessageManager.HomeMode.Home;
                }
                return null;
            }
        }

        public ClientAvatar Avatar
        {
            get
            {
                if (MessageManager.HomeMode != null)
                {
                    return MessageManager.HomeMode.Avatar;
                }
                return null;
            }
        }

        public Connection(Socket socket)
        {
            Socket = socket;
            ReadBuffer = new byte[1024];

            Memory = new MemoryStream();

            Messaging = new Messaging(this);
            MessageManager = new MessageManager(this);

            IsOpen = true;
            MatchmakeSlot = -1;

            UdpSessionId = -1;
        }

        public void PingUpdated(int value)
        {
            Ping = value;
        }

        public void Send(GameMessage message)
        {
            // ТЕСТ: журнал исходящих для поиска вылета в лобби.
            if (message.GetMessageType() != 24109)
                System.Console.WriteLine("[Out] " + System.DateTime.UtcNow.ToString("HH:mm:ss.fff") + " " + message.GetMessageType() + " " + message.GetType().Name + " acc " + (Avatar?.AccountId ?? 0));
            Messaging.Send(message);
        }

        public void Close()
        {
            try
            {
                IsOpen = false;
                Socket.Close();
            }
            catch (Exception) { }
        }

        public void Write(byte[] stream)
        {
            try
            {
                Socket.BeginSend(stream, 0, stream.Length, SocketFlags.None, new AsyncCallback(TCPGateway.OnSend), Socket);
            }
            catch (Exception) { }
        }
    }
}
