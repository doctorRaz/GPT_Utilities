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

    public FileSynchronizerService(
        IChatMetadataReader metadataReader,
        IArchivistLogger logger)
    {
        _metadataReader = metadataReader
            ?? throw new ArgumentNullException(nameof(metadataReader));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
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
                destinationFilePath = FileNameHelper.GetUnique(destinationFilePath);
            }
            else
            {
                destinationFilePath = matchingPath;
                decision = GetCopyDecision(destinationFilePath, sourceMetadata);
            }
        }

        if (decision != FileCopyDecision.Skip)
        {
            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);

            if (sourceMetadata.UpdateTime.HasValue)
            {
                File.SetLastWriteTime(
                    destinationFilePath,
                    sourceMetadata.UpdateTime.Value.LocalDateTime);
            }
        }

        return new FileOperationResult(
            ToStatus(decision),
            sourceFilePath,
            destinationFilePath,
            decision == FileCopyDecision.Skip
                ? "Файл не требует обновления."
                : null);
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

        foreach (string duplicatePath in FileNameHelper.GetExistingDuplicates(destinationFilePath))
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
        if (!File.Exists(destinationFilePath))
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
