using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Chat history as rich text: "[World] Name: text" with the channel tag in its own colour (World yellow, Map green, Party cyan,
    /// Whisper violet, system lines gold). The World tab lists every channel (like the approved design); the other tabs only their own.
    /// The Adventure Paper chat draws the same lines with its own colours (ChatPalette: over the world, or dark ink on the board's paper).
    /// </summary>
    public static class ChatTextFormat
    {
        /// <summary>Old look, on the dark panel and its strips.</summary>
        public static readonly ChatPalette Classic = new ChatPalette("FFD84A", "8BE36B", "4FD8FF", "E07CFF", SystemColor, SelfColor, NameColor, TextColor, false);
        /// <summary>Paper look, floating lines over the world (mockup: World yellow, Map green, Party purple, Whisper pink).</summary>
        public static readonly ChatPalette Feed = new ChatPalette("FFD84A", "8BE36B", "C9A2FF", "FF8ED6", SystemColor, SelfColor, "FFFFFF", "FFFFFF", true);
        /// <summary>Paper look, ink on the board's parchment.</summary>
        public static readonly ChatPalette Paper = new ChatPalette("B5730D", "23883A", "7B3FC4", "C2379B", "C4470A", "1C61B4", "3E180B", "3E180B", true);

        const string SelfColor = "7FE3FF", NameColor = "F5D9A0", SystemColor = "FFC75A", TextColor = "F2EEDD";

        public static string TagColor(ChatChannel c) { return Classic.Tag(c); }

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
        public static string Line(ChatMessage m) { return Line(m, Classic); }

        public static string Line(ChatMessage m, ChatPalette p)
        {
            string tag = m.Channel == ChatChannel.System ? "System" : m.Channel.ToString();
            var sb = new StringBuilder();
            if (p.BoldHead) sb.Append("<b>");
            sb.Append("<color=#").Append(p.Tag(m.Channel)).Append(">[").Append(tag).Append("]</color> ");
            if (m.Channel != ChatChannel.System && !string.IsNullOrEmpty(m.Sender))
                sb.Append("<color=#").Append(m.Sender == ChatHub.LocalName() ? p.Self : p.Name).Append('>').Append(Escape(m.Sender)).Append(":</color> ");
            if (p.BoldHead) sb.Append("</b>");
            return sb.Append("<color=#").Append(m.Channel == ChatChannel.System ? p.System : p.Text).Append('>').Append(Escape(m.Text)).Append("</color>").ToString();
        }

        /// <summary>Rich text is on: break up any "<" a player typed so it cannot inject tags.</summary>
        static string Escape(string s) { return string.IsNullOrEmpty(s) ? "" : s.Replace("<", "<​"); }
    }

    /// <summary>Colours (RRGGBB) of one chat look: the channel tags, system lines, the local player's name, other names, the text.</summary>
    public sealed class ChatPalette
    {
        public readonly string System, Self, Name, Text;
        /// <summary>Tag and sender in bold (the paper look).</summary>
        public readonly bool BoldHead;
        readonly string[] tags;

        public ChatPalette(string world, string map, string party, string whisper, string system, string self, string name, string text, bool boldHead)
        {
            tags = new[] { world, map, party, whisper, system }; // ChatChannel order
            System = system; Self = self; Name = name; Text = text; BoldHead = boldHead;
        }

        public string Tag(ChatChannel c) { return tags[(int)c]; }
    }
}
