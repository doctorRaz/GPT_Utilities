using dRz.GPT_Utilities.Archivist.CommandLine;
using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace dRz.GPT_Utilities.Archivist.Tests.Export
{
    /// <summary>
    /// Тесты для ChatGptExportProcessorTests.
    /// Проверяют обработку ZIP-архивов экспорта ChatGPT и распределение файлов по временным периодам.
    /// </summary>
    public sealed class ChatGptExportProcessorTests
    {
        private readonly IChatGptExportProcessor _processor;

        public ChatGptExportProcessorTests()
        {
            IFileSystem fileSystem = new LocalFileSystem();
            IArchivistLogger logger = new ConsoleArchivistLogger();
            IChatMetadataReader metadataReader = new ChatMetadataReader(fileSystem);
            IFileSynchronizer fileSynchronizer = new FileSynchronizerService(
                metadataReader,
                logger,
                new UniqueFileNameProvider(fileSystem),
                fileSystem);
            IMarkdownFileProcessor markdownProcessor = new MarkdownFileProcessor(
                new ExportPathBuilder(fileSystem),
                metadataReader,
                fileSynchronizer,
                logger,
                new FileNameNormalizer());

            _processor = new ChatGptExportProcessor(
                new FileSystemArchiveSelector(fileSystem),
                new ZipArchiveExtractor(Encoding.GetEncoding(866), fileSystem),
                markdownProcessor,
                logger);
        }
        private static readonly DateTimeOffset CreateTime1 = new(2024, 1, 15, 10, 30, 45, 123, TimeSpan.Zero);
        private static readonly DateTimeOffset CreateTime2 = new(2024, 3, 22, 14, 20, 15, 456, TimeSpan.Zero);
        private static readonly DateTimeOffset UpdateTime1 = new(2024, 1, 16, 11, 45, 30, TimeSpan.Zero);
        private static readonly DateTimeOffset UpdateTime2 = new(2024, 3, 23, 15, 35, 45, TimeSpan.Zero);

        static ChatGptExportProcessorTests()
        {
            // Регистрируем кодировку 866 для работы на всех платформах
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>Проверяет выброс ArgumentNullException при отсутствии каталога источника.</summary>
        [Test]
        public void Process_ThrowsArgumentNullException_WhenSourceDirectoryIsNull()
        {
            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
            sourceDirectory: null!, destinationDirectory: "dest");

            _ = Assert.Throws<ArgumentNullException>(
                () => _processor.Process(options));
        }

        /// <summary>Проверяет выброс ArgumentException при пустом каталоге источника.</summary>
        [Test]
        public void Process_ThrowsArgumentException_WhenSourceDirectoryEmptyString()
        {
            using TempDirectory dest = new();

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
            sourceDirectory: string.Empty, destinationDirectory: dest.Path);

            _ = Assert.Throws<ArgumentException>(
                () => _processor.Process(options));
        }

        /// <summary>Проверяет выброс DirectoryNotFoundException для несуществующего каталога источника.</summary>
        [Test]
        public void Process_ThrowsDirectoryNotFoundException_WhenSourceDirectoryNotExists()
        {
            using TempDirectory dest = new();
            string nonExistentSource = Path.Combine(dest.Path, "nonexistent");

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
            sourceDirectory: nonExistentSource, destinationDirectory: dest.Path);

            _ = Assert.Throws<DirectoryNotFoundException>(
                () => _processor.Process(options));
        }

        /// <summary>Проверяет выброс FileNotFoundException, если в каталоге источника нет ZIP-файлов.</summary>
        [Test]
        public void Process_ThrowsFileNotFoundException_WhenNoZipFilesInSourceDirectory()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(
                () => _processor.Process(options));

            Assert.That(ex.Message, Does.Contain("ZIP").IgnoreCase);
        }

        /// <summary>Проверяет создание отсутствующего каталога назначения при обработке архива.</summary>
        [Test]
        public void Process_CreatesDestinationDirectory_IfNotExists()
        {
            using TempDirectory source = new();
            using TempDirectory parent = new();

            string newDest = Path.Combine(parent.Path, "new_destination");
            Assert.That(Directory.Exists(newDest), Is.False);

            string zipPath = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("test.md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                    sourceDirectory: source.Path, destinationDirectory: newDest);

            _ = _processor.Process(options);

            Assert.That(Directory.Exists(newDest), Is.True);
        }

        /// <summary>Проверяет обработку только самого нового архива при отключённой обработке всех архивов.</summary>
        [Test]
        public void Process_ProcessesLastArchiveOnly_WhenProcessAllArchivesIsFalse()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            string zip1Path = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1)
            });

            string zip2Path = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file2.md", CreateTime2, UpdateTime2)
            });

            File.SetLastWriteTimeUtc(zip1Path, DateTime.UtcNow.AddHours(-1));
            File.SetLastWriteTimeUtc(zip2Path, DateTime.UtcNow);

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path, extractAll: false);

            _ = _processor.Process(options);

            string year2 = CreateTime2.Year.ToString();
            string month2 = CreateTime2.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string file2Path = Path.Combine(dest.Path, year2, month2, "file2.md");
            Assert.That(File.Exists(file2Path), Is.True);
        }

        /// <summary>Проверяет обработку всех архивов при включённом соответствующем параметре.</summary>
        [Test]
        public void Process_ProcessesAllArchives_WhenProcessAllArchivesIsTrue()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1)
            });

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file2.md", CreateTime2, UpdateTime2)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path, extractAll: true);

            _ = _processor.Process(options);

            string year1 = CreateTime1.Year.ToString();
            string year2 = CreateTime2.Year.ToString();

            Assert.That(Directory.Exists(Path.Combine(dest.Path, year1)), Is.True);
            Assert.That(Directory.Exists(Path.Combine(dest.Path, year2)), Is.True);
        }

        /// <summary>Проверяет продолжение обработки следующего архива после ошибки чтения одного из архивов.</summary>
        [Test]
        public void Process_ContinuesWithNextArchive_WhenOneZipIsUnreadable()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("valid.md", CreateTime1, UpdateTime1)
            });

            string brokenArchive = Path.Combine(source.Path, "broken.zip");
            File.WriteAllBytes(brokenArchive, new byte[] { 0x01, 0x02, 0x03 });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path,
                destinationDirectory: dest.Path,
                extractAll: true);

            ExportResult result = _processor.Process(options);

            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string validFile = Path.Combine(
                dest.Path,
                CreateTime1.Year.ToString(),
                month,
                "valid.md");

            Assert.That(File.Exists(validFile), Is.True);
            Assert.That(result.Failed, Is.EqualTo(0));
            Assert.That(result.ArchiveFailed, Is.EqualTo(1));
        }

        /// <summary>Проверяет запись ошибки нечитаемого ZIP-архива как ошибки этапа обработки архива.</summary>
        [Test]
        public void Process_ReportsUnreadableZipAsArchiveError()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();
            RecordingLogger logger = new();
            ChatMetadataReader metadataReader = new(new LocalFileSystem());

            IMarkdownFileProcessor markdownProcessor = new MarkdownFileProcessor(
                new ExportPathBuilder(new LocalFileSystem()),
                metadataReader,
                new FileSynchronizerService(
                    metadataReader,
                    logger,
                    new UniqueFileNameProvider(new LocalFileSystem()),
                    new LocalFileSystem()),
                logger,
                new FileNameNormalizer());

            IChatGptExportProcessor processor = new ChatGptExportProcessor(
                new FileSystemArchiveSelector(new LocalFileSystem()),
                new ZipArchiveExtractor(Encoding.GetEncoding(866), new LocalFileSystem()),
                markdownProcessor,
                logger);

            string brokenArchive = Path.Combine(source.Path, "broken.zip");
            File.WriteAllBytes(brokenArchive, new byte[] { 0x01, 0x02, 0x03 });

            ExportResult result = processor.Process(
                new ExportRequest(source.Path, dest.Path, "*.zip", true));

            Assert.That(result.Failed, Is.EqualTo(0));
            Assert.That(result.ArchiveFailed, Is.EqualTo(1));
            Assert.That(result.ArchiveErrors, Has.Count.EqualTo(1));
            Assert.That(result.MarkdownErrors, Is.Empty);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.ArchiveErrors[0].Path, Is.EqualTo(brokenArchive));
            Assert.That(result.ArchiveErrors[0].Stage, Is.EqualTo("Архив"));
            Assert.That(result.ArchiveErrors[0].ExceptionType, Is.EqualTo(nameof(InvalidDataException)));
            string error = logger.Errors.Single();
            Assert.That(error, Does.Contain("ZIP-архива"));
            Assert.That(error, Does.Contain("broken.zip"));
            Assert.That(error, Does.Not.Contain("обработке файла"));
        }

        private sealed class RecordingLogger : IArchivistLogger
        {
            public List<string> Errors { get; } = new();

            public void Trace(string message) { }
            public void Warning(string message) { }
            public void Success(string message) { }
            public void Update(string message) { }
            public void Error(string message, Exception? exception = null) => Errors.Add(message);
        }

        /// <summary>Проверяет создание структуры каталогов года и месяца на основе create_time.</summary>
        [Test]
        public void Process_CreatesYearMonthStructure()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("test.md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            _ = _processor.Process(options);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string expectedPath = Path.Combine(dest.Path, year, month);

            Assert.That(Directory.Exists(expectedPath), Is.True);
        }

        /// <summary>Проверяет сохранение подчёркиваний в имени файла.</summary>
        [Test]
        public void Process_PreservesUnderscoresInFileName()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file_with_underscores.md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            _ = _processor.Process(options);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir = Path.Combine(dest.Path, year, month);

            string[] files = Directory.GetFiles(monthDir, "*.md")
                .Where(path => !Path.GetFileName(path).Equals("_index.md", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.That(files, Has.Length.EqualTo(1));

            string fileName = Path.GetFileNameWithoutExtension(files[0]);
            Assert.That(fileName, Is.EqualTo("file_with_underscores"));
        }

        /// <summary>Проверяет удаление начальных и конечных пробелов из имени файла.</summary>
        [Test]
        public void Process_TrimsWhitespace_FromFileNames()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("  test  .md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            _ = _processor.Process(options);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir = Path.Combine(dest.Path, year, month);

            string[] files = Directory.GetFiles(monthDir, "*.md")
                .Where(path => !Path.GetFileName(path).Equals("_index.md", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.That(files, Has.Length.EqualTo(1));

            string fileName = Path.GetFileNameWithoutExtension(files[0]);
            Assert.That(fileName, Is.EqualTo("test"));
        }

        /// <summary>Проверяет сведение последовательностей пробелов в имени файла к одному пробелу.</summary>
        [Test]
        public void Process_RemovesExtraSpaces_FromFileNames()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("my   test   file.md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            _ = _processor.Process(options);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir = Path.Combine(dest.Path, year, month);

            string[] files = Directory.GetFiles(monthDir, "*.md")
                .Where(path => !Path.GetFileName(path).Equals("_index.md", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.That(files, Has.Length.EqualTo(1));

            string fileName = Path.GetFileNameWithoutExtension(files[0]);
            Assert.That(fileName, Is.EqualTo("my test file"));
        }

        /// <summary>Проверяет обработку нескольких Markdown-файлов из одного архива.</summary>
        [Test]
        public void Process_ProcessesMultipleFiles_InSingleArchive()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("file2.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("file3.md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            _ = _processor.Process(options);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir = Path.Combine(dest.Path, year, month);

            string[] files = Directory.GetFiles(monthDir, "*.md")
                .Where(path => !Path.GetFileName(path).Equals("_index.md", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.That(files.Length, Is.EqualTo(1));
        }

        /// <summary>Проверяет формирование статистики по количеству обработанных файлов.</summary>
        [Test]
        public void Process_ReturnsStatistics_WithCorrectCounts()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("file2.md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            ExportResult stats = _processor.Process(options);

            Assert.That(stats.Added, Is.EqualTo(1));
            Assert.That(stats.Skipped, Is.EqualTo(1));
            Assert.That(stats.Updated, Is.EqualTo(0));
            Assert.That(stats.Total, Is.EqualTo(2));
        }

        /// <summary>Проверяет фиксацию ошибки некорректной даты метаданных без прекращения обработки.</summary>
        [NUnit.Framework.TestCase("create_time")]
        public void Process_ReportsInvalidMetadataDate_WithoutCrashing(string invalidField)
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateMalformedMetadataZip(source.Path, invalidField);

            ExportResult result = _processor.Process(
                new ExportRequest(source.Path, dest.Path, "*.zip", true));

            global::NUnit.Framework.Assert.That(result.Failed, global::NUnit.Framework.Is.EqualTo(1));
            global::NUnit.Framework.Assert.That(result.MarkdownErrors, global::NUnit.Framework.Has.Count.EqualTo(1));
            global::NUnit.Framework.Assert.That(result.MarkdownErrors[0].Message, global::NUnit.Framework.Does.Contain("В YAML отсутствует или некорректен create_time"));
            global::NUnit.Framework.Assert.That(result.ArchiveErrors, global::NUnit.Framework.Is.Empty);
            global::NUnit.Framework.Assert.That(result.Errors, global::NUnit.Framework.Has.Count.EqualTo(1));
            global::NUnit.Framework.Assert.That(result.Errors[0], global::NUnit.Framework.Is.EqualTo(result.MarkdownErrors[0]));
            global::NUnit.Framework.Assert.That(result.MarkdownErrors[0].Path, global::NUnit.Framework.Does.EndWith("malformed.md"));
            global::NUnit.Framework.Assert.That(result.MarkdownErrors[0].Stage, global::NUnit.Framework.Is.EqualTo("Markdown"));
        }

        /// <summary>Проверяет учёт ошибки чтения метаданных существующего файла назначения в статистике.</summary>
        [Test]
        public void Process_ReportsUnreadableDestinationMetadataInStatistics()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1)
            });

            string destinationDirectory = Path.Combine(
                dest.Path,
                CreateTime1.Year.ToString(CultureInfo.InvariantCulture),
                CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture));
            _ = Directory.CreateDirectory(destinationDirectory);
            File.WriteAllText(Path.Combine(destinationDirectory, "file1.md"), "not a chatgpt export");

            ExportResult result = _processor.Process(
                CommandLineOptionsFactory.CreateOptions(
                    sourceDirectory: source.Path,
                    destinationDirectory: dest.Path));

            Assert.That(result.Total, Is.EqualTo(2));
            Assert.That(result.Failed, Is.EqualTo(1));
            Assert.That(result.Added, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(destinationDirectory, "file1 (1).md")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(destinationDirectory, "file1.md")), Is.EqualTo("not a chatgpt export"));
        }

        /// <summary>Проверяет размещение файлов в разных месяцах по значениям create_time.</summary>
        [Test]
        public void Process_FilesToDifferentMonths_WhenCreateTimeDiffers()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("jan_file.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("mar_file.md", CreateTime2, UpdateTime2)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            _ = _processor.Process(options);

            string year = CreateTime1.Year.ToString();
            string month1 = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir1 = Path.Combine(dest.Path, year, month1);
            string month2 = CreateTime2.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir2 = Path.Combine(dest.Path, year, month2);

            Assert.That(File.Exists(Path.Combine(monthDir1, "jan_file.md")), Is.True);
            Assert.That(File.Exists(Path.Combine(monthDir2, "mar_file.md")), Is.True);
        }

        /// <summary>Проверяет корректную обработку пустого ZIP-архива.</summary>
        [Test]
        public void Process_HandlesEmptyArchive()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateEmptyZip(source.Path);

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            ExportResult stats = _processor.Process(options);

            Assert.That(stats.Total, Is.EqualTo(0));
        }

        /// <summary>Проверяет игнорирование файлов, не имеющих расширения Markdown.</summary>
        [Test]
        public void Process_IgnoresNonMarkdownFiles()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("test.md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            ExportResult stats = _processor.Process(options);

            Assert.That(stats.Total, Is.GreaterThan(0));
        }

        /// <summary>Проверяет обработку Markdown-файлов, расположенных в корне ZIP-архива.</summary>
        [Test]
        public void Process_HandlesRootLevelMarkdownFiles()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("file2.md", CreateTime2, UpdateTime2)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            ExportResult stats = _processor.Process(options);

            Assert.That(stats.Total, Is.EqualTo(2));
        }

        /// <summary>Проверяет обработку имён Markdown-файлов с Unicode-символами.</summary>
        [Test]
        public void Process_HandlesUnicodeFileNames()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            _ = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("Привет_мир.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("文件.md", CreateTime1, UpdateTime1)
            });

            ExportRequest options = CommandLineOptionsFactory.CreateOptions(
                sourceDirectory: source.Path, destinationDirectory: dest.Path);

            ExportResult stats = _processor.Process(options);

            Assert.That(stats.Total, Is.EqualTo(2));
        }

        private static string CreateTestZip(string sourceDirectory, TestMarkdownFile[] files)
        {
            string zipPath = Path.Combine(sourceDirectory, $"test_{Guid.NewGuid():N}.zip");

            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (TestMarkdownFile file in files)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(file.FileName);
                    using var writer = new StreamWriter(entry.Open(), Encoding.GetEncoding(866));
                    writer.WriteLine("---");
                    writer.WriteLine($"create_time: {Format(file.CreateTime)}");

                    if (file.UpdateTime.HasValue)
                    {
                        writer.WriteLine($"update_time: {Format(file.UpdateTime.Value)}");
                    }

                    writer.WriteLine("chat_link: https://chatgpt.com/c/11111111-1111-1111-1111-111111111111");
                    writer.WriteLine("---");
                    writer.WriteLine();
                    writer.WriteLine($"Content of {file.FileName}");
                }
            }

            return zipPath;
        }

        private static string CreateMalformedMetadataZip(string sourceDirectory, string invalidField)
        {
            string zipPath = Path.Combine(sourceDirectory, $"malformed_{Guid.NewGuid():N}.zip");

            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("malformed.md");
                using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
                writer.WriteLine("---");
                writer.WriteLine("create_time: 2024-01-15T10:30:45.000Z");
                writer.WriteLine("update_time: 2024-01-16T11:45:30.000Z");
                writer.WriteLine($"{invalidField}: not-a-date");
                writer.WriteLine("chat_link: https://chatgpt.com/c/11111111-1111-1111-1111-111111111111");
                writer.WriteLine("---");
            }

            return zipPath;
        }

        private static void CreateEmptyZip(string sourceDirectory)
        {
            string zipPath = Path.Combine(sourceDirectory, $"empty_{Guid.NewGuid():N}.zip");
            using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        }

        private static string Format(DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        private record TestMarkdownFile(string FileName, DateTimeOffset CreateTime, DateTimeOffset? UpdateTime = null);
    }
}
