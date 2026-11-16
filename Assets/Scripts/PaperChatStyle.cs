using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Looks shared by the Adventure Paper chat views (mockup docs/reference/ui-redesign-261116/chatgpt/chat-final-mockup-notice-board.png):
    /// the five channel ribbons in mockup order (All gold, World red, Map green, Party purple, Whisper pink) and the chat font.
    /// A "view" is ChatChannelFilter.All or (int)ChatChannel; ribbon index = view + 1.
    /// </summary>
    public static class PaperChatStyle
    {
        public const int Ribbons = ChatChannelFilter.Channels + 1;
        static readonly string[] Names = { "All", "World", "Map", "Party", "Whisper" };
        static readonly Color[] Fills = { PaperTheme.BtnGold, PaperTheme.BtnRed, PaperTheme.Green, PaperTheme.Purple, PaperTheme.Hex("#E0569B") };
        static readonly Color[] Edges = { PaperTheme.BtnGoldEdge, PaperTheme.BtnRedEdge, PaperTheme.LabelGreen, PaperTheme.BtnPurpleEdge, PaperTheme.Hex("#8E2A62") };

        public static string Name(int view) { return Names[view + 1]; }
        public static Color Fill(int view) { return Fills[view + 1]; }
        public static Color Edge(int view) { return Edges[view + 1]; }

        /// <summary>Art of a channel icon (chat/icon-all, chat/icon-world ...): drawn on its ribbon once the C1 sheet is cut.</summary>
        public static string IconPiece(int view) { return PaperPiece.ChatIconPrefix + Names[view + 1].ToLowerInvariant(); }

        /// <summary>Nunito like the rest of the paper UI; the browser build keeps the chat's Inter (ChatArt.TextFont, plan phase 01).</summary>
        public static Font Font
        {
            get { return Application.platform == RuntimePlatform.WebGLPlayer ? ChatArt.TextFont() : PaperTheme.BodyFont; }
        }
    }
}
