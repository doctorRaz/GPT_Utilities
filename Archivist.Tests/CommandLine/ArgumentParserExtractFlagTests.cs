using dRz.GPT_Utilities.Archivist.CommandLine;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    public class ArgumentParserExtractFlagTests
    {
        /// <summary>Проверяет включение обработки всех архивов коротким параметром.</summary>
        [Test]
        public void Parse_ParsesExtractAll_WithShortFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a" });

            Assert.That(result.ExtractAll, Is.True);
        }

        /// <summary>Проверяет включение обработки всех архивов длинным параметром.</summary>
        [Test]
        public void Parse_ParsesExtractAll_WithLongFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "--all" });

            Assert.That(result.ExtractAll, Is.True);
        }

        /// <summary>Проверяет, что обработка всех архивов по умолчанию отключена.</summary>
        [Test]
        public void Parse_ExtractAllDefaultIsFalse()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.That(result.ExtractAll, Is.False);
        }

        /// <summary>Проверяет корректную обработку повторного параметра обработки всех архивов.</summary>
        [Test]
        public void Parse_HandlesMultipleExtractAllFlags()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a", "-a", "-a" });

            Assert.That(result.ExtractAll, Is.True);
        }
    }
}