using dRz.GPT_Utilities.Archivist.CommandLine;
using System;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для CommandLineParser.
    /// Проверяют парсинг аргументов командной строки приложения GPT_Archivist.
    /// </summary>
    public sealed class CommandLineParser
    {
        /// <summary>Parses the is case insensitive for all flags.</summary>
        [Fact]
        public void Parse_IsCaseInsensitive_ForAllFlags()
        {
            var result1 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-S", "C:\\source", "-D", "C:\\dest", "-A", "-P", "test*test" });
            var result2 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--SOURCE", "C:\\source", "--DESTINATION", "C:\\dest", "--ALL", "--PATTERN", "test*test" });

            Assert.Equal("C:\\source", result1.SourceDirectory);
            Assert.True(result1.ExtractAll);
            Assert.Equal("test*test.zip", result1.ZipFilePattern);

            Assert.Equal("C:\\source", result2.SourceDirectory);
            Assert.True(result2.ExtractAll);
            Assert.Equal("test*test.zip", result2.ZipFilePattern);
        }


        /// <summary>Parses the allows all flags in any order.</summary>
        [Fact]
        public void Parse_AllowsAllFlagsInAnyOrder()
        {
            var result1 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a" });
            var result2 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-a", "-s", "C:\\source", "-d", "C:\\dest" });
            var result3 = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-d", "C:\\dest", "-a", "-s", "C:\\source" });

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

        /// <summary>Parses the mixes short and long flags.</summary>
        [Fact]
        public void Parse_MixesShortAndLongFlags()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "--destination", "C:\\dest", "-a" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
            Assert.True(result.ExtractAll);
        }

        /// <summary>Parses the mixes short and long flags alternative.</summary>
        [Fact]
        public void Parse_MixesShortAndLongFlagsAlternative()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source", "C:\\source", "-d", "C:\\dest", "--all" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
            Assert.True(result.ExtractAll);
        }

        /// <summary>Parses the returns correct default values.</summary>
        [Fact]
        public void Parse_ReturnsCorrectDefaultValues()
        {
            var result = Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.NotNull(result.SourceDirectory);
            Assert.NotNull(result.DestinationDirectory);
            Assert.False(result.ShowHelp);
            Assert.False(result.ExtractAll);
        }
    }
}