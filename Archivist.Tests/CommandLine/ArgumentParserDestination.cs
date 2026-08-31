using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией назначения.
    /// </summary>
    public sealed class ArgumentParserDestination
    {
        /// <summary>Parses the parses destination directory with short flag.</summary>
        [Fact]
        public void Parse_ParsesDestinationDirectory_WithShortFlag()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }

        /// <summary>Parses the parses destination directory with long flag.</summary>
        [Fact]
        public void Parse_ParsesDestinationDirectory_WithLongFlag()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source", "C:\\source", "--destination", "C:\\dest" });

            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }
    }
}