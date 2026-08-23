using System;
using System.IO;

namespace dRz.GPT_Utilities.GPTJson2Md
{
    internal class Start
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
            }
            else if (HasHelp(args)) { Help(); return 0; }

            (string? input, string? output) = ParseArgs(args);

            //todo получать список файлов conversations*.json в директории по маске и обрабатывать их в цикле

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
            {
                if (!Prompt(out input, out output))
                {
                    return 0;
                }
            }

            if (!Validate(input, output))
            {
                return 1;
            }

            Console.WriteLine($"OK\nInput: {input}\nOutput: {output}");

            GPTJson2Md.Json2MdParser(input, output);

            //Console.ReadKey();
            return 0;
        }

        private static (string? input, string? output) ParseArgs(string[] a)
        {
            string? i = null, o = null;

            for (int n = 0; n < a.Length; n++)
            {
                switch (a[n])
                {
                    case "--input": case "-i": i = Next(a, ref n); break;
                    case "--out": case "-o": o = Next(a, ref n); break;
                    default:
                        if (i == null)
                        {
                            i = a[n];
                        }
                        else if (o == null)
                        {
                            o = a[n];
                        }

                        break;
                }
            }
            return (i, o);
        }

        private static string? Next(string[] a, ref int n) =>
            n + 1 < a.Length ? a[++n] : null;

        private static bool Prompt(out string input, out string output)
        {
            input = output = "";
            while (true)
            {
                Console.Write("Input JSON (ESC = exit): ");
                string? i = ReadEsc(); if (i == null)
                {
                    return false;
                }

                Console.Write("Output folder (ESC = exit): ");
                string? o = ReadEsc(); if (o == null)
                {
                    return false;
                }

                if (Validate(i, o)) { input = i; output = o; return true; }
                Console.WriteLine("Ошибка. Повтор.\n");
            }
        }

        private static string? ReadEsc()
        {
            string s = "";
            while (true)
            {
                ConsoleKeyInfo k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Escape) { Console.WriteLine(); return null; }
                if (k.Key == ConsoleKey.Enter) { Console.WriteLine(); return s.Trim(); }
                if (k.Key == ConsoleKey.Backspace && s.Length > 0)
                { s = s[..^1]; Console.Write("\b \b"); }
                else if (!char.IsControl(k.KeyChar))
                { s += k.KeyChar; Console.Write(k.KeyChar); }
            }
        }

        private static bool Validate(string input, string output)
        {
            if (!File.Exists(input)) { Console.WriteLine($"Файл не найден: {input}"); return false; }
            if (!Directory.Exists(output))
            {
                Directory.CreateDirectory(output);
            }

            return true;
        }

        private static bool HasHelp(string[] a)
            => Array.Exists(a, x => x is "-h" or "--help" or "/?");

        private static void Help() => Console.WriteLine(@"
Usage:
  MyTool.exe <input.json> <output_folder>
  MyTool.exe --input file.json --out folder

Options:
  -i, --input     Input JSON path
  -o, --out       Output folder
  -h, --help      Show help

If args missing → interactive mode (ESC to exit)
");
    }
}