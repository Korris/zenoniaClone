using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Chat history as rich text: "[World] Name: text" with the channel tag in its own colour (World yellow, Map green, Party cyan,
    /// Whisper violet, system lines gold). The World tab lists every channel (like the approved design); the other tabs only their own.
    /// </summary>
    public static class ChatTextFormat
    {
        const string SelfColor = "7FE3FF", NameColor = "F5D9A0", SystemColor = "FFC75A", TextColor = "F2EEDD";

        public static string TagColor(ChatChannel c)
        {
            switch (c)
            {
                case ChatChannel.World: return "FFD84A";
                case ChatChannel.Map: return "8BE36B";
                case ChatChannel.Party: return "4FD8FF";
                case ChatChannel.Whisper: return "E07CFF";
                default: return SystemColor;
            }
        }

        public static bool Shows(ChatChannel tab, ChatChannel c) { return tab == ChatChannel.World || c == tab; }

        /// <summary>The last `max` lines of `tab`, oldest first.</summary>
        public static List<ChatMessage> Recent(IList<ChatMessage> history, ChatChannel tab, int max)
        {
            var picked = new List<ChatMessage>();
            for (int i = history.Count - 1; i >= 0 && picked.Count < max; i--)
                if (Shows(tab, history[i].Channel)) picked.Insert(0, history[i]);
            return picked;
        }

        /// <summary>Last `max` lines of `tab` as one block.</summary>
        public static string Build(IList<ChatMessage> history, ChatChannel tab, int max)
        {
            List<ChatMessage> picked = Recent(history, tab, max);
            var sb = new StringBuilder();
            for (int i = 0; i < picked.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(Line(picked[i]));
            }
            return sb.ToString();
        }

        /// <summary>One message: coloured channel tag, sender, text.</summary>
        public static string Line(ChatMessage m)
        {
            string tag = m.Channel == ChatChannel.System ? "System" : m.Channel.ToString();
            var sb = new StringBuilder();
            sb.Append("<color=#").Append(TagColor(m.Channel)).Append(">[").Append(tag).Append("]</color> ");
            if (m.Channel == ChatChannel.System) return sb.Append("<color=#" + SystemColor + ">").Append(Escape(m.Text)).Append("</color>").ToString();
            if (!string.IsNullOrEmpty(m.Sender))
                sb.Append("<color=#").Append(m.Sender == ChatHub.LocalName() ? SelfColor : NameColor).Append('>').Append(Escape(m.Sender)).Append(":</color> ");
            return sb.Append("<color=#" + TextColor + ">").Append(Escape(m.Text)).Append("</color>").ToString();
        }

        /// <summary>Rich text is on: break up any "<" a player typed so it cannot inject tags.</summary>
        static string Escape(string s) { return string.IsNullOrEmpty(s) ? "" : s.Replace("<", "<​"); }
    }
}
