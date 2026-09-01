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
/// Адаптер над текущей реализацией синхронизации файлов.
/// </summary>
/// <remarks>
/// Адаптер позволяет постепенно перевести существующий статический код
/// на внедрение зависимостей без изменения его проверенной логики.
/// </remarks>
internal sealed class FileSynchronizerAdapter : IFileSynchronizer
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
