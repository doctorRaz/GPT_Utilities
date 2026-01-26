using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace JsonToMarkdown
{
    class Program_
    {
        //static void Main_(string[] args)
        static void JsonToMd(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: gpt2md <conversations.json> <outputFolder>");
                return;
            }

            Directory.CreateDirectory(args[1]);

            using var fs = File.OpenRead(args[0]);
            using var doc = JsonDocument.Parse(fs);

            foreach (var convo in doc.RootElement.EnumerateArray())
            {
                string? id = convo.TryGetProperty("id", out var idp) ? idp.GetString() : Guid.NewGuid().ToString();
                string? title = convo.TryGetProperty("title", out var tp) ? tp.GetString() : "Untitled";

                double ctime = GetNum(convo, "create_time");
                double utime = GetNum(convo, "update_time");

                if (!convo.TryGetProperty("mapping", out var mapping))
                    continue;

                var nodes = new Dictionary<string, (string Parent, Msg Msg)>();

                foreach (var n in mapping.EnumerateObject())
                {
                    var obj = n.Value;
                    string? parent = obj.TryGetProperty("parent", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

                    Msg msg = null;

                    if (obj.TryGetProperty("message", out var m) && m.ValueKind != JsonValueKind.Null)
                    {
                        string? role = m.TryGetProperty("author", out var a) &&
                                      a.TryGetProperty("role", out var r) &&
                                      r.ValueKind == JsonValueKind.String ? r.GetString() : "unknown";

                        if (role == "user")
                        {
                            role = "👤 User";
                        }
                        else if (role == "assistant")
                        {
                            role = "🤖 Assistant";
                        }

                        double? mt = m.TryGetProperty("create_time", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetDouble() : null;

                        string content = "";
                        if (m.TryGetProperty("content", out var c) &&
                            c.TryGetProperty("parts", out var parts) &&
                            parts.ValueKind == JsonValueKind.Array)
                        {
                            var sb = new StringBuilder();
                            foreach (var part in parts.EnumerateArray())
                                if (part.ValueKind == JsonValueKind.String)
                                    sb.AppendLine(part.GetString());
                            content = sb.ToString();
                        }

                        msg = new Msg(role, mt, content);
                    }

                    nodes[n.Name] = (parent, msg);
                }

                var hasChildren = new HashSet<string>();
                foreach (var kv in nodes)
                    if (kv.Value.Parent != null)
                        hasChildren.Add(kv.Value.Parent);

                string leaf = null;
                foreach (var k in nodes.Keys)
                    if (!hasChildren.Contains(k))
                        leaf = k;

                if (leaf == null) continue;

                var chain = new List<Msg>();
                for (string cur = leaf; cur != null && nodes.TryGetValue(cur, out var node); cur = node.Parent)
                    if (node.Msg != null && !string.IsNullOrWhiteSpace(node.Msg.Content))
                        chain.Add(node.Msg);

                chain.Reverse();
                if (chain.Count == 0) continue;

                string datePrefix = DateStr(ctime > 0 ? ctime : utime);
                string safeTitle = SafeName(title);
                string path = Path.Combine(args[1], $"{datePrefix}_{safeTitle}.md");

                int msgCount = chain.Count;
                string url = $"https://chat.openai.com/c/{id}";
                string project = DetectProject(title);
                string[] tags = Tags(title);

                using (var w = new StreamWriter(path, false, Encoding.UTF8))
                {
                    w.WriteLine("---");
                    w.WriteLine($"title: \"{Yaml(title)}\"");
                    //w.WriteLine($"id: \"{Yaml(id)}\"");
                    w.WriteLine($"project: \"{Yaml(project)}\"");
                    w.WriteLine($"url: \"{url}\"");
                    w.WriteLine($"created: \"{Iso(ctime)}\"");
                    w.WriteLine($"updated: \"{Iso(utime)}\"");
                    w.WriteLine($"message_count: {msgCount}");
                    w.WriteLine("tags:");
                    foreach (var t in tags) w.WriteLine($"  - \"{Yaml(t)}\"");
                    w.WriteLine("---");

                    w.WriteLine("---");
                    foreach (var m in chain)
                    {
                        string time = m.Time.HasValue ? Iso(m.Time.Value) : "unknown";
                        w.WriteLine($"## {m.Role/*.ToUpper()*/}");
                        w.WriteLine($"Date: {time}\n");
                        w.WriteLine(EscapeSmart(m.Content.Trim()));

                        w.WriteLine("---");
                    }
                }

                // Set file times from first/last message
                var first = chain[0].Time;
                var last = chain[^1].Time;

                if (first.HasValue)
                    File.SetCreationTime(path, FromUnix(first.Value));

                if (last.HasValue)
                    File.SetLastWriteTime(path, FromUnix(last.Value));

                Console.WriteLine($"Saved: {path}");
            }
        }

        // -------- Helpers (compact) --------

        static double GetNum(JsonElement e, string p)
            => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

        static string Iso(double u)
            => u <= 0 ? "unknown" : DateTimeOffset.FromUnixTimeSeconds((long)u).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        static DateTime FromUnix(double u)
            => DateTimeOffset.FromUnixTimeSeconds((long)u).ToLocalTime().DateTime;

        static string DateStr(double u)
            => u <= 0 ? "unknown" : DateTimeOffset.FromUnixTimeSeconds((long)u).ToLocalTime().ToString("yyyy-MM-dd");

        static string SafeName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return string.IsNullOrWhiteSpace(s) ? "Untitled" : s;
        }

        static string Yaml(string s) => s?.Replace("\"", "\\\"") ?? "";

        static string DetectProject(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return "General";
            if (t.Contains("NLog", StringComparison.OrdinalIgnoreCase)) return "NLog";
            if (t.Contains("CAD", StringComparison.OrdinalIgnoreCase) || t.Contains("nanoCad", StringComparison.OrdinalIgnoreCase)) return "CAD";
            if (t.Contains("GPT", StringComparison.OrdinalIgnoreCase)) return "GPT";
            return "General";
        }

        static string[] Tags(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return Array.Empty<string>();
            var list = new List<string>();
            foreach (var p in t.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var c = p.Trim(',', '.', ':', ';');
                if (c.Length > 2) list.Add(c);
            }
            return list.ToArray();
        }

        // Smart escape: < > & only in normal text
        static string EscapeSmart(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var sb = new StringBuilder();
            bool block = false, inline = false, sq = false;

            using var r = new StringReader(text);
            string line;

            while ((line = r.ReadLine()) != null)
            {
                if (line.TrimStart().StartsWith("```"))
                {
                    block = !block;
                    sb.AppendLine(line);
                    continue;
                }

                if (block) { sb.AppendLine(line); continue; }

                var ls = new StringBuilder();
                foreach (var c in line)
                {
                    if (c == '`') { inline = !inline; ls.Append(c); continue; }
                    if (c == '\'') { sq = !sq; ls.Append(c); continue; }

                    if (inline || sq) { ls.Append(c); continue; }

                    if (c == '<') ls.Append("&lt;");
                    else if (c == '>') ls.Append("&gt;");
                    else if (c == '&') ls.Append("&amp;");
                    else ls.Append(c);
                }

                sb.AppendLine(ls.ToString());
            }

            return sb.ToString();
        }

        record Msg(string Role, double? Time, string Content);
    }
}