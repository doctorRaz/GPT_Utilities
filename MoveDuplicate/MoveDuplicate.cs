using System;
using System.IO;
using System.Text.RegularExpressions;

namespace dRz.GPT_Utilities.MoveDuplicate
{
    /// <summary>
    /// Выполняет перемещение файлов из корневого каталога
    /// в соответствующие подкаталоги.
    ///
    /// В корневом каталоге имя файла может иметь префикс даты
    /// формата yyyy-MM-dd_.
    ///
    /// Пример:
    ///     2026-12-07_Обсуждение метода.md
    ///
    /// В подкаталоге файл имеет обычное имя:
    ///     Обсуждение метода.md
    ///
    /// Если файл найден и файл в корне новее,
    /// выполняется замена файла в подкаталоге.
    /// </summary>
    public static class MoveDuplicate//only gpt
    {
        private const string extension = "*.md";

        /// <summary>
        /// Регулярное выражение для удаления префикса даты.
        /// </summary>
        private static readonly Regex DatePrefixRegex =
            new(@"^\d{4}-\d{2}-\d{2}_", RegexOptions.Compiled);

        /// <summary>
        /// Перемещает файлы из корневого каталога.
        /// </summary>
        /// <param name="rootDirectory">
        /// Корневой каталог.
        /// </param>
        public static void MoveFiles(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException(rootDirectory);
            }

            //убираем префикс даты у файлов в директориях
            NormalizeSubdirectoriesFiles(rootDirectory);

            int moveCount = 0;//счетчик перемещенных файлов
            int deleteCount = 0;//счетчик удаленных файлов

            // Получаем все файлы корневого каталога.
            string[] rootFiles = Directory.GetFiles(rootDirectory, extension);

            int rootTotal = rootFiles.Length;//файлов для перемещения

            foreach (string rootFile in rootFiles)
            {
                // Имя файла без префикса даты.
                string fileName = RemoveDatePrefix(Path.GetFileName(rootFile));

                // Ищем все подкаталоги.
                foreach (string subDirectory in Directory.GetDirectories(rootDirectory, "*", SearchOption.AllDirectories))
                {
                    // Полный путь к предполагаемому файлу.
                    string targetFile = Path.Combine(subDirectory, fileName);

                    // Файл отсутствует.
                    if (!File.Exists(targetFile))
                    {
                        continue;
                    }

                    // Сравниваем дату последнего изменения.
                    DateTime sourceTime = File.GetLastWriteTime(rootFile);

                    DateTime targetTime = File.GetLastWriteTime(targetFile);

                    // В каталоге находится более новая версия.
                    if (sourceTime < targetTime)// даты равны все равно перемещаем, что бы не копить мусор в роот
                    {
                        try
                        {
                            bool readOnly = FileAttributesExtensions.IsReadOnly(rootFile);

                            FileAttributesExtensions.SetReadOnly(rootFile, false);//

                            //удалить файл в роот если он старше, что бы не копить мусор
                            File.Delete(rootFile);

                            Console.WriteLine($"DELETE: {rootFile} - файл в ROOT старше.");
                            deleteCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR delete: {rootFile}");
                            Console.WriteLine(ex.Message);
                        }
                        break; // Независимо от результата дальнейший поиск для данного файла не требуется.
                    }

                    try
                    {
                        bool readOnly = FileAttributesExtensions.IsReadOnly(targetFile);

                        FileAttributesExtensions.SetReadOnly(targetFile, false);//

                        // Перемещаем с заменой существующего файла.
                        File.Move(rootFile, targetFile, overwrite: true);

                        FileAttributesExtensions.SetReadOnly(targetFile, readOnly);

                        moveCount++;
                        Console.WriteLine($"OK: {rootFile} -> {targetFile}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR move: {rootFile}");
                        Console.WriteLine(ex.Message);
                    }

                    // Независимо от результата дальнейший поиск
                    // для данного файла не требуется.
                    break;
                }
            }

            Console.WriteLine($"All root {rootTotal} files\n\tmoved -> {moveCount} files\n\tdeleted-> {deleteCount} files");
        }

        /// <summary>
        /// Предварительная очистка имен файлов от префиксов дат во всех подкаталогах.
        /// </summary>
        private static void NormalizeSubdirectoriesFiles(string rootDirectory)
        {
            string[] subDirectories = Directory.GetDirectories(rootDirectory, "*", SearchOption.AllDirectories);

            foreach (string subDirectory in subDirectories)
            {
                string[] subFiles = Directory.GetFiles(subDirectory, extension, SearchOption.TopDirectoryOnly);
                foreach (string subFile in subFiles)
                {
                    string rawName = Path.GetFileName(subFile);
                    string baseName = RemoveDatePrefix(rawName);

                    // Если имя уже "чистое", ничего не делаем
                    if (rawName.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string targetBaseName = Path.Combine(subDirectory, baseName);

                    try
                    {
                        File.Move(subFile, targetBaseName, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR rename: {subFile}\n{ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Удаляет префикс даты вида yyyy-MM-dd_.
        /// Если префикса нет, возвращает исходное имя.
        /// </summary>
        private static string RemoveDatePrefix(string fileName)
        {
            return DatePrefixRegex.Replace(fileName, "");
        }
    }
}