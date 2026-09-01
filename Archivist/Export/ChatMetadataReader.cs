using System.Text.RegularExpressions;
using dRz.GPT_Utilities.Archivist.Files;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dRz.GPT_Utilities.Archivist.Export
{
    /// <summary>
    /// Читает YAML front matter из Markdown-файла экспорта ChatGPT.
    /// </summary>
    internal sealed class ChatMetadataReader : IChatMetadataReader
    {
        private static readonly IDeserializer YamlDeserializer =
            new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

        /// <summary>
        /// Regex для поиска front matter только в начале Markdown-файла.
        /// </summary>
        internal static readonly Regex FrontMatterRegex = new(
            @"\A---\s*\r?\n(?<yaml>.*?)\r?\n---\s*(?:\r?\n|$)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly IFileSystem _fileSystem;

        public ChatMetadataReader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem
                ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        // Временная совместимость со старыми тестами и вызывающим кодом.
        public ChatMetadataReader()
            : this(new LocalFileSystem())
        {
        }

        /// <summary>
        /// Читает и проверяет обязательные метаданные разговора.
        /// </summary>
        public ChatMetadata Read(string filePath)
        {
            string content = _fileSystem.ReadAllText(filePath);
            Match match = FrontMatterRegex.Match(content);

            if (!match.Success)
            {
                throw new FormatException(
                    $"В файле отсутствует YAML front matter: {filePath}");
            }

            ChatMetadata? metadata = YamlDeserializer.Deserialize<ChatMetadata>(
                match.Groups["yaml"].Value);

            if (metadata is null)
            {
                throw new FormatException(
                    $"Не удалось прочитать YAML: {filePath}");
            }

            if (metadata.CreateTime == default)
            {
                throw new FormatException(
                    $"В YAML отсутствует или некорректен create_time: {filePath}");
            }

            if (metadata.UpdateTime == default)
            {
                throw new FormatException(
                    $"В YAML отсутствует или некорректен update_time: {filePath}");
            }

            return metadata;
        }
    }
}