using System.Globalization;
using YamlDotNet.Serialization;

namespace dRz.GPT_Utilities.Archivist.Export
{
    /// <summary>
    /// Метаданные Markdown-файла из экспорта ChatGPT.
    /// </summary>
    internal sealed class ChatMetadata
    {
        /// <summary>Дата и время создания разговора.</summary>
        public DateTimeOffset CreateTime { get; set; }

        /// <summary>Исходное текстовое представление create_time.</summary>
        [YamlIgnore]
        internal string? CreateTimeText { get; set; }

        /// <summary>Дата и время последнего изменения разговора.</summary>
        public DateTimeOffset? UpdateTime { get; set; }

        /// <summary>Исходное текстовое представление update_time.</summary>
        [YamlIgnore]
        internal string? UpdateTimeText { get; set; }

        /// <summary>Было ли поле update_time прочитано из исходного YAML.</summary>
        [YamlIgnore]
        internal bool HasUpdateTime { get; set; }

        /// <summary>Модель ChatGPT.</summary>
        public string? Model { get; set; }

        /// <summary>Имя модели ChatGPT.</summary>
        public string? ModelName { get; set; }

        /// <summary>Дата экспорта.</summary>
        public string? DateExport { get; set; }

        /// <summary>Ссылка на разговор ChatGPT.</summary>
        public string? ChatLink { get; set; }

        /// <summary>Название разговора.</summary>
        public string? Title { get; set; }

        /// <summary>Теги.</summary>
        public List<string?> Tags { get; set; } = new();

        /// <summary>Было ли поле tags прочитано из исходного YAML.</summary>
        [YamlIgnore]
        internal bool HasTags { get; set; }

        /// <summary>Псевдонимы.</summary>
        public List<string?> Aliases { get; set; } = new();

        /// <summary>Было ли поле aliases прочитано из исходного YAML.</summary>
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