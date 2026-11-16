using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the compact chat (with --ui-new chat, on top of --chat-demo / --open-chat / --close-chat / --chat-focus /
    /// --hud web|mobile of DebugCapture.cs, which work on both looks; --open-chat = the typing bar on mobile, the history board on PC):
    ///   --chat-vn                  six more lines: Vietnamese with accents, a long line, a quest and a Wanted system line, an own line
    ///   --chat-many N              N numbered lines over the four channels (scrolling, the 40-line history cap; counts as unread)
    ///   --chat-tap "1=ChatIcon;2=ChannelPill;3=Outside"   at that second, clicks that control through its EventSystem handler:
    ///                              ChatIcon (mobile), ChatLines, ChatExpand (PC: history), ChannelPill, ChatSend; Outside = what a click
    ///                              on the world does (the field loses focus). Logs the chat state after each step.
    ///   --chat-log                 logs every change of the chat state (lines / bar / bar typing / history): shows what Enter opened
    ///   --chat-type "text" / --chat-say "text"   put text in the open input (typing bar or board) / also click its send button
    ///   PC history board only:
    ///   --chat-channel all|world|map|party|whisper   tap that channel ribbon
    ///   --chat-chip-off map,party  tap those filter chips (All view)
    ///   --chat-send-menu [channel] tap the send-channel dropdown (its list stays open); with a channel also pick it
    ///   --chat-scroll-top          scroll the lines to the oldest one
    /// The first two only feed ChatHub, so they also work on the old look. Order: lines, ribbon, chips, dropdown, typing, scroll.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiChat()
        {
            if (Flag("--chat-vn")) SeedVietnameseChat();
            string many = Val("--chat-many");
            if (many != null)
                for (int i = 1, n = int.Parse(many); i <= n; i++)
                    ChatHub.Receive((ChatChannel)(i % ChatChannelFilter.Channels), "Rook", "Line " + i + " of " + n + (i % 5 == 0 ? ": a longer one, to see that a line wraps onto a second row of the board" : ""));
            if (!UiScreens.UseNew(UiScreen.Chat)) return;
            if (Flag("--chat-log")) StartCoroutine(LogChatState());
            StartCoroutine(PressChat());
        }

        static IEnumerator LogChatState()
        {
            string shown = null;
            while (true)
            {
                string now = ChatWindow.StateName;
                if (now != shown) { shown = now; Debug.Log("[ZenoniaProto] chat state: " + now + " at " + Time.realtimeSinceStartup.ToString("F1", CultureInfo.InvariantCulture)); }
                yield return null;
            }
        }

        static void SeedVietnameseChat()
        {
            ChatHub.Receive(ChatChannel.World, "Hoàng", "Có ai ở gần Làng Sương Giá không? Mình cần người đi phụ bản.");
            ChatHub.Receive(ChatChannel.Party, "Thuỷ", "Đợi ở cổng phía nam nhé, mình đang sửa đồ ở thợ rèn rồi chạy qua ngay, đừng vào trước kẻo chết.");
            ChatHub.System("Quest accepted: Diệt 10 Sói Băng");
            ChatHub.System("Wanted today: Frostfang King in Frost Hollow. Reward: 500 G, 3 stones. Resets 05:00.");
            ChatHub.Receive(ChatChannel.Whisper, "Kai", "Ừ, <b>được</b> thôi!"); // typed tags must show as typed
            ChatHub.Receive(ChatChannel.Map, ChatHub.LocalName(), "Đã rõ, tới ngay.");
        }

        /// <summary>Two frames later: the compact views are built (the switch is read in Update) and laid out, so a scroll position sticks.</summary>
        static IEnumerator PressChat()
        {
            yield return null; yield return null;
            PaperChatPanelView board = ChatWindow.InputPanel as PaperChatPanelView; // PC with --open-chat
            if (board != null) PressChatBoard(board);
            string say = Val("--chat-say"), type = Val("--chat-type"), taps = Val("--chat-tap");
            ChatPanel input = ChatWindow.InputPanel;
            if (input != null && (say ?? type) != null) input.input.text = say ?? type;
            if (say != null) Click(board != null ? "Send" : "ChatSend");
            if (board != null && Flag("--chat-scroll-top")) { yield return null; board.TestScrollToTop(); }
            if (taps == null) yield break;
            float start = Time.realtimeSinceStartup;
            foreach (string step in taps.Split(';'))
            {
                string[] kv = step.Split('=');
                float at = float.Parse(kv[0], CultureInfo.InvariantCulture);
                while (Time.realtimeSinceStartup - start < at) yield return null;
                string name = kv[1].Trim();
                bool hit = name == "Outside" || Click(name);
                if (name == "Outside") EventSystem.current.SetSelectedGameObject(null);
                yield return new WaitForSecondsRealtime(0.3f); // past the bar's close delay
                Debug.Log("[ZenoniaProto] chat-tap " + name + (hit ? "" : " (NO SUCH CONTROL)") + " at " + at + " => " + ChatWindow.StateName
                    + " | channel " + (ChatWindow.InputPanel != null ? ChatWindow.InputPanel.SendChannel.ToString() : "-") + " | lines " + ChatHub.History.Count);
            }
        }

        /// <summary>A click on the active chat control named `name`, through the handler a pointer would reach.</summary>
        static bool Click(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) return false;
            ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            return true;
        }

        static void PressChatBoard(PaperChatPanelView board)
        {
            string channel = Val("--chat-channel");
            if (channel != null) board.TestRibbon(ChatView(channel));
            string off = Val("--chat-chip-off");
            if (off != null) foreach (string c in off.Split(',')) board.TestChip(ChatView(c.Trim()));
            if (!Flag("--chat-send-menu")) return;
            string pick = Val("--chat-send-menu");
            board.TestSendMenu(pick != null && !pick.StartsWith("--") ? ChatView(pick) : -1);
        }

        static int ChatView(string name)
        {
            return string.Equals(name, "all", StringComparison.OrdinalIgnoreCase) ? ChatChannelFilter.All : (int)(ChatChannel)Enum.Parse(typeof(ChatChannel), name, true);
        }
    }
}
