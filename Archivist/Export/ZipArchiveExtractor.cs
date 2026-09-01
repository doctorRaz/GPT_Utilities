using System.IO.Compression;
using System.Text;
using dRz.GPT_Utilities.Archivist.Files;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Реализация распаковки ZIP-архивов для локальной файловой системы.
/// </summary>
internal sealed class ZipArchiveExtractor : IArchiveExtractor
{
    private readonly Encoding _entryNameEncoding;
    private readonly IFileSystem _fileSystem;

    public ZipArchiveExtractor(
        Encoding entryNameEncoding,
        IFileSystem fileSystem)
    {
        _entryNameEncoding = entryNameEncoding;
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public ExtractedArchive Extract(FileInfo archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        string directory = Path.Combine(
            Path.GetTempPath(),
            $"GPT_Archivist_{Guid.NewGuid():N}");

        _fileSystem.CreateDirectory(directory);

        try
        {
            ZipFile.ExtractToDirectory(
                archive.FullName,
                directory,
                _entryNameEncoding);

            return new ExtractedArchive(directory, _fileSystem);
        }
        catch
        {
            // Если распаковка не завершилась, временный каталог всё равно
            // должен быть удалён до передачи исключения вызывающему коду.
            try
            {
                _fileSystem.DeleteDirectory(directory, recursive: true);
            }
            catch (IOException)
            {
                // Первичное исключение важнее ошибки очистки.
            }
            catch (UnauthorizedAccessException)
            {
                // Первичное исключение важнее ошибки очистки.
            }

            throw;
        }
    }
}
