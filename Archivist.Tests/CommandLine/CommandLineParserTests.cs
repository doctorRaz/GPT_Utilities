using System;
using dRz.GPT_Utilities.Archivist.CommandLine;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для CommandLineParserTests.
    /// Проверяют парсинг аргументов командной строки приложения GPT_Archivist.
    /// </summary>
    public sealed class CommandLineParserTests
    {
        /// <summary>Проверяет регистронезависимый разбор всех параметров.</summary>
        [Theory]
        [InlineData("-S", "C:\\source", "-D", "C:\\dest", "-A", "-P", "test*test")]
        [InlineData("--SOURCE", "C:\\source", "--DESTINATION", "C:\\dest", "--ALL", "--PATTERN", "test*test")]
        public void Parse_IsCaseInsensitive_ForAllFlags(string sourceFlag, string sourceValue, string destinationFlag, string destinationValue, string allFlag, string patternFlag, string patternValue)
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { sourceFlag, sourceValue, destinationFlag, destinationValue, allFlag, patternFlag, patternValue });

            Assert.Equal(sourceValue, result.SourceDirectory);
            Assert.Equal(destinationValue, result.DestinationDirectory);
            Assert.Equal(patternValue, result.ZipFilePattern);
            Assert.True(result.ExtractAll);
        }

        /// <summary>Проверяет обработку параметров в любом порядке.</summary>
        [Fact]
        public void Parse_AllowsAllFlagsInAnyOrder()
        {
            CommandLineOptions result1 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a" });
            CommandLineOptions result2 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-a", "-s", "C:\\source", "-d", "C:\\dest" });
            CommandLineOptions result3 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-d", "C:\\dest", "-a", "-s", "C:\\source" });

            Assert.Equal("C:\\source", result1.SourceDirectory);
            Assert.Equal("C:\\dest", result1.DestinationDirectory);
            Assert.True(result1.ExtractAll);

            Assert.Equal("C:\\source", result2.SourceDirectory);
            Assert.Equal("C:\\dest", result2.DestinationDirectory);
            Assert.True(result2.ExtractAll);

            Assert.Equal("C:\\source", result3.SourceDirectory);
            Assert.Equal("C:\\dest", result3.DestinationDirectory);
            Assert.True(result3.ExtractAll);
        }

        /// <summary>Проверяет совместное использование коротких и длинных параметров.</summary>
        [Fact]
        public void Parse_MixesShortAndLongFlags()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "--destination", "C:\\dest", "-a" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
            Assert.True(result.ExtractAll);
        }

        /// <summary>Проверяет альтернативную комбинацию коротких и длинных параметров.</summary>
        [Fact]
        public void Parse_MixesShortAndLongFlagsAlternative()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source", "C:\\source", "-d", "C:\\dest", "--all" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
            Assert.True(result.ExtractAll);
        }

        /// <summary>Проверяет значения параметров по умолчанию.</summary>
        [Fact]
        public void Parse_ReturnsCorrectDefaultValues()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.NotNull(result.SourceDirectory);
            Assert.NotNull(result.DestinationDirectory);
            Assert.False(result.ShowHelp);
            Assert.False(result.ExtractAll);
        }

        /// <summary>
        /// Проверяет выброс ArgumentNullException при передаче null
        /// вместо массива аргументов.
        /// </summary>
        [Fact]
        public void Parse_ThrowsArgumentNullException_WhenArgumentsAreNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(null!));
        }
    }
}