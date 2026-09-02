using dRz.GPT_Utilities.Archivist.Export;
using dRz.GPT_Utilities.Archivist.Infrastructure;

namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Применяет политику добавления, обновления и пропуска файла.
/// </summary>
internal interface IFileSynchronizer
{
    /// <summary>
    /// Синхронизирует исходный файл с целевым, применяя соответствующую политику (добавление, обновление или пропуск).
    /// </summary>
    /// <param name="sourceFilePath">Путь к исходному файлу.</param>
    /// <param name="destinationFilePath">Путь к целевому файлу.</param>
    /// <param name="sourceMetadata">Метаданные исходного файла.</param>
    /// <returns>Результат операции синхронизации.</returns>
    FileOperationResult Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata);
}

/// <summary>
/// Экземплярный сервис синхронизации Markdown-файлов.
/// </summary>
internal sealed class FileSynchronizerService : IFileSynchronizer
{
    /// <summary>Средство чтения метаданных.</summary>
    private readonly IChatMetadataReader _metadataReader;
    /// <summary>Журналировщик.</summary>
    private readonly IArchivistLogger _logger;
    /// <summary>Провайдер уникальных имен.</summary>
    private readonly IUniqueFileNameProvider _uniqueFileNameProvider;
    /// <summary>Система файловых операций.</summary>
    private readonly IFileSystem _fileSystem;
    /// <summary>Индекс разговоров.</summary>
    private readonly IConversationIndex _conversationIndex;
    private static readonly object SynchronizationLock = new();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="FileSynchronizerService"/>.
    /// </summary>
    /// <param name="metadataReader">Средство чтения метаданных.</param>
    /// <param name="logger">Журналировщик.</param>
    /// <param name="uniqueFileNameProvider">Провайдер уникальных имен.</param>
    /// <param name="fileSystem">Система файловых операций.</param>
    /// <param name="conversationIndex">Индекс разговоров (необязательно).</param>
    public FileSynchronizerService(
        IChatMetadataReader metadataReader,
        IArchivistLogger logger,
        IUniqueFileNameProvider uniqueFileNameProvider,
        IFileSystem fileSystem,
        IConversationIndex? conversationIndex = null)
    {
        _metadataReader = metadataReader
            ?? throw new ArgumentNullException(nameof(metadataReader));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
        _uniqueFileNameProvider = uniqueFileNameProvider
            ?? throw new ArgumentNullException(nameof(uniqueFileNameProvider));
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
        _conversationIndex = conversationIndex ?? new ConversationIndex(
            fileSystem,
            metadataReader,
            logger);
    }

    /// <summary>
    /// Синхронизирует исходный файл с целевым, обрабатывая конфликты имен и обеспечивая консистентность индекса.
    /// </summary>
    /// <param name="sourceFilePath">Путь к исходному файлу.</param>
    /// <param name="destinationFilePath">Путь к целевому файлу.</param>
    /// <param name="sourceMetadata">Метаданные исходного файла.</param>
    /// <returns>Результат операции синхронизации.</returns>
    public FileOperationResult Synchronize(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        lock (SynchronizationLock)
        {
            return SynchronizeCore(
                sourceFilePath,
                destinationFilePath,
                sourceMetadata);
        }
    }

    private FileOperationResult SynchronizeCore(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        ArgumentNullException.ThrowIfNull(sourceMetadata);

        string destinationDirectory = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException(
                $"Не удалось определить каталог: {destinationFilePath}");
        _conversationIndex.EnsureIndexed(destinationDirectory);

        if (sourceMetadata.ConversationId is Guid sourceConversationId)
        {
            return SynchronizeConversation(
                sourceFilePath,
                destinationFilePath,
                sourceMetadata,
                sourceConversationId,
                destinationDirectory);
        }

        FileOperationStatus status = GetOperationStatus(
            destinationFilePath,
            sourceMetadata);

        if (status == FileOperationStatus.AddedUnique)
        {
            string? matchingPath = FindMatchingDuplicate(
                destinationFilePath,
                sourceMetadata.ConversationId);

            if (matchingPath is null)
            {
                FileOperationResult uniqueResult = CopyToUniqueName(
                    sourceFilePath,
                    destinationFilePath,
                    sourceMetadata);
                WriteOperationResult(uniqueResult);
                return uniqueResult;
            }

            else
            {
                destinationFilePath = matchingPath;
                status = GetOperationStatus(destinationFilePath, sourceMetadata);
            }
        }

        if (status == FileOperationStatus.Added)
        {
            if (!_fileSystem.TryCopyFile(sourceFilePath, destinationFilePath))
            {
                // Файл появился после проверки. Не перезаписываем его,
                // а продолжаем обработку как конфликт имён.
                FileOperationResult uniqueResult = CopyToUniqueName(
                    sourceFilePath,
                    destinationFilePath,
                    sourceMetadata);
                WriteOperationResult(uniqueResult);
                return uniqueResult;
            }

            SetLastWriteTimeIfPresent(destinationFilePath, sourceMetadata);
            _conversationIndex.Track(destinationFilePath, sourceMetadata);
        }
        else if (status != FileOperationStatus.Skipped)
        {
            _fileSystem.CopyFile(sourceFilePath, destinationFilePath, overwrite: true);
            SetLastWriteTimeIfPresent(destinationFilePath, sourceMetadata);
            _conversationIndex.Track(destinationFilePath, sourceMetadata);
        }

        FileOperationResult result = new(
            status,
            sourceFilePath,
            destinationFilePath,
            status == FileOperationStatus.Skipped
                ? "Файл не требует обновления."
                : null);

        WriteOperationResult(result);
        return result;
    }

    private FileOperationResult SynchronizeConversation(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata,
        Guid sourceConversationId,
        string destinationDirectory)
    {
        DateTimeOffset sourceUpdateTime = sourceMetadata.UpdateTime ?? default;
        List<string> stalePaths = new();
        bool hasNewerVersion = false;

        foreach (string path in _conversationIndex.FindPaths(
            sourceConversationId,
            destinationDirectory))
        {
            if (!_conversationIndex.TryGet(path, out ChatMetadata existingMetadata))
            {
                existingMetadata = _metadataReader.Read(path);
                _conversationIndex.Track(path, existingMetadata);
            }

            DateTimeOffset existingUpdateTime = existingMetadata.UpdateTime ?? default;
            if (existingUpdateTime <= sourceUpdateTime)
            {
                stalePaths.Add(path);
            }
            else
            {
                hasNewerVersion = true;
            }
        }

        if (hasNewerVersion)
        {
            DeleteStaleVersions(stalePaths, preservedPath: null);

            FileOperationResult skippedResult = new(
                FileOperationStatus.Skipped,
                sourceFilePath,
                destinationFilePath,
                "Существует более новая версия разговора.");
            WriteOperationResult(skippedResult);
            return skippedResult;
        }

        FileOperationResult? uniqueResult = null;
        bool copied;
        if (_fileSystem.FileExists(destinationFilePath))
        {
            if (_conversationIndex.TryGet(
                    destinationFilePath,
                    out ChatMetadata destinationMetadata) &&
                destinationMetadata.ConversationId == sourceConversationId)
            {
                _fileSystem.CopyFile(
                    sourceFilePath,
                    destinationFilePath,
                    overwrite: true);
                copied = true;
            }
            else
            {
                uniqueResult = CopyToUniqueName(
                    sourceFilePath,
                    destinationFilePath,
                    sourceMetadata);
                copied = true;
            }
        }
        else
        {
            copied = _fileSystem.TryCopyFile(
                sourceFilePath,
                destinationFilePath);
            if (!copied)
            {
                ChatMetadata racedMetadata = _metadataReader.Read(destinationFilePath);
                _conversationIndex.Track(destinationFilePath, racedMetadata);
                if (racedMetadata.ConversationId == sourceConversationId)
                {
                    return SynchronizeConversation(
                        sourceFilePath,
                        destinationFilePath,
                        sourceMetadata,
                        sourceConversationId,
                        destinationDirectory);
                }

                uniqueResult = CopyToUniqueName(
                    sourceFilePath,
                    destinationFilePath,
                    sourceMetadata);
                copied = true;
            }
        }

        if (!copied)
        {
            throw new IOException(
                $"Не удалось скопировать файл: {sourceFilePath}");
        }

        string actualDestinationPath = uniqueResult?.DestinationPath
            ?? destinationFilePath;
        SetLastWriteTimeIfPresent(actualDestinationPath, sourceMetadata);
        _conversationIndex.Track(actualDestinationPath, sourceMetadata);
        DeleteStaleVersions(stalePaths, actualDestinationPath);

        FileOperationResult result = uniqueResult is not null
            ? new FileOperationResult(
                FileOperationStatus.Added,
                sourceFilePath,
                actualDestinationPath)
            : new FileOperationResult(
                stalePaths.Count == 0
                    ? FileOperationStatus.Added
                    : FileOperationStatus.Updated,
                sourceFilePath,
                actualDestinationPath);
        WriteOperationResult(result);
        return result;
    }

    private void DeleteStaleVersions(
        IEnumerable<string> stalePaths,
        string? preservedPath)
    {
        foreach (string path in stalePaths)
        {
            if (preservedPath is not null &&
                string.Equals(path, preservedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _fileSystem.DeleteFile(path);
            _conversationIndex.Remove(path);
        }
    }

    /// <summary>
    /// Копирует файл, пытаясь подобрать уникальное имя, если целевое занято или приводит к дубликату.
    /// </summary>
    /// <param name="sourceFilePath">Путь к исходному файлу.</param>
    /// <param name="destinationFilePath">Базовый путь назначения.</param>
    /// <param name="sourceMetadata">Метаданные исходного файла.</param>
    /// <returns>Результат операции с уникальным путем.</returns>
    private FileOperationResult CopyToUniqueName(
        string sourceFilePath,
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        HashSet<string> attemptedPaths = new(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            string candidate = _uniqueFileNameProvider.GetUnique(
                destinationFilePath,
                attemptedPaths);

            attemptedPaths.Add(candidate);

            if (!_fileSystem.TryCopyFile(sourceFilePath, candidate))
            {
                // Имя заняли после проверки. Выбираем следующий кандидат.
                continue;
            }

            SetLastWriteTimeIfPresent(candidate, sourceMetadata);
            _conversationIndex.Track(candidate, sourceMetadata);

            return new FileOperationResult(
                FileOperationStatus.AddedUnique,
                sourceFilePath,
                candidate,
                null);
        }
    }

    private void WriteOperationResult(FileOperationResult result)
    {
        string sourceFileName = Path.GetFileName(result.SourcePath);
        string description =
            $"{sourceFileName}\n\t\tto->{result.DestinationPath}";

        switch (result.Status)
        {
            case FileOperationStatus.Skipped:
                _logger.Trace($"\tПропущен: {description}");
                break;
            case FileOperationStatus.Added:
                _logger.Success($"\tДобавлен: {description}");
                break;
            case FileOperationStatus.AddedUnique:
                _logger.Warning($"\tДобавлен уникальный: {description}");
                break;
            case FileOperationStatus.Updated:
                _logger.Update($"\tОбновлён: {description}");
                break;
            case FileOperationStatus.Failed:
                _logger.Error($"\tОшибка: {description}", result.Error);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(result.Status),
                    result.Status,
                    null);
        }
    }

    /// <summary>
    /// Устанавливает время последней записи файла, если в метаданных указана дата создания.
    /// </summary>
    /// <param name="destinationFilePath">Путь к файлу.</param>
    /// <param name="sourceMetadata">Метаданные файла.</param>
    private void SetLastWriteTimeIfPresent(
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        if (sourceMetadata.UpdateTime.HasValue)
        {
            _fileSystem.SetLastWriteTime(
                destinationFilePath,
                sourceMetadata.UpdateTime.Value.LocalDateTime);
        }
    }

    /// <summary>
    /// Ищет файл с тем же идентификатором разговора, который может конфликтовать с текущим путем.
    /// </summary>
    /// <param name="destinationFilePath">Путь к целевому файлу.</param>
    /// <param name="sourceId">ID разговора.</param>
    /// <returns>Путь к найденному дубликату или null, если не найден.</returns>
    /// <summary>
    /// Ищет файл с тем же идентификатором разговора, который может конфликтовать с текущим путем.
    /// </summary>
    /// <param name="destinationFilePath">Путь к целевому файлу.</param>
    /// <param name="sourceId">ID разговора.</param>
    /// <returns>Путь к найденному дубликату или null, если не найден.</returns>
    private string? FindMatchingDuplicate(
        string destinationFilePath,
        Guid? sourceId)
    {
        if (sourceId is null)
        {
            return null;
        }

        string? matchingPath = null;
        DateTimeOffset? matchingUpdateTime = null;

        string directory = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException(
                $"Не удалось определить каталог: {destinationFilePath}");

        foreach (string duplicatePath in _conversationIndex.FindPaths(
            sourceId.Value,
            directory))
        {
            if (!_conversationIndex.TryGet(duplicatePath, out ChatMetadata metadata))
            {
                continue;
            }

            if (metadata.UpdateTime > matchingUpdateTime || matchingPath is null)
            {
                matchingPath = duplicatePath;
                matchingUpdateTime = metadata.UpdateTime;
            }
        }

        return matchingPath;
    }

    /// <summary>
    /// Определяет статус операции на основе состояния целевого файла и метаданных.
    /// </summary>
    /// <param name="destinationFilePath">Путь к целевому файлу.</param>
    /// <param name="sourceMetadata">Метаданные исходного файла.</param>
    /// <returns>Статус операции.</returns>
    private FileOperationStatus GetOperationStatus(
        string destinationFilePath,
        ChatMetadata sourceMetadata)
    {
        if (!_fileSystem.FileExists(destinationFilePath))
        {
            return FileOperationStatus.Added;
        }

        try
        {
            ChatMetadata destinationMetadata;
            if (!_conversationIndex.TryGet(destinationFilePath, out destinationMetadata!))
            {
                destinationMetadata = _metadataReader.Read(destinationFilePath);
                _conversationIndex.Track(destinationFilePath, destinationMetadata);
            }
            Guid? sourceId = sourceMetadata.ConversationId;
            Guid? destinationId = destinationMetadata.ConversationId;

            if (sourceId is null ||
                destinationId is null ||
                sourceId != destinationId)
            {
                return FileOperationStatus.AddedUnique;
            }

            if (!destinationMetadata.UpdateTime.HasValue)
            {
                return FileOperationStatus.Updated;
            }

            if (!sourceMetadata.UpdateTime.HasValue)
            {
                return FileOperationStatus.Skipped;
            }

            return sourceMetadata.UpdateTime.Value > destinationMetadata.UpdateTime.Value
                ? FileOperationStatus.Updated
                : FileOperationStatus.Skipped;
        }
        catch (FormatException exception)
        {
            _logger.Error($"Не удалось прочитать метаданные: {destinationFilePath}", exception);
            return FileOperationStatus.AddedUnique;
        }
        catch (IOException exception)
        {
            _logger.Error($"Не удалось прочитать файл: {destinationFilePath}", exception);
            return FileOperationStatus.AddedUnique;
        }
    }
}
