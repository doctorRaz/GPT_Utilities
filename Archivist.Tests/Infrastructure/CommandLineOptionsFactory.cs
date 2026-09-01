using dRz.GPT_Utilities.Archivist.CommandLine;

namespace dRz.GPT_Utilities.Archivist.Tests.Infrastructure
{
    internal class CommandLineOptionsFactory
    {
        internal static CommandLineOptions CreateOptions(string? sourceDirectory = null,
                                                        string? destinationDirectory = null,
                                                        string? zipFilePattern = "*",
                                                        bool extractAll = false,
                                                        bool showHelp = false)
        {
            return new CommandLineOptions
            {
                SourceDirectory = sourceDirectory,

                DestinationDirectory = destinationDirectory,

                ZipFilePattern = zipFilePattern,

                ExtractAll = extractAll,

                ShowHelp = showHelp
            };
        }
    }
}
