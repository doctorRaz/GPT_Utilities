using dRz.GPT_Utilities.Archivist.Files;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Выбирает ZIP-файлы из локальной файловой системы.
/// </summary>
internal sealed class FileSystemArchiveSelector : IArchiveSelector
{
    private readonly IFileSystem _fileSystem;

    public FileSystemArchiveSelector()
        : this(new LocalFileSystem())
    {
    }

    public FileSystemArchiveSelector(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public IReadOnlyList<FileInfo> Select(ExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<FileInfo> archives = _fileSystem
            .EnumerateFiles(
                request.SourceDirectory,
                request.ZipFilePattern,
                SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToList();

        if (archives.Count == 0)
        {
            throw new FileNotFoundException(
                $"В каталоге не найден ни один ZIP-архив: {request.SourceDirectory}");
        }

        // Обработка одного архива по умолчанию означает самый новый архив.
        return request.ProcessAllArchives
            ? archives
            : archives.TakeLast(1).ToList();
    }
}
