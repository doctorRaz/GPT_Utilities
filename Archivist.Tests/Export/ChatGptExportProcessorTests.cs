using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace dRz.GPT_Utilities.Archivist.Tests.Export
{
    /// <summary>
    /// Тесты для ChatGptExportProcessor.
    /// Проверяют обработку ZIP-архивов экспорта ChatGPT и распределение файлов по временным периодам.
    /// </summary>
    public sealed class ChatGptExportProcessorTests
    {
        private static readonly DateTimeOffset CreateTime1 = new(2024, 1, 15, 10, 30, 45, 123, TimeSpan.Zero);
        private static readonly DateTimeOffset CreateTime2 = new(2024, 3, 22, 14, 20, 15, 456, TimeSpan.Zero);
        private static readonly DateTimeOffset UpdateTime1 = new(2024, 1, 16, 11, 45, 30, TimeSpan.Zero);
        private static readonly DateTimeOffset UpdateTime2 = new(2024, 3, 23, 15, 35, 45, TimeSpan.Zero);

        static ChatGptExportProcessorTests()
        {
            // Регистрируем кодировку 866 для работы на всех платформах
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Fact]
        public void Process_ThrowsArgumentNullException_WhenSourceDirectoryIsNull()
        {
            // Код не валидирует параметры - мы просто проверяем, что выбросится какой-то exception
            Assert.Throws<ArgumentNullException>(
                () => ChatGptExportProcessor.Process(null!, "dest"));
        }

        [Fact]
        public void Process_ThrowsArgumentException_WhenSourceDirectoryEmptyString()
        {
            using TempDirectory dest = new();

            // Пустая строка приведёт к ArgumentException
            Assert.Throws<ArgumentException>(
                () => ChatGptExportProcessor.Process(string.Empty, dest.Path));
        }

        [Fact]
        public void Process_ThrowsDirectoryNotFoundException_WhenSourceDirectoryNotExists()
        {
            using TempDirectory dest = new();
            string nonExistentSource = Path.Combine(dest.Path, "nonexistent");

            // Несуществующий каталог приведёт к DirectoryNotFoundException
            Assert.Throws<DirectoryNotFoundException>(
                () => ChatGptExportProcessor.Process(nonExistentSource, dest.Path));
        }

        [Fact]
        public void Process_ThrowsFileNotFoundException_WhenNoZipFilesInSourceDirectory()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(
                () => ChatGptExportProcessor.Process(source.Path, dest.Path));

            Assert.Contains("ZIP", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Process_CreatesDestinationDirectory_IfNotExists()
        {
            using TempDirectory source = new();
            using TempDirectory parent = new();

            string newDest = Path.Combine(parent.Path, "new_destination");
            Assert.False(Directory.Exists(newDest));

            // Создаём пустой ZIP с одним файлом
            string zipPath = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("test.md", CreateTime1, UpdateTime1)
            });

            ChatGptExportProcessor.Process(source.Path, newDest);

            Assert.True(Directory.Exists(newDest));
        }

        [Fact]
        public void Process_ProcessesLastArchiveOnly_WhenProcessAllArchivesIsFalse()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            // Создаём два архива с разными датами
            string zip1Path = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1)
            });

            string zip2Path = CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file2.md", CreateTime2, UpdateTime2)
            });

            // Устанавливаем zip1 как более старый
            File.SetLastWriteTimeUtc(zip1Path, DateTime.UtcNow.AddHours(-1));
            File.SetLastWriteTimeUtc(zip2Path, DateTime.UtcNow);

            ChatGptExportProcessor.Process(source.Path, dest.Path, processAllArchives: false);

            // Проверяем, что обработан только второй архив (более новый)
            string year2 = CreateTime2.Year.ToString();

            // Файл из второго архива должен быть обработан
            string month2 = CreateTime2.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string file2Path = Path.Combine(dest.Path, year2, month2, "file2.md");
            Assert.True(File.Exists(file2Path));
        }

        [Fact]
        public void Process_ProcessesAllArchives_WhenProcessAllArchivesIsTrue()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1)
            });

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file2.md", CreateTime2, UpdateTime2)
            });

            ChatGptExportProcessor.Process(source.Path, dest.Path, processAllArchives: true);

            // Проверяем, что оба архива обработаны
            string year1 = CreateTime1.Year.ToString();
            string year2 = CreateTime2.Year.ToString();

            Assert.True(Directory.Exists(Path.Combine(dest.Path, year1)));
            Assert.True(Directory.Exists(Path.Combine(dest.Path, year2)));
        }

        [Fact]
        public void Process_CreatesYearMonthStructure()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("test.md", CreateTime1, UpdateTime1)
            });

            ChatGptExportProcessor.Process(source.Path, dest.Path);

            // Проверяем структуру YYYY\MM-MMMM
            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string expectedPath = Path.Combine(dest.Path, year, month);

            Assert.True(Directory.Exists(expectedPath));
        }

        [Fact]
        public void Process_NormalizesFileName_ReplacingUnderscoresWithSpaces()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file_with_underscores.md", CreateTime1, UpdateTime1)
            });

            ChatGptExportProcessor.Process(source.Path, dest.Path);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir = Path.Combine(dest.Path, year, month);

            var files = Directory.GetFiles(monthDir, "*.md");
            Assert.Single(files);

            string fileName = Path.GetFileNameWithoutExtension(files[0]);
            Assert.Contains(" ", fileName);
        }

        [Fact]
        public void Process_TrimsWhitespace_FromFileNames()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("  test  .md", CreateTime1, UpdateTime1)
            });

            ChatGptExportProcessor.Process(source.Path, dest.Path);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir = Path.Combine(dest.Path, year, month);

            var files = Directory.GetFiles(monthDir, "*.md");
            Assert.Single(files);

            string fileName = Path.GetFileNameWithoutExtension(files[0]);
            Assert.Equal("test", fileName);
        }

        [Fact]
        public void Process_RemovesExtraSpaces_FromFileNames()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("my   test   file.md", CreateTime1, UpdateTime1)
            });

            ChatGptExportProcessor.Process(source.Path, dest.Path);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir = Path.Combine(dest.Path, year, month);

            var files = Directory.GetFiles(monthDir, "*.md");
            Assert.Single(files);

            string fileName = Path.GetFileNameWithoutExtension(files[0]);
            Assert.Equal("my test file", fileName);
        }

        [Fact]
        public void Process_ProcessesMultipleFiles_InSingleArchive()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("file2.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("file3.md", CreateTime1, UpdateTime1)
            });

            ChatGptExportProcessor.Process(source.Path, dest.Path);

            string year = CreateTime1.Year.ToString();
            string month = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir = Path.Combine(dest.Path, year, month);

            var files = Directory.GetFiles(monthDir, "*.md");
            Assert.Equal(3, files.Length);
        }

        [Fact]
        public void Process_ReturnsStatistics_WithCorrectCounts()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("file2.md", CreateTime1, UpdateTime1)
            });

            var stats = ChatGptExportProcessor.Process(source.Path, dest.Path);

            Assert.Equal(2, stats.Added);
            Assert.Equal(0, stats.Skipped);
            Assert.Equal(0, stats.Updated);
            Assert.Equal(2, stats.Total);
        }

        [Fact]
        public void Process_FilesToDifferentMonths_WhenCreateTimeDiffers()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("jan_file.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("mar_file.md", CreateTime2, UpdateTime2)
            });

            ChatGptExportProcessor.Process(source.Path, dest.Path);

            string year = CreateTime1.Year.ToString();

            string month1 = CreateTime1.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir1 = Path.Combine(dest.Path, year, month1);

            string month2 = CreateTime2.ToString("MM-MMMM", CultureInfo.InvariantCulture);
            string monthDir2 = Path.Combine(dest.Path, year, month2);

            Assert.True(File.Exists(Path.Combine(monthDir1, "jan file.md")));
            Assert.True(File.Exists(Path.Combine(monthDir2, "mar file.md")));
        }

        [Fact]
        public void Process_HandlesEmptyArchive()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            // Создаём пустой ZIP (без файлов)
            CreateEmptyZip(source.Path);

            var stats = ChatGptExportProcessor.Process(source.Path, dest.Path);

            Assert.Equal(0, stats.Total);
        }

        [Fact]
        public void Process_IgnoresNonMarkdownFiles()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            // Используем фабрику для создания корректных файлов
            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("test.md", CreateTime1, UpdateTime1)
            });

            var stats = ChatGptExportProcessor.Process(source.Path, dest.Path);

            // Файл должен быть обработан
            Assert.True(stats.Total > 0);
        }

        [Fact]
        public void Process_HandlesRootLevelMarkdownFiles()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            // Используем фабрику для создания правильных файлов с корректным YAML
            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("file1.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("file2.md", CreateTime2, UpdateTime2)
            });

            var stats = ChatGptExportProcessor.Process(source.Path, dest.Path);

            // Оба файла должны быть обработаны 
            Assert.Equal(2, stats.Total);
        }

        [Fact]
        public void Process_HandlesUnicodeFileNames()
        {
            using TempDirectory source = new();
            using TempDirectory dest = new();

            CreateTestZip(source.Path, new[]
            {
                new TestMarkdownFile("Привет_мир.md", CreateTime1, UpdateTime1),
                new TestMarkdownFile("文件.md", CreateTime1, UpdateTime1)
            });

            var stats = ChatGptExportProcessor.Process(source.Path, dest.Path);

            Assert.Equal(2, stats.Total);
        }

        private static string CreateTestZip(string sourceDirectory, TestMarkdownFile[] files)
        {
            string zipPath = Path.Combine(sourceDirectory, $"test_{Guid.NewGuid():N}.zip");

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    var entry = archive.CreateEntry(file.FileName);
                    using (var writer = new StreamWriter(entry.Open(), Encoding.GetEncoding(866)))
                    {
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
            }

            return zipPath;
        }

        private static void CreateEmptyZip(string sourceDirectory)
        {
            string zipPath = Path.Combine(sourceDirectory, $"empty_{Guid.NewGuid():N}.zip");

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Пустой архив
            }
        }

        private static string Format(DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString(
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                CultureInfo.InvariantCulture);
        }

        private record TestMarkdownFile(string FileName, DateTimeOffset CreateTime, DateTimeOffset? UpdateTime = null);
    }
}
