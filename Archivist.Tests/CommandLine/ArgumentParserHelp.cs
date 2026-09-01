using dRz.GPT_Utilities.Archivist.CommandLine;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    public class ArgumentParserHelp
    {
        /// <summary>
        /// Тестирует возврат справки, когда аргументы не переданы.
        /// </summary>
        [Fact]
        public void Parse_ReturnsShowHelp_WhenNoArgumentsProvided()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new string[] { });

            Assert.True(result.ShowHelp);
            Assert.False(result.ExtractAll);
            Assert.Empty(result.SourceDirectory);
            Assert.Empty(result.DestinationDirectory);
        }

        /// <summary>Проверяет показ справки по короткому параметру.</summary>
        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagShort()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-h" });

            Assert.True(result.ShowHelp);
        }

        /// <summary>Проверяет показ справки по длинному параметру.</summary>
        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagLong()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--help" });

            Assert.True(result.ShowHelp);
        }

        /// <summary>Проверяет показ справки по стандартному параметру Windows /?.</summary>
        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagWindows()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "/?" });

            Assert.True(result.ShowHelp);
        }

        /// <summary>Проверяет регистронезависимый разбор параметра справки.</summary>
        [Fact]
        public void Parse_IsCaseInsensitive_ForHelpFlag()
        {
            CommandLineOptions result1 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-H" });
            CommandLineOptions result2 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--HELP" });
            CommandLineOptions result3 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--Help" });

            Assert.True(result1.ShowHelp);
            Assert.True(result2.ShowHelp);
            Assert.True(result3.ShowHelp);
        }

        /// <summary>Проверяет немедленный возврат результата при обнаружении параметра справки.</summary>
        [Fact]
        public void Parse_HelpFlagImmediatelyReturns()
        {
            // Help flag должен немедленно вернуть результат, остальные параметры игнорируются
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-h", "-s", "", "-d", "" });

            Assert.True(result.ShowHelp);
        }
    }
}