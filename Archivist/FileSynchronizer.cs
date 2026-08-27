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
        /// <returns></returns>
        internal static bool CopyIfNewer(string sourceFilePath, string destinationFilePath, ChatMetadata sourceMetadata)
        {
            CopyDecision decision = GetCopyDecision(destinationFilePath, sourceMetadata);

            if (decision == CopyDecision.Skip)
            {
                Console.WriteLine(
                    $"Пропущен: {Path.GetFileName(sourceFilePath)}");

                return false;
            }

            // разные IDs — создаём уникальный файл в destination.
            if (decision == CopyDecision.AddUnique)
            {
                destinationFilePath = FileNamer.GetUnique(destinationFilePath);
            }

            // Единственная операция копирования.
            File.Copy(
                sourceFilePath,
                destinationFilePath,
                overwrite: true);

            // Сохраняем UpdateTime исходного файла.
            if (sourceMetadata.UpdateTime.HasValue)
            {
                File.SetLastWriteTime(
                    destinationFilePath,
                    sourceMetadata.UpdateTime.Value.LocalDateTime);
            }

            string exo = $"{Path.GetFileName(sourceFilePath)}" +
                        $"\n\tupdate_time: {sourceMetadata.UpdateTime:yyyy-MM-dd-HH.mm.sss}" +
                        $"\n\tto->{destinationFilePath}";

            Console.WriteLine(
                decision == CopyDecision.Add 
                    ? $"Добавлен: {exo}"
                    : $"Обновлён: {exo}");

            return true;
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

                // Файлы относятся к разным conversation.
                // Исходный файл нельзя перезаписывать.
                if (sourceMetadata.ConversationId != null &&
                    destinationMetadata.ConversationId != null &&
                    sourceMetadata.ConversationId != destinationMetadata.ConversationId)
                {
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
                Console.WriteLine(ex.Message);
                return CopyDecision.AddUnique;
            }
        }
    }
}