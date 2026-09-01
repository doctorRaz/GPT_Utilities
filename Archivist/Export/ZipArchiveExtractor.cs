using System.IO.Compression;
using System.Text;

namespace dRz.GPT_Utilities.Archivist.Export;

/// <summary>
/// Реализация распаковки ZIP-архивов для локальной файловой системы.
/// </summary>
internal sealed class ZipArchiveExtractor : IArchiveExtractor
{
    private readonly Encoding _entryNameEncoding;

    public ZipArchiveExtractor(Encoding entryNameEncoding)
    {
        _entryNameEncoding = entryNameEncoding;
    }

    public ExtractedArchive Extract(FileInfo archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        string directory = Path.Combine(
            Path.GetTempPath(),
            $"GPT_Archivist_{Guid.NewGuid():N}");

        Directory.CreateDirectory(directory);

        try
        {
            ZipFile.ExtractToDirectory(
                archive.FullName,
                directory,
                _entryNameEncoding);

            return new ExtractedArchive(directory);
        }
        catch
        {
            // Если распаковка не завершилась, временный каталог всё равно
            // должен быть удалён до передачи исключения вызывающему коду.
            try
            {
                Directory.Delete(directory, recursive: true);
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
