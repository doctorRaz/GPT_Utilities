
#if DEBUG

using static dRz.GPT_Utilities.Archivist.Infrastructure.ConsoleWriter;

namespace dRz.GPT_Utilities.Archivist.Infrastructure
{
    internal static class ConsoleDemo
    {

        /// <summary>Runs this instance.</summary>
        internal static void Run()
        {
            ShowColors();
            TestShowColorsBackground();
            ShowStyles();
        }

        /// <summary>Tests the show colors.</summary>
        internal static void ShowColors()
        {
            Info("");

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
        internal static void TestShowColorsBackground()
        {
            WriteLine("");

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
        internal static void ShowStyles()
        {
            Exception ex = new Exception("Пример исключения для демонстрации стиля Fatal");

            Info("");

            WriteLine("<< ShowStyles >>", _contrastColor);

            Trace($"\t{nameof(Trace),-15}: пример сообщения");

            Info($"\t{nameof(Info),-15}: пример сообщения");

            Success($"\t{nameof(Success),-15}: пример сообщения");

            Update($"\t{nameof(Update),-15}: пример сообщения");

            Warn($"\t{nameof(Warn),-15}: пример сообщения");

            Error($"{nameof(Error)}: полный Ex", ex);
            Error($"{nameof(Error)}: без Ex");

            Fatal(ex, $"{nameof(Fatal)}: полный Ex");
            Fatal(ex);
        }

        private static ConsoleColor _contrastColor => Console.BackgroundColor == Console.ForegroundColor ? GetContrastColor(Console.BackgroundColor) : Console.ForegroundColor;

    }
}
#endif