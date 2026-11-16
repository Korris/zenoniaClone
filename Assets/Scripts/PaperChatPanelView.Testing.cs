using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>Hooks for the capture flags of DebugCapture.UiChat.cs: they press what a player would press. Not used in normal play.</summary>
    public sealed partial class PaperChatPanelView
    {
        /// <summary>The board of the running game (null while the old chat look is on).</summary>
        internal static PaperChatPanelView Current;

        /// <summary>Taps a channel ribbon: view = ChatChannelFilter.All or (int)ChatChannel.</summary>
        internal void TestRibbon(int view) { ribbons[view + 1].GetComponent<Button>().onClick.Invoke(); }

        /// <summary>Taps a filter chip (same view values; All = every channel back on).</summary>
        internal void TestChip(int view) { chips[view + 1].Root.GetComponent<Button>().onClick.Invoke(); }

        /// <summary>Taps the send-channel dropdown; with a channel also taps that entry of its list.</summary>
        internal void TestSendMenu(int channel)
        {
            sendButton.GetComponent<Button>().onClick.Invoke();
            if (channel >= 0) sendMenu.GetChild(channel + 1).GetComponent<Button>().onClick.Invoke(); // child 0 is the plate's face
        }

        internal void TestScrollToTop() { scroll.verticalNormalizedPosition = 1f; }
    }
}
