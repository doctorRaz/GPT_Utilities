using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System;
using System.IO;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.Files
{
    public sealed class FileSynchronizerTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private const string ConversationA =
            "https://chatgpt.com/c/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        private const string ConversationB =
            "https://chatgpt.com/c/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

        /// <summary>Copies if newer adds file when destination does not exist.</summary>
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
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.Equal(FileCopyDecision.Add, decision);
            Assert.True(File.Exists(destination));
            Assert.Equal(File.ReadAllText(source), File.ReadAllText(destination));
        }

        /// <summary>Copies if newer replaces file when source is newer.</summary>
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.Equal(FileCopyDecision.Replace, decision);
            Assert.Contains("new", File.ReadAllText(destination));
            Assert.DoesNotContain(" (1)", destination);
        }

        /// <summary>Copies if newer skips file when source is older or equal.</summary>
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.Equal(FileCopyDecision.Skip, decision);
            Assert.Contains("kept", File.ReadAllText(destination));
        }

        /// <summary>Copies if newer adds unique file when conversation ids differ.</summary>
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.Equal(FileCopyDecision.AddUnique, decision);
            Assert.Contains("first", File.ReadAllText(destination));
            Assert.True(File.Exists(unique));
            Assert.Contains("second", File.ReadAllText(unique));
        }

        [Fact]
        public void CopyIfNewer_ReplacesMatchingUniqueFile_InsteadOfAddingDuplicate()
        {
            using TempDirectory temp = new();
            _ = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "first");
            string matchingDestination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat (1).md"),
                CreateTime,
                CreateTime.AddHours(2),
                ConversationB,
                "old second");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(3),
                ConversationB,
                "new second");

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, temp.Combine("dst", "Chat.md"), metadata);

            Assert.Equal(FileCopyDecision.Replace, decision);
            Assert.Contains("new second", File.ReadAllText(matchingDestination));
            Assert.False(File.Exists(temp.Combine("dst", "Chat (2).md")));
        }

        [Fact]
        public void CopyIfNewer_SkipsNewestMatchingUniqueFile_WhenSeveralExist()
        {
            using TempDirectory temp = new();
            _ = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "first");
            string staleMatch = MarkdownFactory.Write(
                temp.Combine("dst", "Chat (1).md"),
                CreateTime,
                CreateTime.AddHours(2),
                ConversationB,
                "stale second");
            string newestMatch = MarkdownFactory.Write(
                temp.Combine("dst", "Chat (2).md"),
                CreateTime,
                CreateTime.AddHours(4),
                ConversationB,
                "newest second");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(3),
                ConversationB,
                "incoming second");

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, temp.Combine("dst", "Chat.md"), metadata);

            Assert.Equal(FileCopyDecision.Skip, decision);
            Assert.Contains("stale second", File.ReadAllText(staleMatch));
            Assert.Contains("newest second", File.ReadAllText(newestMatch));
        }

        [Fact]
        public void CopyIfNewer_SkipsMatchingUniqueFile_WhenItIsNewer()
        {
            using TempDirectory temp = new();
            _ = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "first");
            string matchingDestination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat (1).md"),
                CreateTime,
                CreateTime.AddHours(3),
                ConversationB,
                "new second");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(2),
                ConversationB,
                "old second");

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, temp.Combine("dst", "Chat.md"), metadata);

            Assert.Equal(FileCopyDecision.Skip, decision);
            Assert.Contains("new second", File.ReadAllText(matchingDestination));
            Assert.False(File.Exists(temp.Combine("dst", "Chat (2).md")));
        }

        /// <summary>Copies if newer adds unique file when both conversation ids are missing.</summary>
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.Equal(FileCopyDecision.AddUnique, decision);
            Assert.Contains("old", File.ReadAllText(destination));
            Assert.True(File.Exists(unique));
            Assert.Contains("new", File.ReadAllText(unique));
        }

        /// <summary>Copies if newer adds unique file when one conversation identifier is missing.</summary>
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.Equal(FileCopyDecision.AddUnique, decision);
            Assert.Contains("kept", File.ReadAllText(destination));
            Assert.True(File.Exists(temp.Combine("dst", "Chat (1).md")));
        }

        [Fact]
        public void CopyIfNewer_AddsUniqueFile_WhenDestinationMetadataIsUnreadable()
        {
            using TempDirectory temp = new();
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllText(destination, "not a chatgpt export");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA);

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileCopyDecision decision = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            Assert.Equal(FileCopyDecision.AddUnique, decision);
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
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            _ = FileSynchronizer.CopyIfNewer(source, destination, metadata);

            DateTime expected = updateTime.LocalDateTime;
            DateTime actual = File.GetLastWriteTime(destination);

            Assert.Equal(expected, actual, TimeSpan.FromSeconds(2));
        }
    }
}