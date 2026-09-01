using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Localization;
using System.Text;

namespace dRz.GPT_Utilities.Archivist.Export
{
    /// <summary>
    /// Координирует обработку ZIP-архивов экспорта ChatGPT.
    /// </summary>
    /// <remarks>
    /// Класс не содержит деталей выбора архивов, распаковки и обработки
    /// Markdown. Эти обязанности делегированы отдельным компонентам,
    /// поэтому orchestration можно тестировать и расширять независимо.
    /// </remarks>
    internal sealed class ChatGptExportProcessor : IChatGptExportProcessor
    {
        private readonly IArchiveSelector _archiveSelector;
        private readonly IArchiveExtractor _archiveExtractor;
        private readonly IMarkdownFileProcessor _markdownProcessor;

        /// <summary>
        /// Создаёт процессор с инфраструктурными реализациями по умолчанию.
        /// </summary>
        public ChatGptExportProcessor()
            : this(
                new FileSystemArchiveSelector(),
                new ZipArchiveExtractor(Encoding.GetEncoding(866)),
                new MarkdownFileProcessor(new ExportPathBuilder()))
        {
        }

        /// <summary>
        /// Создаёт процессор с заданными зависимостями.
        /// </summary>
        public ChatGptExportProcessor(
            IArchiveSelector archiveSelector,
            IArchiveExtractor archiveExtractor,
            IMarkdownFileProcessor markdownProcessor)
        {
            _archiveSelector = archiveSelector
                ?? throw new ArgumentNullException(nameof(archiveSelector));
            _archiveExtractor = archiveExtractor
                ?? throw new ArgumentNullException(nameof(archiveExtractor));
            _markdownProcessor = markdownProcessor
                ?? throw new ArgumentNullException(nameof(markdownProcessor));
        }

        /// <summary>
        /// Переходная перегрузка для клиентов, работающих с CLI-моделью.
        /// </summary>
        public ExportResult Process(CommandLineOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return Process(new ExportRequest(
                options.SourceDirectory,
                options.DestinationDirectory,
                options.ZipFilePattern,
                options.ExtractAll));
        }

        /// <summary>
        /// Обрабатывает выбранные экспортные архивы.
        /// </summary>
        public ExportResult Process(ExportRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            IReadOnlyList<FileInfo> archives = _archiveSelector.Select(request);
            ConsoleWriter.Trace($"Найдено {archives.Count.Of(RussianWords.Archives)} для обработки");

            ExportStatistics statistics = new();

            foreach (FileInfo archive in archives)
            {
                ConsoleWriter.Trace($"ZIP: {archive.FullName}");
                ConsoleWriter.Trace($"\tДата изменения ZIP: {archive.LastWriteTime}");

                ExportStatistics archiveStatistics = ProcessArchive(
                    archive,
                    request.DestinationDirectory);

                statistics.Add(archiveStatistics);
            }

            return statistics.ToResult();
        }

        /// <summary>
        /// Распаковывает архив и передаёт Markdown-файлы обработчику.
        /// </summary>
        private ExportStatistics ProcessArchive(
            FileInfo archive,
            string destinationDirectory)
        {
            ExportStatistics statistics = new();

            using ExtractedArchive extractedArchive = _archiveExtractor.Extract(archive);
            ConsoleWriter.Trace(
                $"\tНайдено {extractedArchive.MarkdownFiles.Count.Of(RussianWords.Files)} Markdown");

            foreach (string sourceFile in extractedArchive.MarkdownFiles)
            {
                try
                {
                    FileCopyDecision decision = _markdownProcessor.Process(
                        sourceFile,
                        destinationDirectory);
                    statistics.Add(decision);
                }
                catch (Exception ex)
                {
                    // Ошибка одного файла не останавливает обработку остальных.
                    statistics.AddFailure();
                    ConsoleWriter.Error(
                        $"Ошибка при обработке файла: {sourceFile}",
                        ex);
                }
            }

            return statistics;
        }
    }
}