using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Обрабатывает один Markdown-файл экспортного архива.
/// </summary>
internal interface IMarkdownFileProcessor
{
    FileCopyDecision Process(string sourceFile, string destinationDirectory);
}

/// <summary>
/// Читает метаданные, нормализует имя и синхронизирует Markdown-файл.
/// </summary>
internal sealed class MarkdownFileProcessor : IMarkdownFileProcessor
{
    private readonly IExportPathBuilder _pathBuilder;
    private readonly IChatMetadataReader _metadataReader;
    private readonly IFileSynchronizer _fileSynchronizer;
    private readonly IArchivistLogger _logger;

    public MarkdownFileProcessor(
        IExportPathBuilder pathBuilder,
        IChatMetadataReader metadataReader,
        IFileSynchronizer fileSynchronizer,
        IArchivistLogger logger)
    {
        _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
        _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
        _fileSynchronizer = fileSynchronizer ?? throw new ArgumentNullException(nameof(fileSynchronizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public FileCopyDecision Process(string sourceFile, string destinationDirectory)
    {
        ChatMetadata metadata = _metadataReader.Read(sourceFile);
        string originalName = Path.GetFileNameWithoutExtension(sourceFile);
        string extension = Path.GetExtension(sourceFile);
        string normalizedName = FileNameHelper.Normalize(originalName);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = originalName;
            _logger.Warning($"КОПИРУЮ КАК ЕСТЬ: пустое имя файла: {sourceFile}");
        }

        string destinationFile = _pathBuilder.Build(
            destinationDirectory,
            metadata,
            normalizedName + extension);

        return _fileSynchronizer.Synchronize(
            sourceFile,
            destinationFile,
            metadata);
    }
}
