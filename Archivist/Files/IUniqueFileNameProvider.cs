namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Возвращает свободный путь для файла с учётом существующих дубликатов.
/// </summary>
internal interface IUniqueFileNameProvider
{
    string GetUnique(string filePath);

    string GetUnique(
        string filePath,
        IReadOnlySet<string> excludedPaths);
    IEnumerable<string> GetExistingDuplicates(string filePath);
}

/// <summary>
/// Реализация генерации имён с суффиксами <c>(1)</c>...<c>(100)</c>.
/// </summary>
internal sealed class UniqueFileNameProvider : IUniqueFileNameProvider
{
    private readonly IFileSystem _fileSystem;

    public UniqueFileNameProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    private const int MaxDuplicateNumber = 100;

    public string GetUnique(string filePath)
        => GetUnique(filePath, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    public string GetUnique(
        string filePath,
        IReadOnlySet<string> excludedPaths)
    {
        ArgumentNullException.ThrowIfNull(excludedPaths);

        if (!_fileSystem.FileExists(filePath) && !excludedPaths.Contains(filePath))
        {
            return filePath;
        }

        string directory = GetDirectory(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        for (int number = 1; number <= MaxDuplicateNumber; number++)
        {
            string candidate = Path.Combine(
                directory,
                $"{fileName} ({number}){extension}");

            if (!_fileSystem.FileExists(candidate) && !excludedPaths.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new IOException(
            $"Не удалось подобрать свободное имя для файла: {filePath}. " +
            $"Заняты варианты от (1) до ({MaxDuplicateNumber}).");
    }

    public IEnumerable<string> GetExistingDuplicates(string filePath)
    {
        string directory = GetDirectory(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        for (int number = 1; number <= MaxDuplicateNumber; number++)
        {
            string candidate = Path.Combine(
                directory,
                $"{fileName} ({number}){extension}");

            if (_fileSystem.FileExists(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static string GetDirectory(string filePath) =>
        Path.GetDirectoryName(filePath)
        ?? throw new InvalidOperationException(
            $"Не удалось определить каталог: {filePath}");
}
