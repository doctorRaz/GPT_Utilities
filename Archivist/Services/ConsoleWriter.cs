using System;

namespace dRz.GPT_Utilities.Archivist.Services
{
    internal static class ConsoleWriter
    {
        public static void WriteLine(string message, ConsoleColor color)
        {
            ConsoleColor previousColor = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.WriteLine(message);

            Console.ForegroundColor = previousColor;
        }

        public static void Info(string message)
        {

            WriteLine(message, ConsoleColor.Gray);
        }

        public static void Success(string message)
        {
            WriteLine(message, ConsoleColor.Green);
        }

        public static void Update(string message)
        {
            WriteLine(message, ConsoleColor.Yellow);
        }

        public static void Warning(string message)
        {
            WriteLine(message, ConsoleColor.DarkYellow);
        }

        public static void Error(string message)
        {
            WriteLine(message, ConsoleColor.Red);
        }
    }
}