using System;

namespace dRz.GPT_Utilities.Archivist.Services
{
    public static class ConsoleWriter
    {
        #region Private Properties

        private static ConsoleColor _contrastColor => Console.BackgroundColor == Console.ForegroundColor ? GetContrastColor(Console.BackgroundColor) : Console.ForegroundColor;

        #endregion Private Properties

        #region Public Methods

        public static void Error(string message) => WriteLine(message, ConsoleColor.Red);

        public static void Fatal(string message) => WriteLine(
                message,
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
            WriteLine();

            WriteLine("<< ShowStyles >>", _contrastColor);

            Step($"\t{nameof(Step),-15}: пример сообщения");

            Info($"\t{nameof(Info),-15}: пример сообщения");

            Success($"\t{nameof(Success),-15}: пример сообщения");

            Update($"\t{nameof(Update),-15}: пример сообщения");

            Warning($"\t{nameof(Warning),-15}: пример сообщения");

            Error($"\t{nameof(Error),-15}: пример сообщения");

            Fatal($"\t{nameof(Fatal),-15}: пример сообщения");
        }

        public static void Update(string message) => WriteLine(message, ConsoleColor.Cyan);

        public static void Warning(string message) => WriteLine(message, ConsoleColor.Magenta);

        public static void WriteLine(string message,
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
        #endregion Public Methods

        #region Private Methods

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

        #endregion Private Methods
    }
}