using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Collapsed chat of the RO Soft HUD (02-hud.png): the round chat button (red unread count) and the
    /// navy one-line bar "World | Rin: Anyone for Sograt? | ^" with the newest line of any channel, its channel name in that channel's
    /// colour. Tapping the button, the bar or the chevron opens the chat panel (ChatWindow). Box model: padding 4 / 14, gaps 8 (rules).
    /// Placement (user 19/11: "cái khung chat ở bản mobile cho nằm giữa", a deliberate change from the mockup's bottom-left): button + bar
    /// as one group, centred on the screen at the bottom but kept in the free span between the joystick and the skill ring; the bar
    /// shrinks (never under MinBarW) when that span is narrow (4:3), so it never covers either.
    /// </summary>
    public sealed class RoHudChatBar : ChatFeed
    {
        const int PadX = 14, PadY = 4, Gap = 8;
        const float BubbleGap = 6f, MinBarW = 170f, MaxBarW = 400f;
        /// <summary>Free span edges: right of the joystick ring, left of the skill ring (its left slot), units from the safe-area sides.</summary>
        static readonly float JoyRight = RoHud.JoyX + RoHud.JoySize * 0.5f + 8f, RingLeft = RoHud.AttackX + RoHud.SkillArc * 0.951f + RoHud.SkillSize * 0.5f + 8f;

        /// <summary>Lays the group out each frame the safe-area width changed (rotation, window resize).</summary>
        sealed class Centre : MonoBehaviour
        {
            public RectTransform group;
            float shownW = -1f;
            void LateUpdate()
            {
                float w = ((RectTransform)transform).rect.width;
                if (Mathf.Abs(w - shownW) < 0.5f) return;
                shownW = w;
                float span = w - JoyRight - RingLeft, bubble = RoHud.ChatBubbleSize + BubbleGap;
                float barW = Mathf.Clamp(span - bubble, MinBarW, MaxBarW), gw = bubble + barW;
                float x = Mathf.Clamp((w - gw) * 0.5f, JoyRight, Mathf.Max(JoyRight, w - RingLeft - gw)); // screen centre, kept in the span
                group.sizeDelta = new Vector2(gw, group.sizeDelta.y);
                group.anchoredPosition = new Vector2(x, group.anchoredPosition.y);
            }
        }
        GameObject badge;
        Text badgeText, channel, line;
        float shownTime = -1f;

        public static RoHudChatBar Build(Transform layer, Action open)
        {
            var v = new RoHudChatBar();
            v.root = UiKit.Fill(layer, "RoChat");
            RectTransform group = RoHud.Box(v.root, "Group", RoHud.BottomLeft, 0f, RoHud.ChatY, RoHud.ChatBubbleSize + BubbleGap + RoHud.ChatBarW, RoHud.ChatBarH);
            group.pivot = new Vector2(0f, 0.5f);

            RectTransform bubble = RoHud.Box(group, "ChatButton", new Vector2(0f, 0.5f), RoHud.ChatBubbleSize * 0.5f, 0f, RoHud.ChatBubbleSize, RoHud.ChatBubbleSize);
            Image art = RoHud.Art(bubble, "hud-chat-button", true);
            UiWinKit.MakeButton(art, () => open());
            RectTransform b;
            v.badgeText = RoHud.Pill(bubble, "Badge", RoHud.TopRight, new Vector2(0.5f, 0.5f), -4f, -4f, 18f, "hud-badge-alert", null, 0f, "", 10, Color.white, out b, 18f);
            b.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(4, 4, 1, 1);
            v.badge = b.gameObject;

            RectTransform bar = UiKit.Fill(group, "Bar");
            bar.offsetMin = new Vector2(RoHud.ChatBubbleSize + BubbleGap, 0f); // right of the button, the rest of the group
            v.root.gameObject.AddComponent<Centre>().group = group;
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
            v.channel = RoHud.Label(UiKit.Rect(bar, "Channel", 0f, 0f, 10f, h), "World", 14, RoSoftTheme.Hex("#8BE36B"), TextAnchor.MiddleLeft);
            Text sep = RoHud.Label(UiKit.Rect(bar, "Sep", 0f, 0f, 4f, h), "|", 13, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, false);
            v.line = RoHud.Label(UiKit.Rect(bar, "Line", 0f, 0f, 10f, h), "", 14, Color.white, TextAnchor.MiddleLeft, false);
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
