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
    private readonly List<ExportError> _archiveErrors = new();
    private readonly List<ExportError> _markdownErrors = new();

    public int Total { get; private set; }

    public int Skipped { get; private set; }

    public int Added { get; private set; }

    public int Updated { get; private set; }

    public int Failed { get; private set; }

    /// <summary>
    /// Количество архивов, которые не удалось прочитать или распаковать.
    /// </summary>
    public int ArchiveFailed { get; private set; }

    public IReadOnlyList<ExportError> ArchiveErrors => _archiveErrors;

    public IReadOnlyList<ExportError> MarkdownErrors => _markdownErrors;

    /// <summary>
    /// Добавляет структурированный результат операции.
    /// </summary>
    public void Add(FileOperationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Status == FileOperationStatus.Failed)
        {
            AddFailure();
            Total += result.IndexReadErrors;
            Failed += result.IndexReadErrors;
            AddErrors(result.Errors);
            return;
        }

        Total++;
        Total += result.IndexReadErrors;
        Failed += result.IndexReadErrors;
        AddErrors(result.Errors);
        switch (result.Status)
        {
            case FileOperationStatus.Skipped:
                Skipped++;
                break;
            case FileOperationStatus.Added:
                Added++;
                break;
            case FileOperationStatus.AddedUnique:
                // Альтернативное имя — техническая деталь разрешения
                // конфликта пути, а не отдельный вид бизнес-операции.
                Added++;
                break;
            case FileOperationStatus.Updated:
                Updated++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result), result.Status, null);
        }
    }

    private void AddErrors(IReadOnlyList<ExportError>? errors)
    {
        if (errors is null)
        {
            return;
        }

        foreach (ExportError error in errors)
        {
            AddMarkdownError(error);
        }
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
        ArchiveFailed++;
    }

    public void AddArchiveError(ExportError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArchiveFailed++;
        _archiveErrors.Add(error);
    }

    public void AddMarkdownError(ExportError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Failed++;
        Total++;
        _markdownErrors.Add(error);
    }

    /// <summary>
    /// Объединяет статистику отдельного архива с общей статистикой.
    /// </summary>
    public void Add(ExportStatistics statistics)
    {
        Total += statistics.Total;
        Skipped += statistics.Skipped;
        Added += statistics.Added;
        Updated += statistics.Updated;
        Failed += statistics.Failed;
        ArchiveFailed += statistics.ArchiveFailed;
        _archiveErrors.AddRange(statistics.ArchiveErrors);
        _markdownErrors.AddRange(statistics.MarkdownErrors);
    }

    public ExportResult ToResult() =>
        new(
            Total,
            Skipped,
            Added,
            Updated,
            Failed,
            ArchiveFailed,
            ArchiveErrors.ToArray(),
            MarkdownErrors.ToArray());
}
