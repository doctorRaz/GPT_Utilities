
using dRz.GPT_Utilities.Archivist.CommandLine;

namespace dRz.GPT_Utilities.Archivist.Export;

internal interface IChatGptExportProcessor
{
    /// <summary>
    /// Запускает use case обработки экспорта.
    /// </summary>
    ExportResult Process(ExportRequest request);

    /// <summary>
    /// Временная совместимая перегрузка для существующих клиентов.
    /// Новый код должен использовать <see cref="ExportRequest"/>.
    /// </summary>
    ExportResult Process(CommandLineOptions options);
}