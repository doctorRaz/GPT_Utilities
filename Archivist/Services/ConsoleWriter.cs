using System;

namespace dRz.GPT_Utilities.Archivist.Services
{
    internal static class ConsoleWriter
    {
        public static void WriteLine(
            string message,
            ConsoleColor? foreground = null,
            ConsoleColor? background = null)
        {
            ConsoleColor previousForeground = Console.ForegroundColor;
            ConsoleColor previousBackground = Console.BackgroundColor;

            if (foreground.HasValue)
                Console.ForegroundColor = foreground.Value;

            if (background.HasValue)
                Console.BackgroundColor = background.Value;

            Console.WriteLine(message);

            Console.ForegroundColor = previousForeground;
            Console.BackgroundColor = previousBackground;
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

        public static void Fatal(string message)
        {
            WriteLine(
                message,
                ConsoleColor.White,
                ConsoleColor.DarkRed);
        }

        public static void PressAnyKey()
        {
            ConsoleWriter.Info("");
            ConsoleWriter.Info("Press any key...");
            Console.ReadKey();
        }
    }
}