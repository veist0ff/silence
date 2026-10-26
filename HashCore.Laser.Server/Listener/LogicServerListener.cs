namespace HashCore.Laser.Logic.Listener
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Home;

    public interface LogicServerListener
    {
        public static LogicServerListener Instance;

        ClientAvatar GetAvatar(long id);
        LogicGameListener GetGameListener(long id);
        HomeMode GetHomeMode(long id);
        bool IsPlayerOnline(long id);
    }
}
