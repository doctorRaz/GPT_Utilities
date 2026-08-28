using System;
using System.IO;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests
{
    public sealed class FileSynchronizerTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private const string ConversationA =
            "https://chatgpt.com/c/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        private const string ConversationB =
            "https://chatgpt.com/c/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

        [Fact]
        public void CopyIfNewer_AddsFile_WhenDestinationDoesNotExist()
        {
            using TempDirectory temp = new();
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA);
            string destination = temp.Combine("dst", "Chat.md");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            ChatMetadata metadata = MetadataReader.ReadMetadata(source);

            bool copied = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.True(copied);
            Assert.True(File.Exists(destination));
            Assert.Equal(File.ReadAllText(source), File.ReadAllText(destination));
        }

        [Fact]
        public void CopyIfNewer_ReplacesFile_WhenSourceIsNewer()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "old");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(2),
                ConversationA,
                "new");

            ChatMetadata metadata = MetadataReader.ReadMetadata(source);

            bool copied = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.True(copied);
            Assert.Contains("new", File.ReadAllText(destination));
            Assert.DoesNotContain(" (1)", destination);
        }

        [Fact]
        public void CopyIfNewer_SkipsFile_WhenSourceIsOlderOrEqual()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(2),
                ConversationA,
                "kept");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "stale");

            ChatMetadata metadata = MetadataReader.ReadMetadata(source);

            bool copied = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.False(copied);
            Assert.Contains("kept", File.ReadAllText(destination));
        }

        [Fact]
        public void CopyIfNewer_AddsUniqueFile_WhenConversationIdsDiffer()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "first");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(3),
                ConversationB,
                "second");

            ChatMetadata metadata = MetadataReader.ReadMetadata(source);

            bool copied = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.True(copied);
            Assert.Contains("first", File.ReadAllText(destination));
            Assert.True(File.Exists(unique));
            Assert.Contains("second", File.ReadAllText(unique));
        }

        [Fact]
        public void CopyIfNewer_AddsUniqueFile_WhenBothConversationIdsAreMissing()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                chatLink: null,
                body: "old");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(2),
                chatLink: null,
                body: "new");

            ChatMetadata metadata = MetadataReader.ReadMetadata(source);

            bool copied = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.True(copied);
            Assert.Contains("old", File.ReadAllText(destination));
            Assert.True(File.Exists(unique));
            Assert.Contains("new", File.ReadAllText(unique));
        }

        [Fact]
        public void CopyIfNewer_AddsUniqueFile_WhenOneConversationIdIsMissing()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "kept");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(2),
                chatLink: null,
                body: "new");

            ChatMetadata metadata = MetadataReader.ReadMetadata(source);

            bool copied = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.True(copied);
            Assert.Contains("kept", File.ReadAllText(destination));
            Assert.True(File.Exists(temp.Combine("dst", "Chat (1).md")));
        }

        [Fact]
        public void CopyIfNewer_AddsUniqueFile_WhenDestinationMetadataIsUnreadable()
        {
            using TempDirectory temp = new();
            string destination = temp.Combine("dst", "Chat.md");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllText(destination, "not a chatgpt export");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA);

            ChatMetadata metadata = MetadataReader.ReadMetadata(source);

            bool copied = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.True(copied);
            Assert.Equal("not a chatgpt export", File.ReadAllText(destination));
            Assert.True(File.Exists(temp.Combine("dst", "Chat (1).md")));
        }

        [Fact]
        public void CopyIfNewer_SetsLastWriteTimeFromUpdateTime()
        {
            using TempDirectory temp = new();
            DateTimeOffset updateTime = CreateTime.AddDays(3);
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                updateTime,
                ConversationA);
            string destination = temp.Combine("dst", "Chat.md");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            ChatMetadata metadata = MetadataReader.ReadMetadata(source);

            FileSynchronizer.CopyIfNewer(source, destination, metadata);

            DateTime expected = updateTime.LocalDateTime;
            DateTime actual = File.GetLastWriteTime(destination);

            Assert.Equal(expected, actual, TimeSpan.FromSeconds(2));
        }
    }
}
