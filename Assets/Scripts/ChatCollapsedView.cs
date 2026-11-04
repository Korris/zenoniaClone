using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Collapsed chat: the 3 newest lines (every channel), each on its own dark strip fading to the right, shown for 7 s then fading out,
    /// above a round chat button with a red unread badge. Tapping the button (or a visible line) opens the chat panel.
    /// Mobile: just above the joystick on the left. Web: bottom-left corner (where the panel sits when open).
    /// </summary>
    public sealed class ChatCollapsedView
    {
        const int Rows = 3;
        const float ShowFor = 7f, FadeFor = 1f, RowH = 26f, RowW = 360f;

        public RectTransform root;
        readonly CanvasGroup[] rows = new CanvasGroup[Rows];
        readonly Text[] texts = new Text[Rows];
        GameObject badge;
        Text badgeText;

        /// <param name="bubble">centre of the chat button from the bottom-left of the safe area</param>
        public static ChatCollapsedView Build(Transform layer, Vector2 bubble, float bubbleSize, Action open)
        {
            var v = new ChatCollapsedView();
            v.root = UiKit.Fill(layer, "Collapsed");
            for (int i = 0; i < Rows; i++)
            {
                float y = bubble.y + bubbleSize * 0.5f + 12f + (Rows - 1 - i) * (RowH + 3f); // row 0 = oldest, on top
                RectTransform r = UiWinKit.Anchored(v.root, "Line" + i, Vector2.zero, Vector2.zero, 4f, y, RowW, RowH);
                Image strip = r.gameObject.AddComponent<Image>();
                strip.sprite = ChatArt.Fade(); strip.color = new Color(0.01f, 0.03f, 0.08f, 0.62f); strip.raycastTarget = true;
                var tap = r.gameObject.AddComponent<Button>();
                tap.transition = Selectable.Transition.None; tap.navigation = new Navigation { mode = Navigation.Mode.None };
                tap.onClick.AddListener(() => open());
                v.rows[i] = r.gameObject.AddComponent<CanvasGroup>();
                Text t = v.texts[i] = UiKit.Label(UiKit.Fill(r, "Text"), "", 15, Color.white, TextAnchor.MiddleLeft, true);
                t.font = ChatArt.TextFont();
                t.rectTransform.offsetMin = new Vector2(10f, 0f);
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
                t.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            }

            RectTransform b = TouchControls.Place(v.root, "ChatButton", Vector2.zero, bubble.x, bubble.y, bubbleSize);
            Image frame = TouchControls.Img(b, UiArt.PotionFrame(), Color.white, true);
            UiWinKit.MakeButton(frame, open);
            TouchControls.Child(b, "Glyph", ChatArt.Bubble(), bubbleSize * 0.5f, new Color(1f, 0.97f, 0.88f));
            RectTransform badgeRt = TouchControls.Place(b, "Badge", new Vector2(1f, 1f), -4f, -4f, 24f);
            TouchControls.Img(badgeRt, UiSprites.Circle(), new Color(0.85f, 0.15f, 0.15f, 1f));
            v.badgeText = UiKit.Label(UiKit.Fill(badgeRt, "Count"), "", 14, Color.white, TextAnchor.MiddleCenter, true);
            v.badge = badgeRt.gameObject;
            v.SetUnread(0);
            return v;
        }

        public void SetUnread(int n)
        {
            badge.SetActive(n > 0);
            badgeText.text = n > 99 ? "99+" : n.ToString();
        }

        /// <summary>Re-draws the newest lines; lines older than 7 s fade out over 1 s.</summary>
        public void Refresh(IList<ChatMessage> history)
        {
            List<ChatMessage> recent = ChatTextFormat.Recent(history, ChatChannel.World, Rows);
            int offset = Rows - recent.Count; // newest at the bottom row
            for (int i = 0; i < Rows; i++)
            {
                int k = i - offset;
                if (k < 0) { rows[i].alpha = 0f; rows[i].blocksRaycasts = false; continue; }
                float age = Time.unscaledTime - recent[k].Time;
                float a = age < ShowFor ? 1f : Mathf.Clamp01(1f - (age - ShowFor) / FadeFor);
                rows[i].alpha = a;
                rows[i].blocksRaycasts = a > 0.05f; // a faded line never eats a tap on the world
                texts[i].text = ChatTextFormat.Line(recent[k]);
            }
        }
    }
}
