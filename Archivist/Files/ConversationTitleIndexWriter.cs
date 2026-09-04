using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;
using System.Text;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>Создаёт файл <c>_index.md</c>, используя aliases, title или имя файла.</summary>
internal sealed class ConversationTitleIndexWriter : IConversationIndexWriter
{
    private const string IndexFileName = "_index.md";
    private readonly IFileSystem _fileSystem;
    private readonly ConversationDisplayNameProvider _displayNameProvider;

    public ConversationTitleIndexWriter(
        IFileSystem fileSystem,
        IChatMetadataReader metadataReader)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _displayNameProvider = new ConversationDisplayNameProvider(metadataReader);
    }

    public void Refresh(string directory)
    {
        string year = Directory.GetParent(directory)?.Name ?? string.Empty;
        string month = new DirectoryInfo(directory).Name;
        int separator = month.IndexOf('-');
        string monthName = separator >= 0 ? month[(separator + 1)..] : month;

        IEnumerable<string> files = _fileSystem
            .EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(
                Path.GetFileName(path), IndexFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

        StringBuilder contents = new();
        _ = contents.AppendLine($"# {monthName} {year}");
        _ = contents.AppendLine();
        _ = contents.AppendLine("## Conversations");
        _ = contents.AppendLine();

        foreach (string path in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string visibleName = _displayNameProvider.Get(path, fileName);
            string encodedFileName = Uri.EscapeDataString(Path.GetFileName(path));
            _ = contents.AppendLine($"- [{visibleName}]({encodedFileName})");
        }

        _fileSystem.WriteAllText(Path.Combine(directory, IndexFileName), contents.ToString());
    }
}
