using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Localization;

namespace dRz.GPT_Utilities.Archivist
{
    internal sealed class ArchivistApplication
    {
        private readonly CommandLineOptionsValidator _validator;
        private readonly IChatGptExportProcessor _processor;

        /// <summary>Initializes a new instance of the <see cref="ArchivistApplication"/> class.</summary>
        /// <param name="validator">The validator.</param>
        /// <param name="processor">The processor.</param>
        public ArchivistApplication(
                                CommandLineOptionsValidator validator,
                                IChatGptExportProcessor processor)
        {
            _validator = validator;
            _processor = processor;
        }

        internal int Run(string[] args)
        {
            CommandLineOptions options = CommandLineParser.Parse(args);

            if (options.ShowHelp)
            {
                ShowHelp();

                return SuccessExitCode;
            }

            options = _validator.Validate(options);

            ValidateDirectories(options);
            ExportRequest request = new(
                options.SourceDirectory,
                options.DestinationDirectory,
                options.ZipFilePattern,
                options.ExtractAll);

            ExportResult statistics = _processor.Process(request);

            PrintStatistics(statistics);

            //ConsoleWriter.PressAnyKey();

            return SuccessExitCode;
        }

        private static void ValidateDirectories(CommandLineOptions options)
        {
            if (!Directory.Exists(options.SourceDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Каталог с архивами не найден: {options.SourceDirectory}");
            }

            // Каталог назначения может отсутствовать.
            _ = Directory.CreateDirectory(options.DestinationDirectory);
        }

        private static void PrintStatistics(ExportResult statistics)
        {
            ConsoleWriter.Success("================ TOTAL STATISTICS =====================");

            ConsoleWriter.Trace(
                $"Обработано всего: {statistics.Total.Of(RussianWords.Files)}");

            ConsoleWriter.Trace("Из них:");

            ConsoleWriter.Success(
                $"\tДобавлено {statistics.Added.Of(RussianWords.Files)}");

            ConsoleWriter.Warn(
                $"\tДобавлено уникальных {statistics.AddedUnique.Of(RussianWords.Files)}");

            ConsoleWriter.Update(
                $"\tОбновлено {statistics.Updated.Of(RussianWords.Files)}");

            ConsoleWriter.Trace(
                $"\tПропущено {statistics.Skipped.Of(RussianWords.Files)}");

            ConsoleWriter.Error(
                $"\tОшибок {statistics.Failed.Of(RussianWords.Files)}");

            ConsoleWriter.Error(
                $"\tОшибок архивов {statistics.ArchiveFailed.Of(RussianWords.Archives)}");

            int addedOrUpdated =
                statistics.Added +
                statistics.AddedUnique +
                statistics.Updated;

            ConsoleWriter.Info(
                $"Всего заменено и добавлено {addedOrUpdated.Of(RussianWords.Files)}");

            ConsoleWriter.Success("=======================================================");
        }

        private static void ShowHelp()
        {
            CommandLineHelp.Print();
            //ConsoleWriter.PressAnyKey();
        }

        private const int ErrorExitCode = 1;
        private const int SuccessExitCode = 0;
    }
}