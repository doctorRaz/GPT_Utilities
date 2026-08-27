using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dRz.GPT_Utilities.GPT_Archivist
{
    /// <summary>
    /// Обрабатывает ZIP-архивы экспорта ChatGPT.
    /// </summary>
    internal static class ArchiveProcessor2
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
            Directory.CreateDirectory(destinationDirectory);

            // -------------------------------------------------------------
            // 1. Получаем ZIP-архивы.
            //
            // Сортировка выполняется по дате последнего изменения:
            // от новых архивов к старым.
            // -------------------------------------------------------------
            List<FileInfo> zipFiles = Directory
                .EnumerateFiles(
                    sourceDirectory,
                    "*.zip",
                    SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            if (zipFiles.Count == 0)
            {
                throw new FileNotFoundException(
                    $"В каталоге не найден ZIP-архив: {sourceDirectory}");
            }

            // Если требуется обработать только последний архив,
            // оставляем первый элемент отсортированного списка.
            if (!processAllArchives)
            {
                zipFiles = zipFiles.Take(1).ToList();
            }

            Console.WriteLine(
                $"Архивов для обработки: {zipFiles.Count}");

            int copiedCount = 0;

            foreach (FileInfo zipFile in zipFiles)
            {
                Console.WriteLine();
                Console.WriteLine($"ZIP: {zipFile.FullName}");
                Console.WriteLine(
                    $"Дата изменения ZIP: {zipFile.LastWriteTime}");

                copiedCount += ProcessArchive(
                    zipFile.FullName,
                    destinationDirectory);
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
            string tempDirectory = Path.Combine(
                Path.GetTempPath(),
                $"GPT_Archivist_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDirectory);

            try
            {
                // ---------------------------------------------------------
                // Распаковываем ZIP во временный каталог.
                // ---------------------------------------------------------
                Console.WriteLine();
                Console.WriteLine($"Распаковка ZIP: {tempDirectory}");

                ZipFile.ExtractToDirectory(
                    archiveFilePath,
                    tempDirectory,
                    Encoding.GetEncoding(866));

                // ---------------------------------------------------------
                // Находим все Markdown-файлы.
                // ---------------------------------------------------------
                List<string> markdownFiles = Directory
                    .EnumerateFiles(
                        tempDirectory,
                        "*.md",
                        SearchOption.AllDirectories)
                    .ToList();

#if DEBUG
                foreach (string markdownFile in markdownFiles)
                {
                    Console.WriteLine(markdownFile);
                }
#endif

                Console.WriteLine();
                Console.WriteLine(
                    $"Найдено Markdown-файлов: {markdownFiles.Count}");

                int copiedCount = 0;

                foreach (string sourceFile in markdownFiles)
                {
                    try
                    {
                        //if (ProcessMarkdownFile(
                        //    sourceFile,
                        //    destinationDirectory))
                        //{
                        //    copiedCount++;
                        //}
                    }
                    catch (Exception ex)
                    {
                        // Ошибка одного файла не останавливает обработку
                        // остальных файлов текущего архива.
                        Console.WriteLine();
                        Console.WriteLine($"ОШИБКА: {sourceFile}");
                        Console.WriteLine(ex.Message);
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
        /// Удаляет временный каталог вместе со всем его содержимым.
        /// </summary>
        /// <param name="directory">
        /// Путь к временному каталогу.
        /// </param>
        private static void DeleteTempDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            try
            {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
            catch (Exception ex)
            {
                // Ошибка удаления временного каталога не должна
                // скрывать результат основной обработки.
                Console.WriteLine();
                Console.WriteLine(
                    $"Не удалось удалить временный каталог: {directory}");

                Console.WriteLine(ex.Message);
            }
        }

        // Остальные методы класса:
        //
        // ProcessMarkdownFile(...)
        // ...
    }
}