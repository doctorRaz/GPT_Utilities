using dRz.GPT_Utilities.Archivist.Files;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Распаковывает один экспортный архив во временное хранилище.
/// </summary>
internal interface IArchiveExtractor
{
    ExtractedArchive Extract(FileInfo archive);
}

/// <summary>
/// Распакованный архив и его Markdown-файлы.
/// </summary>
internal sealed class ExtractedArchive : IDisposable
{
    private readonly string _directory;
    private readonly IFileSystem _fileSystem;

    public ExtractedArchive(string directory, IFileSystem fileSystem)
    {
        _directory = directory;
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
        MarkdownFiles = _fileSystem
            .EnumerateFiles(directory, "*.md", SearchOption.AllDirectories)
            .ToArray();
    }

    public IReadOnlyList<string> MarkdownFiles { get; }

    public void Dispose()
    {
        if (!_fileSystem.DirectoryExists(_directory))
        {
            return;
        }

        try
        {
            _fileSystem.DeleteDirectory(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Очистка не должна скрывать ошибку основной обработки.
        }
        catch (UnauthorizedAccessException)
        {
            // Аналогично: отсутствие прав не меняет результат экспорта.
        }
    }
}
