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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.AddedUnique));
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.ReadAllText(matchingDestination), Does.Contain("new second"));
            Assert.That(temp.Combine("dst", "Chat (2).md"), Does.Not.Exist);
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(staleMatch), Does.Contain("stale second"));
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.AddedUnique));
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.AddedUnique));
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.AddedUnique));
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

            ChatMetadata metadata = new ChatMetadataReader().Read(source);

            _ = Synchronize(source, destination, metadata);

            DateTime expected = updateTime.LocalDateTime;
            DateTime actual = File.GetLastWriteTime(destination);

            Assert.That(actual, Is.EqualTo(expected).Within(TimeSpan.FromSeconds(2)));
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
            ChatMetadata metadata = new ChatMetadataReader().Read(source);
            IFileSynchronizer synchronizer = new FileSynchronizerService(
                new ChatMetadataReader(),
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
                new ChatMetadataReader(),
                new ConsoleArchivistLogger(),
                new UniqueFileNameProvider(new LocalFileSystem()),
                new LocalFileSystem()).Synchronize(
                    source,
                    destination,
                    metadata);
        }
    }
}