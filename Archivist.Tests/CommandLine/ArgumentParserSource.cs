using dRz.GPT_Utilities.Archivist.CommandLine;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией источника.
    /// </summary>
    public sealed class ArgumentParserSource
    {
        /// <summary>Проверяет разбор исходного каталога через короткий параметр.</summary>
        [Fact]
        public void Parse_ParsesSourceDirectory_WithShortFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }

        /// <summary>Проверяет разбор исходного каталога через длинный параметр.</summary>
        [Fact]
        public void Parse_ParsesSourceDirectory_WithLongFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source", "C:\\source", "--destination", "C:\\dest" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }
    }
}