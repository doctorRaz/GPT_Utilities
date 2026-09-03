using dRz.GPT_Utilities.Archivist.Export;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Результат обработки одного файла.
/// </summary>
/// <param name="Status">Статус операции.</param>
/// <param name="SourcePath">Путь к исходному файлу.</param>
/// <param name="DestinationPath">Фактический путь назначения.</param>
/// <param name="Reason">Дополнительное пояснение результата.</param>
/// <param name="Error">Ошибка обработки, если она произошла.</param>
internal sealed record FileOperationResult(
    FileOperationStatus Status,
    string SourcePath,
    string? DestinationPath = null,
    string? Reason = null,
    Exception? Error = null,
    int IndexReadErrors = 0,
    IReadOnlyList<ExportError>? Errors = null);

/// <summary>
/// Возможные результаты обработки файла.
/// </summary>
internal enum FileOperationStatus
{
    Skipped,
    Added,
    Updated,
    Failed
}
