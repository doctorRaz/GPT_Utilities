using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Localization;
using System.Text;

namespace dRz.GPT_Utilities.Archivist
{

/*
 * Parser отвечает за то, что пользователь ввёл.
 * Main / application layer отвечает за то, можно ли с этими параметрами реально работать.
 * Processor занимается самой обработкой архивов.
*/
    internal class Program
    {
        //[STAThread]
        private static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ConsoleSetup.Configure(AppDomain.CurrentDomain.FriendlyName);

#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
            //ConsoleDemo.Run();
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

                //todo patern обработаем тут
                // Pattern
                //  нормализуем/проверяем здесь

                // Destination может отсутствовать.
                // сразу проверяем возможность создания, каталога
                // лучше упасть здесь, чем в процессе обработки.
                Directory.CreateDirectory(options.DestinationDirectory);

                //идем разбирать zip
                ChatGptExportProcessor.CopyStatistics totalStatistics = ChatGptExportProcessor.Process(options);

                //total statistics
                ConsoleWriter.Success($"================ TOTAL STATISTICS =====================");

                ConsoleWriter.Trace($"Обработано всего: {totalStatistics.Total.Of(RussianWords.Files)}");

                ConsoleWriter.Trace($"Из них:");

                ConsoleWriter.Success($"\tДобавлено {totalStatistics.Added.Of(RussianWords.Files)}");

                ConsoleWriter.Warn($"\tДобавлено уникальных {totalStatistics.AddedUnique.Of(RussianWords.Files)}");

                ConsoleWriter.Update($"\tОбновлено {totalStatistics.Updated.Of(RussianWords.Files)}");

                ConsoleWriter.Trace($"\tПропущено {totalStatistics.Skipped.Of(RussianWords.Files)}");

                ConsoleWriter.Info($"Всего заменено и добавлено {(totalStatistics.Added + totalStatistics.AddedUnique + totalStatistics.Updated).Of(RussianWords.Files)}");

                ConsoleWriter.Success($"=======================================================");
                ConsoleWriter.PressAnyKey();

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleWriter.Fatal(ex, $"Ошибка: ");

                ConsoleWriter.PressAnyKey();

                return 1;
            }
        }
    }
}