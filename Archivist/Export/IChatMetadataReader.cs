namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Читает метаданные разговора из Markdown-файла.
/// </summary>
internal interface IChatMetadataReader
{
    ChatMetadata Read(string filePath);
}

/// <summary>
/// Сервис чтения YAML-метаданных.
/// </summary>
internal sealed class ChatMetadataReaderService : IChatMetadataReader
{
    public ChatMetadata Read(string filePath) => ChatMetadataReader.Read(filePath);
}
