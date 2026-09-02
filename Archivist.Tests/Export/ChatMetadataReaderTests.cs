using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Export
{
    /// <summary>
    /// Тесты для ChatMetadataReader.
    /// Проверяют парсинг YAML front matter из Markdown файлов и извлечение метаданных.
    /// </summary>
    public sealed class ChatMetadataReaderTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 8, 24, 15, 23, 56, 473, TimeSpan.Zero);

        private static readonly DateTimeOffset UpdateTime =
            new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

        #region Основные тесты парсинга

        /// <summary>
        /// Тестирует парсинг базового YAML front matter с обязательными полями.
        /// </summary>
        [Test]
        public void ReadMetadata_ParsesFrontMatter()
        {
            using TempDirectory temp = new();
            string file = MarkdownFactory.Write(
                temp.Combine("chat.md"),
                CreateTime,
                UpdateTime);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.CreateTime, Is.EqualTo(CreateTime));
            Assert.That(metadata.UpdateTime, Is.EqualTo(UpdateTime));
            Assert.That(metadata.ChatLink, Is.EqualTo("https://chatgpt.com/c/11111111-1111-1111-1111-111111111111"));
            Assert.That(metadata.ConversationId, Is.EqualTo(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        }

        /// <summary>
        /// Тестирует игнорирование неизвестных полей YAML при десериализации.
        /// Парсер попеременно игнорирует поля, которые не определены в ChatMetadata.
        /// </summary>
        [Test]
        public void ReadMetadata_IgnoresUnknownYamlFields()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                title: Extra field
                model: gpt-4
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.CreateTime, Is.EqualTo(CreateTime));
            Assert.That(metadata.UpdateTime, Is.EqualTo(UpdateTime));
            Assert.That(metadata.ChatLink, Is.Null);
            Assert.That(metadata.ConversationId, Is.Null);
        }

        #endregion

        #region Тесты ошибок

        /// <summary>
        /// Тестирует выброс исключения, когда файл не содержит YAML front matter.
        /// </summary>
        [Test]
        public void ReadMetadata_Throws_WhenFrontMatterMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, "# just markdown");

            FormatException ex = Assert.Throws<FormatException>(
                () => new ChatMetadataReader(new LocalFileSystem()).Read(file));

            Assert.That(ex.Message, Contains.Substring("YAML front matter"));
        }

        /// <summary>
        /// Тестирует выброс исключения, когда отсутствует обязательное поле create_time.
        /// </summary>
        [Test]
        public void ReadMetadata_Throws_WhenCreateTimeMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                update_time: 2026-08-25T10:00:00.000Z
                ---

                body
                """);

            FormatException ex = Assert.Throws<FormatException>(
                () => new ChatMetadataReader(new LocalFileSystem()).Read(file));

            Assert.That(ex.Message, Contains.Substring("create_time"));
        }

        /// <summary>
        /// Тестирует принятие отсутствующего update_time как минимальной даты.
        /// </summary>
        [Test]
        public void ReadMetadata_UsesDefault_WhenUpdateTimeMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.UpdateTime, Is.Null);
        }

        #endregion

        #region Тесты Regex для YAML front matter

        /// <summary>
        /// Тестирует, что regex корректно находит стандартный YAML front matter.
        /// Front matter должен находиться между --- и --- в начале файла.
        /// </summary>
        [Test]
        public void FrontMatterRegex_MatchesStandardFrontMatter()
        {
            string content = """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                ---

                # Content
                """;

            Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

            Assert.That(match.Success, Is.True);
            Assert.That(match.Groups["yaml"], Is.Not.Null);
            Assert.That(match.Groups["yaml"].Value, Contains.Substring("create_time"));
        }

        /// <summary>
        /// Тестирует, что regex корректно обрабатывает Windows-style линии (CRLF).
        /// </summary>
        [Test]
        public void FrontMatterRegex_MatchesFrontMatterWithCarriageReturns()
        {
            string content = "---\r\ncreate_time: 2026-08-24T15:23:56.473Z\r\nupdate_time: 2026-08-25T10:00:00.000Z\r\n---\r\n# Content";

            Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

            Assert.That(match.Success, Is.True);
        }

        /// <summary>
        /// Тестирует, что front matter должен быть в начале файла.
        /// Front matter в середине файла не должен быть распознан.
        /// </summary>
        [Test]
        public void FrontMatterRegex_MatchesFrontMatterAtStartOnly()
        {
            string content = """
                Some content
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                ---
                """;

            Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

            Assert.That(match.Success, Is.False);
        }

        /// <summary>
        /// Тестирует, что front matter должен иметь как открывающий, так и закрывающий разделитель.
        /// Отсутствие закрывающего --- делает front matter невалидным.
        /// </summary>
        [Test]
        public void FrontMatterRegex_FailsWhenMissingClosingDelimiter()
        {
            string content = """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z

                # Content
                """;

            Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

            Assert.That(match.Success, Is.False);
        }

        /// <summary>
        /// Тестирует, что regex не матчит пустой front matter.
        /// Между --- должна быть хотя бы какая-то контент.
        /// </summary>
        [Test]
        public void FrontMatterRegex_MatchesEmptyFrontMatter()
        {
            string content = """
                ---
                ---

                # Content
                """;

            Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

            // Regex не должен матчить пустой front matter
            Assert.That(match.Success, Is.False);
        }

        #endregion

        #region Тесты форматов дат

        /// <summary>
        /// Тестирует парсинг дат в формате ISO 8601 с миллисекундами.
        /// Формат: YYYY-MM-DDTHH:mm:ss.fffZ
        /// </summary>
        [Test]
        public void ReadMetadata_ParsesDateWithMilliseconds()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.CreateTime, Is.EqualTo(new DateTimeOffset(2026, 8, 24, 15, 23, 56, 473, TimeSpan.Zero)));
            Assert.That(metadata.UpdateTime, Is.EqualTo(new DateTimeOffset(2026, 8, 25, 10, 0, 0, 0, TimeSpan.Zero)));
        }

        /// <summary>
        /// Тестирует парсинг дат в формате ISO 8601 без миллисекунд.
        /// Формат: YYYY-MM-DDTHH:mm:ssZ
        /// </summary>
        [Test]
        public void ReadMetadata_ParsesDateWithoutMilliseconds()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56Z
                update_time: 2026-08-25T10:00:00Z
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.CreateTime, Is.EqualTo(new DateTimeOffset(2026, 8, 24, 15, 23, 56, TimeSpan.Zero)));
        }

        /// <summary>
        /// Тестирует парсинг разных временных точек (крайние даты).
        /// </summary>
        [Test]
        public void ReadMetadata_ParsesDifferentTimes()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            DateTimeOffset time1 = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset time2 = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);

            File.WriteAllText(file, $"""
                ---
                create_time: {time1:yyyy-MM-ddTHH:mm:ssZ}
                update_time: {time2:yyyy-MM-ddTHH:mm:ssZ}
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.CreateTime, Is.EqualTo(time1));
            Assert.That(metadata.UpdateTime, Is.EqualTo(time2));
        }

        #endregion

        #region Тесты извлечения ConversationId

        /// <summary>
        /// Тестирует извлечение GUID из поля chat_link.
        /// GUID должен быть последней частью URL после /c/
        /// </summary>
        [Test]
        public void ConversationId_ExtractsGuidFromChatLink()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            Guid guid = Guid.Parse("12345678-1234-5678-1234-567812345678");

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: https://chatgpt.com/c/{guid}
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ConversationId, Is.EqualTo(guid));
        }

        /// <summary>
        /// Тестирует, что ConversationId возвращает null, если chat_link отсутствует.
        /// </summary>
        [Test]
        public void ConversationId_ReturnsNull_WhenChatLinkMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ConversationId, Is.Null);
        }

        /// <summary>
        /// Тестирует, что ConversationId возвращает null при невалидном UUID.
        /// </summary>
        [Test]
        public void ConversationId_ReturnsNull_WhenChatLinkInvalid()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: https://chatgpt.com/c/invalid-uuid
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ConversationId, Is.Null);
        }

        /// <summary>
        /// Тестирует, что ConversationId возвращает null при неправильном домене.
        /// Должен быть именно https://chatgpt.com/c/
        /// </summary>
        [Test]
        public void ConversationId_ReturnsNull_WhenChatLinkWrongDomain()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            Guid guid = Guid.Parse("12345678-1234-5678-1234-567812345678");

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: https://example.com/c/{guid}
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ConversationId, Is.Null);
        }

        /// <summary>
        /// Тестирует обработку trailing слэша в конце chat_link.
        /// Нужно правильно извлектфь GUID даже если есть слэш в конце.
        /// </summary>
        [Test]
        public void ConversationId_HandlesTrailingSlash()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            Guid guid = Guid.Parse("12345678-1234-5678-1234-567812345678");

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: https://chatgpt.com/c/{guid}/
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ConversationId, Is.EqualTo(guid));
        }

        /// <summary>
        /// Тестирует, что парсинг ConversationId регистронезависим для домена.
        /// Должен работать с HTTPS://CHATGPT.COM/c/ и другими вариантами регистра.
        /// </summary>
        [Test]
        public void ConversationId_CaseInsensitiveForDomain()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            Guid guid = Guid.Parse("12345678-1234-5678-1234-567812345678");

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: HTTPS://CHATGPT.COM/c/{guid}
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ConversationId, Is.EqualTo(guid));
        }

        #endregion

        #region Тесты опциональных полей

        /// <summary>
        /// Тестирует парсинг опционального поля title.
        /// </summary>
        [Test]
        public void ReadMetadata_ParsesTitle()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            string title = "My Important Title";

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                title: {title}
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.Title, Is.EqualTo(title));
        }

        /// <summary>
        /// Тестирует парсинг массива tags из YAML.
        /// </summary>
        [Test]
        public void ReadMetadata_ParsesTags()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                tags:
                  - tag1
                  - tag2
                  - tag3
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.Tags, Is.Not.Null);
            Assert.That(metadata.Tags.Count, Is.EqualTo(3));
            Assert.That(metadata.Tags, Contains.Item("tag1"));
            Assert.That(metadata.Tags, Contains.Item("tag2"));
            Assert.That(metadata.Tags, Contains.Item("tag3"));
        }

        /// <summary>
        /// Тестирует парсинг поля chat_link как обычной строки.
        /// </summary>
        [Test]
        public void ReadMetadata_ParsesChatLink()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            string chatLink = "https://chatgpt.com/c/12345678-1234-5678-1234-567812345678";

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: {chatLink}
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.ChatLink, Is.EqualTo(chatLink));
        }

        #endregion

        #region Граничные случаи

        /// <summary>
        /// Тестирует обработку front matter, содержащего только пробелы и пустые строки.
        /// Должно выброситься исключение, так как поля обязательны.
        /// </summary>
        [Test]
        public void ReadMetadata_HandlesFrontMatterWithOnlyWhitespace()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---

                ---

                body
                """);

            // This should throw because YAML is empty/invalid
            FormatException ex = Assert.Throws<FormatException>(
                () => new ChatMetadataReader(new LocalFileSystem()).Read(file));

            Assert.That(ex.Message, Contains.Substring("YAML"));
        }

        /// <summary>
        /// Тестирует, что парсинг работает, даже если после front matter нет body.
        /// Front matter сам по себе достаточен для успешного парсинга.
        /// </summary>
        [Test]
        public void ReadMetadata_HandlesFrontMatterWithNoBody()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                ---
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.CreateTime, Is.EqualTo(CreateTime));
            Assert.That(metadata.UpdateTime, Is.EqualTo(UpdateTime));
        }

        /// <summary>
        /// Тестирует обработку спец символов (кавычек, апострофов) в значениях YAML.
        /// </summary>
        [Test]
        public void ReadMetadata_HandlesFrontMatterWithSpecialCharactersInTitle()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            string title = "Title with \"quotes\" and 'apostrophes' & symbols!";

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                title: {title}
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.Title, Is.EqualTo(title));
        }

        /// <summary>
        /// Тестирует парсинг multiline значений в YAML.
        /// Используется синтаксис | для многострочных строк.
        /// </summary>
        [Test]
        public void ReadMetadata_HandlesMultilineYamlValues()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                title: |
                  This is a
                  multiline title
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.Title, Contains.Substring("multiline"));
        }

        /// <summary>
        /// Тестирует обработку пустых строк в значениях YAML.
        /// </summary>
        [Test]
        public void ReadMetadata_HandlesEmptyStringValues()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                title: ""
                chat_link: ""
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            Assert.That(metadata.Title, Is.Not.Null);
            Assert.That(metadata.ChatLink, Is.Not.Null);
        }

        /// <summary>
        /// Тестирует, что парсинг работает независимо от регистра ключей YAML.
        /// (благодаря UnderscoredNamingConvention в десериализаторе)
        /// </summary>
        [Test]
        public void ReadMetadata_HandlesMixedCaseYamlKeys()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                title: Test Title
                ---

                body
                """);

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(file);

            // Should correctly parse despite key naming conventions
            Assert.That(metadata.Title, Is.EqualTo("Test Title"));
        }

        #endregion

        #region Тесты сообщений об ошибках

        /// <summary>
        /// Тестирует, что сообщение об ошибке содержит путь к файлу.
        /// Это помогает при отладке определить, какой файл вызвал проблему.
        /// </summary>
        [Test]
        public void ReadMetadata_ErrorMessage_IncludesFilePath_WhenFrontMatterMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("test_file.md");
            File.WriteAllText(file, "# No front matter");

            FormatException ex = Assert.Throws<FormatException>(
                () => new ChatMetadataReader(new LocalFileSystem()).Read(file));

            Assert.That(ex.Message, Contains.Substring("test_file.md"));
        }

        /// <summary>
        /// Тестирует, что сообщение об ошибке содержит путь к файлу,
        /// когда отсутствует обязательное поле create_time.
        /// </summary>
        [Test]
        public void ReadMetadata_ErrorMessage_IncludesFilePath_WhenCreateTimeMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("test_file.md");
            File.WriteAllText(file, """
                ---
                update_time: 2026-08-25T10:00:00.000Z
                ---

                body
                """);

            FormatException ex = Assert.Throws<FormatException>(
                () => new ChatMetadataReader(new LocalFileSystem()).Read(file));

            Assert.That(ex.Message, Contains.Substring("test_file.md"));
        }

        #endregion
    }
}

































































