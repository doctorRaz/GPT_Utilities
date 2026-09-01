namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Минимальная абстракция файловых операций, необходимых Archivist.
/// </summary>
internal interface IFileSystem
{
    bool FileExists(string path);

    string ReadAllText(string path);

    IEnumerable<string> ReadLines(string path);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    /// <summary>
    /// Пытается создать файл без перезаписи существующего файла.
    /// </summary>
    bool TryCopyFile(string sourcePath, string destinationPath);

    void SetLastWriteTime(string path, DateTime lastWriteTime);

    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    void DeleteDirectory(string path, bool recursive);

    IEnumerable<string> EnumerateFiles(
        string path,
        string searchPattern,
        SearchOption searchOption);
}

/// <summary>
/// Реализация файловых операций через System.IO.
/// </summary>
internal sealed class LocalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public IEnumerable<string> ReadLines(string path) => File.ReadLines(path);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
        File.Copy(sourcePath, destinationPath, overwrite);

    public bool TryCopyFile(string sourcePath, string destinationPath)
    {
        try
        {
            File.Copy(sourcePath, destinationPath, overwrite: false);
            return true;
        }
        catch (IOException) when (File.Exists(destinationPath))
        {
            // Другой процесс успел занять имя между проверкой и копированием.
            return false;
        }
    }

    public void SetLastWriteTime(string path, DateTime lastWriteTime) =>
        File.SetLastWriteTime(path, lastWriteTime);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive) =>
        Directory.Delete(path, recursive);

    public IEnumerable<string> EnumerateFiles(
        string path,
        string searchPattern,
        SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);
}
