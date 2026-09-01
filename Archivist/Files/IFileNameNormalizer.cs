using System.Text.RegularExpressions;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Нормализует имя экспортируемого файла.
/// </summary>
internal interface IFileNameNormalizer
{
    string Normalize(string fileName);
}

/// <summary>
/// Нормализует имена файлов по правилам Archivist.
/// </summary>
internal sealed class FileNameNormalizer : IFileNameNormalizer
{
    private static readonly Regex MultipleSpacesRegex = new(
        @"\s+",
        RegexOptions.Compiled);

    public string Normalize(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        // Символы подчёркивания в экспортных именах заменяются пробелами.
        string normalized = fileName.Replace('_', ' ');
        normalized = MultipleSpacesRegex.Replace(normalized, " ");
        return normalized.Trim();
    }
}
