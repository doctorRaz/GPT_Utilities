using dRz.GPT_Utilities.Archivist.Files;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Строит путь назначения Markdown-файла по его метаданным.
/// </summary>
internal interface IExportPathBuilder
{
    string Build(string destinationDirectory, ChatMetadata metadata, string fileName);
}

/// <summary>
/// Формирует структуру назначения <c>YYYY\MM-MMMM</c>.
/// </summary>
internal sealed class ExportPathBuilder : IExportPathBuilder
{
    private readonly IFileSystem _fileSystem;

    public ExportPathBuilder(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public string Build(
        string destinationDirectory,
        ChatMetadata metadata,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        DateTimeOffset createTime = metadata.CreateTime.ToUniversalTime();
        string monthDirectory = Path.Combine(
            destinationDirectory,
            createTime.ToString("yyyy"),
            createTime.ToString("MM-MMMM", System.Globalization.CultureInfo.InvariantCulture));

        _fileSystem.CreateDirectory(monthDirectory);
        return Path.Combine(monthDirectory, fileName);
    }
}
