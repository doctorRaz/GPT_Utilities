namespace dRz.GPT_Utilities.Archivist.Infrastructure
{
    public static class ConsoleSetup
    {
        public static void Configure(string? title = null)
        {
            if (title != null)
            {
                Console.Title = title;
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
        }
    }
}

//Console.OutputEncoding = Encoding.UTF8;
//Console.InputEncoding = Encoding.UTF8;