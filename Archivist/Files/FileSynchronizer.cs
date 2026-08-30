using dRz.GPT_Utilities.Archivist.Export;
using System;
using System.IO;

namespace dRz.GPT_Utilities.Archivist.Files
{
    internal class FileSynchronizer
    {
        /// <summary>Copies if newer.</summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <param name="destinationFilePath">The destination file path.</param>
        /// <param name="sourceMetadata">The source update time.</param>
        /// <returns> The copy result. </returns>
        internal static FileCopyDecision CopyIfNewer(string sourceFilePath, string destinationFilePath, ChatMetadata sourceMetadata)
        {
            FileCopyDecision decision = GetCopyDecision(destinationFilePath, sourceMetadata);

            if (decision == FileCopyDecision.Skip)
            {
                //вывод в консоль результата копирования
                WriteCopyResult(decision, sourceFilePath, destinationFilePath: null, updateTime: null);
                return decision;
            }
            // разные IDs — создаём уникальный файл в destination.
            if (decision == FileCopyDecision.AddUnique)
            {
                destinationFilePath = FileNameHelper.GetUnique(destinationFilePath);
            }

            // Единственная операция копирования.
            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);

            // Сохраняем updateTime исходного файла.
            if (sourceMetadata.UpdateTime.HasValue)
            {
                File.SetLastWriteTime(destinationFilePath, sourceMetadata.UpdateTime.Value.LocalDateTime);
            }

            // destinationFile мог измениться внутри CopyIfNewer, если было принято решение AddUnique.
            // поэтому для консоли пользуем copyResult.destinationFilePath

            //вывод в консоль результата копирования
            WriteCopyResult(decision, sourceFilePath, destinationFilePath, sourceMetadata.UpdateTime);

            //результат по файлу
            return decision;
        }

        private static FileCopyDecision GetCopyDecision(string destinationFilePath, ChatMetadata sourceMetadata)
        {
            // Destination-файла ещё нет.
            if (!File.Exists(destinationFilePath))
            {
                return FileCopyDecision.Add;
            }

            try
            {
                // Читаем metadata существующего файла.
                ChatMetadata destinationMetadata = ChatMetadataReader.Read(destinationFilePath);

                Guid? sourceId = sourceMetadata.ConversationId;
                Guid? destinationId = destinationMetadata.ConversationId;

                // Обновлять существующий файл можно только если оба
                // conversation_id известны и совпадают.
                // Иначе безопаснее сохранить копию с суффиксом (1)...
                if (sourceId is null ||
                    destinationId is null ||
                    sourceId != destinationId)
                {
                    //todo проверять (1...) file name*.md
                    //в цикле рекурсивно? сравнить по sourceId != destinationId
                    //если совпадут проверить updateTime
                    return FileCopyDecision.AddUnique;
                }

                // Нет даты destination —
                // считаем файл требующим обновления.
                if (!destinationMetadata.UpdateTime.HasValue)
                {
                    return FileCopyDecision.Replace;
                }

                // Нет даты source —
                // сравнить файлы невозможно.
                if (!sourceMetadata.UpdateTime.HasValue)
                {
                    return FileCopyDecision.Skip;
                }

                // Обновляем только если source действительно новее.
                return sourceMetadata.UpdateTime.Value > destinationMetadata.UpdateTime.Value
                    ? FileCopyDecision.Replace
                    : FileCopyDecision.Skip;
            }
            catch (Exception ex)
            {
                // Не удалось прочитать metadata destination.
                // Безопаснее добавить копию файла.
                ConsoleWriter.Error(ex.Message);
                return FileCopyDecision.AddUnique;
            }
        }

        //private static void WriteCopyResult(FileOperationResult fileOperationResult)
        private static void WriteCopyResult(FileCopyDecision decision, string sourceFilePath, string? destinationFilePath, DateTimeOffset? updateTime)
        {
            //вывод в консоль
            string sourceFileName = Path.GetFileName(sourceFilePath);

            string copyDescription = $"{sourceFileName}" +
                        $"\n\t\tupdate_time: {updateTime:yyyy-MM-dd-HH.mm.sss}" +
                        $"\n\t\tto->{destinationFilePath}";

            switch (decision)
            {
                case FileCopyDecision.Add:
                    ConsoleWriter.Success($"\tДобавлен: {copyDescription}");
                    break;

                case FileCopyDecision.AddUnique:
                    ConsoleWriter.Warn($"\tДобавлен уникальный: {copyDescription}");
                    break;

                case FileCopyDecision.Replace:
                    ConsoleWriter.Update($"\tОбновлён: {copyDescription}");
                    break;

                case FileCopyDecision.Skip:
                    ConsoleWriter.Trace($"\tПропущен: {sourceFileName}"); ;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(decision), decision, null);
            }
        }

        // todo не используется, но может быть полезно для...?
        private sealed record FileOperationResult(FileCopyDecision Decision,
                                                        string SourceFilePath,
                                                        string? DestinationFilePath,
                                                        DateTimeOffset? UpdateTime)
        {
            private bool IsCopied => Decision != FileCopyDecision.Skip;
        }
    }
}