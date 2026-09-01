namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Читает метаданные разговора из Markdown-файла.
/// </summary>
internal interface IChatMetadataReader
{
    ChatMetadata Read(string filePath);
}

/// <summary>
/// Адаптер над текущим YAML-читалем метаданных.
/// </summary>
internal sealed class ChatMetadataReaderAdapter : IChatMetadataReader
{
    public ChatMetadata Read(string filePath) => ChatMetadataReader.Read(filePath);
}
