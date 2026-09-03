using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

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
        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.Exists(destination), Is.True);
            Assert.That(File.ReadAllText(destination), Is.EqualTo(File.ReadAllText(source)));
        }

        /// <summary>Copies if newer replaces file when source is newer.</summary>
        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.ReadAllText(destination), Does.Contain("new"));
            Assert.That(destination, Does.Not.Contain(" (1)"));
        }

        /// <summary>Copies if newer skips file when source is older or equal.</summary>
        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(destination), Does.Contain("kept"));
        }

        /// <summary>Copies if newer adds unique file when conversation ids differ.</summary>
        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.ReadAllText(destination), Does.Contain("first"));
            Assert.That(File.Exists(unique), Is.True);
            Assert.That(File.ReadAllText(unique), Does.Contain("second"));
        }

        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.Exists(matchingDestination), Is.False);
            Assert.That(File.ReadAllText(temp.Combine("dst", "Chat (2).md")), Does.Contain("new second"));
        }

        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.Exists(staleMatch), Is.False);
            Assert.That(File.ReadAllText(newestMatch), Does.Contain("newest second"));
        }

        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(matchingDestination), Does.Contain("new second"));
            Assert.That(temp.Combine("dst", "Chat (2).md"), Does.Not.Exist);
        }

        /// <summary>Copies if newer adds unique file when both conversation ids are missing.</summary>
        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.ReadAllText(destination), Does.Contain("old"));
            Assert.That(File.Exists(unique), Is.True);
            Assert.That(File.ReadAllText(unique), Does.Contain("new"));
        }

        /// <summary>Copies if newer adds unique file when one conversation identifier is missing.</summary>
        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.ReadAllText(destination), Does.Contain("kept"));
            Assert.That(temp.Combine("dst", "Chat (1).md"), Does.Exist);
        }

        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.ReadAllText(destination), Is.EqualTo("not a chatgpt export"));
            Assert.That(temp.Combine("dst", "Chat (1).md"), Does.Exist);
        }

        [Test]
        public void Synchronize_ReportsIndexReadError_WhenDestinationMetadataIsUnreadable()
        {
            using TempDirectory temp = new();
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllText(destination, "not a chatgpt export");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "new");

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);
            FileOperationResult result = Synchronize(source, destination, metadata);
            ExportStatistics statistics = new();
            statistics.Add(result);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(result.IndexReadErrors, Is.EqualTo(1));
            Assert.That(statistics.Total, Is.EqualTo(2));
            Assert.That(statistics.Failed, Is.EqualTo(1));
            Assert.That(File.ReadAllText(destination), Is.EqualTo("not a chatgpt export"));
            Assert.That(temp.Combine("dst", "Chat (1).md"), Does.Exist);
        }

        [Test]
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

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            _ = Synchronize(source, destination, metadata);

            DateTime expected = updateTime.LocalDateTime;
            DateTime actual = File.GetLastWriteTime(destination);

            Assert.That(actual, Is.EqualTo(expected).Within(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public void Synchronize_RemovesAllVersionsNotNewer_WhenSourceIsNewest()
        {
            using TempDirectory temp = new();
            _ = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "old");
            string staleVersion = MarkdownFactory.Write(
                temp.Combine("dst", "Chat (1).md"),
                CreateTime,
                CreateTime.AddHours(2),
                ConversationA,
                "stale");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(3),
                ConversationA,
                "new");
            string destination = temp.Combine("dst", "Chat.md");

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);
            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.Exists(destination), Is.True);
            Assert.That(File.ReadAllText(destination), Does.Contain("new"));
            Assert.That(File.Exists(staleVersion), Is.False);
        }

        [Test]
        public void Synchronize_RemovesStaleVersionsAndSkips_WhenNewerVersionExists()
        {
            using TempDirectory temp = new();
            string staleVersion = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "stale");
            string newestVersion = MarkdownFactory.Write(
                temp.Combine("dst", "Chat (1).md"),
                CreateTime,
                CreateTime.AddHours(3),
                ConversationA,
                "newest");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(2),
                ConversationA,
                "incoming");

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);
            FileOperationResult result = Synchronize(
                source,
                temp.Combine("dst", "Chat.md"),
                metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.Exists(staleVersion), Is.False);
            Assert.That(File.ReadAllText(newestVersion), Does.Contain("newest"));
        }

        [Test]
        public void Synchronize_SkipsVersion_WhenUpdateTimesAreEqual()
        {
            using TempDirectory temp = new();
            DateTimeOffset updateTime = CreateTime.AddHours(1);
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                updateTime,
                ConversationA,
                "old");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                updateTime,
                ConversationA,
                "replacement");

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);
            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(destination), Does.Contain("old"));
        }

        [Test]
        public void Synchronize_TruncatesFractionalSecondsBeforeComparing()
        {
            using TempDirectory temp = new();
            DateTimeOffset baseTime = new(2026, 9, 1, 9, 8, 3, 422, TimeSpan.Zero);
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                baseTime,
                ConversationA,
                "old");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                baseTime,
                ConversationA,
                "replacement");
            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);
            metadata.UpdateTime = baseTime.AddTicks(9_130);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(destination), Does.Contain("old"));
        }

        [Test]
        public void Synchronize_ConvertsOffsetsToUtcBeforeComparing()
        {
            using TempDirectory temp = new();
            DateTimeOffset destinationTime = new(2026, 9, 1, 12, 8, 3, 422, TimeSpan.FromHours(3));
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                destinationTime,
                ConversationA,
                "old");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                destinationTime.ToUniversalTime(),
                ConversationA,
                "replacement");
            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(destination), Does.Contain("old"));
        }

        [Test]
        public void Synchronize_ReplacesVersion_WhenSourceUpdateTimeIsMissing()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(
                temp.Combine("dst", "Chat.md"),
                CreateTime,
                updateTime: null,
                chatLink: ConversationA,
                body: "old");
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA,
                "replacement");

            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);
            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.ReadAllText(destination), Does.Contain("replacement"));
        }

        [Test]
        public void Synchronize_WhenSourceIsBetweenVersions_RemovesOlderAndSkips()
        {
            using TempDirectory temp = new();
            string older = MarkdownFactory.Write(temp.Combine("dst", "Old.md"), CreateTime, CreateTime.AddHours(10), ConversationA);
            string newer = MarkdownFactory.Write(temp.Combine("dst", "New.md"), CreateTime, CreateTime.AddHours(14), ConversationA, "newer");
            string source = MarkdownFactory.Write(temp.Combine("src", "Source.md"), CreateTime, CreateTime.AddHours(12), ConversationA, "source");

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Source.md"), Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.Exists(older), Is.False);
            Assert.That(File.ReadAllText(newer), Does.Contain("newer"));
            Assert.That(File.Exists(temp.Combine("dst", "Source.md")), Is.False);
        }

        [Test]
        public void Synchronize_WhenSourceMatchesOneOfSeveralVersions_RemovesOnlyNotNewerVersions()
        {
            using TempDirectory temp = new();
            string first = MarkdownFactory.Write(temp.Combine("dst", "First.md"), CreateTime, CreateTime.AddHours(10), ConversationA);
            string equal = MarkdownFactory.Write(temp.Combine("dst", "Equal.md"), CreateTime, CreateTime.AddHours(12), ConversationA);
            string newer = MarkdownFactory.Write(temp.Combine("dst", "Newer.md"), CreateTime, CreateTime.AddHours(14), ConversationA);
            string source = MarkdownFactory.Write(temp.Combine("src", "Source.md"), CreateTime, CreateTime.AddHours(12), ConversationA);

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Source.md"), Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.Exists(first), Is.False);
            Assert.That(File.Exists(equal), Is.True);
            Assert.That(File.Exists(newer), Is.True);
        }

        [Test]
        public void Synchronize_WhenSourceIsNewerThanAllVersions_ReplacesAllVersions()
        {
            using TempDirectory temp = new();
            string first = MarkdownFactory.Write(temp.Combine("dst", "First.md"), CreateTime, CreateTime.AddHours(10), ConversationA);
            string second = MarkdownFactory.Write(temp.Combine("dst", "Second.md"), CreateTime, CreateTime.AddHours(12), ConversationA);
            string source = MarkdownFactory.Write(temp.Combine("src", "Source.md"), CreateTime, CreateTime.AddHours(16), ConversationA, "source");
            string destination = temp.Combine("dst", "Source.md");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.Exists(first), Is.False);
            Assert.That(File.Exists(second), Is.False);
            Assert.That(File.ReadAllText(destination), Does.Contain("source"));
        }

        [Test]
        public void Synchronize_WhenTitleChanges_UsesSourceNameInsteadOfUniqueName()
        {
            using TempDirectory temp = new();
            string oldPath = MarkdownFactory.Write(temp.Combine("dst", "Old title.md"), CreateTime, CreateTime.AddHours(10), ConversationA);
            string source = MarkdownFactory.Write(temp.Combine("src", "New title.md"), CreateTime, CreateTime.AddHours(11), ConversationA, "source");
            string destination = temp.Combine("dst", "New title.md");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.Exists(oldPath), Is.False);
            Assert.That(File.Exists(destination), Is.True);
            Assert.That(File.Exists(temp.Combine("dst", "New title (1).md")), Is.False);
        }

        [Test]
        public void Synchronize_WhenSourceHasNoUpdateTime_KeepsDatedExistingVersion()
        {
            using TempDirectory temp = new();
            string existing = MarkdownFactory.Write(temp.Combine("dst", "Existing.md"), CreateTime, CreateTime.AddHours(10), ConversationA, "existing");
            string source = MarkdownFactory.Write(temp.Combine("src", "Source.md"), CreateTime, null, ConversationA, "source");

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Source.md"), Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.Exists(existing), Is.True);
            Assert.That(File.Exists(temp.Combine("dst", "Source.md")), Is.False);
        }

        [Test]
        public void Synchronize_WhenBothUpdateTimesAreMissing_ReplacesExistingVersion()
        {
            using TempDirectory temp = new();
            string existing = MarkdownFactory.Write(temp.Combine("dst", "Existing.md"), CreateTime, null, ConversationA, "existing");
            string source = MarkdownFactory.Write(temp.Combine("src", "Source.md"), CreateTime, null, ConversationA, "source");
            string destination = temp.Combine("dst", "Source.md");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.Exists(existing), Is.False);
            Assert.That(File.ReadAllText(destination), Does.Contain("source"));
        }

        [Test]
        public void Synchronize_WhenOtherConversationExists_LeavesItUntouched()
        {
            using TempDirectory temp = new();
            string other = MarkdownFactory.Write(temp.Combine("dst", "Other.md"), CreateTime, CreateTime.AddHours(10), ConversationB, "other");
            string old = MarkdownFactory.Write(temp.Combine("dst", "Old.md"), CreateTime, CreateTime.AddHours(10), ConversationA, "old");
            string source = MarkdownFactory.Write(temp.Combine("src", "Source.md"), CreateTime, CreateTime.AddHours(12), ConversationA, "source");
            string destination = temp.Combine("dst", "Source.md");
            IFileSystem fileSystem = new LocalFileSystem();
            IChatMetadataReader reader = new ChatMetadataReader(fileSystem);
            IConversationIndex index = new ConversationIndex(fileSystem, reader, new ConsoleArchivistLogger());
            IFileSynchronizer synchronizer = new FileSynchronizerService(
                reader,
                new ConsoleArchivistLogger(),
                new UniqueFileNameProvider(fileSystem),
                fileSystem,
                index);

            FileOperationResult result = synchronizer.Synchronize(source, destination, reader.Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.Exists(old), Is.False);
            Assert.That(File.ReadAllText(other), Does.Contain("other"));
            Assert.That(index.FindPaths(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), temp.Combine("dst")), Is.EqualTo(new[] { Path.GetFullPath(other) }));
            Assert.That(index.FindPaths(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), temp.Combine("dst")), Is.EqualTo(new[] { Path.GetFullPath(destination) }));
        }

        [Test]
        public void Synchronize_WhenCopyFails_PreservesExistingVersionsAndIndex()
        {
            using TempDirectory temp = new();
            string existing = MarkdownFactory.Write(temp.Combine("dst", "Old.md"), CreateTime, CreateTime.AddHours(10), ConversationA);
            string source = MarkdownFactory.Write(temp.Combine("src", "New.md"), CreateTime, CreateTime.AddHours(11), ConversationA);
            FailingFileSystem fileSystem = new(failCopy: true, failDelete: false);
            IChatMetadataReader reader = new ChatMetadataReader(fileSystem);
            IConversationIndex index = new ConversationIndex(fileSystem, reader, new ConsoleArchivistLogger());
            IFileSynchronizer synchronizer = new FileSynchronizerService(
                reader,
                new ConsoleArchivistLogger(),
                new UniqueFileNameProvider(fileSystem),
                fileSystem,
                index);

            Assert.Throws<IOException>(() => synchronizer.Synchronize(source, temp.Combine("dst", "New.md"), reader.Read(source)));
            Assert.That(File.Exists(existing), Is.True);
            Assert.That(index.FindPaths(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), temp.Combine("dst")), Is.EqualTo(new[] { Path.GetFullPath(existing) }));
        }

        [Test]
        public void Synchronize_WhenDeleteFails_KeepsFailedDeletionInIndex()
        {
            using TempDirectory temp = new();
            string existing = MarkdownFactory.Write(temp.Combine("dst", "Old.md"), CreateTime, CreateTime.AddHours(10), ConversationA);
            string source = MarkdownFactory.Write(temp.Combine("src", "New.md"), CreateTime, CreateTime.AddHours(11), ConversationA);
            FailingFileSystem fileSystem = new(failCopy: false, failDelete: true);
            IChatMetadataReader reader = new ChatMetadataReader(fileSystem);
            IConversationIndex index = new ConversationIndex(fileSystem, reader, new ConsoleArchivistLogger());
            IFileSynchronizer synchronizer = new FileSynchronizerService(
                reader,
                new ConsoleArchivistLogger(),
                new UniqueFileNameProvider(fileSystem),
                fileSystem,
                index);

            Assert.Throws<IOException>(() => synchronizer.Synchronize(source, temp.Combine("dst", "New.md"), reader.Read(source)));
            Assert.That(File.Exists(existing), Is.True);
            Assert.That(File.Exists(temp.Combine("dst", "New.md")), Is.True);
            Assert.That(index.FindPaths(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), temp.Combine("dst")), Has.Count.EqualTo(2));
        }

        [Test]
        public void Synchronize_WhenProcessedTwice_DoesNotCreateUniqueVersions()
        {
            using TempDirectory temp = new();
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(12), ConversationA, "source");
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(temp.Combine("dst"));
            ChatMetadata metadata = Read(source);

            FileOperationResult first = Synchronize(source, destination, metadata);
            FileOperationResult second = Synchronize(source, destination, metadata);

            Assert.That(first.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(second.Status, Is.EqualTo(FileOperationStatus.Skipped));
            string[] files = Directory.GetFiles(temp.Combine("dst"), "*.md");
            int conversationFiles = 0;
            foreach (string path in files)
            {
                if (!Path.GetFileName(path).Equals("_index.md", StringComparison.OrdinalIgnoreCase))
                {
                    conversationFiles++;
                }
            }

            Assert.That(conversationFiles, Is.EqualTo(1));
            Assert.That(File.ReadAllText(destination), Does.Contain("source"));
        }

        [Test]
        public void Synchronize_WritesOperationToLogger()
        {
            using TempDirectory temp = new();
            string source = MarkdownFactory.Write(
                temp.Combine("src", "Chat.md"),
                CreateTime,
                CreateTime.AddHours(1),
                ConversationA);
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            RecordingLogger logger = new();
            ChatMetadata metadata = new ChatMetadataReader(new LocalFileSystem()).Read(source);
            IFileSynchronizer synchronizer = new FileSynchronizerService(
                new ChatMetadataReader(new LocalFileSystem()),
                logger,
                new UniqueFileNameProvider(new LocalFileSystem()),
                new LocalFileSystem());

            FileOperationResult result = synchronizer.Synchronize(
                source,
                destination,
                metadata);

            global::NUnit.Framework.Assert.That(
                result.Status,
                global::NUnit.Framework.Is.EqualTo(FileOperationStatus.Added));
            global::NUnit.Framework.Assert.That(
                logger.Messages,
                global::NUnit.Framework.Has.Some.Contains("Добавлен"));
        }

        private sealed class FailingFileSystem : IFileSystem
        {
            private readonly LocalFileSystem _inner = new();
            private readonly bool _failCopy;
            private readonly bool _failDelete;

            public FailingFileSystem(bool failCopy, bool failDelete)
            {
                _failCopy = failCopy;
                _failDelete = failDelete;
            }

            public bool FileExists(string path) => _inner.FileExists(path);

            public string ReadAllText(string path) => _inner.ReadAllText(path);

            public void WriteAllText(string path, string contents) =>
                _inner.WriteAllText(path, contents);

            public IEnumerable<string> ReadLines(string path) => _inner.ReadLines(path);

            public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
            {
                if (_failCopy)
                {
                    throw new IOException("copy failed");
                }

                _inner.CopyFile(sourcePath, destinationPath, overwrite);
            }

            public void MoveFile(string sourcePath, string destinationPath) =>
                _inner.MoveFile(sourcePath, destinationPath);

            public bool TryCopyFile(string sourcePath, string destinationPath)
            {
                if (_failCopy)
                {
                    throw new IOException("copy failed");
                }

                return _inner.TryCopyFile(sourcePath, destinationPath);
            }

            public void DeleteFile(string path)
            {
                if (_failDelete)
                {
                    throw new IOException("delete failed");
                }

                _inner.DeleteFile(path);
            }

            public void SetLastWriteTime(string path, DateTime lastWriteTime) =>
                _inner.SetLastWriteTime(path, lastWriteTime);

            public bool DirectoryExists(string path) => _inner.DirectoryExists(path);

            public void CreateDirectory(string path) => _inner.CreateDirectory(path);

            public void DeleteDirectory(string path, bool recursive) =>
                _inner.DeleteDirectory(path, recursive);

            public IEnumerable<string> EnumerateFiles(
                string path,
                string searchPattern,
                SearchOption searchOption) =>
                _inner.EnumerateFiles(path, searchPattern, searchOption);

            public IEnumerable<string> EnumerateDirectories(string path, SearchOption searchOption) =>
                _inner.EnumerateDirectories(path, searchOption);
        }

        private sealed class RecordingLogger : IArchivistLogger
        {
            public List<string> Messages { get; } = new();

            public void Trace(string message) => Messages.Add(message);

            public void Warning(string message) => Messages.Add(message);

            public void Success(string message) => Messages.Add(message);

            public void Update(string message) => Messages.Add(message);

            public void Error(string message, Exception? exception = null) =>
                Messages.Add(message);
        }

        private static ChatMetadata Read(string path) =>
            new ChatMetadataReader(new LocalFileSystem()).Read(path);

        /// <summary>
        /// Сохраняет компактные проверки старого enum-контракта,
        /// выполняя операции через новый экземплярный сервис.
        /// </summary>
        private static FileOperationResult Synchronize(
            string source,
            string destination,
            ChatMetadata metadata)
        {
            return new FileSynchronizerService(
                new ChatMetadataReader(new LocalFileSystem()),
                new ConsoleArchivistLogger(),
                new UniqueFileNameProvider(new LocalFileSystem()),
                new LocalFileSystem()).Synchronize(
                    source,
                    destination,
                    metadata);
        }
    }
}