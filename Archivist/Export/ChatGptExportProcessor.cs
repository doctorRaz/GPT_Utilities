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
        private readonly IArchivistLogger _logger;

        /// <summary>
        /// Создаёт процессор с инфраструктурными реализациями по умолчанию.
        /// </summary>
        public ChatGptExportProcessor()
            : this(
                new FileSystemArchiveSelector(),
                new ZipArchiveExtractor(Encoding.GetEncoding(866)),
                new MarkdownFileProcessor(
                    new ExportPathBuilder(),
                    new ChatMetadataReaderService(),
                    new FileSynchronizerService(
                        new ChatMetadataReaderService(),
                        new ConsoleArchivistLogger()),
                    new ConsoleArchivistLogger()),
                new ConsoleArchivistLogger())
        {
        }

        /// <summary>
        /// Создаёт процессор с заданными зависимостями.
        /// </summary>
        public ChatGptExportProcessor(
            IArchiveSelector archiveSelector,
            IArchiveExtractor archiveExtractor,
            IMarkdownFileProcessor markdownProcessor,
            IArchivistLogger logger)
        {
            _archiveSelector = archiveSelector
                ?? throw new ArgumentNullException(nameof(archiveSelector));
            _archiveExtractor = archiveExtractor
                ?? throw new ArgumentNullException(nameof(archiveExtractor));
            _markdownProcessor = markdownProcessor
                ?? throw new ArgumentNullException(nameof(markdownProcessor));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Обрабатывает выбранные экспортные архивы.
        /// </summary>
        public ExportResult Process(ExportRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            IReadOnlyList<FileInfo> archives = _archiveSelector.Select(request);
            _logger.Trace($"Найдено {archives.Count.Of(RussianWords.Archives)} для обработки");

            ExportStatistics statistics = new();

            foreach (FileInfo archive in archives)
            {
                _logger.Trace($"ZIP: {archive.FullName}");
                _logger.Trace($"\tДата изменения ZIP: {archive.LastWriteTime}");

                try
                {
                    ExportStatistics archiveStatistics = ProcessArchive(
                        archive,
                        request.DestinationDirectory);

                    statistics.Add(archiveStatistics);
                }
                catch (InvalidDataException exception)
                {
                    statistics.AddArchiveFailure();
                    _logger.Error(
                        $"Ошибка чтения ZIP-архива: {archive.FullName}",
                        exception);
                }
                catch (IOException exception)
                {
                    statistics.AddArchiveFailure();
                    _logger.Error(
                        $"Ошибка доступа к ZIP-архиву: {archive.FullName}",
                        exception);
                }
                catch (UnauthorizedAccessException exception)
                {
                    statistics.AddArchiveFailure();
                    _logger.Error(
                        $"Нет доступа к ZIP-архиву: {archive.FullName}",
                        exception);
                }
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
            _logger.Trace(
                $"\tНайдено {extractedArchive.MarkdownFiles.Count.Of(RussianWords.Files)} Markdown");

            foreach (string sourceFile in extractedArchive.MarkdownFiles)
            {
                try
                {
                    FileOperationResult result = _markdownProcessor.Process(
                        sourceFile,
                        destinationDirectory);
                    statistics.Add(result);
                }
                catch (Exception ex)
                {
                    // Ошибка одного файла не останавливает обработку остальных.
                    statistics.AddFailure();
                    _logger.Error(
                        $"Ошибка при обработке файла: {sourceFile}",
                        ex);
                }
            }

            return statistics;
        }
    }
}