using System;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace dRz.GPT_Utilities.Archivist
{
    internal class MetadataReader
    {
        /// <summary>
        /// Десериализатор YAML.
        ///
        /// CamelCaseNamingConvention позволяет сопоставить:
        ///
        ///     create_time
        ///
        /// с:
        ///
        ///     CreateTime
        /// </summary>
        private static readonly IDeserializer YamlDeserializer =
            new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

        /// <summary>
        /// Regex для поиска YAML front matter в начале Markdown-файла.
        ///
        /// Пример:
        ///
        /// ---
        /// create_time: 2026-08-24T15:23:56.473Z
        /// ---
        ///
        /// Группа "yaml" содержит только содержимое front matter.
        /// </summary>
        internal static readonly Regex FrontMatterRegex = new(
            @"\A---\s*\r?\n(?<yaml>.*?)\r?\n---\s*(?:\r?\n|$)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Читает YAML front matter Markdown-файла и преобразует его
        /// в типизированный объект ChatMetadata.
        /// </summary>
        internal static ChatMetadata ReadMetadata(
            string filePath)
        {
            // -------------------------------------------------------------
            // Читаем Markdown целиком.
            //
            // YAML находится в начале файла, поэтому Regex извлекает
            // только front matter.
            // -------------------------------------------------------------
            string content =
                File.ReadAllText(filePath);

            Match match =
                FrontMatterRegex.Match(content);

            if (!match.Success)
            {
                throw new FormatException(
                    $"В файле отсутствует YAML front matter: {filePath}");
            }

            string yaml =
                match.Groups["yaml"].Value;

            // -------------------------------------------------------------
            // Десериализуем YAML в типизированный ChatMetadata.
            // -------------------------------------------------------------
            ChatMetadata? metadata =
                YamlDeserializer.Deserialize<ChatMetadata>(yaml);

            if (metadata is null)
            {
                throw new FormatException(
                    $"Не удалось прочитать YAML: {filePath}");
            }

            // Проверяем, что create_time действительно был получен.
            if (metadata.CreateTime == default)
            {
                throw new FormatException(
                    $"В YAML отсутствует или некорректен " +
                    $"create_time: {filePath}");
            }
            // Проверяем, что update_time действительно был получен.
            if (metadata.UpdateTime == default)
            {
                throw new FormatException(
                    $"В YAML отсутствует или некорректен " +
                    $"update_time: {filePath}");
            }

            // ChatLink необязателен: ConversationId тогда будет null.

            return metadata;
        }
    }
}