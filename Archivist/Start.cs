using dRz.GPT_Utilities.Archivist.Services;
using System;
using System.IO;
using System.Text;

namespace dRz.GPT_Utilities.Archivist
{
    internal class Start
    {
        //[STAThread]
        private static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ConsoleSetup.Initialize(AppDomain.CurrentDomain.FriendlyName);

#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }

            //for (int i = 0; i < 30; i++)
            //{
            //    Console.WriteLine(i.Of(Words.Files));
            //}
            //try
            //{
            //    var f = 0;
            //    _ = 10 / f;

            //}
            //catch (Exception ex)
            //{
            //ConsoleWriter.Fatal(ex, $"Ошибка: ");
            //ConsoleWriter.Error($"Ошибка: ",ex);
            //}
            //Console.WriteLine(5.Of(Words.Archives));

            //ConsoleWriter.TestShowStyles();


            //ConsoleWriter.TestShowColors();
            //ConsoleWriter.TestShowColorsBackground();

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
                ChatGptExportProcessor.CopyStatistics totalStatistics = ChatGptExportProcessor.Process(options.SourceDirectory, options.DestinationDirectory, options.ExtractAll);

                //total statistics
                ConsoleWriter.Success($"================ TOTAL STATISTICS =====================");

                ConsoleWriter.Trace($"Обработано всего: {totalStatistics.Total.Of(Words.Files)}");

                ConsoleWriter.Trace($"Из них:");

                ConsoleWriter.Success($"\tДобавлено {totalStatistics.Added.Of(Words.Files)}");

                ConsoleWriter.Warn($"\tДобавлено уникальных {totalStatistics.AddedUnique.Of(Words.Files)}");

                ConsoleWriter.Update($"\tОбновлено {totalStatistics.Updated.Of(Words.Files)}");

                ConsoleWriter.Trace($"\tПропущено {totalStatistics.Skipped.Of(Words.Files)}");

                ConsoleWriter.Info($"Всего заменено и добавлено {(totalStatistics.Added + totalStatistics.AddedUnique + totalStatistics.Updated).Of(Words.Files)}");

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