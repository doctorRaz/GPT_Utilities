using dRz.GPT_Utilities.Archivist.CommandLine;
using System;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    public class ArgumentParserPattern
    {
        /// <summary>Проверяет сохранение расширения .zip без изменения регистра.</summary>
        [Fact]
        public void Parse_PreservesZipExtensionCase()
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

        /// <summary>Проверяет сохранение маски из одного символа-заполнителя.</summary>
        [Fact]
        public void Parse_PreservesAsteriskPattern()
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

        /// <summary>Проверяет сохранение маски без расширения.</summary>
        [Fact]
        public void Parse_PreservesPatternWithoutExtension()
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

        /// <summary>Проверяет сохранение маски, уже содержащей расширение .zip.</summary>
        [Fact]
        public void Parse_PreservesExistingZipExtension()
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

        /// <summary>Проверяет сохранение маски с другим расширением.</summary>
        [Fact]
        public void Parse_PreservesOtherExtension()
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

        /// <summary>Проверяет разбор маски ZIP-файлов через короткий параметр.</summary>
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

        /// <summary>Проверяет разбор маски ZIP-файлов через длинный параметр.</summary>
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

        /// <summary>Проверяет ошибку при отсутствии значения параметра маски ZIP-файлов.</summary>

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

        /// <summary>Проверяет регистронезависимый разбор параметра маски ZIP-файлов.</summary>
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

        /// <summary>Проверяет поддержку символов подстановки в маске ZIP-файлов.</summary>
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