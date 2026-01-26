namespace GPTJson2Md
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0) Help();
            else if (HasHelp(args)) { Help(); return 0; }

            var (input, output) = ParseArgs(args);

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
                if (!Prompt(out input, out output)) return 0;

            if (!Validate(input, output)) return 1;

            Console.WriteLine($"OK\nInput: {input}\nOutput: {output}");

            JsonTo2Md.JsonToMdParser(input, output);

            Console.ReadKey();
            return 0;
        }

        static (string? input, string? output) ParseArgs(string[] a)
        {
            string? i = null, o = null;

            for (int n = 0; n < a.Length; n++)
            {
                switch (a[n])
                {
                    case "--input": case "-i": i = Next(a, ref n); break;
                    case "--out": case "-o": o = Next(a, ref n); break;
                    default:
                        if (i == null) i = a[n];
                        else if (o == null) o = a[n];
                        break;
                }
            }
            return (i, o);
        }

        static string? Next(string[] a, ref int n) =>
            n + 1 < a.Length ? a[++n] : null;

        static bool Prompt(out string input, out string output)
        {
            input = output = "";
            while (true)
            {
                Console.Write("Input JSON (ESC = exit): ");
                var i = ReadEsc(); if (i == null) return false;

                Console.Write("Output folder (ESC = exit): ");
                var o = ReadEsc(); if (o == null) return false;

                if (Validate(i, o)) { input = i; output = o; return true; }
                Console.WriteLine("Ошибка. Повтор.\n");
            }
        }

        static string? ReadEsc()
        {
            var s = "";
            while (true)
            {
                var k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Escape) { Console.WriteLine(); return null; }
                if (k.Key == ConsoleKey.Enter) { Console.WriteLine(); return s.Trim(); }
                if (k.Key == ConsoleKey.Backspace && s.Length > 0)
                { s = s[..^1]; Console.Write("\b \b"); }
                else if (!char.IsControl(k.KeyChar))
                { s += k.KeyChar; Console.Write(k.KeyChar); }
            }
        }

        static bool Validate(string input, string output)
        {
            if (!File.Exists(input)) { Console.WriteLine($"Файл не найден: {input}"); return false; }
            if (!Directory.Exists(output)) Directory.CreateDirectory(output);
            return true;
        }

        static bool HasHelp(string[] a)
            => Array.Exists(a, x => x is "-h" or "--help" or "/?");

        static void Help() => Console.WriteLine(@"
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