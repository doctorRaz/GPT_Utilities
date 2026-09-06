using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System;
using System.IO;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Files
{
    public sealed class FileSynchronizerCopyTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private const string ConversationA =
            "https://chatgpt.com/c/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        private const string ConversationB =
            "https://chatgpt.com/c/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

        /// <summary>Добавляет файл, если файл назначения отсутствует.</summary>
        [Test]
        public void CopyIfNewer_AddsFile_WhenDestinationDoesNotExist()
        {
            using TempDirectory temp = new();
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA);
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            ChatMetadata metadata = Read(source);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.Exists(destination), Is.True);
            Assert.That(File.ReadAllText(destination), Is.EqualTo(File.ReadAllText(source)));
        }

        /// <summary>Заменяет файл назначения, если исходный файл новее.</summary>
        [Test]
        public void CopyIfNewer_ReplacesFile_WhenSourceIsNewer()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "old");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(2), ConversationA, "new");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.ReadAllText(destination), Does.Contain("new"));
            Assert.That(destination, Does.Not.Contain(" (1)"));
        }

        /// <summary>Пропускает файл, если исходный файл не новее существующего.</summary>
        [Test]
        public void CopyIfNewer_SkipsFile_WhenSourceIsOlderOrEqual()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(2), ConversationA, "kept");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "stale");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(destination), Does.Contain("kept"));
        }

        /// <summary>Создаёт уникальный файл, если идентификаторы диалогов различаются.</summary>
        [Test]
        public void CopyIfNewer_AddsUniqueFile_WhenConversationIdsDiffer()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "first");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(3), ConversationB, "second");

            FileOperationResult result = Synchronize(source, destination, Read(source));
            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.ReadAllText(destination), Does.Contain("first"));
            Assert.That(File.Exists(unique), Is.True);
            Assert.That(File.ReadAllText(unique), Does.Contain("second"));
        }

        /// <summary>Повторный импорт того же разговора обновляет существующий уникальный файл вместо создания следующего дубликата.</summary>
        [Test]
        public void CopyIfNewer_ReimportOfSameConversation_UpdatesExistingUniqueFile()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "first");
            string firstImport = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(2), ConversationB, "second");
            string unique = temp.Combine("dst", "Chat (1).md");

            FileOperationResult firstResult = Synchronize(firstImport, destination, Read(firstImport));

            Assert.That(firstResult.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.Exists(unique), Is.True);
            Assert.That(File.ReadAllText(unique), Does.Contain("second"));

            string secondImport = MarkdownFactory.Write(temp.Combine("src2", "Chat.md"), CreateTime, CreateTime.AddHours(3), ConversationB, "second updated");

            FileOperationResult secondResult = Synchronize(secondImport, destination, Read(secondImport));

            Assert.That(secondResult.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.Exists(unique), Is.True);
            Assert.That(File.ReadAllText(unique), Does.Contain("second updated"));
            Assert.That(temp.Combine("dst", "Chat (2).md"), Does.Not.Exist);
        }

        /// <summary>Заменяет существующую версию с тем же идентификатором диалога вместо создания дубликата.</summary>
        [Test]
        public void CopyIfNewer_ReplacesMatchingUniqueFile_InsteadOfAddingDuplicate()
        {
            using TempDirectory temp = new();
            _ = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "first");
            string matchingDestination = MarkdownFactory.Write(temp.Combine("dst", "Chat (1).md"), CreateTime, CreateTime.AddHours(2), ConversationB, "old second");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(3), ConversationB, "new second");

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.Exists(matchingDestination), Is.False);
            Assert.That(File.ReadAllText(temp.Combine("dst", "Chat (2).md")), Does.Contain("new second"));
        }

        /// <summary>Пропускает копирование, если самая новая существующая версия с тем же идентификатором новее исходного.</summary>
        [Test]
        public void CopyIfNewer_SkipsNewestMatchingUniqueFile_WhenSeveralExist()
        {
            using TempDirectory temp = new();
            _ = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "first");
            string staleMatch = MarkdownFactory.Write(temp.Combine("dst", "Chat (1).md"), CreateTime, CreateTime.AddHours(2), ConversationB, "stale second");
            string newestMatch = MarkdownFactory.Write(temp.Combine("dst", "Chat (2).md"), CreateTime, CreateTime.AddHours(4), ConversationB, "newest second");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(3), ConversationB, "incoming second");

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.Exists(staleMatch), Is.False);
            Assert.That(File.ReadAllText(newestMatch), Does.Contain("newest second"));
        }

        /// <summary>Пропускает копирование, если существующая версия с тем же идентификатором новее исходного файла.</summary>
        [Test]
        public void CopyIfNewer_SkipsMatchingUniqueFile_WhenItIsNewer()
        {
            using TempDirectory temp = new();
            _ = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "first");
            string matchingDestination = MarkdownFactory.Write(temp.Combine("dst", "Chat (1).md"), CreateTime, CreateTime.AddHours(3), ConversationB, "new second");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(2), ConversationB, "old second");

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(matchingDestination), Does.Contain("new second"));
            Assert.That(temp.Combine("dst", "Chat (2).md"), Does.Not.Exist);
        }

        /// <summary>Создаёт уникальный файл, если идентификаторы диалогов отсутствуют у обоих файлов.</summary>
        [Test]
        public void CopyIfNewer_AddsUniqueFile_WhenBothConversationIdsAreMissing()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), chatLink: null, body: "old");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(2), chatLink: null, body: "new");

            FileOperationResult result = Synchronize(source, destination, Read(source));
            string unique = temp.Combine("dst", "Chat (1).md");

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.ReadAllText(destination), Does.Contain("old"));
            Assert.That(File.Exists(unique), Is.True);
            Assert.That(File.ReadAllText(unique), Does.Contain("new"));
        }

        /// <summary>Создаёт уникальный файл, если идентификатор диалога отсутствует у одного из файлов.</summary>
        [Test]
        public void CopyIfNewer_AddsUniqueFile_WhenOneConversationIdIsMissing()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "kept");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(2), chatLink: null, body: "new");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.ReadAllText(destination), Does.Contain("kept"));
            Assert.That(temp.Combine("dst", "Chat (1).md"), Does.Exist);
        }

        /// <summary>Устанавливает время последней записи файла назначения из времени обновления диалога.</summary>
        [Test]
        public void CopyIfNewer_SetsLastWriteTimeFromUpdateTime()
        {
            using TempDirectory temp = new();
            DateTimeOffset updateTime = CreateTime.AddDays(3);
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, updateTime, ConversationA);
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            _ = Synchronize(source, destination, Read(source));

            DateTime expected = updateTime.LocalDateTime;
            DateTime actual = File.GetLastWriteTime(destination);
            Assert.That(actual, Is.EqualTo(expected).Within(TimeSpan.FromSeconds(2)));
        }

        private static ChatMetadata Read(string path) =>
            new ChatMetadataReader(new LocalFileSystem()).Read(path);

        private static FileOperationResult Synchronize(string source, string destination, ChatMetadata metadata) =>
            new FileSynchronizerService(
                new ChatMetadataReader(new LocalFileSystem()),
                new ConsoleArchivistLogger(),
                new UniqueFileNameProvider(new LocalFileSystem()),
                new LocalFileSystem()).Synchronize(source, destination, metadata);
    }
}