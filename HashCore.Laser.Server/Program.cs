namespace HashCore.Laser.Server
{
    using Masuda.Net;
    using Masuda.Net.HelpMessage;
    using Masuda.Net.Models;
    using HashCore.Laser.Server.Handler;
    using System.Drawing;

    static class Program
    {
        
        public const string SERVER_VERSION = "1.2";
        public const string BUILD_TYPE = "Beta";

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
                Logger.Error($"[Fatal] {eventArgs.ExceptionObject}");
            TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
            {
                Logger.Error($"[UnobservedTask] {eventArgs.Exception}");
                eventArgs.SetObserved();
            };

            Colorful.Console.WriteWithGradient(
                @"v64" + "\n\n\n", Color.Red, Color.Blue, 10);

            Logger.Print("[ServerConnection::update] starting ...");
            Configuration.Instance = Configuration.LoadFromFile("Json/config.json");

            // Тема меню из конфига: по ней берётся фон для всех предложений магазина.
            // Тему берём через GetThemeId: он сам перечитывает конфиг по времени
            // изменения файла, поэтому смена ThemeID подхватывается без перезапуска.
            HashCore.Laser.Logic.Home.MenuTheme.ThemeIdProvider = Configuration.GetThemeId;
            HashCore.Laser.Logic.Home.MenuTheme.ActiveThemeId = Configuration.Instance.ThemeId;
            HashCore.Laser.Logic.Home.Skins.CollabRewards.ActiveCollabId = Logic.Game.Events.COLLAB_ID;

            Resources.InitDatabase();
            Resources.InitLogic();
            Resources.InitNetwork();

            Logger.Print("[ServerConnection::init] started");
            ExitHandler.Init();
            CmdHandler.Start();

        }
    }
}
