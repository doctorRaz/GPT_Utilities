using System;
using System.IO;

namespace JsonToMarkdown
{
    class Program
    {
        static int Main(string[] args)
        {
            if (HasHelp(args))
            {
                PrintHelp();
                return 0;
            }

            string inputFile;
            string outputDir;

            if (args.Length >= 2)
            {
                inputFile = args[0];
                outputDir = args[1];
            }
            else
            {
                if (!PromptLoop(out inputFile, out outputDir))
                    return 0;
            }

            if (!Validate(inputFile, outputDir))
                return 1;

            Console.WriteLine("Входной файл: " + inputFile);
            Console.WriteLine("Каталог результата: " + outputDir);

            // TODO: Основная логика обработки
            return 0;
        }

        static bool PromptLoop(out string inputFile, out string outputDir)
        {
            inputFile = "";
            outputDir = "";

            while (true)
            {
                Console.WriteLine("Введите путь к входному JSON (ESC — выход):");

                var input = ReadLineOrEsc();
                if (input == null)
                    return false;

                Console.WriteLine("Введите путь к каталогу результата (ESC — выход):");

                var output = ReadLineOrEsc();
                if (output == null)
                    return false;

                if (Validate(input, output))
                {
                    inputFile = input;
                    outputDir = output;
                    return true;
                }

                Console.WriteLine("Ошибка. Повторите ввод.\n");
            }
        }

        static string? ReadLineOrEsc()
        {
            var buffer = "";

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("\nВыход.");
                    return null;
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return buffer.Trim();
                }

                if (key.Key == ConsoleKey.Backspace && buffer.Length > 0)
                {
                    buffer = buffer[..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    buffer += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }
        }

        static bool Validate(string inputFile, string outputDir)
        {
            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Файл не найден: {inputFile}");
                return false;
            }

            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine($"Каталог не найден, создаем: {outputDir}");
                Directory.CreateDirectory(outputDir);
            }

            return true;
        }

        static bool HasHelp(string[] args)
        {
            foreach (var a in args)
            {
                if (a == "-h" || a == "--help" || a == "/?")
                    return true;
            }
            return false;
        }

        static void PrintHelp()
        {
            Console.WriteLine(@"
Использование:
  MyTool.exe <input.json> <output_folder>

Если аргументы не заданы — программа запросит их интерактивно.

Аргументы:
  input.json       Путь к входному файлу
  output_folder    Каталог результата

Опции:
  -h, --help, /?    Показать справку

Пример:
  MyTool.exe conversations.json output
");
        }
    }
}