namespace dRz.GPT_Utilities.Archivist.Files;

/// <summary>
/// Минимальная абстракция файловых операций, необходимых Archivist.
/// </summary>
internal interface IFileSystem
{
    bool FileExists(string path);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    void SetLastWriteTime(string path, DateTime lastWriteTime);
}

/// <summary>
/// Реализация файловых операций через System.IO.
/// </summary>
internal sealed class LocalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
        File.Copy(sourcePath, destinationPath, overwrite);

    public void SetLastWriteTime(string path, DateTime lastWriteTime) =>
        File.SetLastWriteTime(path, lastWriteTime);
}
