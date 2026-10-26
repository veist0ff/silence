namespace HashCore.Laser.Server.Logic.Game
{
    using HashCore.Laser.Logic.Club;
    using Al;

    public static class Leaderboards
    {
        private static Thread Thread;

        public static void Init()
        {

            Thread = new Thread(Update);
            Thread.Start();
        }

        private static void Update()
        {
            while (true)
            {
                Thread.Sleep(20 * 1000);
            }
        }
    }
}
