using System;
using System.Collections.Generic;
using System.IO;

namespace dRz.GPT_Utilities.NexusRenamer
{
    /// <summary>
    /// Выполняет переименование Markdown-файлов по значению заголовка
    /// <c># Title:</c>.
    /// </summary>
    /// <remarks>
    /// Ожидаемый формат строки:
    /// <code>
    /// # Title: Детерминированное разведение логов
    /// </code>
    /// После обработки файл будет переименован в:
    /// <code>
    /// Детерминированное разведение логов.md
    /// </code>
    /// Поиск выполняется рекурсивно по всем подкаталогам.
    /// </remarks>
    public static class NexusRenamer
    {
        private const string TitlePrefix = "# Title:";

        /// <summary>
        /// Рекурсивно переименовывает все Markdown-файлы в указанном каталоге
        /// согласно значению строки <c># Title:</c>.
        /// </summary>
        /// <param name="directory">
        /// Каталог, с которого начинается поиск.
        /// </param>
        /// <returns>
        /// Список успешно выполненных переименований.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">
        /// Каталог не существует.
        /// </exception>
        public static IReadOnlyList<RenameResult> Rename(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }

            List<RenameResult> result = new();

            foreach (string file in Directory.EnumerateFiles(
                         directory,
                         "*.md",
                         SearchOption.AllDirectories))
            {
                string? title = ReadTitle(file);

                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                string newName = SanitizeFileName(title) + ".md";
                string newPath = Path.Combine(Path.GetDirectoryName(file)!, newName);

                if (string.Equals(file, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (File.Exists(newPath))
                {
                    continue;
                }

                File.Move(file, newPath);

                result.Add(new RenameResult(file, newPath));
            }

            return result;
        }

        /// <summary>
        /// Считывает значение строки <c># Title:</c>.
        /// </summary>
        /// <param name="file">Markdown-файл.</param>
        /// <returns>
        /// Найденный заголовок или <see langword="null"/>, если строка отсутствует.
        /// </returns>
        private static string? ReadTitle(string file)
        {
            foreach (string line in File.ReadLines(file))
            {
                if (line.StartsWith(TitlePrefix, StringComparison.Ordinal))
                {
                    return line.Substring(TitlePrefix.Length).Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Удаляет из имени файла недопустимые символы.
        /// </summary>
        /// <param name="name">Исходное имя.</param>
        /// <returns>Допустимое имя файла.</returns>
        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c.ToString(), "");
            }

            return name.Trim();
        }
    }

    /// <summary>
    /// Результат переименования файла.
    /// </summary>
    /// <param name="OldPath">Исходный путь.</param>
    /// <param name="NewPath">Новый путь.</param>
    public sealed record RenameResult(
        string OldPath,
        string NewPath);
}