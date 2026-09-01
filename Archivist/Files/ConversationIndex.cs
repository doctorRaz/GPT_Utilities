using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Индекс Markdown-файлов по идентификатору разговора.
/// Индекс строится лениво отдельно для каждого каталога и обновляется при записи файлов.
/// </summary>
internal interface IConversationIndex
{
    void EnsureIndexed(string directory);

    bool TryGet(string path, out ChatMetadata metadata);

    IEnumerable<string> FindPaths(Guid conversationId, string directory);

    void Track(string path, ChatMetadata metadata);
}

internal sealed class ConversationIndex : IConversationIndex
{
    private readonly IFileSystem _fileSystem;
    private readonly IChatMetadataReader _metadataReader;
    private readonly IArchivistLogger _logger;
    private readonly HashSet<string> _indexedDirectories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ChatMetadata> _byPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, HashSet<string>> _byConversation = new();

    public ConversationIndex(
        IFileSystem fileSystem,
        IChatMetadataReader metadataReader,
        IArchivistLogger logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

    public bool TryGet(string path, out ChatMetadata metadata) =>
        _byPath.TryGetValue(Normalize(path), out metadata!);

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

    private static string Normalize(string path) => Path.GetFullPath(path);
}
