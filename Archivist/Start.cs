using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace dRz.GPT_Utilities.Archivist
{
    internal class Start
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string destinationDir = "";
            string sourceDir = "";
#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
            destinationDir = @"d:\@Developers\В работе\Reminder\GPT-export\Markdown\";

            sourceDir = @"d:\@Developers\В работе\GPT_export\chatgpt-export-markdown\";

#endif

            //string root = GetDirectory(args, sourceDir, destinationDir);

            if (!(string.IsNullOrWhiteSpace(sourceDir) || string.IsNullOrWhiteSpace(destinationDir)))
            {
                int result = ChatGptExportProcessor.Process(sourceDir, destinationDir,true);
                Console.WriteLine($"Заменено и добавлено файлов: {result}");
            }
            else
            {
                Console.WriteLine("Не заданы пути");
            }

            Console.WriteLine("Press eny key...");
            Console.ReadKey();
        }

        private static string GetDirectory(string[] args, string sourceDir = "", string destinationDir = "")
        {
            //0. проверяем путь по умолчанию (отладка)
            if (!string.IsNullOrWhiteSpace(destinationDir))
            {
                if (Directory.Exists(destinationDir))
                {
                    return destinationDir;
                }
            }

            // 1. Проверяем аргумент командной строки.
            if (args.Length > 0)
            {
                try
                {
                    string path = Path.GetFullPath(args[0]);

                    if (Directory.Exists(path))
                    {
                        return path;
                    }
                }
                catch
                {
                    // Некорректный путь.
                }
            }

            // 2. Если рабочий каталог существует, используем его.
            string currentDirectory = Directory.GetCurrentDirectory();
            if (Directory.Exists(currentDirectory))
            {
                return currentDirectory;
            }

            // 3. Последний вариант ничего не найдено, возвращаем пустую строку.
            return string.Empty;
        }

        /// <summary>
        /// проверка вывода названий месяцев в формате MM-MMMM как у convoviz
        /// </summary>
        private static void PrintMonthDirectoryNames()
        {
            for (int i = 1; i < 13; i++)
            {
                DateTime date = new DateTime(2024, i, 1);
                Console.WriteLine(date.ToString("MM-MMMM", CultureInfo.InvariantCulture));
            }
        }
    }
}