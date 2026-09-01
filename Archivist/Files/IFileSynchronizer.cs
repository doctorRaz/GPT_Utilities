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

    public FileSynchronizerService(
        IChatMetadataReader metadataReader,
        IArchivistLogger logger,
        IUniqueFileNameProvider uniqueFileNameProvider,
        IFileSystem fileSystem)
    {
        _metadataReader = metadataReader
            ?? throw new ArgumentNullException(nameof(metadataReader));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
        _uniqueFileNameProvider = uniqueFileNameProvider
            ?? throw new ArgumentNullException(nameof(uniqueFileNameProvider));
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public FileOperationResult Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        ArgumentNullException.ThrowIfNull(sourceMetadata);

        FileCopyDecision decision = GetCopyDecision(
            destinationFilePath,
            sourceMetadata);

        if (decision == FileCopyDecision.AddUnique)
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
                decision = GetCopyDecision(destinationFilePath, sourceMetadata);
            }
        }

        if (decision == FileCopyDecision.Add)
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
        }
        else if (decision != FileCopyDecision.Skip)
        {
            _fileSystem.CopyFile(sourceFilePath, destinationFilePath, overwrite: true);
            SetLastWriteTimeIfPresent(destinationFilePath, sourceMetadata);
        }

        FileOperationResult result = new(
            ToStatus(decision),
            sourceFilePath,
            destinationFilePath,
            decision == FileCopyDecision.Skip
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

        foreach (string duplicatePath in _uniqueFileNameProvider.GetExistingDuplicates(destinationFilePath))
        {
            try
            {
                ChatMetadata metadata = _metadataReader.Read(duplicatePath);
                if (metadata.ConversationId == sourceId &&
                    (matchingPath is null ||
                     matchingUpdateTime is null ||
                     metadata.UpdateTime > matchingUpdateTime))
                {
                    matchingPath = duplicatePath;
                    matchingUpdateTime = metadata.UpdateTime;
                }
            }
            catch (FormatException exception)
            {
                _logger.Error($"Не удалось прочитать метаданные: {duplicatePath}", exception);
            }
            catch (IOException exception)
            {
                _logger.Error($"Не удалось прочитать файл: {duplicatePath}", exception);
            }
        }

        return matchingPath;
    }

    private FileCopyDecision GetCopyDecision(
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        if (!_fileSystem.FileExists(destinationFilePath))
        {
            return FileCopyDecision.Add;
        }

        try
        {
            ChatMetadata destinationMetadata = _metadataReader.Read(destinationFilePath);
            Guid? sourceId = sourceMetadata.ConversationId;
            Guid? destinationId = destinationMetadata.ConversationId;

            if (sourceId is null ||
                destinationId is null ||
                sourceId != destinationId)
            {
                return FileCopyDecision.AddUnique;
            }

            if (!destinationMetadata.UpdateTime.HasValue)
            {
                return FileCopyDecision.Replace;
            }

            if (!sourceMetadata.UpdateTime.HasValue)
            {
                return FileCopyDecision.Skip;
            }

            return sourceMetadata.UpdateTime.Value > destinationMetadata.UpdateTime.Value
                ? FileCopyDecision.Replace
                : FileCopyDecision.Skip;
        }
        catch (FormatException exception)
        {
            _logger.Error($"Не удалось прочитать метаданные: {destinationFilePath}", exception);
            return FileCopyDecision.AddUnique;
        }
        catch (IOException exception)
        {
            _logger.Error($"Не удалось прочитать файл: {destinationFilePath}", exception);
            return FileCopyDecision.AddUnique;
        }
    }

    private static FileOperationStatus ToStatus(FileCopyDecision decision) => decision switch
    {
        FileCopyDecision.Skip => FileOperationStatus.Skipped,
        FileCopyDecision.Add => FileOperationStatus.Added,
        FileCopyDecision.AddUnique => FileOperationStatus.AddedUnique,
        FileCopyDecision.Replace => FileOperationStatus.Updated,
        _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
    };
}
