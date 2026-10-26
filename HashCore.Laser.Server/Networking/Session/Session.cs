namespace HashCore.Laser.Server.Networking.Session
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Listener;

    public class Session
    {
        public HomeMode Home;
        public Connection Connection;
        public LogicGameListener GameListener => Home.GameListener;

        public Session(HomeMode home, Connection connection)
        {
            Home = home;
            Connection = connection;
        }
    }
}
