using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// ChatWindow: the parts only the compact look has (UiScreens.UseNew(UiScreen.Chat)). The lines (PaperChatFeedView) are always on
    /// screen; `expanded` means the typing bar is open under them (PaperChatBarView) or, on PC only, the history board lies over them
    /// (PaperChatPanelView). `panel` points at whichever of the two owns the input; with nothing open it is the bar, so Enter types there.
    /// </summary>
    public sealed partial class ChatWindow
    {
        const float BarCloseDelay = 0.15f; // the field lost focus: long enough for a tap on the bar's own pill / send button to land first

        ChatPanel bar, board; // board: PC only
        PaperChatFeedView feed;
        float closeBarAt = -1f;

        /// <summary>Capture scripts: what is open ("lines", "bar", "bar typing", "history", old look: "panel" / "collapsed").</summary>
        internal static string StateName
        {
            get
            {
                if (instance == null) return "none";
                string typing = UiWindows.ChatTyping ? " typing" : "";
                if (!instance.paper) return (instance.expanded ? "panel" : "collapsed") + typing;
                return (!instance.expanded ? "lines" : instance.panel == instance.bar ? "bar" : "history") + typing;
            }
        }

        /// <summary>Capture scripts: the view that owns the input right now.</summary>
        internal static ChatPanel InputPanel { get { return instance != null ? instance.panel : null; } }

        /// <summary>The lines of the compact look are on screen (always, except under the PC history board); old look: the chat is collapsed.</summary>
        bool FeedShown { get { return !expanded || (paper && panel == bar); } }

        void BuildCompact()
        {
            collapsed = feed = PaperChatFeedView.Build(content, web, OpenTyping, web ? () => { panel = board; SetExpanded(true); } : (System.Action)null);
            panel = bar = PaperChatBarView.Build(feed.Block, web, FocusInput, () => { SendCurrent(); SetExpanded(false); });
            if (web) Hook(board = PaperChatPanelView.Build(content, Refresh, () => SetExpanded(false), SendCurrent));
        }

        void ShowCompact(bool on)
        {
            bool history = on && panel == board;
            if (board != null) board.root.gameObject.SetActive(history);
            collapsed.root.gameObject.SetActive(!history);
            bar.root.gameObject.SetActive(on && !history);
            feed.SetTyping(on && !history);
        }

        /// <summary>Enter, the tiny chat icon, a tap on the lines: the input opens with the caret in it (the history board keeps its own field).</summary>
        void OpenTyping()
        {
            if (!expanded) SetExpanded(true);
            FocusInput();
        }

        /// <summary>The typing bar closes a moment after its field lost focus (sent, Esc, a tap outside), unless the caret came back.</summary>
        void TickBarClose()
        {
            if (closeBarAt < 0f || Time.unscaledTime < closeBarAt) return;
            closeBarAt = -1f;
            if (expanded && panel == bar) SetExpanded(false);
        }
    }
}
