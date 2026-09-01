using dRz.GPT_Utilities.Archivist.CommandLine;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией назначения.
    /// </summary>
    public sealed class ArgumentParserDestination
    {
        /// <summary>Проверяет разбор каталога назначения через короткий параметр.</summary>
        [Fact]
        public void Parse_ParsesDestinationDirectory_WithShortFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "  -S  ", "C:\\source", "-d", "C:\\dest" });

            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }

        /// <summary>Проверяет разбор каталога назначения через длинный параметр.</summary>
        [Fact]
        public void Parse_ParsesDestinationDirectory_WithLongFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source", "C:\\source", "--destination", "C:\\dest" });

            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }
    }
}