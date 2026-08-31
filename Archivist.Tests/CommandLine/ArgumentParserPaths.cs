using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией назначения.
    /// </summary>
    public sealed class ArgumentParserPaths
    {
        /// <summary>Parses the source and dest can be same.</summary>
        [Fact]
        public void Parse_SourceAndDestCanBeSame()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\shared", "-d", "C:\\shared" });

            Assert.Equal("C:\\shared", result.SourceDirectory);
            Assert.Equal("C:\\shared", result.DestinationDirectory);
        }

        /// <summary>Parses the handles relative paths.</summary>
        [Fact]
        public void Parse_HandlesRelativePaths()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "..\\archives", "-d", ".\\output" });

            Assert.Equal("..\\archives", result.SourceDirectory);
            Assert.Equal(".\\output", result.DestinationDirectory);
        }

        /// <summary>Parses the handles unc paths.</summary>
        [Fact]
        public void Parse_HandlesUNCPaths()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "\\\\server\\share\\archives", "-d", "\\\\server\\share\\output" });

            Assert.Equal("\\\\server\\share\\archives", result.SourceDirectory);
            Assert.Equal("\\\\server\\share\\output", result.DestinationDirectory);
        }

        /// <summary>Parses the parses complex paths.</summary>
        [Fact]
        public void Parse_ParsesComplexPaths()
        {
            var source = "D:\\Users\\MyUser\\Documents\\GPT Export";
            var dest = "E:\\Archive\\2024\\Q1";

            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", source, "-d", dest });

            Assert.Equal(source, result.SourceDirectory);
            Assert.Equal(dest, result.DestinationDirectory);
        }

        /// <summary>Parses the parses quoted paths.</summary>
        [Fact]
        public void Parse_ParsesQuotedPaths()
        {
            var source = "\"C:\\Program Files\\GPT Export\"";
            var dest = "\"D:\\Archive Folder\"";

            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", source, "-d", dest });

            Assert.Equal(source, result.SourceDirectory);
            Assert.Equal(dest, result.DestinationDirectory);
        }

        /// <summary>Parses the handles paths with backslash at end.</summary>
        [Fact]
        public void Parse_HandlesPathsWithBackslashAtEnd()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source\\", "-d", "C:\\dest\\" });

            Assert.Equal("C:\\source\\", result.SourceDirectory);
            Assert.Equal("C:\\dest\\", result.DestinationDirectory);
        }
    }
}