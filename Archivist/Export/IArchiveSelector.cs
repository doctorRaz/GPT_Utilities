namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Выбирает ZIP-архивы, которые должны участвовать в обработке.
/// </summary>
internal interface IArchiveSelector
{
    IReadOnlyList<FileInfo> Select(ExportRequest request);
}
