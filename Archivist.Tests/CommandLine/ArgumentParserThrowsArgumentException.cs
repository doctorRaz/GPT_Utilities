using System;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.CommandLine
{
    /// <summary>
    /// Тесты для парсера аргументов командной строки, связанных с директорией назначения.
    /// </summary>
    public sealed class ArgumentParserThrowsArgumentException
    {
        /// <summary>Проверяет ошибку при передаче пустого исходного каталога.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenSourceDirectoryEmpty()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "  ", "-d", "C:\\dest" }));

            Assert.Contains("Для параметра -s не указано значение.", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Проверяет ошибку при передаче пустого каталога назначения.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenDestinationDirectoryEmpty()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "   " }));

            Assert.Contains("Для параметра -d не указано значение.", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Проверяет ошибку при отсутствии значения исходного каталога.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenSourceValueMissing()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s" }));

            Assert.Contains("-s", ex.Message);
        }

        /// <summary>Проверяет ошибку при отсутствии значения каталога назначения.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenDestinationValueMissing()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d" }));

            Assert.Contains("-d", ex.Message);
        }

        /// <summary>Проверяет ошибку при передаче неизвестного короткого параметра.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenUnknownFlagProvided()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "-x" }));

            Assert.Contains("неизвестный", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("-x", ex.Message);
        }

        /// <summary>Проверяет ошибку при передаче неизвестного длинного параметра.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenUnknownLongFlagProvided()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "C:\\dest", "--unknown" }));

            Assert.Contains("неизвестный", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Проверяет ошибку, если вместо исходного каталога указан другой параметр.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenSourceValueIsAnotherFlag()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "-d", "C:\\dest" }));

            Assert.Contains("-s", ex.Message);
        }

        /// <summary>Проверяет ошибку, если вместо каталога назначения указан другой параметр.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenDestinationValueIsAnotherFlag()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "-d", "-a" }));

            Assert.Contains("-d", ex.Message);
        }

        /// <summary>Проверяет ошибку при отсутствии значения длинного параметра источника.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenLongSourceFlagHasNoValue()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "--source" }));

            Assert.Contains("--source", ex.Message);
        }

        /// <summary>Проверяет ошибку при отсутствии значения длинного параметра назначения.</summary>
        [Fact]
        public void Parse_ThrowsArgumentException_WhenLongDestinationFlagHasNoValue()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => Archivist.CommandLine.CommandLineParser.Parse(new[] { "-s", "C:\\source", "--destination" }));

            Assert.Contains("--destination", ex.Message);
        }
    }
}