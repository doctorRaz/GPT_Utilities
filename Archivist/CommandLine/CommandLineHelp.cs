using dRz.GPT_Utilities.Archivist.Infrastructure;

namespace dRz.GPT_Utilities.Archivist.CommandLine
{
    internal class CommandLineHelp
    {
        /// <summary>
        /// Выводит справку по использованию программы.
        /// </summary>
        public static void Print()
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

      -p, --pattern <маска>
          Маска ZIP-файлов для обработки.
          По умолчанию: *.zip.
          Поддерживается стандартная маска Directory.EnumerateFiles.

    Опции:

      -a, --all
          Обработать все ZIP-архивы.
          По умолчанию обрабатывается только последний архив.

      -h, --help, /?
          Показать эту справку.

    Примеры:

      GPT_Archivist -s "D:\GPT\Archives" -d "D:\GPT\Unpacked"

      GPT_Archivist -s "D:\GPT\Archives" -d "D:\GPT\Unpacked" -a

      GPT_Archivist --source "D:\GPT\Archives" --destination "D:\GPT\Unpacked" --all

      GPT_Archivist -s "D:\GPT\Archives" -d "D:\GPT\Unpacked" -p "*.zip"
    """);
        }
    }
}
