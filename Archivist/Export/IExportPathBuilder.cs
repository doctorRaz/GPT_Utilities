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
///
/// Правила формирования пути:
/// <list type="bullet">
/// <item>время нормализуется к UTC;</item>
/// <item>название месяца форматируется через <see cref="CultureInfo.InvariantCulture"/>;</item>
/// <item>используется стабильный английский формат, например <c>2024\03-March</c>.</item>
/// </list>
/// </summary>
internal sealed class ExportPathBuilder : IExportPathBuilder
{
    private const string YearFormat = "yyyy";
    private const string MonthFormat = "MM-MMMM";

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
            createTime.ToString(YearFormat, System.Globalization.CultureInfo.InvariantCulture),
            createTime.ToString(MonthFormat, System.Globalization.CultureInfo.InvariantCulture));

        _fileSystem.CreateDirectory(monthDirectory);
        return Path.Combine(monthDirectory, fileName);
    }
}
