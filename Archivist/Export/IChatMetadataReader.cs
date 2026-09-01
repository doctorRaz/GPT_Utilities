namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Читает метаданные разговора из Markdown-файла.
/// </summary>
internal interface IChatMetadataReader
{
    ChatMetadata Read(string filePath);
}

