using System;
using System.Collections.Generic;

namespace dRz.GPT_Utilities.Archivist.Services
{
    public static class ConsoleWriter
    {

        #region Private Properties

        private static ConsoleColor _contrastColor => Console.BackgroundColor == Console.ForegroundColor ? GetContrastColor(Console.BackgroundColor) : Console.ForegroundColor;

        #endregion Private Properties

        #region Public Methods

        public static void Error(string message, Exception? ex = null) =>        
                                                            WriteLine(Format(message, ex), 
                                                            ConsoleColor.Red);
        
        public static void Fatal(Exception ex, string? message = null) => WriteLine(
                                                               Format(message, ex, "Fatal error"),
                                                                ConsoleColor.White,
                                                                ConsoleColor.DarkRed);

        public static void Info(string message) => WriteLine(message, ConsoleColor.Gray);

        public static void PressAnyKey()
        {
            Info("");
            Info("Press any key...");
            Console.ReadKey();
        }

        public static void Step(string message) => WriteLine(message, ConsoleColor.DarkGray);

        public static void Success(string message) => WriteLine(message, ConsoleColor.Green);

        /// <summary>Tests the show colors.</summary>
        public static void TestShowColors()
        {
            WriteLine();

            WriteLine("<< ShowColors >>", _contrastColor);

            foreach (ConsoleColor color in Enum.GetValues<ConsoleColor>())
            {
                ConsoleColor background = Console.BackgroundColor;

                string same = "";

                if (color == background)
                {
                    background = GetContrastColor(color);

                    same = "<-- SAME";
                }

                WriteLine($"\t{color,-15} {same}", color, background);
            }
        }

        /// <summary>Tests the show colors background.</summary>
        public static void TestShowColorsBackground()
        {
            WriteLine();

            WriteLine("<< ShowColorsBackground >>", _contrastColor);

            foreach (ConsoleColor background in Enum.GetValues<ConsoleColor>())
            {
                ConsoleColor displayForeground = background == Console.ForegroundColor ? GetContrastColor(background) : Console.ForegroundColor;

                WriteLine($"Background: {background}", displayForeground, background);

                foreach (ConsoleColor foreground in Enum.GetValues<ConsoleColor>())
                {
                    string same = "";

                    displayForeground = foreground;

                    if (foreground == background)
                    {
                        displayForeground = GetContrastColor(background);

                        same = "<-- SAME";
                    }

                    WriteLine($"\t{foreground,-15} on {background,-15} {same}", displayForeground, background);
                }
            }
        }

        /// <summary>Tests the show styles.</summary>
        public static void TestShowStyles()
        {
            Exception ex = new Exception("Пример исключения для демонстрации стиля Fatal");

            WriteLine();

            WriteLine("<< ShowStyles >>", _contrastColor);

            Step($"\t{nameof(Step),-15}: пример сообщения");

            Info($"\t{nameof(Info),-15}: пример сообщения");

            Success($"\t{nameof(Success),-15}: пример сообщения");

            Update($"\t{nameof(Update),-15}: пример сообщения");

            Warning($"\t{nameof(Warning),-15}: пример сообщения");

            Error($"{nameof(Error)}: полный Ex", ex);
            Error($"{nameof(Error)}: без Ex");

            Fatal(ex, $"{nameof(Fatal)}: полный Ex");
            Fatal(ex);
        }

        public static void Update(string message) => WriteLine(message, ConsoleColor.Cyan);

        public static void Warning(string message) => WriteLine(message, ConsoleColor.Magenta);

        #endregion Public Methods

        #region Private Methods

        private static string Format(string? userMessage, Exception? ex, string defaultMessage = "Error")
        {
            if (ex == null && string.IsNullOrEmpty(userMessage))
                return defaultMessage;

            var parts = new List<string>();

            if (!string.IsNullOrEmpty(userMessage))
                parts.Add(userMessage);

            if (ex != null)
            {
                parts.Add($"Exception: {ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                    parts.Add($"StackTrace: {ex.StackTrace}");
            }

            return string.Join(Environment.NewLine, parts);
        }

        private static ConsoleColor GetContrastColor(ConsoleColor background)
        {
            return background switch
            {
                ConsoleColor.Gray or
                ConsoleColor.White or
                ConsoleColor.Yellow
                    => ConsoleColor.Black,

                _ => ConsoleColor.White
            };
        }

        private static void WriteLine(string message,
                                                     ConsoleColor? foreground = null,
                                     ConsoleColor? background = null)
        {
            ConsoleColor previousForeground = Console.ForegroundColor;
            ConsoleColor previousBackground = Console.BackgroundColor;
            try
            {
                if (foreground.HasValue)
                {
                    Console.ForegroundColor = foreground.Value;
                }

                if (background.HasValue)
                {
                    Console.BackgroundColor = background.Value;
                }

                if (Console.ForegroundColor == Console.BackgroundColor)
                {
                    Console.ForegroundColor = GetContrastColor(Console.BackgroundColor);
                }

                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = previousForeground;
                Console.BackgroundColor = previousBackground;
            }
        }

        private static void WriteLine()
        {
            Console.WriteLine();
        }

        #endregion Private Methods
    }
}