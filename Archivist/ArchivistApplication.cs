using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Localization;
using dRz.GPT_Utilities.Archivist.Maintenance;

namespace dRz.GPT_Utilities.Archivist
{
    internal sealed class ArchivistApplication
    {
        private readonly CommandLineOptionsValidator _validator;
        private readonly IChatGptExportProcessor _processor;
        private readonly IFileSystem _fileSystem;
        private readonly ArchiveMaintenance _maintenance;

        /// <summary>Initializes a new instance of the <see cref="ArchivistApplication"/> class.</summary>
        /// <param name="validator">The validator.</param>
        /// <param name="processor">The processor.</param>
        public ArchivistApplication(
                                CommandLineOptionsValidator validator,
                                IChatGptExportProcessor processor)
            : this(validator, processor, new LocalFileSystem())
        {
        }

        public ArchivistApplication(
                                CommandLineOptionsValidator validator,
                                IChatGptExportProcessor processor,
                                IFileSystem fileSystem)
        {
            _validator = validator;
            _processor = processor;
            _fileSystem = fileSystem
                ?? throw new ArgumentNullException(nameof(fileSystem));
            _maintenance = new ArchiveMaintenance(
                _fileSystem,
                new FileNameNormalizer(),
                new DirectoryIndexWriter(_fileSystem));
        }

        internal int Run(string[] args)
        {
            CommandLineOptions options = CommandLineParser.Parse(args);

            if (options.ShowHelp)
            {
                ShowHelp();

                return SuccessExitCode;
            }

            if (options.IsMaintenance)
            {
                ArchiveMaintenanceResult result = _maintenance.Run(options.MaintenanceDirectory);
                PrintMaintenanceStatistics(result);
                return result.Errors == 0 ? SuccessExitCode : ErrorExitCode;
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

            ConsoleWriter.PressAnyKey();

            return statistics.Failed > 0 || statistics.ArchiveFailed > 0
                ? ErrorExitCode
                : SuccessExitCode;
        }

        private static void PrintMaintenanceStatistics(ArchiveMaintenanceResult result)
        {
            ConsoleWriter.Success("================ MAINTENANCE =========================");
            ConsoleWriter.Trace($"Проверено файлов: {result.CheckedFiles}");
            ConsoleWriter.Update($"Переименовано файлов: {result.RenamedFiles}");
            ConsoleWriter.Warn($"Конфликтов: {result.Conflicts}");
            ConsoleWriter.Trace($"Обновлено индексов: {result.UpdatedIndexes}");
            ConsoleWriter.Error($"Ошибок: {result.Errors}");
            ConsoleWriter.Success("=======================================================");
        }

        private void ValidateDirectories(CommandLineOptions options)
        {
            if (!_fileSystem.DirectoryExists(options.SourceDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Каталог с архивами не найден: {options.SourceDirectory}");
            }

            // Каталог назначения может отсутствовать.
            _fileSystem.CreateDirectory(options.DestinationDirectory);
        }

        private static void PrintStatistics(ExportResult statistics)
        {
            ConsoleWriter.Success("================ TOTAL STATISTICS =====================");

            ConsoleWriter.Trace(
                $"Обработано всего: {statistics.Total.Of(RussianWords.Files)}");

            ConsoleWriter.Trace("Из них:");

            ConsoleWriter.Success(
                $"\tДобавлено {statistics.Added.Of(RussianWords.Files)}");

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
                statistics.Updated;

            ConsoleWriter.Info(
                $"Всего заменено и добавлено {addedOrUpdated.Of(RussianWords.Files)}");

            ConsoleWriter.Success("=======================================================");
        }

        private static void ShowHelp()
        {
            CommandLineHelp.Print();
            ConsoleWriter.PressAnyKey();
        }

        private const int ErrorExitCode = 1;
        private const int SuccessExitCode = 0;
    }
}