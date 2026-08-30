using dRz.GPT_Utilities.Archivist.Files;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using dRz.GPT_Utilities.Archivist.Localization;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using static dRz.GPT_Utilities.Archivist.Files.FileSynchronizer;

namespace dRz.GPT_Utilities.Archivist.Export
{
    /// <summary>
    /// Обрабатывает ZIP-архив экспорта ChatGPT и формирует
    /// структурированный архив Markdown-файлов.
    ///
    /// Алгоритм:
    ///
    /// 1. Находит последний ZIP в исходном каталоге.
    /// 2. Распаковывает ZIP во временный каталог.
    /// 3. Находит все Markdown-файлы.
    /// 4. Читает create_time из YAML front matter.
    /// 5. Создаёт структуру:
    ///
    ///       YYYY
    ///       └── MM
    ///
    /// 6. Обрабатывает имя файла:
    ///      "_" -> " "
    ///      несколько пробелов -> один пробел
    ///      пробелы в начале/конце удаляются
    ///
    /// 7. Копирует файл в соответствующий каталог.
    /// 8. При совпадении имени добавляет (1)...(100).
    /// 9. Удаляет временный каталог.
    /// </summary>
    internal static class ChatGptExportProcessor
    {
        /// <summary>
        /// Обрабатывает ZIP-архивы в исходном каталоге.
        /// </summary>
        /// <param name="sourceDirectory">
        /// Каталог, содержащий ZIP-файлы экспорта.
        /// </param>
        /// <param name="destinationDirectory">
        /// Каталог назначения.
        ///
        /// В нём автоматически создаётся структура:
        ///
        ///     YYYY\MM-MMMM
        /// </param>
        /// <param name="processAllArchives">
        /// Если <see langword="true"/>, обрабатываются все ZIP-архивы
        /// в исходном каталоге.<br/>
        ///
        /// Если <see langword="false"/>, обрабатывается только последний
        /// изменённый ZIP-архив.
        /// </param>
        /// <returns>
        /// Количество скопированных Markdown-файлов.
        /// </returns>
        internal static CopyStatistics Process(
            string sourceDirectory,
            string destinationDirectory,
            bool processAllArchives = false)
        {
            // -------------------------------------------------------------
            // 1. Получаем ZIP-архивы.
            //
            // Сортировка выполняется по дате последнего изменения:
            // от старых к новым
            // -------------------------------------------------------------
            List<FileInfo> zipFiles = Directory
                                            .EnumerateFiles(
                                            sourceDirectory,
                                            "*.zip",
                                            SearchOption.TopDirectoryOnly)
                                            .Select(path => new FileInfo(path))
                                            .OrderBy(file => file.LastWriteTimeUtc)
                                            .ToList();

            if (zipFiles.Count == 0)
            {
                throw new FileNotFoundException(
                    $"В каталоге не найден ни один ZIP-архив: {sourceDirectory}");
            }

            // Если требуется обработать только последний архив,
            // оставляем последний элемент отсортированного списка.
            if (!processAllArchives)
            {
                zipFiles = zipFiles
                           .TakeLast(1)
                           .ToList();
            }

            ConsoleWriter.Trace($"Найден {zipFiles.Count.Of(RussianWords.Archives)} для обработки");

            //обработано копий
            CopyStatistics statistics = new CopyStatistics();

            //отправляем архивы  на обработку
            foreach (FileInfo zipFile in zipFiles)
            {
                ConsoleWriter.Trace($"ZIP: {zipFile.FullName}");

                ConsoleWriter.Trace($"\tДата изменения ZIP: {zipFile.LastWriteTime}");

                CopyStatistics archiveStatistics = ProcessArchive(zipFile.FullName, destinationDirectory);//по текущему архиву возвращаем статистику по обработанным файлам
                //суммирование статистики по каждому zip
                statistics.Add(archiveStatistics);
            }

            // общая статистика
            return statistics;
        }

        /// <summary>
        /// Распаковывает и обрабатывает один ZIP-архив.
        /// </summary>
        /// <param name="archiveFilePath">
        /// Полный путь к ZIP-архиву.
        /// </param>
        /// <param name="destinationDirectory">
        /// Каталог назначения.
        /// </param>
        /// <returns>
        /// Количество скопированных Markdown-файлов.
        /// </returns>
        private static CopyStatistics ProcessArchive(
            string archiveFilePath,
            string destinationDirectory)
        {
            // -------------------------------------------------------------
            // Создаём уникальный временный каталог для текущего архива.
            // -------------------------------------------------------------
            string tempDirectory = Path.Combine(Path.GetTempPath(), $"GPT_Archivist_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDirectory);

            try
            {
                // ---------------------------------------------------------
                // Распаковываем ZIP во временный каталог.
                // ---------------------------------------------------------

                ConsoleWriter.Trace($"Распаковка ZIP в: {tempDirectory}");

                ZipFile.ExtractToDirectory(archiveFilePath, tempDirectory, Encoding.GetEncoding(866));

                // ---------------------------------------------------------
                // Находим все Markdown-файлы.
                // ---------------------------------------------------------
                List<string> markdownFiles = Directory
                    .EnumerateFiles(
                        tempDirectory,
                        "*.md",
                        SearchOption.AllDirectories)
                    .ToList();

                ConsoleWriter.Trace($"\tНайдено {markdownFiles.Count.Of(RussianWords.Files)} Markdown");

                CopyStatistics statistics = new CopyStatistics();

                foreach (string sourceFile in markdownFiles)
                {
                    try
                    {
                        FileCopyDecision decision = ProcessMarkdownFile(sourceFile, destinationDirectory);

                        //добавляем статистику по каждому файлу
                        statistics.Add(decision);
                    }
                    catch (Exception ex)
                    {
                        // Ошибка одного файла не останавливает обработку
                        // остальных файлов текущего архива.
                        ConsoleWriter.Error($"Ошибка при обработке файла: {sourceFile}", ex);
                    }
                }

                //статистика по zip
                return statistics;
            }
            finally
            {
                // ---------------------------------------------------------
                // Временный каталог удаляется в любом случае:
                // и после успешной обработки, и после исключения.
                // ---------------------------------------------------------
                DeleteTempDirectory(tempDirectory);
            }
        }

        /// <summary>
        /// Обрабатывает один Markdown-файл.
        /// </summary>
        private static FileCopyDecision ProcessMarkdownFile(
            string sourceFile,
            string destinationDirectory)
        {
            // -------------------------------------------------------------
            // 4. Читаем create_time из YAML front matter.
            // -------------------------------------------------------------
            ChatMetadata sourceMetadata = ChatMetadataReader.Read(sourceFile);

            // -------------------------------------------------------------
            // 5. Формируем:
            //
            // destination\YYYY\MM-MMMM
            //
            // create_time приходит из экспорта с часовым поясом.
            // Используем UTC, так как исходное значение содержит "Z".
            // -------------------------------------------------------------
            DateTimeOffset createTime = sourceMetadata.CreateTime.ToUniversalTime();

            //string yearDirectory = Path.Combine(destinationDirectory, createTime.ToString("yyyy"));

            //string monthDirectory = Path.Combine(
            //    yearDirectory,
            //    createTime.ToString("MM"));
            //string monthDirectory = Path.Combine(yearDirectory, createTime.ToString("MM-MMMM", CultureInfo.InvariantCulture));

            string monthDirectory = Path.Combine(destinationDirectory, createTime.ToString("yyyy"), createTime.ToString("MM-MMMM", CultureInfo.InvariantCulture));

            Directory.CreateDirectory(monthDirectory);

            // -------------------------------------------------------------
            // 6. Обрабатываем имя файла.
            // -------------------------------------------------------------
            string fileName = Path.GetFileNameWithoutExtension(sourceFile);

            string extension = Path.GetExtension(sourceFile);

            fileName = FileNameHelper.Normalize(fileName);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = Path.GetFileNameWithoutExtension(sourceFile);

                ConsoleWriter.Warn($"КОПИРУЮ КАК ЕСТЬ: пустое имя файла: {sourceFile}");
            }

            // -------------------------------------------------------------
            // 7. Формируем путь назначения.
            // -------------------------------------------------------------
            string destinationFile = Path.Combine(monthDirectory, fileName + extension);

            // Проверяем, требуется ли копирование и в методе копируем файл, если нужно.
            FileCopyDecision copyDecision = CopyIfNewer(sourceFile, destinationFile, sourceMetadata);

            //прокидываем статистику в ProcessArchive, чтобы суммировать количество обработанных файлов
            return copyDecision;
        }

        /// <summary>
        /// Удаляет временный каталог вместе со всем содержимым.
        /// </summary>
        private static void DeleteTempDirectory(
            string tempDirectory)
        {
            if (!Directory.Exists(tempDirectory))
            {
                return;
            }

            try
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true);

                ConsoleWriter.Trace($"Удалён временный каталог: {tempDirectory}");
            }
            catch (Exception ex)
            {
                // Не выбрасываем исключение отсюда, чтобы не скрыть
                // возможную ошибку основной обработки.

                ConsoleWriter.Error($"Не удалось удалить временный каталог: {tempDirectory}" + $"\n{ex.Message}");
            }
        }

        internal sealed class CopyStatistics
        {
            public int Total { get; private set; }

            public int Skipped { get; private set; }

            public int Added { get; private set; }

            public int Updated { get; private set; }

            public int AddedUnique { get; private set; }

            public void Add(FileCopyDecision decision)
            {
                Total++;

                switch (decision)
                {
                    case FileCopyDecision.Skip:
                        Skipped++;
                        break;

                    case FileCopyDecision.Add:
                        Added++;
                        break;

                    case FileCopyDecision.AddUnique:
                        AddedUnique++;
                        break;

                    case FileCopyDecision.Replace:
                        Updated++;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(decision),
                            decision,
                            null);
                }
            }

            /// <summary>
            /// Добавляет статистику другого объекта.
            /// </summary>
            public void Add(CopyStatistics statistics)
            {
                Total += statistics.Total;
                Skipped += statistics.Skipped;
                Added += statistics.Added;
                AddedUnique += statistics.AddedUnique;
                Updated += statistics.Updated;
            }
        }
    }
}