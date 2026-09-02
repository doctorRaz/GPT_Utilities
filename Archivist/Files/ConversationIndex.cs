using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Интерфейс для индекса Markdown-файлов по идентификатору разговора.
/// Индекс строится лениво отдельно для каждого каталога и обновляется при записи файлов.
/// </summary>
internal interface IConversationIndex
{
    /// <summary>
    /// Обеспечивает индексацию указанного каталога, если он еще не проиндексирован.
    /// При индексации анализируются все Markdown-файлы в каталоге и их метаданные добавляются в индекс.
    /// </summary>
    /// <param name="directory">Каталог для индексации</param>
    void EnsureIndexed(string directory);

    /// <summary>
    /// Пытается получить метаданные для указанного файла.
    /// </summary>
    /// <param name="path">Путь к файлу</param>
    /// <param name="metadata">Метаданные файла, если они найдены</param>
    /// <returns>True, если метаданные найдены, иначе false</returns>
    bool TryGet(string path, out ChatMetadata metadata);

    /// <summary>
    /// Находит все пути к файлам для указанного идентификатора разговора в заданном каталоге.
    /// </summary>
    /// <param name="conversationId">Идентификатор разговора</param>
    /// <param name="directory">Каталог для поиска</param>
    /// <returns>Перечисление путей к файлам</returns>
    IEnumerable<string> FindPaths(Guid conversationId, string directory);

    /// <summary>
    /// Добавляет файл в индекс.
    /// </summary>
    /// <param name="path">Путь к файлу</param>
    /// <param name="metadata">Метаданные файла</param>
    void Track(string path, ChatMetadata metadata);
}

/// <summary>
/// Реализация индекса Markdown-файлов для быстрого поиска по идентификатору разговора.
/// </summary>
internal sealed class ConversationIndex : IConversationIndex
{
    /// <summary>Система файловых операций.</summary>
    private readonly IFileSystem _fileSystem;
    /// <summary>Средство чтения метаданных.</summary>
    private readonly IChatMetadataReader _metadataReader;
    /// <summary>Журналировщик.</summary>
    private readonly IArchivistLogger _logger;
    /// <summary>Набор уже проиндексированных каталогов.</summary>
    private readonly HashSet<string> _indexedDirectories = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Словарь метаданных, индексируемый по пути к файлу.</summary>
    private readonly Dictionary<string, ChatMetadata> _byPath = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Словарь путей к файлам, индексируемый по идентификатору разговора.</summary>
    private readonly Dictionary<Guid, HashSet<string>> _byConversation = new();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ConversationIndex"/>.
    /// </summary>
    /// <param name="fileSystem">Система файловых операций.</param>
    /// <param name="metadataReader">Средство чтения метаданных.</param>
    /// <param name="logger">Журналировщик.</param>
    public ConversationIndex(
        IFileSystem fileSystem,
        IChatMetadataReader metadataReader,
        IArchivistLogger logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Проверяет, проиндексирован ли каталог, и выполняет индексацию при необходимости.
    /// </summary>
    /// <param name="directory">Путь к каталогу.</param>
    public void EnsureIndexed(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Каталог не может быть пустым.", nameof(directory));
        }
        directory = Path.GetFullPath(directory);

        if (!_indexedDirectories.Add(directory))
        {
            return;
        }

        foreach (string path in _fileSystem.EnumerateFiles(
            directory, "*.md", SearchOption.TopDirectoryOnly))
        {
            try
            {
                Track(path, _metadataReader.Read(path));
            }
            catch (FormatException exception)
            {
                _logger.Error($"Не удалось прочитать метаданные: {path}", exception);
            }
            catch (IOException exception)
            {
                _logger.Error($"Не удалось прочитать файл: {path}", exception);
            }
        }
    }

    /// <summary>
    /// Пытается извлечь метаданные для указанного пути.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <param name="metadata">Выходной параметр с метаданными.</param>
    /// <returns>True, если метаданные найдены.</returns>
    public bool TryGet(string path, out ChatMetadata metadata) =>
        _byPath.TryGetValue(Normalize(path), out metadata!);

    /// <summary>
    /// Находит все пути к файлам, принадлежащим указанному разговору, в пределах заданного каталога.
    /// </summary>
    /// <param name="conversationId">ID разговора.</param>
    /// <param name="directory">Каталог поиска.</param>
    /// <returns>Коллекция путей к файлам.</returns>
    public IEnumerable<string> FindPaths(Guid conversationId, string directory)
    {
        if (!_byConversation.TryGetValue(conversationId, out HashSet<string>? paths))
        {
            return Enumerable.Empty<string>();
        }

        string normalizedDirectory = Normalize(directory).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return paths.Where(path => path.StartsWith(
            normalizedDirectory, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Добавляет файл и его метаданные в индекс.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <param name="metadata">Метаданные файла.</param>
    public void Track(string path, ChatMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        string normalizedPath = Normalize(path);

        if (_byPath.TryGetValue(normalizedPath, out ChatMetadata? previous) &&
            previous.ConversationId is Guid previousId &&
            _byConversation.TryGetValue(previousId, out HashSet<string>? previousPaths))
        {
            _ = previousPaths.Remove(normalizedPath);
        }

        _byPath[normalizedPath] = metadata;
        if (metadata.ConversationId is Guid conversationId)
        {
            if (!_byConversation.TryGetValue(conversationId, out HashSet<string>? paths))
            {
                paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _byConversation.Add(conversationId, paths);
            }

            _ = paths.Add(normalizedPath);
        }
    }

    /// <summary>
    /// Нормализует путь, преобразуя его в полный путь.
    /// </summary>
    /// <param name="path">Путь для нормализации.</param>
    /// <returns>Полный путь.</returns>
    private static string Normalize(string path) => Path.GetFullPath(path);
}
