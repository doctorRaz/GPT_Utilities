using System.Globalization;
using YamlDotNet.Serialization;

namespace dRz.GPT_Utilities.Archivist.Export
{
    /// <summary>
    /// Метаданные Markdown-файла из экспорта ChatGPT.
    /// </summary>
    internal sealed class ChatMetadata
    {
        public DateTimeOffset CreateTime { get; set; }

        [YamlIgnore]
        internal string? CreateTimeText { get; set; }

        public DateTimeOffset? UpdateTime { get; set; }

        [YamlIgnore]
        internal string? UpdateTimeText { get; set; }

        [YamlIgnore]
        internal bool HasUpdateTime { get; set; }

        public string? DateExport { get; set; }

        public string? ChatLink { get; set; }

        public string? Title { get; set; }

        public List<string?> Tags { get; set; } = new();

        [YamlIgnore]
        internal bool HasTags { get; set; }

        public List<string?> Aliases { get; set; } = new();

        [YamlIgnore]
        internal bool HasAliases { get; set; }

        [YamlIgnore]
        public DateTime? ExportDateTime
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DateExport))
                {
                    return null;
                }
                return DateTime.ParseExact(DateExport,
                                            "yyyy-MM-dd'T'HH-mm-ss",
                                            CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Уникальный идентификатор conversation.
        /// Если значение было прочитано из YAML, оно имеет приоритет.
        /// Иначе идентификатор получается из ChatLink.
        /// </summary>
        [YamlIgnore]
        public Guid? ConversationId
        {
            get => _conversationId ?? ParseConversationId(ChatLink);
            set => _conversationId = value;
        }

        private Guid? _conversationId;

        private static Guid? ParseConversationId(string? chatLink)
        {
            if (string.IsNullOrWhiteSpace(chatLink))
            {
                return null;
            }

            if (!Uri.TryCreate(chatLink, UriKind.Absolute, out Uri? uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, "chatgpt.com", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string[] segments = uri.AbsolutePath.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2 ||
                !string.Equals(segments[0], "c", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return Guid.TryParse(segments[1], out Guid id)
                ? id
                : null;
        }
    }
}