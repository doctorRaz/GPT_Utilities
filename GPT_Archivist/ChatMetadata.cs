using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace dRz.GPT_Utilities.GPT_Archivist
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
        public DateTimeOffset? UpdateTime { get; set; }
        public string? ChatLink { get; set; }

        /// <summary>
        /// Уникальный идентификатор conversation.
        ///
        /// Получается из ChatLink и не участвует
        /// в сериализации YAML.
        /// </summary>
        [YamlIgnore]
        public Guid? ConversationId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ChatLink))
                    throw new InvalidDataException(
                        "Chat link is empty.");

                const string prefix = "https://chatgpt.com/c/";

                if (!ChatLink.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Invalid ChatGPT chat link: {ChatLink}");
                }

                string value = ChatLink[prefix.Length..]
                    .TrimEnd('/');

                if (!Guid.TryParse(value, out Guid id))
                {
                    throw new InvalidDataException(
                        $"Conversation ID not found in chat link: {ChatLink}");
                }

                return id;
            }
        }

    }
}
