using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Counters, sampled series (tick ms, snapshot bytes ...) and event marks of one network process. `--net-stats file.json` writes the summary on
    /// exit; Tools/net-check-summary.mjs checks the budgets (tick p99, KB/s per client, snapshot cap, exceptions, boss seen by everybody).
    /// </summary>
    public sealed class NetStats
    {
        public static NetStats Current;
        readonly Dictionary<string, double> values = new Dictionary<string, double>();
        readonly Dictionary<string, List<float>> samples = new Dictionary<string, List<float>>();
        readonly Dictionary<string, List<string>> marks = new Dictionary<string, List<string>>();
        readonly Dictionary<string, string> text = new Dictionary<string, string>();
        readonly float startRealtime = Time.realtimeSinceStartup;

        public NetStats(string role)
        {
            text["role"] = role;
            Application.logMessageReceived += OnLog;
        }

        public void Dispose() { Application.logMessageReceived -= OnLog; }

        void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Exception) return;
            Add("exceptions", 1);
            Mark("exceptionTexts", condition.Length > 200 ? condition.Substring(0, 200) : condition, 5);
        }

        public static long UtcMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }

        public void Add(string key, double delta) { double v; values.TryGetValue(key, out v); values[key] = v + delta; }
        public void Max(string key, double v) { double o; if (!values.TryGetValue(key, out o) || v > o) values[key] = v; }
        public void Set(string key, double v) { values[key] = v; }
        public void SetText(string key, string v) { text[key] = v; }
        public double Get(string key) { double v; return values.TryGetValue(key, out v) ? v : 0.0; }

        public void Sample(string key, float v)
        {
            List<float> l;
            if (!samples.TryGetValue(key, out l)) samples[key] = l = new List<float>(4096);
            if (l.Count < 200000) l.Add(v);
        }

        public void Mark(string key, string v, int cap = 200)
        {
            List<string> l;
            if (!marks.TryGetValue(key, out l)) marks[key] = l = new List<string>();
            if (l.Count < cap) l.Add(v);
        }

        public void OnSnapshotSent(int bytes)
        {
            Add("snapshotsSent", 1); Add("snapshotBytes", bytes); Max("snapshotMaxBytes", bytes);
            if (bytes > ZenoniaProto.Net.NetProtocol.SnapshotMaxBytes) Add("snapshotsOverCap", 1);
        }

        public void OnEventBatchSent(int bytes, int events) { Add("eventBatchesSent", 1); Add("eventBytes", bytes); Add("eventsSent", events); }

        static string F(double v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

        static string Esc(string s)
        {
            var sb = new StringBuilder(s.Length + 2);
            foreach (char c in s) { if (c == '"' || c == '\\') sb.Append('\\').Append(c); else if (c < 32) sb.Append(' '); else sb.Append(c); }
            return sb.ToString();
        }

        public string ToJson()
        {
            var sb = new StringBuilder(2048);
            sb.Append("{\n");
            foreach (KeyValuePair<string, string> kv in text) sb.Append("  \"").Append(kv.Key).Append("\": \"").Append(Esc(kv.Value)).Append("\",\n");
            sb.Append("  \"uptimeSeconds\": ").Append(F(Time.realtimeSinceStartup - startRealtime));
            foreach (KeyValuePair<string, double> kv in values) sb.Append(",\n  \"").Append(kv.Key).Append("\": ").Append(F(kv.Value));
            foreach (KeyValuePair<string, List<float>> kv in samples)
            {
                List<float> l = new List<float>(kv.Value);
                l.Sort();
                int n = l.Count;
                if (n == 0) continue;
                double sum = 0; foreach (float f in l) sum += f;
                sb.Append(",\n  \"").Append(kv.Key).Append("\": { \"n\": ").Append(n).Append(", \"avg\": ").Append(F(sum / n)).Append(", \"p50\": ").Append(F(l[n / 2]))
                  .Append(", \"p99\": ").Append(F(l[Math.Min(n - 1, (int)(n * 0.99))])).Append(", \"max\": ").Append(F(l[n - 1])).Append(" }");
            }
            foreach (KeyValuePair<string, List<string>> kv in marks)
            {
                sb.Append(",\n  \"").Append(kv.Key).Append("\": [");
                for (int i = 0; i < kv.Value.Count; i++) sb.Append(i > 0 ? ", " : "").Append('"').Append(Esc(kv.Value[i])).Append('"');
                sb.Append(']');
            }
            sb.Append("\n}\n");
            return sb.ToString();
        }

        public void Write(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(path));
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, ToJson());
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] could not write net stats: " + e.Message); }
        }
    }
}
