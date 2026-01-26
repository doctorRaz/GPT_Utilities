using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

// https://forum.obsidian.md/t/sorting-files-by-date-dd-mm-yyyy-dataview/48762
class Program2
{
    /// <summary>
    /// Defines the entry point of the application.<br/>
    /// заработал экран
    /// </summary>
    /// <param name="args">The arguments.</param>
    static void Main2(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: gpt2md <conversations.json> <outputFolder>");
            return;
        }

        string inputPath = args[0];
        string outputFolder = args[1];

        Directory.CreateDirectory(outputFolder);

        using FileStream fs = File.OpenRead(inputPath);
        using JsonDocument doc = JsonDocument.Parse(fs);

        foreach (var convo in doc.RootElement.EnumerateArray())
        {
            string id = GetString(convo, "id") ?? Guid.NewGuid().ToString();
            string title = GetString(convo, "title") ?? "Untitled";

            double createTime = GetDouble(convo, "create_time");
            double updateTime = GetDouble(convo, "update_time");

            if (!convo.TryGetProperty("mapping", out var mapping))
                continue;

            var nodes = new Dictionary<string, Node>();

            // Parse mapping nodes
            foreach (var item in mapping.EnumerateObject())
            {
                string nodeId = item.Name;
                var obj = item.Value;

                string? parent = obj.TryGetProperty("parent", out var p) && p.ValueKind == JsonValueKind.String
                    ? p.GetString()
                    : null;

                Message msg = null;

                if (obj.TryGetProperty("message", out var m) && m.ValueKind != JsonValueKind.Null)
                {
                    string role = GetRole(m);
                    double? msgTime = GetNullableDouble(m, "create_time");
                    string content = GetContent(m);

                    msg = new Message(role, msgTime, content);
                }

                nodes[nodeId] = new Node(parent, msg);
            }

            // Find leaf node
            var hasChildren = new HashSet<string>();
            foreach (var kv in nodes)
                if (kv.Value.Parent != null)
                    hasChildren.Add(kv.Value.Parent);

            string leaf = null;
            foreach (var key in nodes.Keys)
                if (!hasChildren.Contains(key))
                    leaf = key;

            if (leaf == null)
                continue;

            // Walk chain backwards
            var chain = new List<Message>();
            string current = leaf;

            while (current != null && nodes.TryGetValue(current, out var node))
            {
                if (node.Message != null && !string.IsNullOrWhiteSpace(node.Message.Content))
                    chain.Add(node.Message);

                current = node.Parent;
            }

            chain.Reverse();

            // Build filename: date_title.md
            string datePrefix = GetDatePrefix(createTime, updateTime);
            string safeTitle = MakeSafeFileName(title);
            string outputPath = Path.Combine(outputFolder, $"{datePrefix}_{safeTitle}.md");

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

            // YAML header
            writer.WriteLine("---");
            writer.WriteLine($"id: \"{EscapeYaml(id)}\"");
            writer.WriteLine($"title: \"{EscapeYaml(title)}\"");
            writer.WriteLine($"created: \"{UnixToIso(createTime)}\"");
            writer.WriteLine($"updated: \"{UnixToIso(updateTime)}\"");
            writer.WriteLine("---");
            writer.WriteLine();

            // Messages
            foreach (var msg in chain)
            {
                string time = msg.CreateTime.HasValue
                    ? UnixToIso(msg.CreateTime.Value)
                    : "unknown";

                writer.WriteLine($"## {msg.Role.ToUpper()} — {time}");
                writer.WriteLine();
                writer.WriteLine(msg.Content.Trim());
                writer.WriteLine();
            }

            Console.WriteLine($"Saved: {outputPath}");
        }
    }

    // Helpers

    static string GetString(JsonElement obj, string prop)
        => obj.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;

    static double GetDouble(JsonElement obj, string prop)
        => obj.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number
            ? p.GetDouble()
            : 0;

    static double? GetNullableDouble(JsonElement obj, string prop)
        => obj.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number
            ? p.GetDouble()
            : null;

    static string GetRole(JsonElement msg)
    {
        if (msg.TryGetProperty("author", out var author) &&
            author.TryGetProperty("role", out var roleProp) &&
            roleProp.ValueKind == JsonValueKind.String)
        {
            return roleProp.GetString();
        }

        return "unknown";
    }

    static string GetContent(JsonElement msg)
    {
        if (msg.TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var parts) &&
            parts.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
                if (part.ValueKind == JsonValueKind.String)
                    sb.AppendLine(part.GetString());

            return sb.ToString();
        }

        return "";
    }

    static string UnixToIso(double unix)
    {
        if (unix <= 0) return "unknown";

        return DateTimeOffset
            .FromUnixTimeSeconds((long)unix)
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss");
    }

    static string GetDatePrefix(double create, double update)
    {
        double unix = create > 0 ? create : update;

        if (unix <= 0)
            return "unknown";

        return DateTimeOffset
            .FromUnixTimeSeconds((long)unix)
            .ToLocalTime()
            .ToString("yyyy-MM-dd");
    }

    static string MakeSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return string.IsNullOrWhiteSpace(name) ? "Untitled" : name;
    }

    static string EscapeYaml(string text)
        => text?.Replace("\"", "\\\"") ?? "";

    record Node(string Parent, Message Message);
    record Message(string Role, double? CreateTime, string Content);
}
