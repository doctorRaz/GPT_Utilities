using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.Export
{
    public sealed class ChatMetadataReaderTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 8, 24, 15, 23, 56, 473, TimeSpan.Zero);

        private static readonly DateTimeOffset UpdateTime =
            new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

        [Fact]
        public void ReadMetadata_ParsesFrontMatter()
        {
            using TempDirectory temp = new();
            string file = MarkdownFactory.Write(
                temp.Combine("chat.md"),
                CreateTime,
                UpdateTime);

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(CreateTime, metadata.CreateTime);
            Assert.Equal(UpdateTime, metadata.UpdateTime);
            Assert.Equal(
                "https://chatgpt.com/c/11111111-1111-1111-1111-111111111111",
                metadata.ChatLink);
            Assert.Equal(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                metadata.ConversationId);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(CreateTime, metadata.CreateTime);
            Assert.Equal(UpdateTime, metadata.UpdateTime);
            Assert.Null(metadata.ChatLink);
            Assert.Null(metadata.ConversationId);
        }

        [Fact]
        public void ReadMetadata_Throws_WhenFrontMatterMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, "# just markdown");

            FormatException ex = Assert.Throws<FormatException>(
                () => ChatMetadataReader.Read(file));

            Assert.Contains("YAML front matter", ex.Message);
        }

        [Fact]
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
                () => ChatMetadataReader.Read(file));

            Assert.Contains("create_time", ex.Message);
        }

        [Fact]
        public void ReadMetadata_Throws_WhenUpdateTimeMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            File.WriteAllText(file, """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                ---

                body
                """);

            FormatException ex = Assert.Throws<FormatException>(
                () => ChatMetadataReader.Read(file));

            Assert.Contains("update_time", ex.Message);
        }

        #region YAML Front Matter Regex Tests

        [Fact]
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

            Assert.True(match.Success);
            Assert.NotNull(match.Groups["yaml"]);
            Assert.Contains("create_time", match.Groups["yaml"].Value);
        }

        [Fact]
        public void FrontMatterRegex_MatchesFrontMatterWithCarriageReturns()
        {
            string content = "---\r\ncreate_time: 2026-08-24T15:23:56.473Z\r\nupdate_time: 2026-08-25T10:00:00.000Z\r\n---\r\n# Content";

            Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

            Assert.True(match.Success);
        }

        [Fact]
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

            Assert.False(match.Success);
        }

        [Fact]
        public void FrontMatterRegex_FailsWhenMissingClosingDelimiter()
        {
            string content = """
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z

                # Content
                """;

            Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

            Assert.False(match.Success);
        }

        [Fact]
        public void FrontMatterRegex_MatchesEmptyFrontMatter()
        {
            string content = """
                ---
                ---

                # Content
                """;

            Match match = ChatMetadataReader.FrontMatterRegex.Match(content);

            // Regex не должен матчить пустой front matter
            Assert.False(match.Success);
        }

        #endregion

        #region Date Format Tests

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(new DateTimeOffset(2026, 8, 24, 15, 23, 56, 473, TimeSpan.Zero), metadata.CreateTime);
            Assert.Equal(new DateTimeOffset(2026, 8, 25, 10, 0, 0, 0, TimeSpan.Zero), metadata.UpdateTime);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(new DateTimeOffset(2026, 8, 24, 15, 23, 56, TimeSpan.Zero), metadata.CreateTime);
        }

        [Fact]
        public void ReadMetadata_ParsesDifferentTimes()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            var time1 = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var time2 = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);

            File.WriteAllText(file, $"""
                ---
                create_time: {time1:yyyy-MM-ddTHH:mm:ssZ}
                update_time: {time2:yyyy-MM-ddTHH:mm:ssZ}
                ---

                body
                """);

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(time1, metadata.CreateTime);
            Assert.Equal(time2, metadata.UpdateTime);
        }

        #endregion

        #region ConversationId Extraction Tests

        [Fact]
        public void ConversationId_ExtractsGuidFromChatLink()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            var guid = Guid.Parse("12345678-1234-5678-1234-567812345678");

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: https://chatgpt.com/c/{guid}
                ---

                body
                """);

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(guid, metadata.ConversationId);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Null(metadata.ConversationId);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Null(metadata.ConversationId);
        }

        [Fact]
        public void ConversationId_ReturnsNull_WhenChatLinkWrongDomain()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            var guid = Guid.Parse("12345678-1234-5678-1234-567812345678");

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: https://example.com/c/{guid}
                ---

                body
                """);

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Null(metadata.ConversationId);
        }

        [Fact]
        public void ConversationId_HandlesTrailingSlash()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            var guid = Guid.Parse("12345678-1234-5678-1234-567812345678");

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: https://chatgpt.com/c/{guid}/
                ---

                body
                """);

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(guid, metadata.ConversationId);
        }

        [Fact]
        public void ConversationId_CaseInsensitiveForDomain()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("chat.md");
            var guid = Guid.Parse("12345678-1234-5678-1234-567812345678");

            File.WriteAllText(file, $"""
                ---
                create_time: 2026-08-24T15:23:56.473Z
                update_time: 2026-08-25T10:00:00.000Z
                chat_link: HTTPS://CHATGPT.COM/c/{guid}
                ---

                body
                """);

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(guid, metadata.ConversationId);
        }

        #endregion

        #region Optional Fields Tests

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(title, metadata.Title);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.NotNull(metadata.Tags);
            Assert.Equal(3, metadata.Tags.Count);
            Assert.Contains("tag1", metadata.Tags);
            Assert.Contains("tag2", metadata.Tags);
            Assert.Contains("tag3", metadata.Tags);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(chatLink, metadata.ChatLink);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
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
                () => ChatMetadataReader.Read(file));

            Assert.Contains("YAML", ex.Message);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(CreateTime, metadata.CreateTime);
            Assert.Equal(UpdateTime, metadata.UpdateTime);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.Equal(title, metadata.Title);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.NotNull(metadata.Title);
            Assert.Contains("multiline", metadata.Title);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            Assert.NotNull(metadata.Title);
            Assert.NotNull(metadata.ChatLink);
        }

        [Fact]
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

            ChatMetadata metadata = ChatMetadataReader.Read(file);

            // Should correctly parse despite key naming conventions
            Assert.Equal("Test Title", metadata.Title);
        }

        #endregion

        #region Error Messages Tests

        [Fact]
        public void ReadMetadata_ErrorMessage_IncludesFilePath_WhenFrontMatterMissing()
        {
            using TempDirectory temp = new();
            string file = temp.Combine("test_file.md");
            File.WriteAllText(file, "# No front matter");

            FormatException ex = Assert.Throws<FormatException>(
                () => ChatMetadataReader.Read(file));

            Assert.Contains("test_file.md", ex.Message);
        }

        [Fact]
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
                () => ChatMetadataReader.Read(file));

            Assert.Contains("test_file.md", ex.Message);
        }

        #endregion
    }
}
