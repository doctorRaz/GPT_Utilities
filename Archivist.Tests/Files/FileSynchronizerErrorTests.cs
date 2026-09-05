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
    public sealed class FileSynchronizerErrorTests
    {
        private static readonly DateTimeOffset CreateTime =
            new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private const string ConversationA =
            "https://chatgpt.com/c/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        /// <summary>Создаёт уникальный файл, если метаданные существующего файла назначения невозможно прочитать.</summary>
        [Test]
        public void CopyIfNewer_AddsUniqueFile_WhenDestinationMetadataIsUnreadable()
        {
            using TempDirectory temp = new();
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllText(destination, "not a chatgpt export");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA);

            FileOperationResult result = Synchronize(source, destination, Read(source));

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(File.ReadAllText(destination), Is.EqualTo("not a chatgpt export"));
            Assert.That(temp.Combine("dst", "Chat (1).md"), Does.Exist);
        }

        /// <summary>Фиксирует ошибку чтения индекса, когда метаданные существующего файла назначения некорректны.</summary>
        [Test]
        public void Synchronize_ReportsIndexReadError_WhenDestinationMetadataIsUnreadable()
        {
            using TempDirectory temp = new();
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllText(destination, "not a chatgpt export");
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA, "new");

            FileOperationResult result = Synchronize(source, destination, Read(source));
            ExportStatistics statistics = new();
            statistics.Add(result);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(result.IndexReadErrors, Is.EqualTo(1));
            Assert.That(statistics.Total, Is.EqualTo(2));
            Assert.That(statistics.Failed, Is.EqualTo(1));
            Assert.That(File.ReadAllText(destination), Is.EqualTo("not a chatgpt export"));
            Assert.That(temp.Combine("dst", "Chat (1).md"), Does.Exist);
        }

        /// <summary>Сохраняет существующие версии и индекс, если копирование исходного файла завершается ошибкой.</summary>
        [Test]
        public void Synchronize_WhenCopyFails_PreservesExistingVersionsAndIndex()
        {
            using TempDirectory temp = new();
            string existing = MarkdownFactory.Write(temp.Combine("dst", "Old.md"), CreateTime, CreateTime.AddHours(10), ConversationA);
            string source = MarkdownFactory.Write(temp.Combine("src", "New.md"), CreateTime, CreateTime.AddHours(11), ConversationA);
            FailingFileSystem fileSystem = new(failCopy: true, failDelete: false);
            IChatMetadataReader reader = new ChatMetadataReader(fileSystem);
            IConversationIndex index = new ConversationIndex(fileSystem, reader, new ConsoleArchivistLogger());
            IFileSynchronizer synchronizer = new FileSynchronizerService(reader, new ConsoleArchivistLogger(), new UniqueFileNameProvider(fileSystem), fileSystem, index);

            Assert.Throws<IOException>(() => synchronizer.Synchronize(source, temp.Combine("dst", "New.md"), reader.Read(source)));
            Assert.That(File.Exists(existing), Is.True);
            Assert.That(index.FindPaths(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), temp.Combine("dst")), Is.EqualTo(new[] { Path.GetFullPath(existing) }));
        }

        /// <summary>Сохраняет в индексе файл, удаление которого завершилось ошибкой.</summary>
        [Test]
        public void Synchronize_WhenDeleteFails_KeepsFailedDeletionInIndex()
        {
            using TempDirectory temp = new();
            string existing = MarkdownFactory.Write(temp.Combine("dst", "Old.md"), CreateTime, CreateTime.AddHours(10), ConversationA);
            string source = MarkdownFactory.Write(temp.Combine("src", "New.md"), CreateTime, CreateTime.AddHours(11), ConversationA);
            FailingFileSystem fileSystem = new(failCopy: false, failDelete: true);
            IChatMetadataReader reader = new ChatMetadataReader(fileSystem);
            IConversationIndex index = new ConversationIndex(fileSystem, reader, new ConsoleArchivistLogger());
            IFileSynchronizer synchronizer = new FileSynchronizerService(reader, new ConsoleArchivistLogger(), new UniqueFileNameProvider(fileSystem), fileSystem, index);

            Assert.Throws<IOException>(() => synchronizer.Synchronize(source, temp.Combine("dst", "New.md"), reader.Read(source)));
            Assert.That(File.Exists(existing), Is.True);
            Assert.That(File.Exists(temp.Combine("dst", "New.md")), Is.True);
            Assert.That(index.FindPaths(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), temp.Combine("dst")), Has.Count.EqualTo(2));
        }

        /// <summary>Записывает результат операции синхронизации в журнал.</summary>
        [Test]
        public void Synchronize_WritesOperationToLogger()
        {
            using TempDirectory temp = new();
            string source = MarkdownFactory.Write(temp.Combine("src", "Chat.md"), CreateTime, CreateTime.AddHours(1), ConversationA);
            string destination = temp.Combine("dst", "Chat.md");
            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            RecordingLogger logger = new();
            ChatMetadata metadata = Read(source);
            IFileSynchronizer synchronizer = new FileSynchronizerService(
                new ChatMetadataReader(new LocalFileSystem()),
                logger,
                new UniqueFileNameProvider(new LocalFileSystem()),
                new LocalFileSystem());

            FileOperationResult result = synchronizer.Synchronize(source, destination, metadata);

            Assert.That(result.Status, Is.EqualTo(FileOperationStatus.Added));
            Assert.That(logger.Messages, Has.Some.Contains("Добавлен"));
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
            public void WriteAllText(string path, string contents) => _inner.WriteAllText(path, contents);
            public IEnumerable<string> ReadLines(string path) => _inner.ReadLines(path);

            public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
            {
                if (_failCopy) throw new IOException("copy failed");
                _inner.CopyFile(sourcePath, destinationPath, overwrite);
            }

            public void MoveFile(string sourcePath, string destinationPath) => _inner.MoveFile(sourcePath, destinationPath);

            public bool TryCopyFile(string sourcePath, string destinationPath)
            {
                if (_failCopy) throw new IOException("copy failed");
                return _inner.TryCopyFile(sourcePath, destinationPath);
            }

            public void DeleteFile(string path)
            {
                if (_failDelete) throw new IOException("delete failed");
                _inner.DeleteFile(path);
            }

            public void SetLastWriteTime(string path, DateTime lastWriteTime) => _inner.SetLastWriteTime(path, lastWriteTime);
            public bool DirectoryExists(string path) => _inner.DirectoryExists(path);
            public void CreateDirectory(string path) => _inner.CreateDirectory(path);
            public void DeleteDirectory(string path, bool recursive) => _inner.DeleteDirectory(path, recursive);
            public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => _inner.EnumerateFiles(path, searchPattern, searchOption);
            public IEnumerable<string> EnumerateDirectories(string path, SearchOption searchOption) => _inner.EnumerateDirectories(path, searchOption);
        }

        private sealed class RecordingLogger : IArchivistLogger
        {
            public List<string> Messages { get; } = new();
            public void Trace(string message) => Messages.Add(message);
            public void Warning(string message) => Messages.Add(message);
            public void Success(string message) => Messages.Add(message);
            public void Update(string message) => Messages.Add(message);
            public void Error(string message, Exception? exception = null) => Messages.Add(message);
        }

        private static ChatMetadata Read(string path) => new ChatMetadataReader(new LocalFileSystem()).Read(path);

        private static FileOperationResult Synchronize(string source, string destination, ChatMetadata metadata) =>
            new FileSynchronizerService(
                new ChatMetadataReader(new LocalFileSystem()),
                new ConsoleArchivistLogger(),
                new UniqueFileNameProvider(new LocalFileSystem()),
                new LocalFileSystem()).Synchronize(source, destination, metadata);
    }
}
