using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dRz.GPT_Utilities.Archivist
{
    /// <summary>
    /// Параметры командной строки GPT_Archivist.
    /// </summary>
    public sealed class CommandLineOptions
    {
        /// <summary>
        /// Каталог, содержащий ZIP-архивы экспорта ChatGPT.
        /// </summary>
        public string SourceDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Каталог назначения для распаковки архивов.
        /// Если каталог отсутствует, приложение должно его создать.
        /// </summary>
        public string DestinationDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Признак обработки всех найденных архивов.
        ///
        /// false — обрабатывается только последний архив.
        /// true  — обрабатываются все архивы.
        /// </summary>
        public bool ExtractAll { get; init; }

        /// <summary>
        /// Признак запроса справки.
        /// </summary>
        public bool ShowHelp { get; init; }
    }
}
