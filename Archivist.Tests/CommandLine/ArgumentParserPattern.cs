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
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "*"
         });

            Assert.Equal("*.zip", result.ZipFilePattern);
        }

        /// <summary>Parses the appends zip extension when pattern has no extension.</summary>
        [Fact]
        public void Parse_AppendsZipExtension_WhenPatternHasNoExtension()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "chatgpt-export-markdown*"
         });

            Assert.Equal(
                "chatgpt-export-markdown*.zip",
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
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "chatgpt-export-markdown*.txt"
         });

            Assert.Equal(
                "chatgpt-export-markdown*.txt.zip",
                result.ZipFilePattern);
        }

        /// <summary>Parses the parses zip file pattern with short flag.</summary>
        [Fact]
        public void Parse_ParsesZipFilePattern_WithShortFlag()
        {
            CommandLineOptions result1 = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "chatgpt-export-markdown*"
         });

            CommandLineOptions result2 = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "chatgpt-export-markdown*.zip"
         });

            CommandLineOptions result3 = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "chatgpt-export-markdown*.txt"
         });

            Assert.Equal("chatgpt-export-markdown*.zip", result1.ZipFilePattern);
            Assert.Equal("chatgpt-export-markdown*.zip", result2.ZipFilePattern);
            Assert.Equal("chatgpt-export-markdown*.txt.zip", result3.ZipFilePattern);
        }

        /// <summary>Parses the parses zip file pattern with long flag.</summary>
        [Fact]
        public void Parse_ParsesZipFilePattern_WithLongFlag()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                    "--source", "C:\\source",
                    "--destination", "C:\\dest",
                    "--pattern", "my-export*"
                });

            Assert.Equal("my-export*.zip", result.ZipFilePattern);
        }

        /// <summary>Parses the is case insensitive for zip file pattern flag.</summary>
        [Fact]
        public void Parse_IsCaseInsensitive_ForZipFilePatternFlag()
        {
            var result1 = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                    "-s", "C:\\source",
                    "-d", "C:\\dest",
                    "-P", "*.ZIP"
                });

            var result2 = Archivist.CommandLine.CommandLineParser.Parse(new[]
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