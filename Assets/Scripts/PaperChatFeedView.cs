using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The chat of the compact HUD: only the newest lines, as plain text over the world. No board, no frame: the channel tag
    /// in its colour, the rest light, each line with an outline and a drop shadow so it reads over snow and over dark maps.
    ///   Mobile  4 lines bottom-centre, in the free span between the joystick and the action cluster (HudAnchors). Tapping the
    ///           block opens the typing bar (PaperChatBarView) under the lines; no icon, no badge.
    ///           A see-through dark plate sits behind them and the block is always there, lines never fade (user, 16/11/2026:
    ///           "để khung chat trong suốt liên tục ở đó, khi nào ấn mới mở ô nhập").
    ///   PC      5 lines flush in the bottom-left corner on the same plate, "[Enter] to chat" under them; a click on the block opens
    ///           the scrollable history board, Enter the typing bar.
    /// Texts are rebuilt only when a line arrived; the timer only moves one alpha.
    /// </summary>
    public sealed class PaperChatFeedView : ChatFeed
    {
        public const float WebWidth = 340f; // as wide as the old web panel
        /// <summary>PC: bottom of the hint / typing row. Raised from 6: "[Enter] to chat" sat so low it was cut off (user, 16/11/2026).</summary>
        public const float WebBarBottom = 14f;
        const float MaxWidth = 430f, RowH = 17f, TextH = 23f; // a text rect is taller than its row: Truncate drops a line that does not fit whole
        static readonly Color Hint = new Color(1f, 0.97f, 0.88f, 0.9f), Plate = new Color(0.03f, 0.05f, 0.09f, 0.32f);

        /// <summary>The rect the lines live in; the typing bar sits on its bottom edge.</summary>
        public RectTransform Block { get; private set; }

        Text[] texts;
        RectTransform span;
        GameObject hint;
        bool web;
        int shownCount = -1;
        float newestTime, shownSpan = -1f;
        string newestText;

        /// <param name="openTyping">the icon (mobile) or the lines were tapped</param>
        /// <param name="openHistory">PC: the small icon was clicked; null on mobile, where there is no board at all</param>
        public static PaperChatFeedView Build(Transform layer, bool web, Action openTyping, Action openHistory)
        {
            var v = new PaperChatFeedView { web = web };
            v.root = UiKit.Fill(layer, "PaperChatFeed");
            int rows = web ? 5 : 4;
            float bar = web ? 24f : HudAnchors.ChatBarHeight, barBottom = web ? WebBarBottom : HudAnchors.ChatBarBottom, linesBottom = barBottom + bar + 4f, linesH = rows * RowH;
            // The span the block may use: PC = a fixed column bottom-left; mobile = between the joystick and the action cluster.
            v.span = UiKit.Fill(v.root, "Span");
            v.span.anchorMax = new Vector2(web ? 0f : 1f, 0f);
            v.span.pivot = new Vector2(0.5f, 0f);
            v.span.offsetMin = new Vector2(web ? 0f : HudAnchors.ChatLeft, 0f); // PC: flush with the left edge (user, 16/11/2026)
            v.span.offsetMax = new Vector2(web ? WebWidth : -(HudAnchors.ActionClusterWidth + 12f), linesBottom + linesH);
            v.Block = UiWinKit.Anchored(v.span, "Block", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 0f, 0f, web ? WebWidth : MaxWidth, linesBottom + linesH);

            // The see-through plate under the lines (PC: under the hint row too). Mobile: a tap on it opens typing; PC: a click opens the history board.
            RectTransform plate = UiKit.Fill(v.Block, "Plate");
            plate.offsetMin = new Vector2(web ? 0f : -4f, web ? barBottom - 4f : linesBottom - 3f);
            plate.offsetMax = new Vector2(4f, 3f);
            UiWinKit.MakeButton(UiKit.Panel(plate, Plate, true), web ? openHistory : openTyping);
            RectTransform box = UiKit.Fill(v.Block, "ChatLines");
            box.offsetMin = new Vector2(6f, linesBottom);
            UiWinKit.MakeButton(UiKit.Panel(box, Color.clear, true), web ? openHistory : openTyping);
            v.texts = new Text[rows];
            for (int i = 0; i < rows; i++) // row 0 = oldest, on top
            {
                Text t = v.texts[i] = PaperTheme.Body(box, "Line" + i, 0f, i * RowH - (TextH - RowH) * 0.5f, 100f, TextH, "", 13, Color.white, PaperInk.World);
                t.rectTransform.anchorMax = new Vector2(1f, 1f); // as wide as the block
                t.rectTransform.sizeDelta = new Vector2(0f, TextH);
                t.verticalOverflow = VerticalWrapMode.Truncate; // one row per message: a long line is cut at the block's end
                OverWorld(t);
            }

            if (!web) return v;
            // No chat icon and no unread badge on either layout (user, 16/11/2026): the lines are always there.
            {
                Text h = PaperTheme.Body(v.Block, "Hint", 6f, linesBottom + linesH - barBottom - bar, WebWidth - 6f, bar, "[" + InputAdapter.KeyLabel(GameKey.Chat) + "] to chat", 12, Hint, PaperInk.World);
                h.fontStyle = FontStyle.Italic;
                OverWorld(h);
                v.hint = h.gameObject;
            }
            return v;
        }

        /// <summary>
        /// The chat font, and a thin dark edge all round on top of PaperInk.World's drop shadow: light text straight on snow needs
        /// both. The two are kept small (their offsets add up), or the letters look printed twice.
        /// </summary>
        static void OverWorld(Text t)
        {
            t.font = PaperChatStyle.Font;
            t.GetComponent<Shadow>().effectDistance = new Vector2(0.4f, -0.8f);
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.8f);
            o.effectDistance = new Vector2(0.7f, -0.7f);
        }

        public override void SetUnread(int n) { } // no badge: nothing collapses any more

        /// <summary>The typing bar is open under the lines: they stay lit, and the PC hint makes room for it.</summary>
        public void SetTyping(bool on)
        {
            if (hint != null) hint.SetActive(!on);
        }

        public override void Refresh(IList<ChatMessage> history)
        {
            int n = history.Count;
            if (n != shownCount || (n > 0 && (history[n - 1].Time != newestTime || !ReferenceEquals(history[n - 1].Text, newestText)))) Rebuild(history);
            if (!web) Fit();
        }

        /// <summary>Mobile: the block is centred in the free span and never wider than it (4:3 phones leave about 380 units).</summary>
        void Fit()
        {
            float w = span.rect.width;
            if (Mathf.Approximately(w, shownSpan)) return;
            shownSpan = w;
            Block.sizeDelta = new Vector2(Mathf.Min(MaxWidth, w), Block.sizeDelta.y);
        }

        /// <summary>Newest line on the bottom row, older ones above it.</summary>
        void Rebuild(IList<ChatMessage> history)
        {
            int n = shownCount = history.Count;
            newestTime = n > 0 ? history[n - 1].Time : 0f;
            newestText = n > 0 ? history[n - 1].Text : null;
            for (int row = texts.Length - 1, i = n - 1; row >= 0; row--, i--)
                texts[row].text = i >= 0 ? ChatTextFormat.Line(history[i], ChatTextFormat.Feed) : "";
        }
    }
}
