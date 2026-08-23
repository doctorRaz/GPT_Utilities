using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace dRz.GPT_Utilities.GPTJson2Md
{
    public class GPTJson2Md
    {
        public static void Json2MdParser(string fileJson, string folderOut)
        {
            int countFiiles = 0;

            using FileStream fs = File.OpenRead(fileJson);
            using JsonDocument doc = JsonDocument.Parse(fs);

            foreach (JsonElement convo in doc.RootElement.EnumerateArray())
            {
                string? id = convo.TryGetProperty("id", out JsonElement idp) ? idp.GetString() : Guid.NewGuid().ToString();
                string? title = convo.TryGetProperty("title", out JsonElement tp) ? tp.GetString() : "Untitled";

                double ctime = GetNum(convo, "create_time");
                double utime = GetNum(convo, "update_time");

                if (!convo.TryGetProperty("mapping", out JsonElement mapping))
                {
                    continue;
                }

                Dictionary<string, (string Parent, Msg Msg)> nodes = new Dictionary<string, (string Parent, Msg Msg)>();

                foreach (JsonProperty n in mapping.EnumerateObject())
                {
                    JsonElement obj = n.Value;
                    string? parent = obj.TryGetProperty("parent", out JsonElement p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

                    Msg msg = null;

                    if (obj.TryGetProperty("message", out JsonElement m) && m.ValueKind != JsonValueKind.Null)
                    {
                        string? role = m.TryGetProperty("author", out JsonElement a) &&
                                      a.TryGetProperty("role", out JsonElement r) &&
                                      r.ValueKind == JsonValueKind.String ? r.GetString() : "unknown";

                        if (role == "user")
                        {
                            role = "👤 User";
                        }
                        else if (role == "assistant")
                        {
                            role = "🤖 Assistant";
                        }

                        double? mt = m.TryGetProperty("create_time", out JsonElement t) && t.ValueKind == JsonValueKind.Number ? t.GetDouble() : null;

                        string content = "";
                        if (m.TryGetProperty("content", out JsonElement c) &&
                            c.TryGetProperty("parts", out JsonElement parts) &&
                            parts.ValueKind == JsonValueKind.Array)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (JsonElement part in parts.EnumerateArray())
                            {
                                if (part.ValueKind == JsonValueKind.String)
                                {
                                    sb.AppendLine(part.GetString());
                                }
                            }

                            content = sb.ToString();
                        }

                        msg = new Msg(role, mt, content);
                    }

                    nodes[n.Name] = (parent, msg);
                }

                HashSet<string> hasChildren = new HashSet<string>();
                foreach (KeyValuePair<string, (string Parent, Msg Msg)> kv in nodes)
                {
                    if (kv.Value.Parent != null)
                    {
                        hasChildren.Add(kv.Value.Parent);
                    }
                }

                string leaf = null;
                foreach (string k in nodes.Keys)
                {
                    if (!hasChildren.Contains(k))
                    {
                        leaf = k;
                    }
                }

                if (leaf == null)
                {
                    continue;
                }

                List<Msg> chain = new List<Msg>();
                for (string cur = leaf; cur != null && nodes.TryGetValue(cur, out (string Parent, Msg Msg) node); cur = node.Parent)
                {
                    if (node.Msg != null && !string.IsNullOrWhiteSpace(node.Msg.Content))
                    {
                        chain.Add(node.Msg);
                    }
                }

                chain.Reverse();
                if (chain.Count == 0)
                {
                    continue;
                }

                string datePrefix = DateStr(ctime > 0 ? ctime : utime);
                string safeTitle = SafeName(title);
                string path = Path.Combine(folderOut, $"{datePrefix}_{safeTitle}.md");

                int msgCount = chain.Count;
                string url = $"https://chat.openai.com/c/{id}";
                string project = DetectProject(title);
                string[] tags = Tags(title);

                using (StreamWriter w = new StreamWriter(path, false, Encoding.UTF8))
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
                    foreach (string t in tags)
                    {
                        w.WriteLine($"  - \"{Yaml(t)}\"");
                    }

                    w.WriteLine("---");

                    w.WriteLine("---");
                    foreach (Msg m in chain)
                    {
                        string time = m.Time.HasValue ? Iso(m.Time.Value) : "unknown";
                        w.WriteLine($"## {m.Role}");
                        w.WriteLine($"Date: {time}\n");
                        w.WriteLine(EscapeSmart(m.Content.Trim()));

                        w.WriteLine("---");
                    }
                }

                // Set file times from first/last message
                double? first = chain[0].Time;
                double? last = chain[^1].Time;

                if (first.HasValue)
                {
                    File.SetCreationTime(path, FromUnix(first.Value));
                }

                if (last.HasValue)
                {
                    File.SetLastWriteTime(path, FromUnix(last.Value));
                }

                countFiiles++;
                Console.WriteLine($"Saved: {path}");
            }
            Console.WriteLine($"Total saved: {countFiiles} files");
        }

        // -------- Helpers (compact) --------

        private static double GetNum(JsonElement e, string p)
            => e.TryGetProperty(p, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

        private static string Iso(double u)
            => u <= 0 ? "unknown" : DateTimeOffset.FromUnixTimeSeconds((long)u).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        private static DateTime FromUnix(double u)
            => DateTimeOffset.FromUnixTimeSeconds((long)u).ToLocalTime().DateTime;

        private static string DateStr(double u)
            => u <= 0 ? "unknown" : DateTimeOffset.FromUnixTimeSeconds((long)u).ToLocalTime().ToString("yyyy-MM-dd");

        private static string SafeName(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(s) ? "Untitled" : s;
        }

        private static string Yaml(string s) => s?.Replace("\"", "\\\"") ?? "";

        private static string DetectProject(string t)
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                return "General";
            }

            if (t.Contains("NLog", StringComparison.OrdinalIgnoreCase))
            {
                return "NLog";
            }

            if (t.Contains("CAD", StringComparison.OrdinalIgnoreCase) || t.Contains("nanoCad", StringComparison.OrdinalIgnoreCase))
            {
                return "CAD";
            }

            if (t.Contains("GPT", StringComparison.OrdinalIgnoreCase))
            {
                return "GPT";
            }

            return "General";
        }

        private static string[] Tags(string t)
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                return Array.Empty<string>();
            }

            List<string> list = new List<string>();
            foreach (string p in t.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string c = p.Trim(',', '.', ':', ';');
                if (c.Length > 2)
                {
                    list.Add(c);
                }
            }
            return list.ToArray();
        }

        // Smart escape: < > & only in normal text
        private static string EscapeSmart(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            StringBuilder sb = new StringBuilder();
            bool block = false, inline = false, sq = false;

            using StringReader r = new StringReader(text);
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

                StringBuilder ls = new StringBuilder();
                foreach (char c in line)
                {
                    if (c == '`') { inline = !inline; ls.Append(c); continue; }
                    if (c == '\'') { sq = !sq; ls.Append(c); continue; }

                    if (inline || sq) { ls.Append(c); continue; }

                    if (c == '<')
                    {
                        ls.Append("&lt;");
                    }
                    else if (c == '>')
                    {
                        ls.Append("&gt;");
                    }
                    else if (c == '&')
                    {
                        ls.Append("&amp;");
                    }
                    else
                    {
                        ls.Append(c);
                    }
                }

                sb.AppendLine(ls.ToString());
            }

            return sb.ToString();
        }

        private record Msg(string Role, double? Time, string Content);
    }
}