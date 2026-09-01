using dRz.GPT_Utilities.Archivist.CommandLine;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией источника.
    /// </summary>
    public sealed class ArgumentParserSourceTests
    {
        /// <summary>Проверяет разбор исходного каталога через короткий параметр.</summary>
        [Test]
        public void Parse_ParsesSourceDirectory_WithShortFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.That(result.SourceDirectory, Is.EqualTo("C:\\source"));
            Assert.That(result.DestinationDirectory, Is.EqualTo("C:\\dest"));
        }

        /// <summary>Проверяет разбор исходного каталога через длинный параметр.</summary>
        [Test]
        public void Parse_ParsesSourceDirectory_WithLongFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source", "C:\\source", "--destination", "C:\\dest" });

            Assert.That(result.SourceDirectory, Is.EqualTo("C:\\source"));
            Assert.That(result.DestinationDirectory, Is.EqualTo("C:\\dest"));
        }
    }
}