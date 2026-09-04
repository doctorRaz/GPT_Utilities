using dRz.GPT_Utilities.Archivist.Export;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>Определяет отображаемое имя разговора для навигационного индекса.</summary>
internal sealed class ConversationDisplayNameProvider
{
    private readonly IChatMetadataReader _metadataReader;

    public ConversationDisplayNameProvider(IChatMetadataReader metadataReader)
    {
        _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
    }

    public string Get(string path, string fallback)
    {
        try
        {
            ChatMetadata metadata = _metadataReader.Read(path);

            string? alias = metadata.Aliases?
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            if (!string.IsNullOrWhiteSpace(alias))
                return alias.Trim();

            if (!string.IsNullOrWhiteSpace(metadata.Title))
                return metadata.Title.Trim();
        }
        catch (FormatException)
        {
            // При невалидном YAML используем имя файла.
        }

        return fallback;
    }
}
