using dRz.GPT_Utilities.Archivist.CommandLine;
using System;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    public class ArgumentParserPattern
    {
        //todo добавить тесты на zip патерн /\

        /// <summary>Parses the does not append zip extension when pattern ends with zip case insensitive.</summary>
        [Fact]
        public void Parse_DoesNotAppendZipExtension_WhenPatternEndsWithZipCaseInsensitive()
        {
            CommandLineOptions result1 = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "*.ZIP"
         });
            CommandLineOptions result2 = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "*.zip"
         });

            Assert.Equal("*.ZIP", result1.ZipFilePattern);
            Assert.Equal("*.zip", result2.ZipFilePattern);
        }

        /// <summary>Parses the appends zip extension when pattern is asterisk.</summary>
        [Fact]
        public void Parse_AppendsZipExtension_WhenPatternIsAsterisk()
        {
            string pattern = "*";

            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", pattern
         });

            Assert.Equal(pattern, result.ZipFilePattern);
        }

        /// <summary>Parses the appends zip extension when pattern has no extension.</summary>
        [Fact]
        public void Parse_AppendsZipExtension_WhenPatternHasNoExtension()
        {
            string pattern = "chatgpt-export-markdown*";

            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", pattern
         });

            Assert.Equal(
                pattern,
                result.ZipFilePattern);
        }

        /// <summary>Parses the does not append zip extension when pattern already ends with zip.</summary>
        [Fact]
        public void Parse_DoesNotAppendZipExtension_WhenPatternAlreadyEndsWithZip()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "chatgpt-export-markdown*.zip"
         });

            Assert.Equal(
                "chatgpt-export-markdown*.zip",
                result.ZipFilePattern);
        }

        /// <summary>Parses the appends zip extension when pattern has other extension.</summary>
        [Fact]
        public void Parse_AppendsZipExtension_WhenPatternHasOtherExtension()
        {
            string pattern = "chatgpt-export-markdown*.txt";
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", pattern
         });

            Assert.Equal(
                pattern,
                result.ZipFilePattern);
        }

        /// <summary>Parses the parses zip file pattern with short flag.</summary>
        [Theory]
        [InlineData("chatgpt-export-markdown*.zip", "chatgpt-export-markdown*.zip")]
        [InlineData("chatgpt-export-markdown*", "chatgpt-export-markdown*")]
        [InlineData("my-export*.txt", "my-export*.txt")]
        public void Parse_ParsesZipFilePattern_WithShortFlag(string pattern, string expected)
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", pattern
         });

            Assert.Equal(expected, result.ZipFilePattern);
        }

        /// <summary>Parses the parses zip file pattern with long flag.</summary>
        [Theory]
        [InlineData("chatgpt-export-markdown*.zip", "chatgpt-export-markdown*.zip")]
        [InlineData("chatgpt-export-markdown*", "chatgpt-export-markdown*")]
        [InlineData("my-export*.txt", "my-export*.txt")]
        public void Parse_ParsesZipFilePattern_WithLongFlag(string pattern, string expected)
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                    "--source", "C:\\source",
                    "--destination", "C:\\dest",
                    "--pattern", pattern
                });

            Assert.Equal(expected, result.ZipFilePattern);
        }

        /// <summary>Parses the throws argument exception zip file pattern null.</summary>

        [Theory]
        [InlineData("-p")]
        [InlineData("--pattern")]
        [InlineData("-P")]
        [InlineData("--PATTERN")]
        public void Parse_ThrowsArgumentException_ZipFilePattern_Null(string option)
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                 "--source", "C:\\source",
                 "--destination", "C:\\dest",
                 option
             }));

            Assert.Contains($"Для параметра {option} не указано значение.", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Parses the is case insensitive for zip file pattern flag.</summary>
        [Fact]
        public void Parse_IsCaseInsensitive_ForZipFilePatternFlag()
        {
            CommandLineOptions result1 = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                    "-s", "C:\\source",
                    "-d", "C:\\dest",
                    "-P", "*.ZIP"
                });

            CommandLineOptions result2 = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                    "--source", "C:\\source",
                    "--destination", "C:\\dest",
                    "--PATTERN", "*.ZIP"
                });

            Assert.Equal("*.ZIP", result1.ZipFilePattern);
            Assert.Equal("*.ZIP", result2.ZipFilePattern);
        }

        /// <summary>Parses the accepts wildcard characters in zip file pattern.</summary>
        [Fact]
        public void Parse_AcceptsWildcardCharacters_InZipFilePattern()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                  "-s", @"c:\temp",  "-p", "test*?.zip","-d", @"c:\dest"
                });

            Assert.Equal("test*?.zip", result.ZipFilePattern);
        }
    }
}