using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Net;

class ProgramGPT
{
    static void Main(string[] args)
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

        int index = 1;

        foreach (var convo in doc.RootElement.EnumerateArray())
        {
            string id = convo.GetProperty("id").GetString() ?? $"chat_{index}";
            string title = convo.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";

            double createTime = convo.TryGetProperty("create_time", out var ct) ? ct.GetDouble() : 0;
            double updateTime = convo.TryGetProperty("update_time", out var ut) ? ut.GetDouble() : 0;

            var mapping = convo.GetProperty("mapping");

            // Build node map
            var nodes = new Dictionary<string, Node>();

            foreach (var item in mapping.EnumerateObject())
            {
                string nodeId = item.Name;
                var obj = item.Value;

                string parent = obj.TryGetProperty("parent", out var p) && p.ValueKind != JsonValueKind.Null
                    ? p.GetString()
                    : null;

                Message msg = null;

                if (obj.TryGetProperty("message", out var m) && m.ValueKind != JsonValueKind.Null)
                {
                    string role = m.GetProperty("author").GetProperty("role").GetString();
                    //double? msgTime = m.TryGetProperty("create_time", out var mt) ? mt.GetDouble() :  null;
                    double? msgTime = null;

                    if (m.TryGetProperty("create_time", out var mt) &&
                        mt.ValueKind == JsonValueKind.Number)
                    {
                        msgTime = mt.GetDouble();
                    }


                    string content = "";
                    if (m.TryGetProperty("content", out var c) &&
                        c.TryGetProperty("parts", out var parts) &&
                        parts.ValueKind == JsonValueKind.Array)
                    {
                        var sb = new StringBuilder();
                        foreach (var part in parts.EnumerateArray())
                            sb.AppendLine(part.GetString());
                        content = sb.ToString();
                    }

                    msg = new Message(role, msgTime, content);
                }

                nodes[nodeId] = new Node(parent, msg);
            }

            // Find last node (leaf)
            string leaf = null;
            var hasChildren = new HashSet<string>();

            foreach (var kv in nodes)
                if (kv.Value.Parent != null)
                    hasChildren.Add(kv.Value.Parent);

            foreach (var id2 in nodes.Keys)
                if (!hasChildren.Contains(id2))
                    leaf = id2;

            // Walk backwards
            var chain = new List<Message>();
            string current = leaf;

            while (current != null && nodes.TryGetValue(current, out var node))
            {
                if (node.Message != null && !string.IsNullOrWhiteSpace(node.Message.Content))
                    chain.Add(node.Message);

                current = node.Parent;
            }

            chain.Reverse();

            // Output file
            string safeTitle = MakeSafeFileName(title);
            string outputPath = Path.Combine(outputFolder, $"{index:000}_{safeTitle}.md");

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);

            // YAML header
            writer.WriteLine("---");
            writer.WriteLine($"id: \"{id}\"");
            writer.WriteLine($"title: \"{EscapeYaml(title)}\"");
            writer.WriteLine($"created: \"{UnixToIso(createTime)}\"");
            writer.WriteLine($"updated: \"{UnixToIso(updateTime)}\"");
            writer.WriteLine("---");
            writer.WriteLine();

            // Body
            foreach (var msg in chain)
            {
                string time = msg.CreateTime.HasValue
                    ? UnixToIso(msg.CreateTime.Value)
                    : "unknown";

                writer.WriteLine($"## {msg.Role.ToUpper()} — {time}");
                writer.WriteLine();
                writer.WriteLine(WebUtility.HtmlEncode(msg.Content.Trim()));
                writer.WriteLine();
            }

            Console.WriteLine($"Saved: {outputPath}");
            index++;
        }
    }

    static string UnixToIso(double unix)
    {
        if (unix <= 0) return "unknown";
        return DateTimeOffset.FromUnixTimeSeconds((long)unix)
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss");
    }

    static string MakeSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return string.IsNullOrWhiteSpace(name) ? "Untitled" : name;
    }

    static string EscapeYaml(string text)
        => text.Replace("\"", "\\\"");

    record Node(string Parent, Message Message);
    record Message(string Role, double? CreateTime, string Content);
}