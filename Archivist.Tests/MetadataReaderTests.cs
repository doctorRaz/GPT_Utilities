using System;
using System.IO;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests
{
    public sealed class MetadataReaderTests
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

            ChatMetadata metadata = MetadataReader.ReadMetadata(file);

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

            ChatMetadata metadata = MetadataReader.ReadMetadata(file);

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
                () => MetadataReader.ReadMetadata(file));

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
                () => MetadataReader.ReadMetadata(file));

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
                () => MetadataReader.ReadMetadata(file));

            Assert.Contains("update_time", ex.Message);
        }
    }
}
