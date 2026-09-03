using System.Text.RegularExpressions;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Нормализует имя экспортируемого файла.
/// </summary>
internal interface IFileNameNormalizer
{
    /// <summary>
    /// Нормализует имя файла.
    /// </summary>
    /// <param name="fileName">Имя файла для нормализации.</param>
    /// <returns>Нормализованное имя файла.</returns>
    string Normalize(string fileName);
}

/// <summary>
/// Нормализует имена файлов по правилам Archivist.
/// </summary>
internal sealed class FileNameNormalizer : IFileNameNormalizer
{
    /// <summary>Регулярное выражение для поиска последовательностей пробельных символов.</summary>
    private static readonly Regex MultipleSpacesRegex = new(
        @"\s+",
        RegexOptions.Compiled);

    /// <summary>
    /// Нормализует имя файла, заменяя подчёркивания пробелами и убирая лишние пробелы.
    /// </summary>
    /// <param name="fileName">Имя файла.</param>
    /// <returns>Нормализованное имя файла.</returns>
    public string Normalize(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        // Символы подчёркивания и # в экспортных именах заменяются пробелами.
        // # в имени файла Obsidian трактует как начало block/heading-ссылки.
        string normalized = fileName
            .Replace('_', ' ')
            .Replace('#', ' ');
        normalized = MultipleSpacesRegex.Replace(normalized, " ");
        return normalized.Trim();
    }
}
