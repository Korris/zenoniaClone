using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Collapsed chat of the RO Soft HUD (02-hud.png), bottom-left under the joystick: the round chat button (red unread count) and the
    /// navy one-line bar "World | Rin: Anyone for Sograt? | ^" with the newest line of any channel, its channel name in that channel's
    /// colour. Tapping the button, the bar or the chevron opens the chat panel (ChatWindow). Box model: padding 4 / 14, gaps 8 (rules).
    /// </summary>
    public sealed class RoHudChatBar : ChatFeed
    {
        const int PadX = 14, PadY = 4, Gap = 8;
        GameObject badge;
        Text badgeText, channel, line;
        float shownTime = -1f;

        public static RoHudChatBar Build(Transform layer, Action open)
        {
            var v = new RoHudChatBar();
            v.root = UiKit.Fill(layer, "RoChat");

            RectTransform bubble = RoHud.Box(v.root, "ChatButton", RoHud.BottomLeft, RoHud.ChatBubbleX, RoHud.ChatY, RoHud.ChatBubbleSize, RoHud.ChatBubbleSize);
            Image art = RoHud.Art(bubble, "hud-chat-button", true);
            UiWinKit.MakeButton(art, () => open());
            RectTransform b;
            v.badgeText = RoHud.Pill(bubble, "Badge", RoHud.TopRight, new Vector2(0.5f, 0.5f), -4f, -4f, 18f, "hud-badge-alert", null, 0f, "", 10, Color.white, out b, 18f);
            b.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 4, 1, 1);
            v.badge = b.gameObject;

            RectTransform bar = RoHud.Box(v.root, "Bar", RoHud.BottomLeft, 0f, RoHud.ChatY, RoHud.ChatBarW, RoHud.ChatBarH);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.anchoredPosition = new Vector2(RoHud.ChatBarX, RoHud.ChatY);
            Image barArt = RoHud.Art(bar, "hud-chat-bar", true);
            barArt.color = new Color(1f, 1f, 1f, 0.9f);
            UiWinKit.MakeButton(barArt, () => open());
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(PadX, PadX, PadY, PadY);
            row.spacing = Gap;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            float h = RoHud.ChatBarH - PadY * 2;
            v.channel = RoHud.Label(UiKit.Rect(bar, "Channel", 0f, 0f, 10f, h), "World", 13, RoSoftTheme.Hex("#8BE36B"), TextAnchor.MiddleLeft);
            Text sep = RoHud.Label(UiKit.Rect(bar, "Sep", 0f, 0f, 4f, h), "|", 13, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, false);
            v.line = RoHud.Label(UiKit.Rect(bar, "Line", 0f, 0f, 10f, h), "", 13, Color.white, TextAnchor.MiddleLeft, false);
            v.line.horizontalOverflow = HorizontalWrapMode.Wrap;
            v.line.verticalOverflow = VerticalWrapMode.Truncate;
            v.line.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            v.line.GetComponent<LayoutElement>().minWidth = 40f;
            v.line.GetComponent<LayoutElement>().preferredWidth = 1f; // a long line must not squeeze the channel name: it only takes the space left
            RectTransform chev = UiKit.Rect(bar, "Expand", 0f, 0f, 14f, 10f);
            RoHud.Art(chev, "hud-chevron-up");
            var le = chev.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = 16f; le.preferredHeight = le.minHeight = 10f;
            v.SetUnread(0);
            return v;
        }

        public override void SetUnread(int n)
        {
            badge.SetActive(n > 0);
            badgeText.text = n > 99 ? "99+" : n.ToString();
        }

        public override void Refresh(IList<ChatMessage> history)
        {
            if (history.Count == 0) { channel.text = "World"; line.text = ""; shownTime = -1f; return; }
            ChatMessage m = history[history.Count - 1];
            if (m.Time == shownTime) return; // the bar has no fading: only a new line changes it
            shownTime = m.Time;
            channel.text = "<color=#" + ChatTextFormat.Feed.Tag(m.Channel) + ">" + m.Channel + "</color>";
            line.text = string.IsNullOrEmpty(m.Sender) ? m.Text : m.Sender + ": " + m.Text;
        }
    }
}
