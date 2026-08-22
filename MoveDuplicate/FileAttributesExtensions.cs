using System.IO;

namespace dRz.GPT_Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class FileAttributesExtensions
    {
        /// <summary>Determines whether [is read only] [the specified path].</summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is read only] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsReadOnly(string path) => File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly);

        /// <summary>Sets the read only.</summary>
        /// <param name="path">The path.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetReadOnly(string path, bool value)
        {
            FileAttributes attributes = File.GetAttributes(path);

            if (value)
            {
                attributes |= FileAttributes.ReadOnly;
            }
            else
            {
                attributes &= ~FileAttributes.ReadOnly;
            }

            File.SetAttributes(path, attributes);
        }
    }
}
