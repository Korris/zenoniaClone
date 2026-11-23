using System;
using System.Collections.Generic;
using System.Text;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// Reads server-api answers without a JSON library (pure C#, so the tests compile it): the error fields of any refusal, and a tiny object / array splitter for the wager answers, whose `state` is a whole
    /// character that nobody here needs to parse. Errors are read by hand because `details` is an object on one error and a list on another.
    /// </summary>
    public static class ZoneApiReplyParser
    {
        const int MaxErrorText = 400;

        public static ZoneApiReply Reply(int status, string text)
        {
            var r = new ZoneApiReply { Status = status, CurrentRev = -1, Rev = -1, RevB = -1 };
            if (r.Ok || status == 0) return r;
            r.Code = TextAfter(text, "\"code\":\"");
            r.CurrentRev = IntAfter(text, "\"currentRev\":");
            r.Message = text == null ? "" : text.Length > MaxErrorText ? text.Substring(0, MaxErrorText) : text;
            return r;
        }

        public static string TextAfter(string text, string key)
        {
            if (text == null) return null;
            int i = text.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return null;
            i += key.Length;
            int end = text.IndexOf('"', i);
            return end < 0 ? null : text.Substring(i, end - i);
        }

        public static int IntAfter(string text, string key)
        {
            if (text == null) return -1;
            int i = text.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return -1;
            i += key.Length;
            int v = 0, n = 0;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9' && n++ < 9) v = v * 10 + (text[i++] - '0');
            return n > 0 ? v : -1;
        }

        /// <summary>The top-level members of a JSON object as raw text (strings keep their quotes, objects and arrays their brackets). Null when `json` is not an object.</summary>
        public static Dictionary<string, string> Members(string json)
        {
            if (json == null) return null;
            int i = SkipSpace(json, 0);
            if (i >= json.Length || json[i] != '{') return null;
            var d = new Dictionary<string, string>();
            i++;
            while (true)
            {
                i = SkipSpace(json, i);
                if (i >= json.Length) return null;
                if (json[i] == '}') return d;
                if (json[i] == ',') { i++; continue; }
                if (json[i] != '"') return null;
                int keyEnd = EndOfString(json, i);
                if (keyEnd < 0) return null;
                string key = json.Substring(i + 1, keyEnd - i - 1);
                i = SkipSpace(json, keyEnd + 1);
                if (i >= json.Length || json[i] != ':') return null;
                i = SkipSpace(json, i + 1);
                int end = EndOfValue(json, i);
                if (end < 0) return null;
                d[key] = json.Substring(i, end - i);
                i = end;
            }
        }

        /// <summary>The elements of a JSON array as raw text; null when it is not an array.</summary>
        public static List<string> Elements(string json)
        {
            if (json == null) return null;
            int i = SkipSpace(json, 0);
            if (i >= json.Length || json[i] != '[') return null;
            var list = new List<string>();
            i++;
            while (true)
            {
                i = SkipSpace(json, i);
                if (i >= json.Length) return null;
                if (json[i] == ']') return list;
                if (json[i] == ',') { i++; continue; }
                int end = EndOfValue(json, i);
                if (end < 0) return null;
                list.Add(json.Substring(i, end - i));
                i = end;
            }
        }

        /// <summary>A raw JSON string value without quotes (simple escapes only: ids and codes never need more), null when it is not a string.</summary>
        public static string Unquote(string raw)
        {
            if (raw == null || raw.Length < 2 || raw[0] != '"' || raw[raw.Length - 1] != '"') return null;
            return raw.Substring(1, raw.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        public static int ToInt(string raw, int fallback)
        {
            int v;
            return raw != null && int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        static int SkipSpace(string s, int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; return i; }

        // index of the closing quote of the string that starts at i, -1 if it never closes
        static int EndOfString(string s, int i)
        {
            for (i++; i < s.Length; i++)
            {
                if (s[i] == '\\') { i++; continue; }
                if (s[i] == '"') return i;
            }
            return -1;
        }

        // index just after the value that starts at i, -1 if it is cut short
        static int EndOfValue(string s, int i)
        {
            if (i >= s.Length) return -1;
            char c = s[i];
            if (c == '"') { int e = EndOfString(s, i); return e < 0 ? -1 : e + 1; }
            if (c == '{' || c == '[')
            {
                int depth = 0;
                for (; i < s.Length; i++)
                {
                    char x = s[i];
                    if (x == '"') { i = EndOfString(s, i); if (i < 0) return -1; }
                    else if (x == '{' || x == '[') depth++;
                    else if (x == '}' || x == ']') { if (--depth == 0) return i + 1; }
                }
                return -1;
            }
            while (i < s.Length && s[i] != ',' && s[i] != '}' && s[i] != ']' && !char.IsWhiteSpace(s[i])) i++;
            return i;
        }

        /// <summary>A JSON string literal for `s` (quotes, backslashes and control characters escaped).</summary>
        public static string Str(string s)
        {
            var b = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                if (c == '"' || c == '\\') b.Append('\\').Append(c);
                else if (c < 32) b.Append("\\u").Append(((int)c).ToString("x4"));
                else b.Append(c);
            }
            return b.Append('"').ToString();
        }
    }
}
