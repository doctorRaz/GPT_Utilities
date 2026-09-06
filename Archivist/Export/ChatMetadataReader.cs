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

        internal static readonly Regex FrontMatterRegex = new(
            @"\A---\s*\r?\n(?<yaml>.*?)\r?\n---\s*(?:\r?\n|$)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly IFileSystem _fileSystem;

        public ChatMetadataReader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem
                ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        private sealed class RawChatMetadata
        {
            public string? CreateTime { get; set; }
            public string? UpdateTime { get; set; }
            public string? DateExport { get; set; }
            public string? ChatLink { get; set; }
            public string? Title { get; set; }
            public List<string?>? Tags { get; set; }
            public List<string?>? Aliases { get; set; }
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

            string? createTimeText = rawMetadata.CreateTime?.Trim();
            if (string.IsNullOrWhiteSpace(createTimeText) ||
                !DateTimeOffset.TryParse(
                    createTimeText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset createTime))
            {
                throw new FormatException(
                    $"В YAML отсутствует или некорректен create_time: {filePath}");
            }

            string? updateTimeText = rawMetadata.UpdateTime?.Trim();
            DateTimeOffset? updateTime = null;
            if (!string.IsNullOrWhiteSpace(updateTimeText))
            {
                if (!DateTimeOffset.TryParse(
                        updateTimeText,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out DateTimeOffset parsedUpdateTime))
                {
                    throw new FormatException(
                        $"В YAML указана некорректная дата update_time: {filePath}");
                }

                updateTime = parsedUpdateTime;
            }

            return new ChatMetadata
            {
                CreateTime = createTime,
                CreateTimeText = createTimeText,
                UpdateTime = updateTime,
                UpdateTimeText = updateTimeText,
                HasUpdateTime = rawMetadata.UpdateTime is not null,
                DateExport = rawMetadata.DateExport,
                ChatLink = rawMetadata.ChatLink,
                Title = rawMetadata.Title,
                Tags = rawMetadata.Tags ?? new List<string?>(),
                HasTags = rawMetadata.Tags is not null,
                Aliases = rawMetadata.Aliases ?? new List<string?>(),
                HasAliases = rawMetadata.Aliases is not null
            };
        }
    }
}