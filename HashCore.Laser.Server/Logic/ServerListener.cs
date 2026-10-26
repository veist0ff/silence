namespace HashCore.Laser.Server.Logic
{
    using Al;
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Listener;
    using HashCore.Laser.Server.Database;
    using HashCore.Laser.Server.Networking.Session;

    public class ServerListener : LogicServerListener
    {
        public ClientAvatar GetAvatar(long id)
        {
            return Accounts.Load(id)?.Avatar;
        }

        public LogicGameListener GetGameListener(long id)
        {
            if (Sessions.IsSessionActive(id))
            {
                return Sessions.GetSession(id).GameListener;
            }
            return null;
        }

        public HomeMode GetHomeMode(long id)
        {
            if (Sessions.IsSessionActive(id))
            {
                return Sessions.GetSession(id).Home;
            }
            return null;
        }

        public bool IsPlayerOnline(long id)
        {
            return Sessions.IsSessionActive(id);
        }
    }
}
