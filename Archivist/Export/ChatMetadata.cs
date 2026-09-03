using System.Globalization;
using YamlDotNet.Serialization;

namespace dRz.GPT_Utilities.Archivist.Export
{
    /// <summary>
    /// Метаданные Markdown-файла из экспорта ChatGPT.
    ///
    /// Класс содержит только те поля YAML, которые сейчас необходимы
    /// GPT Archivist. Остальные поля YAML при десериализации игнорируются.
    /// </summary>
    internal sealed class ChatMetadata
    {
        /// <summary>
        /// Дата и время создания разговора.
        ///
        /// Пример:
        /// 2026-08-24T15:23:56.473Z
        /// </summary>
        public DateTimeOffset CreateTime { get; set; }

        /// <summary>Gets or sets the update time.</summary>
        /// <value>The update time.</value>
        public DateTimeOffset? UpdateTime { get; set; }

        /// <summary>Gets or sets the date export.</summary>
        /// <value>The date export.</value>
        public string? DateExport { get; set; }

        /// <summary>Gets or sets the chat link.</summary>
        /// <value>The chat link.</value>
        public string? ChatLink { get; set; }

        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        public string? Title { get; set; }

        /// <summary>Gets or sets the tags.</summary>
        /// <value>The tags.</value>
        public List<string?> Tags { get; set; }

        /// <summary>Gets the export date time.</summary>
        /// <value>The export date time.</value>
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
        ///
        /// Получается из ChatLink и не участвует
        /// в сериализации YAML.
        ///
        /// Возвращает <see langword="null"/>, если ChatLink отсутствует
        /// или не содержит корректный идентификатор.
        /// </summary>
        [YamlIgnore]
        public Guid? ConversationId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ChatLink))
                {
                    return null;
                }

                if (!Uri.TryCreate(ChatLink, UriKind.Absolute, out Uri? uri) ||
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
}