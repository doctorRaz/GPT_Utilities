using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using System.Text;

namespace dRz.GPT_Utilities.Archivist
{
    /*
     * Parser отвечает за то, что пользователь ввёл.
     * Main / application layer отвечает за то, можно ли с этими параметрами реально работать.
     * Processor занимается самой обработкой архивов.
    */

    internal static class Program
    {
        //[STAThread]
        private static int Main(string[] args)
        {
            ConfigureApplication();

#if DEBUG
            //LaunchDebugger();
#endif

            try
            {
                // Program является composition root приложения: здесь
                // собираются конкретные инфраструктурные реализации.
                IArchivistLogger logger = new ConsoleArchivistLogger();
                IFileSystem fileSystem = new LocalFileSystem();
                IUniqueFileNameProvider uniqueFileNameProvider =
                    new UniqueFileNameProvider(fileSystem);
                IExportPathBuilder pathBuilder = new ExportPathBuilder(fileSystem);
                IChatMetadataReader metadataReader = new ChatMetadataReader(fileSystem);
                IFileSynchronizer fileSynchronizer = new FileSynchronizerService(
                    metadataReader,
                    logger,
                    uniqueFileNameProvider,
                    fileSystem,
                    conversationIndexWriter: new ConversationTitleIndexWriter(fileSystem, metadataReader));
                IMarkdownFileProcessor markdownProcessor = new MarkdownFileProcessor(
                    pathBuilder,
                    metadataReader,
                    fileSynchronizer,
                    logger,
                    new FileNameNormalizer());

                IChatGptExportProcessor processor = new ChatGptExportProcessor(
                    new FileSystemArchiveSelector(fileSystem),
                    new ZipArchiveExtractor(Encoding.GetEncoding(866), fileSystem),
                    markdownProcessor,
                    logger);

                ArchivistApplication application =
                    new ArchivistApplication(
                        new CommandLineOptionsValidator(),
                        processor);
                return application.Run(args);
            }
            catch (Exception ex)
            {
                ConsoleWriter.Fatal(ex, $"Ошибка: ");

                ConsoleWriter.PressAnyKey();

                return 1;
            }
        }

        private static void ConfigureApplication()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ConsoleSetup.Configure(AppDomain.CurrentDomain.FriendlyName);
        }

#if DEBUG

        private static void LaunchDebugger()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                _ = System.Diagnostics.Debugger.Launch();
            }
        }

#endif
    }
}