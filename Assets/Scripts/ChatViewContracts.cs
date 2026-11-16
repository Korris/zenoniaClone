using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// What ChatWindow needs from the open chat, whichever look is on: the old gold panel (ChatPanelView) or the Adventure Paper
    /// notice board (PaperChatPanelView, behind UiScreens.UseNew(UiScreen.Chat)). The view owns which channel is shown and which one a
    /// typed line goes to; the window owns focus, sending, the WebGL HTML input and the unread count.
    /// </summary>
    public abstract class ChatPanel
    {
        public RectTransform root, field;
        public Text placeholder;
        public InputField input;

        /// <summary>Channel a typed line is sent on.</summary>
        public abstract ChatChannel SendChannel { get; }

        /// <summary>Tab while typing: the next channel.</summary>
        public abstract void NextChannel();

        /// <summary>Re-draws the lines from the history (called when a line arrived or the channel changed, never per frame).</summary>
        public abstract void Refresh(IList<ChatMessage> history);
    }

    /// <summary>The collapsed chat: the newest lines floating over the world + the chat button with its unread badge.</summary>
    public abstract class ChatFeed
    {
        public RectTransform root;

        public abstract void SetUnread(int n);

        /// <summary>Called on every new line and about ten times a second while collapsed (old lines fade with time).</summary>
        public abstract void Refresh(IList<ChatMessage> history);
    }
}
