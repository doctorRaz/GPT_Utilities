using dRz.GPT_Utilities.Archivist.Services;
using System;
using System.IO;

namespace dRz.GPT_Utilities.Archivist
{
    internal class FileSynchronizer
    {
        /// <summary>Copies if newer.</summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <param name="destinationFilePath">The destination file path.</param>
        /// <param name="sourceMetadata">The source update time.</param>
        /// <returns> The copy result. </returns>
        internal static CopyDecision CopyIfNewer(string sourceFilePath, string destinationFilePath, ChatMetadata sourceMetadata)
        {
            CopyDecision decision = GetCopyDecision(destinationFilePath, sourceMetadata);

            if (decision == CopyDecision.Skip)
            {
                //вывод в консоль результата копирования
                WriteCopyResult(decision, sourceFilePath, destinationFilePath: null, updateTime: null);
                return decision;
            }
            // разные IDs — создаём уникальный файл в destination.
            if (decision == CopyDecision.AddUnique)
            {
                destinationFilePath = FileNamer.GetUnique(destinationFilePath);
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

        private static CopyDecision GetCopyDecision(string destinationFilePath, ChatMetadata sourceMetadata)
        {
            // Destination-файла ещё нет.
            if (!File.Exists(destinationFilePath))
            {
                return CopyDecision.Add;
            }

            try
            {
                // Читаем metadata существующего файла.
                ChatMetadata destinationMetadata = MetadataReader.ReadMetadata(destinationFilePath);

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
                    return CopyDecision.AddUnique;
                }

                // Нет даты destination —
                // считаем файл требующим обновления.
                if (!destinationMetadata.UpdateTime.HasValue)
                {
                    return CopyDecision.Replace;
                }

                // Нет даты source —
                // сравнить файлы невозможно.
                if (!sourceMetadata.UpdateTime.HasValue)
                {
                    return CopyDecision.Skip;
                }

                // Обновляем только если source действительно новее.
                return sourceMetadata.UpdateTime.Value > destinationMetadata.UpdateTime.Value
                    ? CopyDecision.Replace
                    : CopyDecision.Skip;
            }
            catch (Exception ex)
            {
                // Не удалось прочитать metadata destination.
                // Безопаснее добавить копию файла.
                ConsoleWriter.Error(ex.Message);
                return CopyDecision.AddUnique;
            }
        }

        //private static void WriteCopyResult(FileOperationResult fileOperationResult)
        private static void WriteCopyResult(CopyDecision decision, string sourceFilePath, string? destinationFilePath, DateTimeOffset? updateTime)
        {
            //вывод в консоль
            string sourseFileName = Path.GetFileName(sourceFilePath);

            string exo = $"{sourseFileName}" +
                        $"\n\t\tupdate_time: {updateTime:yyyy-MM-dd-HH.mm.sss}" +
                        $"\n\t\tto->{destinationFilePath}";

            switch (decision)
            {
                case CopyDecision.Add:
                    ConsoleWriter.Success($"\tДобавлен: {exo}");
                    break;

                case CopyDecision.AddUnique:
                    ConsoleWriter.Warn($"\tДобавлен уникальный: {exo}");
                    break;

                case CopyDecision.Replace:
                    ConsoleWriter.Update($"\tОбновлён: {exo}");
                    break;

                case CopyDecision.Skip:
                    ConsoleWriter.Trace($"\tПропущен: {sourseFileName}"); ;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(decision), decision, null);
            }
        }

        // todo не используется, но может быть полезно для...?
        private sealed record FileOperationResult(CopyDecision Decision,
                                                        string SourceFilePath,
                                                        string? DestinationFilePath,
                                                        DateTimeOffset? UpdateTime)
        {
            private bool IsCopied => Decision != CopyDecision.Skip;
        }
    }
}