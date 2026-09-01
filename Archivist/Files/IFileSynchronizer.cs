using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Применяет политику добавления, обновления и пропуска файла.
/// </summary>
internal interface IFileSynchronizer
{
    FileOperationResult Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata);
}

/// <summary>
/// Экземплярный сервис синхронизации Markdown-файлов.
/// </summary>
internal sealed class FileSynchronizerService : IFileSynchronizer
{
    private readonly IChatMetadataReader _metadataReader;
    private readonly IArchivistLogger _logger;
    private readonly IUniqueFileNameProvider _uniqueFileNameProvider;
    private readonly IFileSystem _fileSystem;
    private readonly IConversationIndex _conversationIndex;

    public FileSynchronizerService(
        IChatMetadataReader metadataReader,
        IArchivistLogger logger,
        IUniqueFileNameProvider uniqueFileNameProvider,
        IFileSystem fileSystem,
        IConversationIndex? conversationIndex = null)
    {
        _metadataReader = metadataReader
            ?? throw new ArgumentNullException(nameof(metadataReader));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
        _uniqueFileNameProvider = uniqueFileNameProvider
            ?? throw new ArgumentNullException(nameof(uniqueFileNameProvider));
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
        _conversationIndex = conversationIndex ?? new ConversationIndex(
            fileSystem,
            metadataReader,
            logger);
    }

    public FileOperationResult Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        ArgumentNullException.ThrowIfNull(sourceMetadata);

        string destinationDirectory = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException(
                $"Не удалось определить каталог: {destinationFilePath}");
        _conversationIndex.EnsureIndexed(destinationDirectory);

        FileOperationStatus status = GetOperationStatus(
            destinationFilePath,
            sourceMetadata);

        if (status == FileOperationStatus.AddedUnique)
        {
            string? matchingPath = FindMatchingDuplicate(
                destinationFilePath,
                sourceMetadata.ConversationId);

            if (matchingPath is null)
            {
                FileOperationResult uniqueResult = CopyToUniqueName(
                    sourceFilePath,
                    destinationFilePath,
                    sourceMetadata);
                WriteOperationResult(uniqueResult);
                return uniqueResult;
            }

            else
            {
                destinationFilePath = matchingPath;
                status = GetOperationStatus(destinationFilePath, sourceMetadata);
            }
        }

        if (status == FileOperationStatus.Added)
        {
            if (!_fileSystem.TryCopyFile(sourceFilePath, destinationFilePath))
            {
                // Файл появился после проверки. Не перезаписываем его,
                // а продолжаем обработку как конфликт имён.
                FileOperationResult uniqueResult = CopyToUniqueName(
                    sourceFilePath,
                    destinationFilePath,
                    sourceMetadata);
                WriteOperationResult(uniqueResult);
                return uniqueResult;
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
            status == FileOperationStatus.Skipped
                ? "Файл не требует обновления."
                : null);

        WriteOperationResult(result);
        return result;
    }

    private FileOperationResult CopyToUniqueName(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        HashSet<string> attemptedPaths = new(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            string candidate = _uniqueFileNameProvider.GetUnique(
                destinationFilePath,
                attemptedPaths);

            attemptedPaths.Add(candidate);

            if (!_fileSystem.TryCopyFile(sourceFilePath, candidate))
            {
                // Имя заняли после проверки. Выбираем следующий кандидат.
                continue;
            }

            SetLastWriteTimeIfPresent(candidate, sourceMetadata);
            _conversationIndex.Track(candidate, sourceMetadata);

            return new FileOperationResult(
                FileOperationStatus.AddedUnique,
                sourceFilePath,
                candidate,
                null);
        }
    }

    private void WriteOperationResult(FileOperationResult result)
    {
        string sourceFileName = Path.GetFileName(result.SourcePath);
        string description =
            $"{sourceFileName}\n\t\tto->{result.DestinationPath}";

        switch (result.Status)
        {
            case FileOperationStatus.Skipped:
                _logger.Trace($"\tПропущен: {description}");
                break;
            case FileOperationStatus.Added:
                _logger.Success($"\tДобавлен: {description}");
                break;
            case FileOperationStatus.AddedUnique:
                _logger.Warning($"\tДобавлен уникальный: {description}");
                break;
            case FileOperationStatus.Updated:
                _logger.Update($"\tОбновлён: {description}");
                break;
            case FileOperationStatus.Failed:
                _logger.Error($"\tОшибка: {description}", result.Error);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(result.Status),
                    result.Status,
                    null);
        }
    }

    private void SetLastWriteTimeIfPresent(
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        if (sourceMetadata.UpdateTime.HasValue)
        {
            _fileSystem.SetLastWriteTime(
                destinationFilePath,
                sourceMetadata.UpdateTime.Value.LocalDateTime);
        }
    }

    private string? FindMatchingDuplicate(
        string destinationFilePath,
        Guid? sourceId)
    {
        if (sourceId is null)
        {
            return null;
        }

        string? matchingPath = null;
        DateTimeOffset? matchingUpdateTime = null;

        string directory = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException(
                $"Не удалось определить каталог: {destinationFilePath}");

        foreach (string duplicatePath in _conversationIndex.FindPaths(
            sourceId.Value,
            directory))
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

            if (sourceId is null ||
                destinationId is null ||
                sourceId != destinationId)
            {
                return FileOperationStatus.AddedUnique;
            }

            if (!destinationMetadata.UpdateTime.HasValue)
            {
                return FileOperationStatus.Updated;
            }

            if (!sourceMetadata.UpdateTime.HasValue)
            {
                return FileOperationStatus.Skipped;
            }

            return sourceMetadata.UpdateTime.Value > destinationMetadata.UpdateTime.Value
                ? FileOperationStatus.Updated
                : FileOperationStatus.Skipped;
        }
        catch (FormatException exception)
        {
            _logger.Error($"Не удалось прочитать метаданные: {destinationFilePath}", exception);
            return FileOperationStatus.AddedUnique;
        }
        catch (IOException exception)
        {
            _logger.Error($"Не удалось прочитать файл: {destinationFilePath}", exception);
            return FileOperationStatus.AddedUnique;
        }
    }
}
