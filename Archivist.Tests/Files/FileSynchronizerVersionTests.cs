using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System;
using System.IO;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Files
{
    public sealed class FileSynchronizerVersionTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private const string ConversationA =
            "https://chatgpt.com/c/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        /// <summary>Удаляет все не более новые версии, когда исходный файл является самым новым.</summary>
        [Test]
        public void Synchronize_RemovesAllVersionsNotNewer_WhenSourceIsNewest()
        {
            using TempDirectory temp = new();
            _ = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "old");
            string staleVersion = MarkdownFactory.Write(temp.Combine("dst", "Chat (1).md"), CreateTime, CreateTime.AddHours(2), ConversationA, "stale");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(3), ConversationA, "new");
            string destination = temp.Combine("dst", "Chat.md");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.Exists(destination), Is.True);
            Assert.That(File.ReadAllText(destination), Does.Contain("new"));
            Assert.That(File.Exists(staleVersion), Is.False);
        }

        /// <summary>Удаляет устаревшие версии и пропускает исходный файл, когда уже существует более новая версия.</summary>
        [Test]
        public void Synchronize_RemovesStaleVersionsAndSkips_WhenNewerVersionExists()
        {
            using TempDirectory temp = new();
            string staleVersion = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "stale");
            string newestVersion = MarkdownFactory.Write(temp.Combine("dst", "Chat (1).md"), CreateTime, CreateTime.AddHours(3), ConversationA, "newest");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(2), ConversationA, "incoming");

            FileOperationResult result = Synchronize(source, temp.Combine("dst", "Chat.md"), Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.Exists(staleVersion), Is.False);
            Assert.That(File.ReadAllText(newestVersion), Does.Contain("newest"));
        }

        /// <summary>Пропускает файл, когда время обновления совпадает с существующей версией.</summary>
        [Test]
        public void Synchronize_SkipsVersion_WhenUpdateTimesAreEqual()
        {
            using TempDirectory temp = new();
            DateTimeOffset updateTime = CreateTime.AddHours(1);
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, updateTime, ConversationA, "old");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, updateTime, ConversationA, "replacement");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(destination), Does.Contain("old"));
        }

        /// <summary>Сравнивает время обновления после усечения долей секунды.</summary>
        [Test]
        public void Synchronize_TruncatesFractionalSecondsBeforeComparing()
        {
            using TempDirectory temp = new();
            DateTimeOffset baseTime = new(2026, 9, 1, 9, 8, 3, 422, TimeSpan.Zero);
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, baseTime, ConversationA, "old");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, baseTime, ConversationA, "replacement");
            ChatMetadata metadata = Read(source);
            metadata.UpdateTime = baseTime.AddTicks(9_130);

            FileOperationResult result = Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(destination), Does.Contain("old"));
        }

        /// <summary>Сравнивает время обновления после приведения часовых поясов к UTC.</summary>
        [Test]
        public void Synchronize_ConvertsOffsetsToUtcBeforeComparing()
        {
            using TempDirectory temp = new();
            DateTimeOffset destinationTime = new(2026, 9, 1, 12, 8, 3, 422, TimeSpan.FromHours(3));
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, destinationTime, ConversationA, "old");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, destinationTime.ToUniversalTime(), ConversationA, "replacement");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Skipped));
            Assert.That(File.ReadAllText(destination), Does.Contain("old"));
        }

        /// <summary>Заменяет существующую версию, когда у исходного файла отсутствует время обновления.</summary>
        [Test]
        public void Synchronize_ReplacesVersion_WhenSourceUpdateTimeIsMissing()
        {
            using TempDirectory temp = new();
            string destination = MarkdownFactory.Write(temp.Combine("dst", "Chat.md"), CreateTime, updateTime: null, chatLink: ConversationA, body: "old");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "replacement");

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.ReadAllText(destination), Does.Contain("replacement"));
        }

        /// <summary>Удаляет более старую версию и пропускает исходный файл, когда его время находится между существующими версиями.</summary>
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

        /// <summary>Удаляет версии, которые старше исходного файла, сохраняя версию с равным и более новым временем.</summary>
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

        /// <summary>Заменяет все существующие версии, когда исходный файл новее их всех.</summary>
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

        /// <summary>Использует имя исходного файла при изменении заголовка вместо добавления уникального суффикса.</summary>
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

        /// <summary>Сохраняет существующую версию с заданным временем обновления, если у исходного файла оно отсутствует.</summary>
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

        /// <summary>Заменяет существующую версию, когда время обновления отсутствует у обоих файлов.</summary>
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

        /// <summary>Не изменяет файл другого диалога при синхронизации текущего диалога.</summary>
        [Test]
        public void Synchronize_WhenOtherConversationExists_LeavesItUntouched()
        {
            using TempDirectory temp = new();
            const string conversationB = "https://chatgpt.com/c/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
            string other = MarkdownFactory.Write(temp.Combine("dst", "Other.md"), CreateTime, CreateTime.AddHours(10), conversationB, "other");
            string old = MarkdownFactory.Write(temp.Combine("dst", "Old.md"), CreateTime, CreateTime.AddHours(10), ConversationA, "old");
            string source = MarkdownFactory.Write(temp.Combine("src", "Source.md"), CreateTime, CreateTime.AddHours(12), ConversationA, "source");
            string destination = temp.Combine("dst", "Source.md");
            IFileSystem fileSystem = new LocalFileSystem();
            IChatMetadataReader reader = new ChatMetadataReader(fileSystem);
            IConversationIndex index = new ConversationIndex(fileSystem, reader, new ConsoleArchivistLogger());
            IFileSynchronizer synchronizer = new FileSynchronizerService(reader, new ConsoleArchivistLogger(), new UniqueFileNameProvider(fileSystem), fileSystem, index);

            FileOperationResult result = synchronizer.Synchronize(source, destination, reader.Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Updated));
            Assert.That(File.Exists(old), Is.False);
            Assert.That(File.ReadAllText(other), Does.Contain("other"));
            Assert.That(index.FindPaths(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), temp.Combine("dst")), Is.EqualTo(new[] { Path.GetFullPath(other) }));
            Assert.That(index.FindPaths(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), temp.Combine("dst")), Is.EqualTo(new[] { Path.GetFullPath(destination) }));
        }

        /// <summary>Не создаёт дополнительные уникальные версии при повторной обработке того же файла.</summary>
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
