using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Export;

namespace dRz.GPT_Utilities.Archivist.Tests.Infrastructure
{
    internal class CommandLineOptionsFactory
    {
        internal static ExportRequest CreateOptions(string? sourceDirectory = null,
                                                        string? destinationDirectory = null,
                                                        string? zipFilePattern = "*",
                                                        bool extractAll = false,
                                                        bool showHelp = false)
        {
            return new ExportRequest(
                sourceDirectory!,
                destinationDirectory!,
                zipFilePattern!,
                extractAll);
        }
    }
}
