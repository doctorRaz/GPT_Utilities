namespace dRz.GPT_Utilities.Archivist.CommandLine
{
    /// <summary>
    /// Разбирает параметры командной строки GPT_Archivist.
    /// </summary>
    internal static class CommandLineParser
    {
        /// <summary>
        /// Разбирает параметры командной строки.
        /// </summary>
        /// <param name="args">
        /// Аргументы командной строки, переданные в <c>Program.Main</c>.
        /// </param>
        /// <returns>
        /// Объект с разобранными параметрами.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Возникает при отсутствии обязательного параметра,
        /// отсутствии его значения или наличии неизвестного параметра.
        /// </exception>
        public static CommandLineOptions Parse(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            // Если параметры вообще не переданы,
            // показываем справку.
            if (args.Length == 0)
            {
                return new CommandLineOptions
                {
                    ShowHelp = true
                };
            }

            string? sourceDirectory = null;
            string? destinationDirectory = null;
            string? zipFilePattern = null;

            // Флаг по умолчанию выключен.
            // Его наличие в командной строке переключит значение в true.
            bool extractAll = false;

            // Последовательно обрабатываем все аргументы.
            for (int i = 0; i < args.Length; i++)
            {
                string argument = args[i].Trim().ToLowerInvariant();

                switch (argument)
                {
                    // ---------------------------------------------------------
                    // Help
                    // ---------------------------------------------------------

                    case "-h":
                    case "--help":
                    case "/?":
                        return new CommandLineOptions
                        {
                            ShowHelp = true
                        };

                    // ---------------------------------------------------------
                    // Source directory
                    // ---------------------------------------------------------

                    case "-s":
                    case "--source":
                        sourceDirectory = ReadValue(
                            args,
                            ref i,
                            argument);

                        break;

                    // ---------------------------------------------------------
                    // Destination directory
                    // ---------------------------------------------------------

                    case "-d":
                    case "--destination":
                        destinationDirectory = ReadValue(
                            args,
                            ref i,
                            argument);

                        break;

                    // ---------------------------------------------------------
                    // ZIP file pattern
                    // ---------------------------------------------------------

                    case "-p":
                    case "--pattern":
                        zipFilePattern = ReadValue(
                            args,
                            ref i,
                            argument);

                        break;

                    // ---------------------------------------------------------
                    // Extract all
                    // ---------------------------------------------------------

                    case "-a":
                    case "--all":
                        extractAll = true;
                        break;

                    // ---------------------------------------------------------
                    // Unknown parameter
                    // ---------------------------------------------------------

                    default:
                        throw new ArgumentException(
                            $"Неизвестный параметр: {argument}");
                }
            }

          

            return new CommandLineOptions
            {
                SourceDirectory = sourceDirectory ?? string.Empty,

                DestinationDirectory = destinationDirectory ?? string.Empty,

                ExtractAll = extractAll,

                ZipFilePattern = zipFilePattern ?? string.Empty,
            };
        }

        /// <summary>
        /// Читает значение параметра, расположенное следующим аргументом.
        /// </summary>
        /// <param name="args">
        /// Все аргументы командной строки.
        /// </param>
        /// <param name="index">
        /// Индекс текущего параметра.
        /// После успешного чтения значения индекс увеличивается
        /// на один, чтобы основной цикл пропустил это значение.
        /// </param>
        /// <param name="parameterName">
        /// Имя параметра, для которого требуется значение.
        /// </param>
        /// <returns>
        /// Значение параметра.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Возникает, если значение параметра отсутствует
        /// или вместо значения указан другой параметр.
        /// </exception>
        private static string ReadValue(
            string[] args,
            ref int index,
            string parameterName)
        {
            // У параметра должен быть следующий аргумент.
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException(
                    $"Для параметра {parameterName} не указано значение.");
            }

            // Переходим к следующему аргументу.
            string value = args[++index];

            // Если следующий аргумент начинается с '-',
            // считаем, что пользователь указал другой параметр,
            // но забыл значение текущего.
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith("-", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Для параметра {parameterName} не указано значение.");
            }

            return value;
        }
    }
}