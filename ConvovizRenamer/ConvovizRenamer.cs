using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace dRz.GPT_Utilities
{
    /// <summary>
    /// Переименовывает Markdown-файлы на основании первого значения
    /// из YAML-свойства <c>aliases</c>.
    /// </summary>
    /// <remarks>
    /// Например, файл:
    ///
    /// AI.md
    ///
    /// с frontmatter:
    ///
    /// aliases:
    /// - "Движок AI модели"
    ///
    /// будет переименован в:
    ///
    /// Движок AI модели.md
    ///
    /// Каталоги и содержимое Markdown-файлов не изменяются.
    /// </remarks>
    public static  class ConvovizRenamer
    {
        /// <summary>
        /// Переименовывает все Markdown-файлы в указанном каталоге
        /// и его подкаталогах.
        /// </summary>
        /// <param name="rootDirectory">
        /// Корневой каталог с Markdown-файлами.
        /// </param>
        /// <returns>
        /// Результат выполнения с количеством переименованных файлов,
        /// пропущенных файлов и конфликтов.
        /// </returns>
        public static RenameResult Rename(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException(
                    "Не указан каталог.",
                    nameof(rootDirectory));

            if (!Directory.Exists(rootDirectory))
                throw new DirectoryNotFoundException(
                    $"Каталог не найден: {rootDirectory}");

            var result = new RenameResult();

            foreach (string file in EnumerateMarkdownFiles(rootDirectory))
            {
                result.Total++;

                try
                {
                    RenameFile(file, result);
                }
                catch (Exception ex)
                {
                    result.Errors++;

                    Console.WriteLine(
                        $"[ERROR] {file}{Environment.NewLine}" +
                        $"        {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Рекурсивно получает Markdown-файлы.
        /// </summary>
        private static IEnumerable<string> EnumerateMarkdownFiles(
            string rootDirectory)
        {
            return Directory.EnumerateFiles(
                rootDirectory,
                "*.md",
                SearchOption.AllDirectories);
        }

        /// <summary>
        /// Обрабатывает один Markdown-файл.
        /// </summary>
        private static void RenameFile(
            string file,
            RenameResult result)
        {
            string? alias = ReadFirstAlias(file);

            // Если aliases отсутствует или пустой,
            // файл оставляем без изменений.
            if (string.IsNullOrWhiteSpace(alias))
            {
                result.Skipped++;

                Console.WriteLine(
                    $"[SKIP] Alias не найден: {file}");

                return;
            }

            // Удаляем только символы, которые действительно
            // недопустимы в имени файла Windows.
            //
            // В частности, кириллица, китайские и другие
            // Unicode-символы здесь сохраняются.
            string newName = SanitizeFileName(alias);

            if (string.IsNullOrWhiteSpace(newName))
            {
                result.Skipped++;

                Console.WriteLine(
                    $"[SKIP] После очистки имя пустое: {file}");

                return;
            }

            string directory =
                Path.GetDirectoryName(file)!;

            string newFile = Path.Combine(
                directory,
                newName + ".md");

            // Уже правильное имя.
            if (string.Equals(
                    file,
                    newFile,
                    StringComparison.OrdinalIgnoreCase))
            {
                result.AlreadyCorrect++;
                return;
            }

            // Никогда не перезаписываем существующий файл.
            if (File.Exists(newFile))
            {
                result.Conflicts++;

                Console.WriteLine(
                    $"[CONFLICT] Файл уже существует:{Environment.NewLine}" +
                    $"           {newFile}{Environment.NewLine}" +
                    $"           ← {file}");

                return;
            }

            // File.Move не изменяет содержимое файла.
            // Также он не меняет время последнего изменения
            // самого файла.
            File.Move(file, newFile);

            result.Renamed++;

            Console.WriteLine(
                $"[OK] {Path.GetFileName(file)}{Environment.NewLine}" +
                $"     → {Path.GetFileName(newFile)}");
        }

        /// <summary>
        /// Читает первое значение YAML-свойства <c>aliases</c>.
        /// </summary>
        /// <param name="file">
        /// Путь к Markdown-файлу.
        /// </param>
        /// <returns>
        /// Первое значение aliases либо <c>null</c>, если оно отсутствует.
        /// </returns>
        private static string? ReadFirstAlias(string file)
        {
            // UTF-8 используется явно, чтобы корректно работать
            // с кириллицей независимо от системной кодировки.
            using var reader = new StreamReader(
                file,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true));

            string? line = reader.ReadLine();

            // Ожидаем YAML frontmatter:
            //
            // ---
            // title: ...
            // aliases:
            // - "..."
            // ---
            if (line?.Trim() != "---")
                return null;

            bool inAliases = false;

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                // Конец YAML frontmatter.
                if (trimmed == "---")
                    break;

                if (trimmed == "aliases:")
                {
                    inAliases = true;
                    continue;
                }

                if (!inAliases)
                    continue;

                // Первый элемент списка aliases.
                //
                // Поддерживаются варианты:
                //
                // - "Движок AI модели"
                // - 'Движок AI модели'
                // - Движок AI модели
                //
                if (trimmed.StartsWith("-"))
                {
                    string value = trimmed.Substring(1).Trim();

                    return UnquoteYamlString(value);
                }

                // Если после aliases встретился другой YAML-ключ,
                // список aliases закончился.
                inAliases = false;
            }

            return null;
        }

        /// <summary>
        /// Убирает окружающие одинарные или двойные кавычки
        /// у значения YAML.
        /// </summary>
        private static string UnquoteYamlString(string value)
        {
            if (value.Length >= 2)
            {
                char first = value[0];
                char last = value[value.Length - 1];

                if ((first == '"' && last == '"') ||
                    (first == '\'' && last == '\''))
                {
                    return value.Substring(
                        1,
                        value.Length - 2);
                }
            }

            return value;
        }

        /// <summary>
        /// Подготавливает строку для использования в качестве
        /// имени файла Windows.
        /// </summary>
        /// <remarks>
        /// Здесь намеренно НЕ используется ASCII-фильтрация
        /// или \w из регулярных выражений.
        ///
        /// Такие подходы могут уничтожить кириллицу.
        ///
        /// Path.GetInvalidFileNameChars() позволяет сохранить
        /// допустимые Unicode-символы и удалить только символы,
        /// недопустимые в имени файла.
        /// </remarks>
        private static string SanitizeFileName(string name)
        {
            foreach (char invalidChar
                in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(
                    invalidChar.ToString(),
                    string.Empty);
            }

            // Windows не допускает пробел или точку
            // в конце имени файла.
            name = name.TrimEnd(' ', '.');

            // Точка "." и ".." имеют специальное значение
            // и не должны использоваться как имена файла.
            if (name == "." || name == "..")
                return string.Empty;

            return name;
        }
    }

    /// <summary>
    /// Результат переименования Markdown-файлов.
    /// </summary>
    public sealed class RenameResult
    {
        /// <summary>
        /// Общее количество найденных Markdown-файлов.
        /// </summary>
        public int Total { get; internal set; }

        /// <summary>
        /// Количество успешно переименованных файлов.
        /// </summary>
        public int Renamed { get; internal set; }

        /// <summary>
        /// Количество файлов, которые уже имели правильное имя.
        /// </summary>
        public int AlreadyCorrect { get; internal set; }

        /// <summary>
        /// Количество файлов, пропущенных из-за отсутствия
        /// или некорректности aliases.
        /// </summary>
        public int Skipped { get; internal set; }

        /// <summary>
        /// Количество конфликтов имён.
        /// </summary>
        public int Conflicts { get; internal set; }

        /// <summary>
        /// Количество файлов, при обработке которых произошла ошибка.
        /// </summary>
        public int Errors { get; internal set; }
    }
}