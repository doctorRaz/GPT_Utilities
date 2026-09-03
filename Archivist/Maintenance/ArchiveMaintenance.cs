using dRz.GPT_Utilities.Archivist.Files;

namespace dRz.GPT_Utilities.Archivist.Maintenance;

/// <summary>Результат обслуживания существующего vault.</summary>
internal sealed class ArchiveMaintenanceResult
{
    public int CheckedFiles { get; internal set; }
    public int RenamedFiles { get; internal set; }
    public int Conflicts { get; internal set; }
    public int UpdatedIndexes { get; internal set; }
    public int Errors { get; internal set; }
}

/// <summary>Нормализует существующий vault и перестраивает навигационные индексы.</summary>
internal sealed class ArchiveMaintenance
{
    private const string IndexFileName = "_index.md";
    private readonly IFileSystem _fileSystem;
    private readonly IFileNameNormalizer _normalizer;
    private readonly DirectoryIndexWriter _indexWriter;

    public ArchiveMaintenance(IFileSystem fileSystem, IFileNameNormalizer normalizer, DirectoryIndexWriter indexWriter)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        _indexWriter = indexWriter ?? throw new ArgumentNullException(nameof(indexWriter));
    }

    public ArchiveMaintenanceResult Run(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("Не указан каталог vault.", nameof(rootDirectory));
        if (!_fileSystem.DirectoryExists(rootDirectory))
            throw new DirectoryNotFoundException($"Каталог vault не найден: {rootDirectory}");

        ArchiveMaintenanceResult result = new();
        NormalizeFiles(Path.GetFullPath(rootDirectory), result);
        _indexWriter.Rebuild(Path.GetFullPath(rootDirectory), result);
        return result;
    }

    private void NormalizeFiles(string root, ArchiveMaintenanceResult result)
    {
        string[] files = _fileSystem.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), IndexFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        result.CheckedFiles = files.Length;

        var moves = new List<(string Source, string Target)>();
        var occupied = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string source in files)
        {
            string name = Path.GetFileNameWithoutExtension(source);
            string normalized = _normalizer.Normalize(name);
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = name;
            string target = Path.Combine(Path.GetDirectoryName(source)!, normalized + Path.GetExtension(source));
            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
                continue;

            if (occupied.Contains(target) || !targets.Add(target))
            {
                result.Conflicts++;
                try
                {
                    target = GetUniqueTarget(target, occupied, targets);
                    _ = targets.Add(target);
                }
                catch (Exception exception)
                {
                    result.Errors++;
                    continue;
                }
            }
            moves.Add((source, target));
        }

        var temporary = new List<(string Temp, string Target)>();
        foreach ((string source, string target) in moves)
        {
            try
            {
                string temp = source + $".archivist-maintenance-{Guid.NewGuid():N}.tmp";
                _fileSystem.MoveFile(source, temp);
                temporary.Add((temp, target));
            }
            catch (Exception)
            {
                result.Errors++;
            }
        }

        foreach ((string temp, string target) in temporary)
        {
            try
            {
                _fileSystem.MoveFile(temp, target);
                result.RenamedFiles++;
            }
            catch (Exception)
            {
                result.Errors++;
            }
        }
    }

    private static string GetUniqueTarget(string path, ISet<string> occupied, ISet<string> targets)
    {
        string directory = Path.GetDirectoryName(path)!;
        string stem = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        for (int number = 1; number <= 100; number++)
        {
            string candidate = Path.Combine(directory, $"{stem} ({number}){extension}");
            if (!occupied.Contains(candidate) && !targets.Contains(candidate))
                return candidate;
        }
        throw new IOException($"Не удалось подобрать свободное имя: {path}");
    }
}
