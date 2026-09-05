using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Обрабатывает один Markdown-файл экспортного архива.
/// </summary>
internal interface IMarkdownFileProcessor
{
    FileOperationResult Process(string sourceFile, string destinationDirectory);
}

/// <summary>
/// Читает метаданные, нормализует имя и синхронизирует Markdown-файл.
/// </summary>
internal sealed class MarkdownFileProcessor : IMarkdownFileProcessor
{
    private readonly IExportPathBuilder _pathBuilder;
    private readonly IChatMetadataReader _metadataReader;
    private readonly IFileSynchronizer _fileSynchronizer;
    private readonly IChatMetadataWriter _metadataWriter;
    private readonly IArchivistLogger _logger;
    private readonly IFileNameNormalizer _fileNameNormalizer;

    public MarkdownFileProcessor(
        IExportPathBuilder pathBuilder,
        IChatMetadataReader metadataReader,
        IFileSynchronizer fileSynchronizer,
        IArchivistLogger logger,
        IFileNameNormalizer fileNameNormalizer,
        IChatMetadataWriter metadataWriter)
    {
        _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
        _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
        _fileSynchronizer = fileSynchronizer ?? throw new ArgumentNullException(nameof(fileSynchronizer));
        _metadataWriter = metadataWriter ?? throw new ArgumentNullException(nameof(metadataWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileNameNormalizer = fileNameNormalizer
            ?? throw new ArgumentNullException(nameof(fileNameNormalizer));
    }

    public FileOperationResult Process(string sourceFile, string destinationDirectory)
    {
        ChatMetadata metadata = _metadataReader.Read(sourceFile);
        string originalName = Path.GetFileNameWithoutExtension(sourceFile);
        string extension = Path.GetExtension(sourceFile);
        string normalizedName = _fileNameNormalizer.Normalize(originalName);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = originalName;
            _logger.Warning($"КОПИРУЮ КАК ЕСТЬ: пустое имя файла: {sourceFile}");
        }

        string destinationFile = _pathBuilder.Build(
            destinationDirectory,
            metadata,
            normalizedName + extension);

        FileOperationResult result = _fileSynchronizer.Synchronize(
            sourceFile,
            destinationFile,
            metadata);

        if (result.Status is FileOperationStatus.Added or FileOperationStatus.Updated)
        {
            string actualDestination = result.DestinationPath
                ?? throw new InvalidOperationException("Путь назначения отсутствует.");
            _metadataWriter.Write(actualDestination, metadata);
        }

        return result;
    }
}
