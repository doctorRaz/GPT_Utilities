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

    public ExtractedArchive(string directory)
    {
        _directory = directory;
        MarkdownFiles = Directory
            .EnumerateFiles(directory, "*.md", SearchOption.AllDirectories)
            .ToArray();
    }

    public IReadOnlyList<string> MarkdownFiles { get; }

    public void Dispose()
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }

        try
        {
            Directory.Delete(_directory, recursive: true);
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
