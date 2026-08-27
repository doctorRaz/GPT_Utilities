using dRz.GPT_Utilities.Archivist.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace dRz.GPT_Utilities.Archivist
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
    public static class ChatGptExportProcessor
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
        ///     YYYY\MM
        /// </param>
        /// <param name="processAllArchives">
        /// Если <see langword="true"/>, обрабатываются все ZIP-архивы
        /// в исходном каталоге.
        ///
        /// Если <see langword="false"/>, обрабатывается только последний
        /// изменённый ZIP-архив.
        /// </param>
        /// <returns>
        /// Количество скопированных Markdown-файлов.
        /// </returns>
        public static int Process(
            string sourceDirectory,
            string destinationDirectory,
            bool processAllArchives = false)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                throw new ArgumentException(
                    "Не указан исходный каталог.",
                    nameof(sourceDirectory));
            }

            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new ArgumentException(
                    "Не указан каталог назначения.",
                    nameof(destinationDirectory));
            }

            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Исходный каталог не найден: {sourceDirectory}");
            }

            // Каталог назначения может отсутствовать.
            //Directory.CreateDirectory(destinationDirectory);

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
                    $"В каталоге не найден ZIP-архив: {sourceDirectory}");
            }

            // Если требуется обработать только последний архив,
            // оставляем последний элемент отсортированного списка.
            if (!processAllArchives)
            {
                zipFiles = zipFiles
                           .TakeLast(1)
                           .ToList();
            }

            ConsoleWriter.Info($"Архивов для обработки: {zipFiles.Count}");

            //обработано копий
            int copiedCount = 0;

            //отправляем архивы  на обработку
            foreach (FileInfo zipFile in zipFiles)
            {
                ConsoleWriter.Info($"ZIP: {zipFile.FullName}");
                ConsoleWriter.Info($"Дата изменения ZIP: {zipFile.LastWriteTime}");

                copiedCount += ProcessArchive(zipFile.FullName, destinationDirectory);
            }

            return copiedCount;
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
        private static int ProcessArchive(
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

                ConsoleWriter.Info($"Распаковка ZIP: {tempDirectory}");

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

                ConsoleWriter.Info($"Найдено Markdown-файлов: {markdownFiles.Count}");

                int copiedCount = 0;

                foreach (string sourceFile in markdownFiles)
                {
                    try
                    {
                        if (ProcessMarkdownFile(sourceFile, destinationDirectory))
                        {
                            copiedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ошибка одного файла не останавливает обработку
                        // остальных файлов текущего архива.
                        ConsoleWriter.Error($"ОШИБКА: {sourceFile}");
                        ConsoleWriter.Error(ex.Message);
                    }
                }

                return copiedCount;
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
        private static bool ProcessMarkdownFile(
            string sourceFile,
            string destinationDirectory)
        {
            // -------------------------------------------------------------
            // 4. Читаем create_time из YAML front matter.
            // -------------------------------------------------------------
            ChatMetadata sourceMetadata = MetadataReader.ReadMetadata(sourceFile);

            // -------------------------------------------------------------
            // 5. Формируем:
            //
            // destination\YYYY\MM
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

            fileName = FileNamer.Normalize(fileName);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = Path.GetFileNameWithoutExtension(sourceFile);

                ConsoleWriter.Warning($"КОПИРУЮ КАК ЕСТЬ: пустое имя файла: {sourceFile}");
            }

            // -------------------------------------------------------------
            // 7. Формируем путь назначения.
            // -------------------------------------------------------------
            string destinationFile = Path.Combine(monthDirectory, fileName + extension);

            // Проверяем, требуется ли копирование и в методе копируем файл, если нужно.
            return FileSynchronizer.CopyIfNewer(sourceFile, destinationFile, sourceMetadata);
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

                ConsoleWriter.Info($"Временный каталог: {tempDirectory} удалён.");
            }
            catch (Exception ex)
            {
                // Не выбрасываем исключение отсюда, чтобы не скрыть
                // возможную ошибку основной обработки.

                ConsoleWriter.Error($"Не удалось удалить временный каталог: {tempDirectory}" + $"\n{ex.Message}");
            }
        }
    }
}