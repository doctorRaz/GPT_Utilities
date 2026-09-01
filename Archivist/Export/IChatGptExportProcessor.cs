
namespace dRz.GPT_Utilities.Archivist.Export;

internal interface IChatGptExportProcessor
{
    /// <summary>
    /// Запускает use case обработки экспорта.
    /// </summary>
    ExportResult Process(ExportRequest request);
}