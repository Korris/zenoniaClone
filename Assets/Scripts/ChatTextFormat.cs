using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Turns chat history into one rich-text block: "[World] Name: text" with a stable colour per sender name.</summary>
    public static class ChatTextFormat
    {
        static readonly string[] NameColors = { "4FD8FF", "A6F04F", "FF6CF0", "FFB347", "FFE066", "8FA8FF", "FF8A80" };
        const string SelfColor = "7FE3FF", SystemColor = "FFD36B", TagColor = "AEB6C6", TextColor = "F2EEDD";

        /// <summary>Last `max` lines of `tab` (the World tab also lists system lines). Lines older than `fadeAfter` s are drawn at `oldAlpha` (0 = never fade).</summary>
        public static string Build(IList<ChatMessage> history, ChatChannel tab, int max, float fadeAfter = 0f, float oldAlpha = 1f)
        {
            var picked = new List<ChatMessage>();
            for (int i = history.Count - 1; i >= 0 && picked.Count < max; i--)
            {
                ChatChannel c = history[i].Channel;
                if (c == tab || (tab == ChatChannel.World && c == ChatChannel.System)) picked.Insert(0, history[i]);
            }
            var sb = new StringBuilder();
            for (int i = 0; i < picked.Count; i++)
            {
                ChatMessage m = picked[i];
                string a = fadeAfter > 0f && Time.unscaledTime - m.Time > fadeAfter ? ((int)(oldAlpha * 255f)).ToString("X2") : "FF";
                if (i > 0) sb.Append('\n');
                if (m.Channel == ChatChannel.System) sb.Append("<color=#" + SystemColor + a + ">").Append(Escape(m.Text)).Append("</color>");
                else sb.Append("<color=#" + TagColor + a + ">[").Append(m.Channel).Append("]</color> <color=#").Append(ColorOf(m.Sender)).Append(a).Append('>')
                    .Append(Escape(m.Sender)).Append("</color><color=#" + TextColor + a + ">: ").Append(Escape(m.Text)).Append("</color>");
            }
            return sb.ToString();
        }

        static string ColorOf(string sender)
        {
            if (string.IsNullOrEmpty(sender)) return TextColor;
            if (sender == ChatHub.LocalName()) return SelfColor;
            int h = 17;
            for (int i = 0; i < sender.Length; i++) h = h * 31 + sender[i];
            return NameColors[(h & 0x7fffffff) % NameColors.Length];
        }

        /// <summary>Rich text is on: break up any "<" a player typed so it cannot inject tags.</summary>
        static string Escape(string s) { return string.IsNullOrEmpty(s) ? "" : s.Replace("<", "<​"); }
    }
}
