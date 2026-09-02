using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using dRz.GPT_Utilities.Archivist.Files;
using YamlDotNet.Core;
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

        /// <summary>
        /// Читает и проверяет metadata файла.
        /// </summary>
        private sealed class RawChatMetadata
        {
            public DateTimeOffset CreateTime { get; set; }

            public string? UpdateTime { get; set; }

            public string? DateExport { get; set; }

            public string? ChatLink { get; set; }

            public string? Title { get; set; }

            public List<string?>? Tags { get; set; }
        }

        public ChatMetadata Read(string filePath)
        {
            StringBuilder frontMatter = new();

            foreach (string line in _fileSystem.ReadLines(filePath))
            {
                frontMatter.AppendLine(line);

                if (line.Trim() == "---" && frontMatter.Length > line.Length + Environment.NewLine.Length)
                {
                    break;
                }
            }

            string content = frontMatter.ToString();
            Match match = FrontMatterRegex.Match(content);

            if (!match.Success)
            {
                throw new FormatException(
                    $"В файле отсутствует YAML front matter: {filePath}");
            }

            RawChatMetadata? rawMetadata;

            try
            {
                rawMetadata = YamlDeserializer.Deserialize<RawChatMetadata>(
                    match.Groups["yaml"].Value);
            }
            catch (YamlException exception)
            {
                throw new FormatException(
                    $"Некорректный YAML или дата в файле: {filePath}",
                    exception);
            }

            if (rawMetadata is null)
            {
                throw new FormatException(
                    $"Не удалось прочитать YAML: {filePath}");
            }

            if (rawMetadata.CreateTime == default)
            {
                throw new FormatException(
                    $"В YAML отсутствует или некорректен create_time: {filePath}");
            }

            DateTimeOffset? updateTime = null;
            if (DateTimeOffset.TryParse(
                    rawMetadata.UpdateTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset parsedUpdateTime))
            {
                updateTime = parsedUpdateTime;
            }

            return new ChatMetadata
            {
                CreateTime = rawMetadata.CreateTime,
                UpdateTime = updateTime,
                DateExport = rawMetadata.DateExport,
                ChatLink = rawMetadata.ChatLink,
                Title = rawMetadata.Title,
                Tags = rawMetadata.Tags ?? new List<string?>()
            };
        }
    }
}