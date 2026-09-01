using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Export;
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
                ArchivistApplication application =
                                                new ArchivistApplication(
                                                new CommandLineOptionsValidator(),
                                                new ChatGptExportProcessor());
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