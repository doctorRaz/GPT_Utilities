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

    public MarkdownFileProcessor(IExportPathBuilder pathBuilder)
    {
        _pathBuilder = pathBuilder;
    }

    public FileCopyDecision Process(string sourceFile, string destinationDirectory)
    {
        ChatMetadata metadata = ChatMetadataReader.Read(sourceFile);
        string originalName = Path.GetFileNameWithoutExtension(sourceFile);
        string extension = Path.GetExtension(sourceFile);
        string normalizedName = FileNameHelper.Normalize(originalName);

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = originalName;
            ConsoleWriter.Warn($"КОПИРУЮ КАК ЕСТЬ: пустое имя файла: {sourceFile}");
        }

        string destinationFile = _pathBuilder.Build(
            destinationDirectory,
            metadata,
            normalizedName + extension);

        return FileSynchronizer.CopyIfNewer(
            sourceFile,
            destinationFile,
            metadata);
    }
}
