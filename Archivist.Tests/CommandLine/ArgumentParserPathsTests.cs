using dRz.GPT_Utilities.Archivist.CommandLine;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией назначения.
    /// </summary>
    public sealed class ArgumentParserPathsTests
    {
        /// <summary>Проверяет возможность указать один каталог источником и назначением.</summary>
        [Test]
        public void Parse_SourceAndDestCanBeSame()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\shared", "-d", "C:\\shared" });

            Assert.That(result.SourceDirectory, Is.EqualTo("C:\\shared"));
            Assert.That(result.DestinationDirectory, Is.EqualTo("C:\\shared"));
        }

        /// <summary>Проверяет обработку относительных путей.</summary>
        [Test]
        public void Parse_HandlesRelativePaths()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "..\\archives", "-d", ".\\output" });

            Assert.That(result.SourceDirectory, Is.EqualTo("..\\archives"));
            Assert.That(result.DestinationDirectory, Is.EqualTo(".\\output"));
        }

        /// <summary>Проверяет обработку UNC-путей.</summary>
        [Test]
        public void Parse_HandlesUNCPaths()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "\\\\server\\share\\archives", "-d", "\\\\server\\share\\output" });

            Assert.That(result.SourceDirectory, Is.EqualTo("\\\\server\\share\\archives"));
            Assert.That(result.DestinationDirectory, Is.EqualTo("\\\\server\\share\\output"));
        }

        /// <summary>Проверяет обработку сложных путей с пробелами и несколькими каталогами.</summary>
        [Test]
        public void Parse_ParsesComplexPaths()
        {
            string source = "D:\\Users\\MyUser\\Documents\\GPT Export";
            string dest = "E:\\Archive\\2024\\Q1";

            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", source, "-d", dest });

            Assert.That(result.SourceDirectory, Is.EqualTo(source));
            Assert.That(result.DestinationDirectory, Is.EqualTo(dest));
        }

        /// <summary>Проверяет сохранение путей, переданных в кавычках.</summary>
        [Test]
        public void Parse_ParsesQuotedPaths()
        {
            string source = "\"C:\\Program Files\\GPT Export\"";
            string dest = "\"D:\\Archive Folder\"";

            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", source, "-d", dest });

            Assert.That(result.SourceDirectory, Is.EqualTo(source));
            Assert.That(result.DestinationDirectory, Is.EqualTo(dest));
        }

        /// <summary>Проверяет обработку путей с завершающим обратным слешем.</summary>
        [Test]
        public void Parse_HandlesPathsWithBackslashAtEnd()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source\\", "-d", "C:\\dest\\" });

            Assert.That(result.SourceDirectory, Is.EqualTo("C:\\source\\"));
            Assert.That(result.DestinationDirectory, Is.EqualTo("C:\\dest\\"));
        }
    }
}