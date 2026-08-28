using dRz.GPT_Utilities.Archivist.Services;
using System;
using System.IO;
using System.Text;

namespace dRz.GPT_Utilities.Archivist
{
    internal class Start
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //string destinationDir = "";
            //string sourceDir = "";
#if DEBUG

            ConsoleWriter.Info($"Info:");
            ConsoleWriter.Success($"Success");
            ConsoleWriter.Update($"Update");
            ConsoleWriter.Warning($"Warning");
            ConsoleWriter.Error($"Error");
            ConsoleWriter.Fatal($"Fatal");
            Console.WriteLine("test");

            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
            //destinationDir = @"d:\@Developers\В работе\Reminder\GPT-export\Markdown\";

            //sourceDir = @"d:\@Developers\В работе\GPT_export\chatgpt-export-markdown\";

#endif

            try
            {
                CommandLineOptions options = CommandLineParser.Parse(args);

                if (options.ShowHelp)
                {
                    CommandLineParser.PrintHelp();

                    ConsoleWriter.PressAnyKey();

                    return 0;
                }

                // Source должен существовать.
                if (!Directory.Exists(options.SourceDirectory))
                {
                    throw new DirectoryNotFoundException(
                        $"Каталог с архивами не найден: " +
                        $"{options.SourceDirectory}");
                }
                // Destination может отсутствовать.
                // сразу проверяем возможность создания
                Directory.CreateDirectory(options.DestinationDirectory);

                //идем парсить zip
                int result = ChatGptExportProcessor.Process(options.SourceDirectory, options.DestinationDirectory, options.ExtractAll);

                ConsoleWriter.Info($"Заменено и добавлено файлов: {result}");

                ConsoleWriter.PressAnyKey();

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleWriter.Fatal($"Ошибка: {ex.Message}\n {ex.StackTrace}");

                ConsoleWriter.PressAnyKey();

                return 1;
            }
        }
    }
}