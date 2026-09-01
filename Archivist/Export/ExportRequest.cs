namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Независимый от командной строки запрос на обработку экспорта.
/// </summary>
/// <remarks>
/// Этот тип отделяет use case экспорта от способа, которым пользователь
/// передал параметры. В дальнейшем процессор сможет вызываться не только
/// из консольного приложения.
/// </remarks>
internal sealed record ExportRequest(
    string SourceDirectory,
    string DestinationDirectory,
    string ZipFilePattern,
    bool ProcessAllArchives);
