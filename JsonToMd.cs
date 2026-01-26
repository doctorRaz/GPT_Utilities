using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

var inputPath = "conversations.json";
var outputDir = "out";

Directory.CreateDirectory(outputDir);

using var doc = JsonDocument.Parse(File.ReadAllText(inputPath));
var root = doc.RootElement;

// Экспорт ChatGPT обычно содержит массив conversation-объектов
foreach (var conv in root.EnumerateArray())
{
    if (!conv.TryGetProperty("title", out var titleProp))
        continue;

    var title = Sanitize(titleProp.GetString() ?? "chat");
    var createTime = conv.TryGetProperty("create_time", out var ct)
        ? DateTimeOffset.FromUnixTimeSeconds((long)ct.GetDouble()).DateTime
        : DateTime.MinValue;

    var sb = new StringBuilder();
    sb.AppendLine($"# {createTime:yyyy-MM-dd} — {title}");
    sb.AppendLine();

    if (conv.TryGetProperty("mapping", out var mapping))
    {
        foreach (var node in mapping.EnumerateObject())
        {
            if (!node.Value.TryGetProperty("message", out var msg))
                continue;

            if (msg.ValueKind==JsonValueKind.Null || !msg.TryGetProperty("author", out var author) ||
                !author.TryGetProperty("role", out var roleProp))
                continue;

            var role = roleProp.GetString();
            if (!msg.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts))
                continue;

            var text = string.Join("\n", parts.EnumerateArray()
                .Select(p => p.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            if (string.IsNullOrWhiteSpace(text))
                continue;

            sb.AppendLine(role == "user" ? "## 👤 User" : "## 🤖 Assistant");
            sb.AppendLine(text);
            sb.AppendLine();
        }
    }

    var fileName = $"{createTime:yyyy-MM-dd}_{title}.md";
    File.WriteAllText(Path.Combine(outputDir, fileName), sb.ToString(), Encoding.UTF8);
}

static string Sanitize(string s)
{
    foreach (var c in Path.GetInvalidFileNameChars())
        s = s.Replace(c, '_');
    return s.Trim();
}