using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace dRz.GPT_Utilities.Archivist.Tests.Infrastructure
{
    internal static class MarkdownFactory
    {
        public static string Write(
            string filePath,
            DateTimeOffset createTime,
            DateTimeOffset? updateTime = null,
            string? chatLink = "https://chatgpt.com/c/11111111-1111-1111-1111-111111111111",
            string body = "body")
        {
            _ = Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath)!);

            StringBuilder yaml = new();
            _ = yaml.AppendLine("---");
            _ = yaml.AppendLine($"create_time: {Format(createTime)}");

            if (updateTime.HasValue)
            {
                _ = yaml.AppendLine($"update_time: {Format(updateTime.Value)}");
            }

            if (chatLink is not null)
            {
                _ = yaml.AppendLine($"chat_link: {chatLink}");
            }

            _ = yaml.AppendLine("---");
            _ = yaml.AppendLine();
            _ = yaml.AppendLine(body);

            File.WriteAllText(filePath, yaml.ToString());

            return filePath;
        }

        private static string Format(DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString(
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                CultureInfo.InvariantCulture);
        }
    }
}
