using dRz.GPT_Utilities.Archivist.CommandLine;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией назначения.
    /// </summary>
    public sealed class ArgumentParserPaths
    {
        /// <summary>Проверяет возможность указать один каталог источником и назначением.</summary>
        [Fact]
        public void Parse_SourceAndDestCanBeSame()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\shared", "-d", "C:\\shared" });

            Assert.Equal("C:\\shared", result.SourceDirectory);
            Assert.Equal("C:\\shared", result.DestinationDirectory);
        }

        /// <summary>Проверяет обработку относительных путей.</summary>
        [Fact]
        public void Parse_HandlesRelativePaths()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "..\\archives", "-d", ".\\output" });

            Assert.Equal("..\\archives", result.SourceDirectory);
            Assert.Equal(".\\output", result.DestinationDirectory);
        }

        /// <summary>Проверяет обработку UNC-путей.</summary>
        [Fact]
        public void Parse_HandlesUNCPaths()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "\\\\server\\share\\archives", "-d", "\\\\server\\share\\output" });

            Assert.Equal("\\\\server\\share\\archives", result.SourceDirectory);
            Assert.Equal("\\\\server\\share\\output", result.DestinationDirectory);
        }

        /// <summary>Проверяет обработку сложных путей с пробелами и несколькими каталогами.</summary>
        [Fact]
        public void Parse_ParsesComplexPaths()
        {
            string source = "D:\\Users\\MyUser\\Documents\\GPT Export";
            string dest = "E:\\Archive\\2024\\Q1";

            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", source, "-d", dest });

            Assert.Equal(source, result.SourceDirectory);
            Assert.Equal(dest, result.DestinationDirectory);
        }

        /// <summary>Проверяет сохранение путей, переданных в кавычках.</summary>
        [Fact]
        public void Parse_ParsesQuotedPaths()
        {
            string source = "\"C:\\Program Files\\GPT Export\"";
            string dest = "\"D:\\Archive Folder\"";

            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", source, "-d", dest });

            Assert.Equal(source, result.SourceDirectory);
            Assert.Equal(dest, result.DestinationDirectory);
        }

        /// <summary>Проверяет обработку путей с завершающим обратным слешем.</summary>
        [Fact]
        public void Parse_HandlesPathsWithBackslashAtEnd()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source\\", "-d", "C:\\dest\\" });

            Assert.Equal("C:\\source\\", result.SourceDirectory);
            Assert.Equal("C:\\dest\\", result.DestinationDirectory);
        }
    }
}