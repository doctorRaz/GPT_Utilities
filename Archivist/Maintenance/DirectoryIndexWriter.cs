using System.Globalization;
using System.Text;
using dRz.GPT_Utilities.Archivist.Files;

namespace dRz.GPT_Utilities.Archivist.Maintenance;

/// <summary>Перестраивает пользовательские навигационные индексы vault.</summary>
internal sealed class DirectoryIndexWriter
{
    private const string IndexFileName = "_index.md";
    private readonly IFileSystem _fileSystem;
    private readonly ConversationDisplayNameProvider _displayNameProvider;

    public DirectoryIndexWriter(
        IFileSystem fileSystem,
        ConversationDisplayNameProvider displayNameProvider)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _displayNameProvider = displayNameProvider ?? throw new ArgumentNullException(nameof(displayNameProvider));
    }

    public void Rebuild(string root, ArchiveMaintenanceResult result)
    {
        string[] years = _fileSystem.EnumerateDirectories(root, SearchOption.TopDirectoryOnly)
            .Where(IsYearDirectory).OrderByDescending(Path.GetFileName, StringComparer.Ordinal).ToArray();

        foreach (string year in years)
        {
            string[] months = _fileSystem.EnumerateDirectories(year, SearchOption.TopDirectoryOnly)
                .Where(IsMonthDirectory).OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray();
            foreach (string month in months)
            {
                string[] conversations = ConversationFiles(month);
                string index = Path.Combine(month, IndexFileName);
                if (conversations.Length > 0 || _fileSystem.FileExists(index))
                    WriteIfChanged(index, BuildMonth(month, year, conversations), result);
            }
            WriteIfChanged(Path.Combine(year, IndexFileName), BuildYear(year, months), result);
        }
        WriteIfChanged(Path.Combine(root, IndexFileName), BuildRoot(years), result);
    }

    private string[] ConversationFiles(string directory) => _fileSystem
        .EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
        .Where(path => !string.Equals(Path.GetFileName(path), IndexFileName, StringComparison.OrdinalIgnoreCase))
        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private string BuildMonth(string monthDirectory, string yearDirectory, string[] files)
    {
        string month = Path.GetFileName(monthDirectory);
        int separator = month.IndexOf('-');
        string monthName = separator >= 0 ? month[(separator + 1)..] : month;
        StringBuilder text = new();
        _ = text.AppendLine($"# {monthName} {Path.GetFileName(yearDirectory)}");
        _ = text.AppendLine();
        _ = text.AppendLine("## Conversations");
        _ = text.AppendLine();
        foreach (string file in files)
        {
            string fallback = Path.GetFileNameWithoutExtension(file);
            string visibleName = _displayNameProvider.Get(file, fallback);
            _ = text.AppendLine($"- [{visibleName}]({Uri.EscapeDataString(Path.GetFileName(file))})");
        }
        return text.ToString();
    }

    private static string BuildYear(string year, string[] months)
    {
        StringBuilder text = new();
        _ = text.AppendLine($"# {Path.GetFileName(year)}");
        _ = text.AppendLine();
        _ = text.AppendLine("## Months");
        _ = text.AppendLine();
        foreach (string month in months)
        {
            string name = Path.GetFileName(month);
            int separator = name.IndexOf('-');
            string label = separator >= 0 ? name[(separator + 1)..] : name;
            _ = text.AppendLine($"- [{label}]({name}/{IndexFileName})");
        }
        return text.ToString();
    }

    private static string BuildRoot(string[] years)
    {
        StringBuilder text = new();
        _ = text.AppendLine("# ChatGPT Conversations");
        _ = text.AppendLine();
        _ = text.AppendLine("## Years");
        _ = text.AppendLine();
        foreach (string year in years)
        {
            string name = Path.GetFileName(year);
            _ = text.AppendLine($"- [{name}]({name}/{IndexFileName})");
        }
        return text.ToString();
    }

    private void WriteIfChanged(string path, string contents, ArchiveMaintenanceResult result)
    {
        if (_fileSystem.FileExists(path) && string.Equals(_fileSystem.ReadAllText(path), contents, StringComparison.Ordinal))
            return;
        _fileSystem.WriteAllText(path, contents);
        result.UpdatedIndexes++;
    }

    private static bool IsYearDirectory(string path) =>
        int.TryParse(Path.GetFileName(path), NumberStyles.None, CultureInfo.InvariantCulture, out int year) && year is >= 1000 and <= 9999;

    private static bool IsMonthDirectory(string path)
    {
        string name = Path.GetFileName(path);
        return name.Length >= 4 && name[2] == '-' && int.TryParse(name[..2], out int month) && month is >= 1 and <= 12;
    }
}
