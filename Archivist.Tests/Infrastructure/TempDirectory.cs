using System;
using System.IO;

namespace dRz.GPT_Utilities.Archivist.Tests.Infrastructure
{
    internal sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"ArchivistTests_{Guid.NewGuid():N}");

            Directory.CreateDirectory(Path);
        }

        public string Combine(params string[] parts)
        {
            string result = Path;

            foreach (string part in parts)
            {
                result = System.IO.Path.Combine(result, part);
            }

            return result;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
