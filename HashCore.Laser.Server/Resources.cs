using Al;
using HashCore.Laser.Logic.Data;
using HashCore.Laser.Logic.Listener;
using HashCore.Laser.Server.Logic;
using HashCore.Laser.Server.Logic.Game;
using HashCore.Laser.Server.Message;
using HashCore.Laser.Server.Networking;
using HashCore.Laser.Server.Networking.Session;

internal static class Resources
    {
        public static void InitDatabase()
        {
            Accounts.Init(Configuration.Instance.DatabaseUsername, Configuration.Instance.DatabasePassword);
            Alliances.Init(Configuration.Instance.DatabaseUsername, Configuration.Instance.DatabasePassword);
            // Было захардкожено "root"/"FidBonGay" — после смены креды в config.json
            // AuthServer перестал бы коннектиться к MySQL. Берём из конфига, как Accounts/Alliances.
            AuthServer.Init(Configuration.Instance.DatabaseUsername,
                            Configuration.Instance.DatabasePassword,
                            Configuration.Instance.DatabaseName);
            
        }

        public static void InitLogic()
        {
            DataTables.Load();
            Events.Init();
            Sessions.Init();
            Leaderboards.Init();
            Battles.Init();
            Matchmaking.Init();
            Teams.Init();
        }

    public static void InitNetwork()
    {
        Processor.Init();
        Connections.Init();
        LogicServerListener.Instance = new ServerListener();
        UDPGateway.Init("0.0.0.0", Configuration.Instance.UdpPort);
        TCPGateway.Init("0.0.0.0", Configuration.Instance.TcpPort);
    }
}
