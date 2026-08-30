using dRz.GPT_Utilities.Archivist.CommandLine;
using System;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для CommandLineParser.
    /// Проверяют парсинг аргументов командной строки приложения GPT_Archivist.
    /// </summary>
    public sealed class CommandLineParserTests
    {
        /// <summary>
        /// Тестирует возврат справки, когда аргументы не переданы.
        /// </summary>
        [Fact]
        public void Parse_ReturnsShowHelp_WhenNoArgumentsProvided()
        {
            var result = CommandLineParser.Parse(new string[] { });

            Assert.True(result.ShowHelp);
            Assert.False(result.ExtractAll);
            Assert.Empty(result.SourceDirectory);
            Assert.Empty(result.DestinationDirectory);
        }

        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagShort()
        {
            var result = CommandLineParser.Parse(new[] { "-h" });

            Assert.True(result.ShowHelp);
        }

        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagLong()
        {
            var result = CommandLineParser.Parse(new[] { "--help" });

            Assert.True(result.ShowHelp);
        }

        [Fact]
        public void Parse_ReturnsShowHelp_WhenHelpFlagWindows()
        {
            var result = CommandLineParser.Parse(new[] { "/?" });

            Assert.True(result.ShowHelp);
        }

        [Fact]
        public void Parse_IsCaseInsensitive_ForHelpFlag()
        {
            var result1 = CommandLineParser.Parse(new[] { "-H" });
            var result2 = CommandLineParser.Parse(new[] { "--HELP" });
            var result3 = CommandLineParser.Parse(new[] { "--Help" });

            Assert.True(result1.ShowHelp);
            Assert.True(result2.ShowHelp);
            Assert.True(result3.ShowHelp);
        }

        [Fact]
        public void Parse_ParsesSourceDirectory_WithShortFlag()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }

        [Fact]
        public void Parse_ParsesSourceDirectory_WithLongFlag()
        {
            var result = CommandLineParser.Parse(new[] { "--source", "C:\\source", "--destination", "C:\\dest" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }

        [Fact]
        public void Parse_ParsesDestinationDirectory_WithShortFlag()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }

        [Fact]
        public void Parse_ParsesDestinationDirectory_WithLongFlag()
        {
            var result = CommandLineParser.Parse(new[] { "--source", "C:\\source", "--destination", "C:\\dest" });

            Assert.Equal("C:\\dest", result.DestinationDirectory);
        }

        [Fact]
        public void Parse_ParsesExtractAll_WithShortFlag()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a" });

            Assert.True(result.ExtractAll);
        }

        [Fact]
        public void Parse_ParsesExtractAll_WithLongFlag()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "--all" });

            Assert.True(result.ExtractAll);
        }

        [Fact]
        public void Parse_ExtractAllDefaultIsFalse()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.False(result.ExtractAll);
        }

        [Fact]
        public void Parse_IsCaseInsensitive_ForAllFlags()
        {
            var result1 = CommandLineParser.Parse(new[] { "-S", "C:\\source", "-D", "C:\\dest", "-A" });
            var result2 = CommandLineParser.Parse(new[] { "--SOURCE", "C:\\source", "--DESTINATION", "C:\\dest", "--ALL" });

            Assert.Equal("C:\\source", result1.SourceDirectory);
            Assert.True(result1.ExtractAll);
            Assert.Equal("C:\\source", result2.SourceDirectory);
            Assert.True(result2.ExtractAll);
        }

        [Fact]
        public void Parse_ParsesComplexPaths()
        {
            var source = "D:\\Users\\MyUser\\Documents\\GPT Export";
            var dest = "E:\\Archive\\2024\\Q1";

            var result = CommandLineParser.Parse(new[] { "-s", source, "-d", dest });

            Assert.Equal(source, result.SourceDirectory);
            Assert.Equal(dest, result.DestinationDirectory);
        }

        [Fact]
        public void Parse_ParsesQuotedPaths()
        {
            var source = "\"C:\\Program Files\\GPT Export\"";
            var dest = "\"D:\\Archive Folder\"";

            var result = CommandLineParser.Parse(new[] { "-s", source, "-d", dest });

            Assert.Equal(source, result.SourceDirectory);
            Assert.Equal(dest, result.DestinationDirectory);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenSourceDirectoryMissing()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-d", "C:\\dest" }));

            Assert.Contains("ZIP", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-s", ex.Message);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenDestinationDirectoryMissing()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "C:\\source" }));

            Assert.Contains("назначения", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-d", ex.Message);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenSourceDirectoryEmpty()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "  ", "-d", "C:\\dest" }));

            Assert.Contains("ZIP", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenDestinationDirectoryEmpty()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "   " }));

            Assert.Contains("назначения", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenSourceValueMissing()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s" }));

            Assert.Contains("-s", ex.Message);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenDestinationValueMissing()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d" }));

            Assert.Contains("-d", ex.Message);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenUnknownFlagProvided()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-x" }));

            Assert.Contains("неизвестный", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-x", ex.Message);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenUnknownLongFlagProvided()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "--unknown" }));

            Assert.Contains("неизвестный", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenSourceValueIsAnotherFlag()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "-d", "C:\\dest" }));

            Assert.Contains("-s", ex.Message);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenDestinationValueIsAnotherFlag()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "-a" }));

            Assert.Contains("-d", ex.Message);
        }

        [Fact]
        public void Parse_AllowsAllFlagsInAnyOrder()
        {
            var result1 = CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a" });
            var result2 = CommandLineParser.Parse(new[] { "-a", "-s", "C:\\source", "-d", "C:\\dest" });
            var result3 = CommandLineParser.Parse(new[] { "-d", "C:\\dest", "-a", "-s", "C:\\source" });

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

        [Fact]
        public void Parse_HandlesMultipleExtractAllFlags()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-a", "-a", "-a" });

            Assert.True(result.ExtractAll);
        }

        [Fact]
        public void Parse_HelpFlagImmediatelyReturns()
        {
            // Help flag должен немедленно вернуть результат, остальные параметры игнорируются
            var result = CommandLineParser.Parse(new[] { "-h", "-s", "", "-d", "" });

            Assert.True(result.ShowHelp);
        }

        [Fact]
        public void Parse_SourceAndDestCanBeSame()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\shared", "-d", "C:\\shared" });

            Assert.Equal("C:\\shared", result.SourceDirectory);
            Assert.Equal("C:\\shared", result.DestinationDirectory);
        }

        [Fact]
        public void Parse_HandlesRelativePaths()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "..\\archives", "-d", ".\\output" });

            Assert.Equal("..\\archives", result.SourceDirectory);
            Assert.Equal(".\\output", result.DestinationDirectory);
        }

        [Fact]
        public void Parse_HandlesUNCPaths()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "\\\\server\\share\\archives", "-d", "\\\\server\\share\\output" });

            Assert.Equal("\\\\server\\share\\archives", result.SourceDirectory);
            Assert.Equal("\\\\server\\share\\output", result.DestinationDirectory);
        }

        [Fact]
        public void Parse_MixesShortAndLongFlags()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source", "--destination", "C:\\dest", "-a" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
            Assert.True(result.ExtractAll);
        }

        [Fact]
        public void Parse_MixesShortAndLongFlagsAlternative()
        {
            var result = CommandLineParser.Parse(new[] { "--source", "C:\\source", "-d", "C:\\dest", "--all" });

            Assert.Equal("C:\\source", result.SourceDirectory);
            Assert.Equal("C:\\dest", result.DestinationDirectory);
            Assert.True(result.ExtractAll);
        }

        [Fact]
        public void Parse_HandlesPathsWithBackslashAtEnd()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source\\", "-d", "C:\\dest\\" });

            Assert.Equal("C:\\source\\", result.SourceDirectory);
            Assert.Equal("C:\\dest\\", result.DestinationDirectory);
        }

        [Fact]
        public void Parse_ReturnsCorrectDefaultValues()
        {
            var result = CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest" });

            Assert.NotNull(result.SourceDirectory);
            Assert.NotNull(result.DestinationDirectory);
            Assert.False(result.ShowHelp);
            Assert.False(result.ExtractAll);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenLongSourceFlagHasNoValue()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "--source" }));

            Assert.Contains("--source", ex.Message);
        }

        [Fact]
        public void Parse_ThrowsArgumentException_WhenLongDestinationFlagHasNoValue()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => CommandLineParser.Parse(new[] { "-s", "C:\\source", "--destination" }));

            Assert.Contains("--destination", ex.Message);
        }
    }
}
