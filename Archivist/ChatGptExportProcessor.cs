using dRz.GPT_Utilities.Archivist.dRz.GPT_Utilities.Archivist;
using dRz.GPT_Utilities.Archivist.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using static dRz.GPT_Utilities.Archivist.FileSynchronizer;

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

            ConsoleWriter.Step($"Найдено {zipFiles.Count.Of(Words.Archives)} для обработки");

            //обработано копий
            CopyStatistics statistics = new CopyStatistics();

            //отправляем архивы  на обработку
            foreach (FileInfo zipFile in zipFiles)
            {
                ConsoleWriter.Step($"ZIP: {zipFile.FullName}");

                ConsoleWriter.Step($"\tДата изменения ZIP: {zipFile.LastWriteTime}");

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

                ConsoleWriter.Step($"Распаковка ZIP в: {tempDirectory}");

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

                ConsoleWriter.Step($"\tНайдено {markdownFiles.Count.Of(Words.Files)} Markdown");

                CopyStatistics statistics = new CopyStatistics();

                foreach (string sourceFile in markdownFiles)
                {
                    try
                    {
                        CopyDecision decision = ProcessMarkdownFile(sourceFile, destinationDirectory);

                        statistics.Add(decision);//добавляем статистику по каждому файлу
                    }
                    catch (Exception ex)
                    {
                        // Ошибка одного файла не останавливает обработку
                        // остальных файлов текущего архива.
                        ConsoleWriter.Error($"ОШИБКА: {sourceFile}");
                        ConsoleWriter.Error(ex.Message);
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
        private static CopyDecision ProcessMarkdownFile(
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
            FileOperationResult copyResult = FileSynchronizer.CopyIfNewer(sourceFile, destinationFile, sourceMetadata);

            // destinationFile мог измениться внутри CopyIfNewer, если было принято решение AddUnique.
            // поэтому для консоли пользуем copyResult.DestinationFilePath 

            //вывод в консоль результата копирования
            WriteCopyResult(copyResult);

            //прокидываем статистику в ProcessArchive, чтобы суммировать количество обработанных файлов
            return copyResult.Decision;
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

                ConsoleWriter.Step($"Удалён временный каталог: {tempDirectory}");
            }
            catch (Exception ex)
            {
                // Не выбрасываем исключение отсюда, чтобы не скрыть
                // возможную ошибку основной обработки.

                ConsoleWriter.Error($"Не удалось удалить временный каталог: {tempDirectory}" + $"\n{ex.Message}");
            }
        }

        internal static void WriteCopyResult(FileOperationResult fileOperationResult)
        {
            //todo причесать вывод в консоль
            string sourseFileName = Path.GetFileName(fileOperationResult.SourceFilePath);

            string exo = $"{sourseFileName}" +
                        $"\n\t\tupdate_time: {fileOperationResult.UpdateTime:yyyy-MM-dd-HH.mm.sss}" +
                        $"\n\t\tto->{fileOperationResult.DestinationFilePath}";

            switch (fileOperationResult.Decision)
            {
                case CopyDecision.Add:
                    ConsoleWriter.Success($"\tДобавлен: {exo}");
                    break;

                case CopyDecision.AddUnique:
                    ConsoleWriter.Warning($"\tДобавлен уникальный: {exo}");
                    break;

                case CopyDecision.Replace:
                    ConsoleWriter.Update($"\tОбновлён: {exo}");
                    break;

                case CopyDecision.Skip:
                    ConsoleWriter.Step($"\tПропущен: {sourseFileName}"); ;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(fileOperationResult.Decision), fileOperationResult.Decision, null);
            }
        }

        internal sealed class CopyStatistics
        {
            public int Total { get; private set; }

            public int Skipped { get; private set; }

            public int Added { get; private set; }

            public int Updated { get; private set; }

            public int AddedUnique { get; private set; }

            public void Add(CopyDecision decision)
            {
                Total++;

                switch (decision)
                {
                    case CopyDecision.Skip:
                        Skipped++;
                        break;

                    case CopyDecision.Add:
                        Added++;
                        break;

                    case CopyDecision.AddUnique:
                        AddedUnique++;
                        break;

                    case CopyDecision.Replace:
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