using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace dRz.GPT_Utilities.ConvovizRenamer
{
    /// <summary>
    /// Переименовывает Markdown-файлы Convoviz по значению
    /// первого элемента YAML-свойства <c>aliases</c>,
    /// обновляет YAML-свойство <c>title</c> и
    /// соответствующий <c>_index.md</c>.
    /// </summary>
    /// <remarks>
    /// Обработка выполняется отдельно для каждого каталога.
    ///
    /// Например:
    ///
    /// <code>
    /// 2026/
    ///     _index.md
    ///     AI.md
    ///     untitled (1).md
    /// </code>
    ///
    /// После обработки:
    ///
    /// <code>
    /// 2026/
    ///     _index.md
    ///     Движок AI модели.md
    ///     Сохранение проекта как шаблон.md
    /// </code>
    ///
    /// При этом:
    ///
    /// 1. Имя файла берётся из первого элемента <c>aliases</c>.
    /// 2. YAML-свойство <c>title</c> заменяется на это же значение.
    /// 3. Ссылки в <c>_index.md</c> автоматически исправляются.
    /// </remarks>
    public sealed class ConvovizRenamer
    {
        /// <summary>
        /// Расширение Markdown-файлов.
        /// </summary>
        private const string MarkdownExtension = ".md";

        /// <summary>
        /// Имя индексного файла Convoviz.
        /// </summary>
        private const string IndexFileName = "_index.md";

        /// <summary>
        /// Переименовывает Markdown-файлы во всех каталогах
        /// указанного дерева.
        /// </summary>
        /// <param name="rootDirectory">
        /// Корневой каталог экспорта Convoviz.
        /// </param>
        /// <returns>
        /// Общий результат обработки.
        /// </returns>
        public RenameResult Rename(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException(
                    "Не указан каталог.",
                    nameof(rootDirectory));
            }

            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Каталог не найден: {rootDirectory}");
            }

            RenameResult result = new RenameResult();

            // Каждый каталог обрабатывается отдельно.
            //
            // Это важно, поскольку _index.md содержит ссылки
            // только на файлы своего каталога.
            foreach (string directory in EnumerateDirectories(rootDirectory))
            {
                ProcessDirectory(directory, result);
            }

            return result;
        }

        /// <summary>
        /// Обрабатывает один каталог.
        /// </summary>
        private static void ProcessDirectory(
            string directory,
            RenameResult result)
        {
            string[] files = Directory.GetFiles(
                directory,
                "*.md",
                SearchOption.TopDirectoryOnly);

            // _index.md сам не переименовываем.
            string[] markdownFiles = files
                .Where(file =>
                    !string.Equals(
                        Path.GetFileName(file),
                        IndexFileName,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (markdownFiles.Length == 0)
            {
                return;
            }

            // Первый проход:
            // только определяем необходимые операции.
            //
            // Файлы пока НЕ переименовываем.
            Dictionary<string, RenameOperation> renameMap =
                new Dictionary<string, RenameOperation>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string file in markdownFiles)
            {
                result.Total++;

                try
                {
                    string? alias = ReadFirstAlias(file);

                    if (string.IsNullOrWhiteSpace(alias))
                    {
                        result.Skipped++;

                        Console.WriteLine(
                            $"[SKIP] Alias не найден: {file}");

                        continue;
                    }

                    // Из alias формируем имя файла.
                    string newName = SanitizeFileName(alias);

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        result.Skipped++;

                        Console.WriteLine(
                            $"[SKIP] Некорректное имя: {file}");

                        continue;
                    }

                    string newFile = Path.Combine(
                        directory,
                        newName + MarkdownExtension);

                    // Если имя уже правильное,
                    // файл вообще не трогаем.
                    if (string.Equals(
                            file,
                            newFile,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        result.AlreadyCorrect++;
                        continue;
                    }

                    // Если конечное имя уже занято другим файлом,
                    // ничего не делаем.
                    if (File.Exists(newFile))
                    {
                        result.Conflicts++;

                        Console.WriteLine(
                            $"[CONFLICT] Файл уже существует:{Environment.NewLine}" +
                            $"           {newFile}{Environment.NewLine}" +
                            $"           ← {file}");

                        continue;
                    }

                    renameMap[file] =
                        new RenameOperation(
                            newFile,
                            alias);
                }
                catch (Exception ex)
                {
                    result.Errors++;

                    Console.WriteLine(
                        $"[ERROR] {file}{Environment.NewLine}" +
                        $"        {ex.Message}");
                }
            }

            if (renameMap.Count == 0)
            {
                return;
            }

            // ---------------------------------------------------------
            // Проверяем конфликты между самими переименованиями.
            //
            // Например:
            //
            // A.md → Test.md
            // B.md → Test.md
            //
            // В этом случае оба файла оставляем без изменений.
            // ---------------------------------------------------------

            HashSet<string> duplicateTargets = renameMap
                .GroupBy(
                    pair => pair.Value.NewFile,
                    StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string target in duplicateTargets)
            {
                string[] sources = renameMap
                    .Where(pair =>
                        string.Equals(
                            pair.Value.NewFile,
                            target,
                            StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToArray();

                foreach (string source in sources)
                {
                    renameMap.Remove(source);
                }

                result.Conflicts++;

                Console.WriteLine(
                    $"[CONFLICT] Несколько файлов имеют одно имя:{Environment.NewLine}" +
                    $"           {target}");
            }

            if (renameMap.Count == 0)
            {
                return;
            }

            // ---------------------------------------------------------
            // Второй проход.
            //
            // Теперь выполняем реальные переименования.
            // ---------------------------------------------------------

            Dictionary<string, string> performedRenames =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, RenameOperation> pair in renameMap)
            {
                string sourceFile = pair.Key;
                RenameOperation operation = pair.Value;

                try
                {
                    // Сначала физически переименовываем файл.
                    File.Move(
                        sourceFile,
                        operation.NewFile);

                    // title обновляем только после успешного Move.
                    //
                    // Если обновление title завершится ошибкой,
                    // переименование файла всё равно считается успешным,
                    // а ошибка попадёт в result.Errors.
                    try
                    {
                        UpdateTitle(
                            operation.NewFile,
                            operation.Title);
                    }
                    catch (Exception ex)
                    {
                        result.Errors++;

                        Console.WriteLine(
                            $"[ERROR] Не удалось обновить title:{Environment.NewLine}" +
                            $"        {operation.NewFile}{Environment.NewLine}" +
                            $"        {ex.Message}");
                    }

                    performedRenames[sourceFile] =
                        operation.NewFile;

                    result.Renamed++;

                    Console.WriteLine(
                        $"[OK] {Path.GetFileName(sourceFile)}" +
                        $" → {Path.GetFileName(operation.NewFile)}");
                }
                catch (Exception ex)
                {
                    result.Errors++;

                    Console.WriteLine(
                        $"[ERROR] Не удалось переименовать:{Environment.NewLine}" +
                        $"        {sourceFile}{Environment.NewLine}" +
                        $"        → {operation.NewFile}{Environment.NewLine}" +
                        $"        {ex.Message}");
                }
            }

            // ---------------------------------------------------------
            // Обновляем _index.md только после успешных переименований.
            // ---------------------------------------------------------

            if (performedRenames.Count == 0)
            {
                return;
            }

            string indexFile = Path.Combine(
                directory,
                IndexFileName);

            if (!File.Exists(indexFile))
            {
                return;
            }

            try
            {
                UpdateIndex(
                    indexFile,
                    performedRenames);

                result.IndexFilesUpdated++;

                Console.WriteLine(
                    $"[INDEX] {indexFile}");
            }
            catch (Exception ex)
            {
                result.Errors++;

                Console.WriteLine(
                    $"[ERROR] Не удалось обновить:{Environment.NewLine}" +
                    $"        {indexFile}{Environment.NewLine}" +
                    $"        {ex.Message}");
            }
        }

        /// <summary>
        /// Получает все каталоги дерева, включая корневой каталог.
        /// </summary>
        private static IEnumerable<string> EnumerateDirectories(
            string rootDirectory)
        {
            yield return rootDirectory;

            foreach (string directory in Directory.EnumerateDirectories(
                         rootDirectory,
                         "*",
                         SearchOption.AllDirectories))
            {
                yield return directory;
            }
        }

        /// <summary>
        /// Обновляет ссылки в <c>_index.md</c> согласно выполненным
        /// переименованиям.
        /// </summary>
        /// <param name="indexFile">
        /// Путь к <c>_index.md</c>.
        /// </param>
        /// <param name="renameMap">
        /// Соответствие старых имён файлов новым.
        /// </param>
        private static void UpdateIndex(
            string indexFile,
            IReadOnlyDictionary<string, string> renameMap)
        {
            string text = File.ReadAllText(
                indexFile,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

            foreach (KeyValuePair<string, string> pair in renameMap)
            {
                string oldFileName =
                    Path.GetFileName(pair.Key);

                string newFileName =
                    Path.GetFileName(pair.Value);

                string oldLink =
                    UrlEncodeFileName(oldFileName);

                string newLink =
                    UrlEncodeFileName(newFileName);

                // В index ссылка обычно имеет вид:
                //
                // [Название](File%20Name.md)
                //
                // Меняем только destination ссылки,
                // а не отображаемый текст.
                text = text.Replace(
                    $"({oldLink})",
                    $"({newLink})",
                    StringComparison.Ordinal);
            }

            File.WriteAllText(
                indexFile,
                text,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));
        }

        /// <summary>
        /// Обновляет YAML-свойство <c>title</c>.
        /// </summary>
        /// <remarks>
        /// Обрабатывается только YAML frontmatter в начале файла.
        ///
        /// Например:
        ///
        /// <code>
        /// ---
        /// title: "Старое название"
        /// aliases:
        ///   - "Новое название"
        /// ---
        /// </code>
        ///
        /// после обработки:
        ///
        /// <code>
        /// ---
        /// title: "Новое название"
        /// aliases:
        ///   - "Новое название"
        /// ---
        /// </code>
        ///
        /// Если <c>title</c> отсутствует, он добавляется
        /// непосредственно после строки <c>---</c>.
        /// </remarks>
        private static void UpdateTitle(
            string file,
            string title)
        {
            string text = File.ReadAllText(
                file,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true));

            string newline = DetectNewLine(text);

            string[] lines = SplitLines(text);

            // YAML frontmatter должен начинаться с "---".
            if (lines.Length == 0 ||
                lines[0].Trim() != "---")
            {
                return;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();

                // Конец YAML frontmatter.
                if (trimmed == "---")
                {
                    break;
                }

                // Нашли существующий title.
                if (IsYamlProperty(trimmed, "title"))
                {
                    string indentation =
                        lines[i].Substring(
                            0,
                            lines[i].Length -
                            lines[i].TrimStart().Length);

                    lines[i] =
                        indentation +
                        "title: " +
                        QuoteYamlString(title);

                    File.WriteAllText(
                        file,
                        string.Join(newline, lines),
                        new UTF8Encoding(
                            encoderShouldEmitUTF8Identifier: false));

                    return;
                }
            }

            // title не найден.
            //
            // Добавляем его сразу после открывающего "---".
            string[] newLines = new string[lines.Length + 1];

            newLines[0] = lines[0];
            newLines[1] =
                "title: " +
                QuoteYamlString(title);

            Array.Copy(
                lines,
                1,
                newLines,
                2,
                lines.Length - 1);

            File.WriteAllText(
                file,
                string.Join(newline, newLines),
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));
        }

        /// <summary>
        /// Определяет, является ли строка YAML-свойством
        /// с указанным именем.
        /// </summary>
        private static bool IsYamlProperty(
            string line,
            string propertyName)
        {
            return line.StartsWith(
                       propertyName + ":",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Читает первый элемент YAML-списка <c>aliases</c>.
        /// </summary>
        /// <remarks>
        /// Ожидаемый формат:
        ///
        /// <code>
        /// ---
        /// aliases:
        ///   - "Название"
        ///   - "Другое название"
        /// ---
        /// </code>
        ///
        /// Возвращается только первый элемент списка.
        /// </remarks>
        private static string? ReadFirstAlias(string file)
        {
            using StreamReader reader = new StreamReader(
                file,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true));

            string? line = reader.ReadLine();

            // YAML frontmatter должен начинаться с "---".
            if (line?.Trim() != "---")
            {
                return null;
            }

            bool inAliases = false;

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                // Конец YAML frontmatter.
                if (trimmed == "---")
                {
                    break;
                }

                if (trimmed == "aliases:")
                {
                    inAliases = true;
                    continue;
                }

                if (!inAliases)
                {
                    continue;
                }

                // Первый элемент списка aliases.
                if (trimmed.StartsWith("-"))
                {
                    string value =
                        trimmed.Substring(1).Trim();

                    return UnquoteYamlString(value);
                }

                // Начался следующий YAML-ключ.
                if (!line.StartsWith(" ") &&
                    !line.StartsWith("\t"))
                {
                    inAliases = false;
                }
            }

            return null;
        }

        /// <summary>
        /// Удаляет окружающие кавычки YAML-строки.
        /// </summary>
        private static string UnquoteYamlString(
            string value)
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
        /// Формирует YAML-строку в двойных кавычках.
        /// </summary>
        private static string QuoteYamlString(
            string value)
        {
            return "\"" +
                value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"") +
                "\"";
        }

        /// <summary>
        /// Удаляет из имени только символы, недопустимые
        /// файловой системой.
        /// </summary>
        /// <remarks>
        /// ASCII-фильтрация не используется.
        ///
        /// Поэтому кириллица и другие допустимые Unicode-символы
        /// сохраняются.
        /// </remarks>
        private static string SanitizeFileName(
            string name)
        {
            foreach (char invalidChar
                in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(
                    invalidChar.ToString(),
                    string.Empty);
            }

            // Windows запрещает пробелы и точки
            // в конце имени.
            name = name.TrimEnd(
                ' ',
                '.');

            // "." и ".." имеют специальное значение.
            if (name == "." ||
                name == "..")
            {
                return string.Empty;
            }

            return name;
        }

        /// <summary>
        /// Кодирует имя файла для использования
        /// в Markdown-ссылке.
        /// </summary>
        /// <remarks>
        /// Например:
        ///
        /// <code>
        /// Multi-target 23 26.md
        /// </code>
        ///
        /// превращается в:
        ///
        /// <code>
        /// Multi-target%2023%2026.md
        /// </code>
        ///
        /// Unicode-символы сохраняются.
        /// </remarks>
        private static string UrlEncodeFileName(
            string fileName)
        {
            StringBuilder builder = new StringBuilder();

            foreach (char c in fileName)
            {
                switch (c)
                {
                    case ' ':
                        builder.Append("%20");
                        break;

                    case '#':
                        builder.Append("%23");
                        break;

                    case '?':
                        builder.Append("%3F");
                        break;

                    case '%':
                        builder.Append("%25");
                        break;

                    case '+':
                        builder.Append("%2B");
                        break;

                    case '(':
                        builder.Append("%28");
                        break;

                    case ')':
                        builder.Append("%29");
                        break;

                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Разбивает текст на строки без потери пустых строк.
        /// </summary>
        private static string[] SplitLines(
            string text)
        {
            return text.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);
        }

        /// <summary>
        /// Определяет перенос строки, используемый в файле.
        /// </summary>
        private static string DetectNewLine(
            string text)
        {
            int index = text.IndexOf('\n');

            if (index < 0)
            {
                return Environment.NewLine;
            }

            if (index > 0 &&
                text[index - 1] == '\r')
            {
                return "\r\n";
            }

            return "\n";
        }

        /// <summary>
        /// Операция переименования файла.
        /// </summary>
        private sealed class RenameOperation
        {
            /// <summary>
            /// Создаёт операцию переименования.
            /// </summary>
            /// <param name="newFile">
            /// Новое полное имя файла.
            /// </param>
            /// <param name="title">
            /// Новое значение YAML-свойства <c>title</c>.
            /// </param>
            public RenameOperation(
                string newFile,
                string title)
            {
                NewFile = newFile;
                Title = title;
            }

            /// <summary>
            /// Новое полное имя файла.
            /// </summary>
            public string NewFile { get; }

            /// <summary>
            /// Новое значение YAML-свойства <c>title</c>.
            /// </summary>
            public string Title { get; }
        }
    }

    /// <summary>
    /// Результат работы <see cref="ConvovizRenamer"/>.
    /// </summary>
    public sealed class RenameResult
    {
        /// <summary>
        /// Количество обработанных Markdown-файлов.
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
        /// Количество пропущенных файлов.
        /// </summary>
        public int Skipped { get; internal set; }

        /// <summary>
        /// Количество конфликтов имён.
        /// </summary>
        public int Conflicts { get; internal set; }

        /// <summary>
        /// Количество ошибок.
        /// </summary>
        public int Errors { get; internal set; }

        /// <summary>
        /// Количество обновлённых <c>_index.md</c>.
        /// </summary>
        public int IndexFilesUpdated { get; internal set; }
    }
}