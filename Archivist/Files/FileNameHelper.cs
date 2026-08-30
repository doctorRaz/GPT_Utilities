using System.Text.RegularExpressions;

namespace dRz.GPT_Utilities.Archivist.Files
{
    internal class FileNameHelper
    {
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

        internal static string Normalize(string fileName)
        {
            // "_" -> " "
            fileName = fileName.Replace('_', ' ');

            // Несколько пробельных символов подряд -> один пробел.
            //
            // Например:
            //
            // Проверка___Staged__Diff`
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

            return fileName;
        }

        /// <summary>
        /// Максимальный номер суффикса при совпадении имён.
        /// </summary>
        private const int MaxDuplicateNumber = 100;

        internal static IEnumerable<string> GetExistingDuplicates(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath)
                ?? throw new InvalidOperationException(
                    $"Не удалось определить каталог: {filePath}");
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            for (int number = 1; number <= MaxDuplicateNumber; number++)
            {
                string candidate = Path.Combine(
                    directory,
                    $"{fileName} ({number}){extension}");
                if (File.Exists(candidate))
                {
                    yield return candidate;
                }
            }
        }

        /// <summary>
        /// Возвращает свободное имя файла.
        /// Если:
        ///     Test.md
        /// уже существует:
        ///     Test (1).md
        ///     Test (2).md
        ///     ...
        ///     Test (100).md
        /// Если все варианты заняты, выбрасывается IOException.
        /// </summary>
        internal static string GetUnique(string filePath)
        {
            // Исходное имя свободно.
            if (!File.Exists(filePath))
            {
                return filePath;
            }

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
                {
                    return candidate;
                }
            }

            throw new IOException(
                $"Не удалось подобрать свободное имя для файла: " +
                $"{filePath}. " +
                $"Заняты варианты от (1) до ({MaxDuplicateNumber}).");
        }
    }
}