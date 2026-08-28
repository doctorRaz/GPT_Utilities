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

            ConsoleSetup.Initialize(AppDomain.CurrentDomain.FriendlyName);

#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }

            ConsoleWriter.TestShowStyles();
            ConsoleWriter.TestShowColors();
            ConsoleWriter.TestShowColorsBackground();

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
                // сразу проверяем возможность создания, каталога
                // лучше упасть здесь, чем в процессе обработки.
                Directory.CreateDirectory(options.DestinationDirectory);

                //идем разбирать zip
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