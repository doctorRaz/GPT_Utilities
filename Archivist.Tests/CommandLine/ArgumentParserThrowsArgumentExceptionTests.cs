using System;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией назначения.
    /// </summary>
    public sealed class ArgumentParserThrowsArgumentExceptionTests
    {
        /// <summary>Проверяет ошибку при передаче пустого исходного каталога.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenSourceDirectoryEmpty()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "  ", "-d", "C:\\dest" }));

            Assert.That(ex.Message, Does.Contain("Для параметра -s не указано значение.").IgnoreCase);
        }

        /// <summary>Проверяет ошибку при передаче пустого каталога назначения.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenDestinationDirectoryEmpty()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "   " }));

            Assert.That(ex.Message, Does.Contain("Для параметра -d не указано значение.").IgnoreCase);
        }

        /// <summary>Проверяет ошибку при отсутствии значения исходного каталога.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenSourceValueMissing()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s" }));

            Assert.That(ex.Message, Does.Contain("-s"));
        }

        /// <summary>Проверяет ошибку при отсутствии значения каталога назначения.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenDestinationValueMissing()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d" }));

            Assert.That(ex.Message, Does.Contain("-d"));
        }

        /// <summary>Проверяет ошибку при передаче неизвестного короткого параметра.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenUnknownFlagProvided()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-x" }));

            Assert.That(ex.Message, Does.Contain("неизвестный").IgnoreCase);
            Assert.That(ex.Message, Does.Contain("-x"));
        }

        /// <summary>Проверяет ошибку при передаче неизвестного длинного параметра.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenUnknownLongFlagProvided()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "--unknown" }));

            Assert.That(ex.Message, Does.Contain("неизвестный").IgnoreCase);
        }

        /// <summary>Проверяет ошибку, если вместо исходного каталога указан другой параметр.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenSourceValueIsAnotherFlag()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "-d", "C:\\dest" }));

            Assert.That(ex.Message, Does.Contain("-s"));
        }

        /// <summary>Проверяет ошибку, если вместо каталога назначения указан другой параметр.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenDestinationValueIsAnotherFlag()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "-a" }));

            Assert.That(ex.Message, Does.Contain("-d"));
        }

        /// <summary>Проверяет ошибку при отсутствии значения длинного параметра источника.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenLongSourceFlagHasNoValue()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source" }));

            Assert.That(ex.Message, Does.Contain("--source"));
        }

        /// <summary>Проверяет ошибку при отсутствии значения длинного параметра назначения.</summary>
        [Test]
        public void Parse_ThrowsArgumentException_WhenLongDestinationFlagHasNoValue()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "--destination" }));

            Assert.That(ex.Message, Does.Contain("--destination"));
        }
    }
}