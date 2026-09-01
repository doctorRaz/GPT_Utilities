using System;
using dRz.GPT_Utilities.Archivist.CommandLine;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для CommandLineParserTests.
    /// Проверяют парсинг аргументов командной строки приложения GPT_Archivist.
    /// </summary>
    public sealed class CommandLineParserTests
    {
        /// <summary>Проверяет регистронезависимый разбор всех параметров.</summary>
        [Test]
        [TestCase("-S", "C:\\source", "-D", "C:\\dest", "-A", "-P", "test*test")]
        [TestCase("--SOURCE", "C:\\source", "--DESTINATION", "C:\\dest", "--ALL", "--PATTERN", "test*test")]
        public void Parse_IsCaseInsensitive_ForAllFlags(string sourceFlag, string sourceValue, string destinationFlag, string destinationValue, string allFlag, string patternFlag, string patternValue)
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { sourceFlag, sourceValue, destinationFlag, destinationValue, allFlag, patternFlag, patternValue });

            Assert.That(result.SourceDirectory, Is.EqualTo(sourceValue));
            Assert.That(result.DestinationDirectory, Is.EqualTo(destinationValue));
            Assert.That(result.ZipFilePattern, Is.EqualTo(patternValue));
            Assert.That(result.ExtractAll, Is.True);
        }

        /// <summary>Проверяет обработку параметров в любом порядке.</summary>
        [Test]
        public void Parse_AllowsAllFlagsInAnyOrder()
        {
            CommandLineOptions result1 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a" });
            CommandLineOptions result2 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-a", "-s", "C:\\source", "-d", "C:\\dest" });
            CommandLineOptions result3 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-d", "C:\\dest", "-a", "-s", "C:\\source" });

            Assert.That(result1.SourceDirectory, Is.EqualTo("C:\\source"));
            Assert.That(result1.DestinationDirectory, Is.EqualTo("C:\\dest"));
            Assert.That(result1.ExtractAll, Is.True);

            Assert.That(result2.SourceDirectory, Is.EqualTo("C:\\source"));
            Assert.That(result2.DestinationDirectory, Is.EqualTo("C:\\dest"));
            Assert.That(result2.ExtractAll, Is.True);

            Assert.That(result3.SourceDirectory, Is.EqualTo("C:\\source"));
            Assert.That(result3.DestinationDirectory, Is.EqualTo("C:\\dest"));
            Assert.That(result3.ExtractAll, Is.True);
        }

        /// <summary>Проверяет совместное использование коротких и длинных параметров.</summary>
        [Test]
        public void Parse_MixesShortAndLongFlags()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "--destination", "C:\\dest", "-a" });

            Assert.That(result.SourceDirectory, Is.EqualTo("C:\\source"));
            Assert.That(result.DestinationDirectory, Is.EqualTo("C:\\dest"));
            Assert.That(result.ExtractAll, Is.True);
        }

        /// <summary>Проверяет альтернативную комбинацию коротких и длинных параметров.</summary>
        [Test]
        public void Parse_MixesShortAndLongFlagsAlternative()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source", "C:\\source", "-d", "C:\\dest", "--all" });

            Assert.That(result.SourceDirectory, Is.EqualTo("C:\\source"));
            Assert.That(result.DestinationDirectory, Is.EqualTo("C:\\dest"));
            Assert.That(result.ExtractAll, Is.True);
        }

        /// <summary>Проверяет значения параметров по умолчанию.</summary>
        [Test]
        public void Parse_ReturnsCorrectDefaultValues()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.That(result.SourceDirectory, Is.Not.Null);
            Assert.That(result.DestinationDirectory, Is.Not.Null);
            Assert.That(result.ShowHelp, Is.False);
            Assert.That(result.ExtractAll, Is.False);
        }

        /// <summary>
        /// Проверяет выброс ArgumentNullException при передаче null
        /// вместо массива аргументов.
        /// </summary>
        [Test]
        public void Parse_ThrowsArgumentNullException_WhenArgumentsAreNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(null!));
        }
    }
}