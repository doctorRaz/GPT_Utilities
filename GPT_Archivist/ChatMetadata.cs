using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
