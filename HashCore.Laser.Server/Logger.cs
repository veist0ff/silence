namespace HashCore.Laser.Server
{
    using System;

    public static class Logger
    {
        public static void Print(string log)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[d] " + log);
        }
        public static void Warning(string log)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[W] " + log);
        }

        public static void Error(string log)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[e] " + log);
        }
    }

}
