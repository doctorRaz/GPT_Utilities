using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using System.Text;

namespace dRz.GPT_Utilities.Archivist.Files;

internal interface IFileSynchronizer
{
    FileOperationResult Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata);
}

internal interface IConversationIndexWriter
{
    void Refresh(string directory);
}

internal sealed class ConversationIndexWriter : IConversationIndexWriter
{
    private const string IndexFileName = "_index.md";
    private readonly IFileSystem _fileSystem;

    public ConversationIndexWriter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public void Refresh(string directory)
    {
        string year = Directory.GetParent(directory)?.Name ?? string.Empty;
        string month = new DirectoryInfo(directory).Name;
        int separator = month.IndexOf('-');
        string monthName = separator >= 0 ? month[(separator + 1)..] : month;

        IEnumerable<string> files = _fileSystem
            .EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(
                Path.GetFileName(path), IndexFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

        StringBuilder contents = new();
        _ = contents.AppendLine($"# {monthName} {year}");
        _ = contents.AppendLine();
        _ = contents.AppendLine("## Conversations");
        _ = contents.AppendLine();

        foreach (string path in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string encodedFileName = Uri.EscapeDataString(Path.GetFileName(path));
            _ = contents.AppendLine($"- [{fileName}]({encodedFileName})");
        }

        _fileSystem.WriteAllText(Path.Combine(directory, IndexFileName), contents.ToString());
    }
}

internal sealed class FileSynchronizerService : IFileSynchronizer
{
    private readonly IChatMetadataReader _metadataReader;
    private readonly IArchivistLogger _logger;
    private readonly IUniqueFileNameProvider _uniqueFileNameProvider;
    private readonly IFileSystem _fileSystem;
    private readonly IConversationIndex _conversationIndex;
    private readonly IConversationIndexWriter _conversationIndexWriter;
    private readonly List<ExportError> _operationErrors = new();
    private static readonly object SynchronizationLock = new();

    public FileSynchronizerService(
        IChatMetadataReader metadataReader,
        IArchivistLogger logger,
        IUniqueFileNameProvider uniqueFileNameProvider,
        IFileSystem fileSystem,
        IConversationIndex? conversationIndex = null,
        IConversationIndexWriter? conversationIndexWriter = null)
    {
        _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uniqueFileNameProvider = uniqueFileNameProvider ?? throw new ArgumentNullException(nameof(uniqueFileNameProvider));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _conversationIndex = conversationIndex ?? new ConversationIndex(fileSystem, metadataReader, logger);
        _conversationIndexWriter = conversationIndexWriter ?? new ConversationIndexWriter(fileSystem);
    }

    public FileOperationResult Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        lock (SynchronizationLock)
        {
            _operationErrors.Clear();
            FileOperationResult result = SynchronizeCore(sourceFilePath, destinationFilePath, sourceMetadata);

            if (result.Status is FileOperationStatus.Added or FileOperationStatus.Updated)
            {
                string destinationPath = result.DestinationPath
                    ?? throw new InvalidOperationException("Путь назначения отсутствует.");
                string directory = Path.GetDirectoryName(destinationPath)
                    ?? throw new InvalidOperationException($"Не удалось определить каталог: {destinationPath}");
                _conversationIndexWriter.Refresh(directory);
            }

            return _operationErrors.Count == 0
                ? result
                : result with { Errors = _operationErrors.ToArray() };
        }
    }

    private FileOperationResult SynchronizeCore(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        ArgumentNullException.ThrowIfNull(sourceMetadata);

        string destinationDirectory = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException($"Не удалось определить каталог: {destinationFilePath}");
        int indexErrorsBefore = _conversationIndex.ReadErrorCount;
        _conversationIndex.EnsureIndexed(destinationDirectory);
        int indexReadErrors = _conversationIndex.ReadErrorCount - indexErrorsBefore;

        if (sourceMetadata.ConversationId is Guid sourceConversationId)
        {
            return AddIndexErrors(
                SynchronizeConversation(
                    sourceFilePath,
                    destinationFilePath,
                    sourceMetadata,
                    sourceConversationId,
                    destinationDirectory),
                indexReadErrors);
        }

        FileOperationStatus status = GetOperationStatus(destinationFilePath, sourceMetadata);

        if (status == FileOperationStatus.Added && _fileSystem.FileExists(destinationFilePath))
        {
            string? matchingPath = FindMatchingDuplicate(destinationFilePath, sourceMetadata.ConversationId);
            if (matchingPath is null)
            {
                FileOperationResult uniqueResult = CopyToUniqueName(sourceFilePath, destinationFilePath, sourceMetadata);
                WriteOperationResult(uniqueResult);
                return AddIndexErrors(uniqueResult, indexReadErrors);
            }

            destinationFilePath = matchingPath;
            status = GetOperationStatus(destinationFilePath, sourceMetadata);
        }

        if (status == FileOperationStatus.Added)
        {
            if (!_fileSystem.TryCopyFile(sourceFilePath, destinationFilePath))
            {
                FileOperationResult uniqueResult = CopyToUniqueName(sourceFilePath, destinationFilePath, sourceMetadata);
                WriteOperationResult(uniqueResult);
                return AddIndexErrors(uniqueResult, indexReadErrors);
            }

            SetLastWriteTimeIfPresent(destinationFilePath, sourceMetadata);
            _conversationIndex.Track(destinationFilePath, sourceMetadata);
        }
        else if (status != FileOperationStatus.Skipped)
        {
            _fileSystem.CopyFile(sourceFilePath, destinationFilePath, overwrite: true);
            SetLastWriteTimeIfPresent(destinationFilePath, sourceMetadata);
            _conversationIndex.Track(destinationFilePath, sourceMetadata);
        }

        FileOperationResult result = new(
            status,
            sourceFilePath,
            destinationFilePath,
            status == FileOperationStatus.Skipped ? "Файл не требует обновления." : null);

        WriteOperationResult(result);
        return AddIndexErrors(result, indexReadErrors);
    }

    private static FileOperationResult AddIndexErrors(FileOperationResult result, int indexReadErrors) =>
        indexReadErrors == 0 ? result : result with { IndexReadErrors = indexReadErrors };

    private FileOperationResult SynchronizeConversation(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata,
        Guid sourceConversationId,
        string destinationDirectory)
    {
        DateTimeOffset sourceUpdateTime = NormalizeUpdateTime(sourceMetadata.UpdateTime);
        List<string> stalePaths = new();
        bool hasNewerVersion = false;
        bool hasEqualVersion = false;

        foreach (string path in _conversationIndex.FindPaths(sourceConversationId, destinationDirectory))
        {
            if (!_conversationIndex.TryGet(path, out ChatMetadata existingMetadata))
            {
                existingMetadata = _metadataReader.Read(path);
                _conversationIndex.Track(path, existingMetadata);
            }

            DateTimeOffset existingUpdateTime = NormalizeUpdateTime(existingMetadata.UpdateTime);
            bool bothHaveUpdateTime = existingMetadata.UpdateTime.HasValue && sourceMetadata.UpdateTime.HasValue;

            if (!existingMetadata.UpdateTime.HasValue && !sourceMetadata.UpdateTime.HasValue)
            {
                stalePaths.Add(path);
            }
            else if (bothHaveUpdateTime && existingUpdateTime == sourceUpdateTime)
            {
                hasEqualVersion = true;
            }
            else if (existingUpdateTime < sourceUpdateTime)
            {
                stalePaths.Add(path);
            }
            else
            {
                hasNewerVersion = true;
            }
        }

        if (hasNewerVersion)
        {
            DeleteStaleVersions(stalePaths, preservedPath: null);
            FileOperationResult skippedResult = new(
                FileOperationStatus.Skipped,
                sourceFilePath,
                destinationFilePath,
                "Существует более новая версия разговора.");
            WriteOperationResult(skippedResult);
            return skippedResult;
        }

        if (hasEqualVersion)
        {
            DeleteStaleVersions(stalePaths, preservedPath: null);
            FileOperationResult skippedResult = new(
                FileOperationStatus.Skipped,
                sourceFilePath,
                destinationFilePath,
                "Существует версия разговора с той же датой обновления.");
            WriteOperationResult(skippedResult);
            return skippedResult;
        }

        // Наличие stalePaths означает, что этот conversation_id уже существовал.
        // Сначала удаляем все устаревшие версии, и только после этого выбираем имя.
        bool isUpdate = stalePaths.Count > 0;
        DeleteStaleVersions(stalePaths, preservedPath: null);

        FileOperationResult? uniqueResult = null;
        bool copied;

        if (_fileSystem.FileExists(destinationFilePath))
        {
            if (_conversationIndex.TryGet(destinationFilePath, out ChatMetadata destinationMetadata) &&
                destinationMetadata.ConversationId == sourceConversationId)
            {
                _fileSystem.CopyFile(sourceFilePath, destinationFilePath, overwrite: true);
                copied = true;
            }
            else
            {
                uniqueResult = CopyToUniqueName(sourceFilePath, destinationFilePath, sourceMetadata);
                copied = true;
            }
        }
        else
        {
            copied = _fileSystem.TryCopyFile(sourceFilePath, destinationFilePath);
            if (!copied)
            {
                ChatMetadata racedMetadata = _metadataReader.Read(destinationFilePath);
                _conversationIndex.Track(destinationFilePath, racedMetadata);
                if (racedMetadata.ConversationId == sourceConversationId)
                {
                    return SynchronizeConversation(
                        sourceFilePath,
                        destinationFilePath,
                        sourceMetadata,
                        sourceConversationId,
                        destinationDirectory);
                }

                uniqueResult = CopyToUniqueName(sourceFilePath, destinationFilePath, sourceMetadata);
                copied = true;
            }
        }

        if (!copied)
        {
            throw new IOException($"Не удалось скопировать файл: {sourceFilePath}");
        }

        string actualDestinationPath = uniqueResult?.DestinationPath ?? destinationFilePath;
        SetLastWriteTimeIfPresent(actualDestinationPath, sourceMetadata);
        _conversationIndex.Track(actualDestinationPath, sourceMetadata);

        FileOperationResult result = new(
            isUpdate ? FileOperationStatus.Updated : FileOperationStatus.Added,
            sourceFilePath,
            actualDestinationPath,
            null);

        WriteOperationResult(result);
        return result;
    }

    private static DateTimeOffset NormalizeUpdateTime(DateTimeOffset? updateTime)
    {
        DateTimeOffset utc = (updateTime ?? default).ToUniversalTime();
        long ticks = utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond;
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private void DeleteStaleVersions(IEnumerable<string> stalePaths, string? preservedPath)
    {
        foreach (string path in stalePaths)
        {
            if (preservedPath is not null && string.Equals(path, preservedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _fileSystem.DeleteFile(path);
            _conversationIndex.Remove(path);
        }
    }

    private FileOperationResult CopyToUniqueName(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        HashSet<string> attemptedPaths = new(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            string candidate = _uniqueFileNameProvider.GetUnique(destinationFilePath, attemptedPaths);
            attemptedPaths.Add(candidate);

            if (!_fileSystem.TryCopyFile(sourceFilePath, candidate))
            {
                continue;
            }

            SetLastWriteTimeIfPresent(candidate, sourceMetadata);
            _conversationIndex.Track(candidate, sourceMetadata);

            return new FileOperationResult(
                FileOperationStatus.Added,
                sourceFilePath,
                candidate,
                null);
        }
    }

    private void WriteOperationResult(FileOperationResult result)
    {
        string sourceFileName = Path.GetFileName(result.SourcePath);
        string description = $"{sourceFileName}\t\tto->{result.DestinationPath}";

        switch (result.Status)
        {
            case FileOperationStatus.Skipped:
                _logger.Trace($"\tПропущен: {description}");
                break;
            case FileOperationStatus.Added:
                _logger.Success($"\tДобавлен: {description}");
                break;
            case FileOperationStatus.Updated:
                _logger.Update($"\tОбновлён: {description}");
                break;
            case FileOperationStatus.Failed:
                _logger.Error($"\tОшибка: {description}", result.Error);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
        }
    }

    private void SetLastWriteTimeIfPresent(string destinationFilePath, ChatMetadata sourceMetadata)
    {
        if (sourceMetadata.UpdateTime.HasValue)
        {
            _fileSystem.SetLastWriteTime(destinationFilePath, sourceMetadata.UpdateTime.Value.LocalDateTime);
        }
    }

    private string? FindMatchingDuplicate(string destinationFilePath, Guid? sourceId)
    {
        if (sourceId is null)
        {
            return null;
        }

        string? matchingPath = null;
        DateTimeOffset? matchingUpdateTime = null;
        string directory = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException($"Не удалось определить каталог: {destinationFilePath}");

        foreach (string duplicatePath in _conversationIndex.FindPaths(sourceId.Value, directory))
        {
            if (!_conversationIndex.TryGet(duplicatePath, out ChatMetadata metadata))
            {
                continue;
            }

            if (metadata.UpdateTime > matchingUpdateTime || matchingPath is null)
            {
                matchingPath = duplicatePath;
                matchingUpdateTime = metadata.UpdateTime;
            }
        }

        return matchingPath;
    }

    private FileOperationStatus GetOperationStatus(
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        if (!_fileSystem.FileExists(destinationFilePath))
        {
            return FileOperationStatus.Added;
        }

        try
        {
            ChatMetadata destinationMetadata;
            if (!_conversationIndex.TryGet(destinationFilePath, out destinationMetadata!))
            {
                destinationMetadata = _metadataReader.Read(destinationFilePath);
                _conversationIndex.Track(destinationFilePath, destinationMetadata);
            }

            Guid? sourceId = sourceMetadata.ConversationId;
            Guid? destinationId = destinationMetadata.ConversationId;

            if (sourceId is null || destinationId is null || sourceId != destinationId)
            {
                return FileOperationStatus.Added;
            }

            if (!destinationMetadata.UpdateTime.HasValue)
            {
                return FileOperationStatus.Updated;
            }

            if (!sourceMetadata.UpdateTime.HasValue)
            {
                return FileOperationStatus.Skipped;
            }

            return NormalizeUpdateTime(sourceMetadata.UpdateTime) >
                   NormalizeUpdateTime(destinationMetadata.UpdateTime)
                ? FileOperationStatus.Updated
                : FileOperationStatus.Skipped;
        }
        catch (FormatException exception)
        {
            _logger.Error($"Не удалось прочитать метаданные: {destinationFilePath}", exception);
            AddDestinationErrorIfNew(destinationFilePath, exception);
            return FileOperationStatus.Added;
        }
        catch (IOException exception)
        {
            _logger.Error($"Не удалось прочитать файл: {destinationFilePath}", exception);
            AddDestinationErrorIfNew(destinationFilePath, exception);
            return FileOperationStatus.Added;
        }
    }

    private void AddDestinationErrorIfNew(string path, Exception exception)
    {
        if (_conversationIndex.HasReadError(path))
        {
            return;
        }

        _operationErrors.Add(new ExportError(
            path,
            exception.GetType().Name,
            exception.Message,
            "Destination"));
    }
}
