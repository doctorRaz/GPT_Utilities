using dRz.GPT_Utilities.Archivist.Export;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Применяет политику добавления, обновления и пропуска файла.
/// </summary>
internal interface IFileSynchronizer
{
    FileCopyDecision Synchronize(
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
    public FileCopyDecision Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata) =>
        FileSynchronizer.CopyIfNewer(
            sourceFilePath,
            destinationFilePath,
            sourceMetadata);
}
