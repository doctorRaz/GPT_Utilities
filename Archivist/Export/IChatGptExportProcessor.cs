
using dRz.GPT_Utilities.Archivist.CommandLine;

namespace dRz.GPT_Utilities.Archivist.Export;

internal interface IChatGptExportProcessor
{
    ChatGptExportProcessor.CopyStatistics Process(
        CommandLineOptions options);
}