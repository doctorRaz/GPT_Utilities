using System;
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

        /// <summary>Gets or sets the chat link.</summary>
        /// <value>The chat link.</value>
        public string? ChatLink { get; set; }

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

                const string prefix = "https://chatgpt.com/c/";

                if (!ChatLink.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                string value = ChatLink[prefix.Length..]
                    .TrimEnd('/');

                return Guid.TryParse(value, out Guid id)
                    ? id
                    : null;
            }
        }
    }
}