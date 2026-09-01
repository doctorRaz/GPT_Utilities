using dRz.GPT_Utilities.Archivist.CommandLine;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    public class ArgumentParserExtractFlagTests
    {
        /// <summary>Проверяет включение обработки всех архивов коротким параметром.</summary>
        [Fact]
        public void Parse_ParsesExtractAll_WithShortFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a" });

            Assert.True(result.ExtractAll);
        }

        /// <summary>Проверяет включение обработки всех архивов длинным параметром.</summary>
        [Fact]
        public void Parse_ParsesExtractAll_WithLongFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "--all" });

            Assert.True(result.ExtractAll);
        }

        /// <summary>Проверяет, что обработка всех архивов по умолчанию отключена.</summary>
        [Fact]
        public void Parse_ExtractAllDefaultIsFalse()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.False(result.ExtractAll);
        }

        /// <summary>Проверяет корректную обработку повторного параметра обработки всех архивов.</summary>
        [Fact]
        public void Parse_HandlesMultipleExtractAllFlags()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a", "-a", "-a" });

            Assert.True(result.ExtractAll);
        }
    }
}