namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Структурированная ошибка обработки экспорта.
/// </summary>
internal sealed record ExportError(
    string Path,
    string ExceptionType,
    string Message,
    string Stage);
