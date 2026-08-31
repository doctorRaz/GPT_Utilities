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
            var result = Archivist.CommandLine.CommandLineParser.Parse(new string[] { });

            Assert.True(result.ShowHelp);
            Assert.False(result.ExtractAll);
            Assert.Empty(result.SourceDirectory);
            Assert.Empty(result.DestinationDirectory);
        }

        /// <summary>Parses the returns show help when help flag short.</summary>
        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagShort()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-h" });

            Assert.True(result.ShowHelp);
        }

        /// <summary>Parses the returns show help when help flag long.</summary>
        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagLong()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--help" });

            Assert.True(result.ShowHelp);
        }

        /// <summary>Parses the returns show help when help flag windows.</summary>
        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagWindows()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "/?" });

            Assert.True(result.ShowHelp);
        }

        /// <summary>Parses the is case insensitive for help flag.</summary>
        [Fact]
        public void Parse_IsCaseInsensitive_ForHelpFlag()
        {
            var result1 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-H" });
            var result2 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--HELP" });
            var result3 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--Help" });

            Assert.True(result1.ShowHelp);
            Assert.True(result2.ShowHelp);
            Assert.True(result3.ShowHelp);
        }

        /// <summary>Parses the help flag immediately returns.</summary>
        [Fact]
        public void Parse_HelpFlagImmediatelyReturns()
        {
            // Help flag должен немедленно вернуть результат, остальные параметры игнорируются
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-h", "-s", "", "-d", "" });

            Assert.True(result.ShowHelp);
        }
    }
}