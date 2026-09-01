using dRz.GPT_Utilities.Archivist.Files;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Внутренний аккумулятор статистики обработки.
/// </summary>
/// <remarks>
/// Изменяемость нужна только во время обработки. Наружу возвращается
/// неизменяемый <see cref="ExportResult"/>.
/// </remarks>
internal sealed class ExportStatistics
{
    public int Total { get; private set; }

    public int Skipped { get; private set; }

    public int Added { get; private set; }

    public int AddedUnique { get; private set; }

    public int Updated { get; private set; }

    public int Failed { get; private set; }

    /// <summary>
    /// Добавляет результат обработки одного Markdown-файла.
    /// </summary>
    public void Add(FileCopyDecision decision)
    {
        Total++;

        switch (decision)
        {
            case FileCopyDecision.Skip:
                Skipped++;
                break;
            case FileCopyDecision.Add:
                Added++;
                break;
            case FileCopyDecision.AddUnique:
                AddedUnique++;
                break;
            case FileCopyDecision.Replace:
                Updated++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(decision), decision, null);
        }
    }

    /// <summary>
    /// Добавляет структурированный результат операции.
    /// </summary>
    public void Add(FileOperationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status == FileOperationStatus.Failed)
        {
            AddFailure();
            return;
        }

        Add(result.Status switch
        {
            FileOperationStatus.Skipped => FileCopyDecision.Skip,
            FileOperationStatus.Added => FileCopyDecision.Add,
            FileOperationStatus.AddedUnique => FileCopyDecision.AddUnique,
            FileOperationStatus.Updated => FileCopyDecision.Replace,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null)
        });
    }

    /// <summary>
    /// Регистрирует ошибку обработки файла.
    /// </summary>
    public void AddFailure()
    {
        Total++;
        Failed++;
    }

    /// <summary>
    /// Регистрирует ошибку архива, в котором не удалось начать обработку.
    /// Количество Markdown-файлов не увеличивается, поскольку они не были
    /// обработаны.
    /// </summary>
    public void AddArchiveFailure()
    {
        Failed++;
    }

    /// <summary>
    /// Объединяет статистику отдельного архива с общей статистикой.
    /// </summary>
    public void Add(ExportStatistics statistics)
    {
        Total += statistics.Total;
        Skipped += statistics.Skipped;
        Added += statistics.Added;
        AddedUnique += statistics.AddedUnique;
        Updated += statistics.Updated;
        Failed += statistics.Failed;
    }

    public ExportResult ToResult() =>
        new(Total, Skipped, Added, AddedUnique, Updated, Failed);
}
