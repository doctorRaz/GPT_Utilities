using dRz.GPT_Utilities.Archivist.Export;

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
/// Сервис синхронизации, использующий текущую файловую политику.
/// </summary>
/// <remarks>
/// Сервис изолирует процессор от статической точки входа файловой политики.
/// </remarks>
internal sealed class FileSynchronizerService : IFileSynchronizer
{
    public FileOperationResult Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata) =>
        CreateResult(FileSynchronizer.CopyIfNewer(
            sourceFilePath,
            destinationFilePath,
            sourceMetadata),
            sourceFilePath,
            destinationFilePath);

    private static FileOperationResult CreateResult(
        FileCopyDecision decision,
        string sourcePath,
        string destinationPath) =>
        new(
            decision switch
            {
                FileCopyDecision.Skip => FileOperationStatus.Skipped,
                FileCopyDecision.Add => FileOperationStatus.Added,
                FileCopyDecision.AddUnique => FileOperationStatus.AddedUnique,
                FileCopyDecision.Replace => FileOperationStatus.Updated,
                _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
            },
            sourcePath,
            destinationPath,
            decision == FileCopyDecision.Skip ? "Файл не требует обновления." : null);
}
