using dRz.GPT_Utilities.Archivist.CommandLine;
using System;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    public class ArgumentParserPatternTests
    {
        /// <summary>Проверяет сохранение расширения .zip без изменения регистра.</summary>
        [Test]
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

            Assert.That(result1.ZipFilePattern, Is.EqualTo("*.ZIP"));
            Assert.That(result2.ZipFilePattern, Is.EqualTo("*.zip"));
        }

        /// <summary>Проверяет сохранение маски из одного символа-заполнителя.</summary>
        [Test]
        public void Parse_PreservesAsteriskPattern()
        {
            string pattern = "*";

            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", pattern
         });

            Assert.That(result.ZipFilePattern, Is.EqualTo(pattern));
        }

        /// <summary>Проверяет сохранение маски без расширения.</summary>
        [Test]
        public void Parse_PreservesPatternWithoutExtension()
        {
            string pattern = "chatgpt-export-markdown*";

            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", pattern
         });

            Assert.That(result.ZipFilePattern, Is.EqualTo(pattern));
        }

        /// <summary>Проверяет сохранение маски, уже содержащей расширение .zip.</summary>
        [Test]
        public void Parse_PreservesExistingZipExtension()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", "chatgpt-export-markdown*.zip"
         });

            Assert.That(result.ZipFilePattern, Is.EqualTo("chatgpt-export-markdown*.zip"));
        }

        /// <summary>Проверяет сохранение маски с другим расширением.</summary>
        [Test]
        public void Parse_PreservesOtherExtension()
        {
            string pattern = "chatgpt-export-markdown*.txt";
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", pattern
         });

            Assert.That(result.ZipFilePattern, Is.EqualTo(pattern));
        }

        /// <summary>Проверяет разбор маски ZIP-файлов через короткий параметр.</summary>
        [Test]
        [TestCase("chatgpt-export-markdown*.zip", "chatgpt-export-markdown*.zip")]
        [TestCase("chatgpt-export-markdown*", "chatgpt-export-markdown*")]
        [TestCase("my-export*.txt", "my-export*.txt")]
        public void Parse_ParsesZipFilePattern_WithShortFlag(string pattern, string expected)
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
             "-s", "C:\\source",
             "-d", "C:\\dest",
             "-p", pattern
         });

            Assert.That(result.ZipFilePattern, Is.EqualTo(expected));
        }

        /// <summary>Проверяет разбор маски ZIP-файлов через длинный параметр.</summary>
        [Test]
        [TestCase("chatgpt-export-markdown*.zip", "chatgpt-export-markdown*.zip")]
        [TestCase("chatgpt-export-markdown*", "chatgpt-export-markdown*")]
        [TestCase("my-export*.txt", "my-export*.txt")]
        public void Parse_ParsesZipFilePattern_WithLongFlag(string pattern, string expected)
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                    "--source", "C:\\source",
                    "--destination", "C:\\dest",
                    "--pattern", pattern
                });

            Assert.That(result.ZipFilePattern, Is.EqualTo(expected));
        }

        /// <summary>Проверяет ошибку при отсутствии значения параметра маски ZIP-файлов.</summary>

        [Test]
        [TestCase("-p")]
        [TestCase("--pattern")]
        [TestCase("-P")]
        [TestCase("--PATTERN")]
        public void Parse_ThrowsArgumentException_ZipFilePattern_Null(string option)
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                 "--source", "C:\\source",
                 "--destination", "C:\\dest",
                 option
             }));

            Assert.That(ex.Message, Does.Contain($"Для параметра {option} не указано значение.").IgnoreCase);
        }

        /// <summary>Проверяет регистронезависимый разбор параметра маски ZIP-файлов.</summary>
        [Test]
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

            Assert.That(result1.ZipFilePattern, Is.EqualTo("*.ZIP"));
            Assert.That(result2.ZipFilePattern, Is.EqualTo("*.ZIP"));
        }

        /// <summary>Проверяет поддержку символов подстановки в маске ZIP-файлов.</summary>
        [Test]
        public void Parse_AcceptsWildcardCharacters_InZipFilePattern()
        {
            CommandLineOptions result = Archivist.CommandLine.CommandLineParser.Parse(new[]
                {
                  "-s", @"c:\temp",  "-p", "test*?.zip","-d", @"c:\dest"
                });

            Assert.That(result.ZipFilePattern, Is.EqualTo("test*?.zip"));
        }
    }
}