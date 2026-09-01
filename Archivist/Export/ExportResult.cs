namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Неизменяемый результат обработки экспортных архивов.
/// </summary>
internal sealed record ExportResult(
    int Total,
    int Skipped,
    int Added,
    int AddedUnique,
    int Updated,
    int Failed)
{
    /// <summary>
    /// Общее количество успешно добавленных или обновлённых файлов.
    /// </summary>
    public int AddedOrUpdated => Added + AddedUnique + Updated;
}
