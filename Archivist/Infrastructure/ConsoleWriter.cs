namespace dRz.GPT_Utilities.Archivist.Infrastructure
{
    public static class ConsoleWriter
    {
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

        public static void Trace(string message) => WriteLine(message, ConsoleColor.DarkGray);

        public static void Success(string message) => WriteLine(message, ConsoleColor.Green);

        public static void Update(string message) => WriteLine(message, ConsoleColor.Cyan);

        public static void Warn(string message) => WriteLine(message, ConsoleColor.Magenta);

#if DEBUG

        internal static string Format(string? userMessage, Exception? ex, string defaultMessage = "Error")
#else

        private static string Format(string? userMessage, Exception? ex, string defaultMessage = "Error")
#endif
        {
            if (ex == null && string.IsNullOrEmpty(userMessage))
            {
                return defaultMessage;
            }

            List<string> parts = new List<string>();

            if (!string.IsNullOrEmpty(userMessage))
            {
                parts.Add(userMessage);
            }

            if (ex != null)
            {
                parts.Add($"Exception: {ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    parts.Add($"StackTrace: {ex.StackTrace}");
                }
            }

            return string.Join(Environment.NewLine, parts);
        }

#if DEBUG

        internal static ConsoleColor GetContrastColor(ConsoleColor background)
#else

        private static ConsoleColor GetContrastColor(ConsoleColor background)
#endif
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

#if DEBUG

        internal static void WriteLine(string message,
                                      ConsoleColor? foreground = null,
                                     ConsoleColor? background = null)
#else

        private static void WriteLine(string message,
                                      ConsoleColor? foreground = null,
                                     ConsoleColor? background = null)
#endif
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
    }
}