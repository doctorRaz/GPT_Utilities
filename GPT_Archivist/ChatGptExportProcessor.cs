using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dRz.GPT_Utilities.GPT_Archivist
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
        /// Максимальный номер суффикса при совпадении имён.
        /// </summary>
        private const int MaxDuplicateNumber = 100;

        /// <summary>
        /// Regex для поиска YAML front matter в начале Markdown-файла.
        ///
        /// Пример:
        ///
        /// ---
        /// create_time: 2026-08-24T15:23:56.473Z
        /// ---
        ///
        /// Группа "yaml" содержит только содержимое front matter.
        /// </summary>
        private static readonly Regex FrontMatterRegex = new(
            @"\A---\s*\r?\n(?<yaml>.*?)\r?\n---\s*(?:\r?\n|$)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Схлопывает последовательность пробельных символов в один пробел.
        ///
        /// Например:
        ///
        /// "Моя   тема" -> "Моя тема"
        /// "Моя     тема" -> "Моя тема"
        /// </summary>
        private static readonly Regex MultipleSpacesRegex = new(
            @"\s+",
            RegexOptions.Compiled);

        /// <summary>
        /// Десериализатор YAML.
        ///
        /// CamelCaseNamingConvention позволяет сопоставить:
        ///
        ///     create_time
        ///
        /// с:
        ///
        ///     CreateTime
        /// </summary>
        private static readonly IDeserializer YamlDeserializer =
            new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

        /// <summary>
        /// Обрабатывает последний ZIP-архив в исходном каталоге.
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
        /// <returns>
        /// Количество скопированных Markdown-файлов.
        /// </returns>
        public static int Process(
            string sourceDirectory,
            string destinationDirectory)
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
            // 1. Находим последний ZIP.
            //
            // Используем дату последнего изменения файла.
            // -------------------------------------------------------------
            FileInfo? latestZip = Directory
                .EnumerateFiles(
                    sourceDirectory,
                    "*.zip",
                    SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestZip is null)
            {
                throw new FileNotFoundException(
                    $"В каталоге не найден ZIP-архив: {sourceDirectory}");
            }

            Console.WriteLine($"ZIP: {latestZip.FullName}");
            Console.WriteLine(
                $"Дата изменения: {latestZip.LastWriteTime}");

            // -------------------------------------------------------------
            // 2. Создаём уникальный временный каталог.
            // -------------------------------------------------------------
            string tempDirectory = Path.Combine(
                Path.GetTempPath(),
                $"GPT_Archivist_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDirectory);

            try
            {
                // ---------------------------------------------------------
                // 3. Распаковываем ZIP.
                // ---------------------------------------------------------
                Console.WriteLine();
                Console.WriteLine($"Распаковка: {tempDirectory}");

                ZipFile.ExtractToDirectory(
                    latestZip.FullName,
                    tempDirectory);

                // ---------------------------------------------------------
                // 4. Находим все Markdown-файлы.
                // ---------------------------------------------------------
                List<string> markdownFiles = Directory
                    .EnumerateFiles(
                        tempDirectory,
                        "*.md",
                        SearchOption.AllDirectories)
                    .ToList();

                Console.WriteLine();
                Console.WriteLine(
                    $"Найдено Markdown-файлов: {markdownFiles.Count}");

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
                        // остальных файлов архива.
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
                // 9. Временный каталог удаляется в любом случае:
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
            ChatMetadata sourceMetadata =
                ReadMetadata(sourceFile);

            // -------------------------------------------------------------
            // 5. Формируем:
            //
            // destination\YYYY\MM
            //
            // create_time приходит из экспорта с часовым поясом.
            // Используем UTC, так как исходное значение содержит "Z".
            // -------------------------------------------------------------
            DateTimeOffset createTime =
                sourceMetadata.CreateTime.ToUniversalTime();

            string yearDirectory = Path.Combine(
                destinationDirectory,
                createTime.ToString("yyyy"));

            //string monthDirectory = Path.Combine(
            //    yearDirectory,
            //    createTime.ToString("MM"));
            string monthDirectory = Path.Combine(
                                yearDirectory,
                                createTime.ToString("MM-MMMM", CultureInfo.InvariantCulture));

            Directory.CreateDirectory(monthDirectory);

            // -------------------------------------------------------------
            // 6. Обрабатываем имя файла.
            // -------------------------------------------------------------
            string fileName = Path.GetFileNameWithoutExtension(sourceFile);

            string extension = Path.GetExtension(sourceFile);

            // "_" -> " "
            fileName = fileName.Replace('_', ' ');

            // Несколько пробельных символов подряд -> один пробел.
            //
            // Например:
            //
            // Проверка___Staged__Diff
            //
            // после Replace:
            //
            // Проверка   Staged  Diff
            //
            // после Regex:
            //
            // Проверка Staged Diff
            fileName = MultipleSpacesRegex.Replace(fileName, " ");

            // Убираем пробелы в начале и конце имени.
            fileName = fileName.Trim();

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = Path.GetFileNameWithoutExtension(sourceFile);

                Console.WriteLine($"КОПИРУЮ КАК ЕСТЬ: пустое имя файла: {sourceFile}");

             //   return false;
            }

            // -------------------------------------------------------------
            // 7. Формируем путь назначения.
            // -------------------------------------------------------------
            string destinationFile = Path.Combine(
                monthDirectory,
                fileName + extension);

            // -------------------------------------------------------------
            // 8. Если файл уже существует, подбираем:
            //
            // File.md
            // File (1).md
            // File (2).md
            // ...
            // File (100).md
            // -------------------------------------------------------------
            //todo заменить на сравнение UpdateDate
            destinationFile =
                GetUniqueFileName(destinationFile);

            // -------------------------------------------------------------
            // Копируем файл.
            //
            // Содержимое Markdown при этом не изменяется.
            // Изменяется только его расположение и имя.
            // -------------------------------------------------------------
            File.Copy(
                sourceFile,
                destinationFile,
                overwrite: true);

            Console.WriteLine(
                $"{createTime:yyyy-MM-dd}  " +
                $"{Path.GetFileName(sourceFile)} -> " +
                $"{Path.GetRelativePath(
                    destinationDirectory,
                    destinationFile)}");

            return true;
        }


        /// <summary>
        /// Читает YAML front matter Markdown-файла и преобразует его
        /// в типизированный объект ChatMetadata.
        /// </summary>
        private static ChatMetadata ReadMetadata(
            string filePath)
        {
            // -------------------------------------------------------------
            // Читаем Markdown целиком.
            //
            // YAML находится в начале файла, поэтому Regex извлекает
            // только front matter.
            // -------------------------------------------------------------
            string content =
                File.ReadAllText(filePath);

            Match match =
                FrontMatterRegex.Match(content);

            if (!match.Success)
            {
                throw new FormatException(
                    $"В файле отсутствует YAML front matter: {filePath}");
            }

            string yaml =
                match.Groups["yaml"].Value;

            // -------------------------------------------------------------
            // Десериализуем YAML в типизированный ChatMetadata.
            // -------------------------------------------------------------
            ChatMetadata? metadata =
                YamlDeserializer.Deserialize<ChatMetadata>(yaml);

            if (metadata is null)
            {
                throw new FormatException(
                    $"Не удалось прочитать YAML: {filePath}");
            }

            // Проверяем, что create_time действительно был получен.
            if (metadata.CreateTime == default)
            {
                throw new FormatException(
                    $"В YAML отсутствует или некорректен " +
                    $"create_time: {filePath}");
            }
            // Проверяем, что update_time действительно был получен.
            if (metadata.UpdateTime == default)
            {
                throw new FormatException(
                    $"В YAML отсутствует или некорректен " +
                    $"update_time: {filePath}");
            }

            //sourceMetadata.ChatLink пока не проверяем

            return metadata;
        }


        /// <summary>
        /// Возвращает свободное имя файла.
        ///
        /// Если:
        ///
        ///     Test.md
        ///
        /// уже существует:
        ///
        ///     Test (1).md
        ///     Test (2).md
        ///     ...
        ///     Test (100).md
        ///
        /// Если все варианты заняты, выбрасывается IOException.
        /// </summary>
        private static string GetUniqueFileName(
            string filePath)
        {
            // Исходное имя свободно.
            if (!File.Exists(filePath))
                return filePath;

            string directory =
                Path.GetDirectoryName(filePath)
                ?? throw new InvalidOperationException(
                    $"Не удалось определить каталог: {filePath}");

            string fileName =
                Path.GetFileNameWithoutExtension(filePath);

            string extension =
                Path.GetExtension(filePath);

            // Проверяем варианты (1)...(100).
            for (int number = 1;
                 number <= MaxDuplicateNumber;
                 number++)
            {
                string candidate = Path.Combine(
                    directory,
                    $"{fileName} ({number}){extension}");

                if (!File.Exists(candidate))
                    return candidate;
            }

            throw new IOException(
                $"Не удалось подобрать свободное имя для файла: " +
                $"{filePath}. " +
                $"Заняты варианты от (1) до ({MaxDuplicateNumber}).");
        }


        /// <summary>
        /// Удаляет временный каталог вместе со всем содержимым.
        /// </summary>
        private static void DeleteTempDirectory(
            string tempDirectory)
        {
            if (!Directory.Exists(tempDirectory))
                return;

            try
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true);

                Console.WriteLine();
                Console.WriteLine(
                    "Временный каталог удалён.");
            }
            catch (Exception ex)
            {
                // Не выбрасываем исключение отсюда, чтобы не скрыть
                // возможную ошибку основной обработки.
                Console.WriteLine();
                Console.WriteLine(
                    $"Не удалось удалить временный каталог: " +
                    $"{ex.Message}");
            }
        }
    }
}