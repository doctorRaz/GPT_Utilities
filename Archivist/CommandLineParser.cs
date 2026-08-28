using dRz.GPT_Utilities.Archivist.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dRz.GPT_Utilities.Archivist
{
    /// <summary>
    /// Разбирает параметры командной строки GPT_Archivist.
    /// </summary>
    public static class CommandLineParser
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

            // Флаг по умолчанию выключен.
            // Его наличие в командной строке переключит значение в true.
            bool extractAll = false;

            // Последовательно обрабатываем все аргументы.
            for (int i = 0; i < args.Length; i++)
            {
                string argument = args[i];

                switch (argument.ToLowerInvariant())
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

            // Source обязателен.
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                throw new ArgumentException(
                    "Не указан каталог с ZIP-архивами. " +
                    "Используй параметр -s или --source.");
            }

            // Destination обязателен.
            // При этом сам каталог может физически отсутствовать.
            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new ArgumentException(
                    "Не указан каталог назначения. " +
                    "Используй параметр -d или --destination.");
            }

            return new CommandLineOptions
            {
                SourceDirectory = sourceDirectory,
                DestinationDirectory = destinationDirectory,
                ExtractAll = extractAll
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
            if (value.StartsWith("-", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Для параметра {parameterName} не указано значение.");
            }

            return value;
        }

        /// <summary>
        /// Выводит справку по использованию программы.
        /// </summary>
        public static void PrintHelp()
        {
            ConsoleWriter.Info("""
            GPT_Archivist — обработка архивов экспорта ChatGPT

            Использование:
              GPT_Archivist -s <каталог> -d <каталог> [опции]

            Параметры:

              -s, --source <каталог>
                  Каталог с ZIP-архивами экспорта ChatGPT.
                  Каталог должен существовать.

              -d, --destination <каталог>
                  Каталог для распаковки архивов.
                  Если каталог отсутствует, он будет создан.

            Опции:

              -a, --all
                  Обработать все ZIP-архивы.
                  По умолчанию обрабатывается только последний архив.

              -h, --help
                  Показать эту справку.

            Примеры:

              GPT_Archivist -s "D:\GPT\Archives" -d "D:\GPT\Unpacked"

              GPT_Archivist -s "D:\GPT\Archives" -d "D:\GPT\Unpacked" -a

              GPT_Archivist --source "D:\GPT\Archives" --destination "D:\GPT\Unpacked" --all
            """);
        }
    }
}
