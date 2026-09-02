namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Возвращает свободный путь для файла с учётом существующих дубликатов.
/// </summary>
internal interface IUniqueFileNameProvider
{
    /// <summary>
    /// Возвращает уникальный путь для файла. Если файл существует, добавляется числовой суффикс.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Уникальный путь.</returns>
    string GetUnique(string filePath);

    /// <summary>
    /// Возвращает уникальный путь, исключая заданный набор путей.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <param name="excludedPaths">Набор исключаемых путей.</param>
    /// <returns>Уникальный путь.</returns>
    string GetUnique(
        string filePath,
        IReadOnlySet<string> excludedPaths);

    /// <summary>
    /// Находит все существующие файлы-дубликаты для указанного пути.
    /// </summary>
    /// <param name="filePath">Путь к исходному файлу.</param>
    /// <returns>Коллекция путей к дубликатам.</returns>
    IEnumerable<string> GetExistingDuplicates(string filePath);
}

/// <summary>
/// Реализация генерации имён с суффиксами <c>(1)</c>...<c>(100)</c>.
/// </summary>
internal sealed class UniqueFileNameProvider : IUniqueFileNameProvider
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UniqueFileNameProvider"/>.
    /// </summary>
    /// <param name="fileSystem">Система файловых операций.</param>
    public UniqueFileNameProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <summary>Максимальное количество попыток подбора суффикса.</summary>
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

    /// <summary>
    /// Извлекает путь к каталогу из полного пути к файлу.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Путь к каталогу.</returns>
    private static string GetDirectory(string filePath) =>
        Path.GetDirectoryName(filePath)
        ?? throw new InvalidOperationException(
            $"Не удалось определить каталог: {filePath}");
}
