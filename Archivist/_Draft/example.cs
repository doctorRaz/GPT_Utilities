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
        ConsoleWriter.Info(date.ToString("MM-MMMM", CultureInfo.InvariantCulture));
    }
}