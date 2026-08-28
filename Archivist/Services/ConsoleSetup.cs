using System;

namespace dRz.GPT_Utilities.Archivist.Services
{
    public static class ConsoleSetup
    {
        public static void Initialize(string? title = null)
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