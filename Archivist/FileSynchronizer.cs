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
        internal static FileOperationResult CopyIfNewer(string sourceFilePath, string destinationFilePath, ChatMetadata sourceMetadata)
        {
            CopyDecision decision = GetCopyDecision(destinationFilePath, sourceMetadata);

            if (decision == CopyDecision.Skip)
            {
                //результат по файлу
                return new FileOperationResult(decision, sourceFilePath, DestinationFilePath: null, UpdateTime: null);

            }
            // разные IDs — создаём уникальный файл в destination.
            if (decision == CopyDecision.AddUnique)
            {
                destinationFilePath = FileNamer.GetUnique(destinationFilePath);
            }

            // Единственная операция копирования.
            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);

            // Сохраняем UpdateTime исходного файла.
            if (sourceMetadata.UpdateTime.HasValue)
            {
                File.SetLastWriteTime(destinationFilePath, sourceMetadata.UpdateTime.Value.LocalDateTime);
            }

            //результат по файлу
            return new FileOperationResult(decision, sourceFilePath, destinationFilePath,sourceMetadata.UpdateTime);
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
                 //если совпадут проверить UpdateTime
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

        internal sealed record FileOperationResult(CopyDecision Decision,
                                                        string SourceFilePath,
                                                        string? DestinationFilePath,
                                                        DateTimeOffset? UpdateTime)
        {
            public bool IsCopied => Decision != CopyDecision.Skip;
        }
    }
}