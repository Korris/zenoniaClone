using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// A monster card built ONLY from kit pieces: the ChatGPT portrait under the ChatGPT card frame of its rarity (boss: gold frame), the rarity badge, the four
    /// duel numbers in the frame's hex / diamond slots (numbers are text, as the sheets carry no digits) and an xN tag. Geometry = fractions of the frame art
    /// (cream window x .12-.88, y .17-.72; slots: top .5/.075, right .93/.47, bottom .5/.775, left .07/.47).
    /// </summary>
    public sealed class GemCardView
    {
        public readonly RectTransform Root;
        readonly Image portrait, frame, badge, select, tag, unknown;
        readonly Text top, right, bottom, left, count, name;
        readonly float w, h;
        readonly bool barName, fill;

        /// <summary>`edgePx` > 0 = a duel card: the four numbers are drawn that big (px) in bold white with a dark rim, centred on each edge, so they stay readable on a small card (board, hand).</summary>
        public GemCardView(Transform parent, string name, float x, float y, float w, float h, Action onClick, bool showName, bool barName = true, int edgePx = 0)
        {
            this.w = w; this.h = h; this.barName = barName; fill = edgePx > 0; // a duel card fills its box: the frame art is stretched a little (0.82 -> 0.9) instead of leaving gaps in the cell
            Root = UiKit.Rect(parent, name, x, y, w, h + (showName ? 20f : 0f));
            RectTransform card = UiKit.Rect(Root, "Card", 0f, 0f, w, h);
            frame = Piece(card, "Frame", 0f, 0f, w, h);
            portrait = Piece(card, "Portrait", w * 0.13f, h * 0.17f, w * 0.74f, h * 0.55f); // the frame's cream window is opaque: the portrait is drawn over it
            badge = Piece(card, "Badge", w * 0.03f, h * 0.02f, w * 0.34f, w * 0.18f);
            int sBase = Mathf.Max(11, Mathf.RoundToInt(h * 0.1f)), s = edgePx > 0 ? edgePx : sBase;
            bool big = edgePx > 0; // the slots are drawn small in the frame: a big number sits a little further in so it stays on the card
            Transform numbers = big ? Root : card; // a big number lives on the root so NumbersOnTop can lift it over the owner / last-move rings the board adds later
            top = Num(numbers, w * 0.5f, h * (big ? 0.14f : 0.075f), s, big); right = Num(numbers, w * (big ? 0.9f : 0.93f), h * 0.47f, s, big);
            bottom = Num(numbers, w * 0.5f, h * (big ? 0.8f : 0.775f), s, big); left = Num(numbers, w * (big ? 0.1f : 0.07f), h * 0.47f, s, big);
            this.name = U.Txt(card, "Name", w * 0.06f, h * 0.835f, w * 0.88f, h * 0.11f, "", Mathf.Max(10, sBase - 1), U.Ink, TextAnchor.MiddleCenter);
            tag = Piece(card, "CountTag", w * 0.55f, h * 0.60f, w * 0.4f, w * 0.19f);
            count = U.Txt(card, "Count", w * 0.55f, h * 0.60f, w * 0.4f, w * 0.19f, "", sBase, Color.white, TextAnchor.MiddleCenter);
            unknown = Piece(card, "Unknown", w * 0.3f, h * 0.27f, w * 0.4f, h * 0.3f);
            select = Piece(Root, "Select", -4f, -4f, w + 8f, h + 8f);
            // the caption is wider than the card so a long name (Frostfang King) stays on one line
            if (showName) { Text n = U.Txt(Root, "Caption", -10f, h + 1f, w + 20f, 18f, "", U.TextSmall, U.Ink, TextAnchor.MiddleCenter); caption = n; }
            if (onClick != null) { UiWinKit.MakeButton(frame, onClick); frame.raycastTarget = true; }
            if (big) NumbersOnTop();
        }

        Text caption;

        /// <summary>Draws the four numbers above every overlay added to this card after it was built (a ring must never cover a digit).</summary>
        public void NumbersOnTop() { top.transform.SetAsLastSibling(); right.transform.SetAsLastSibling(); bottom.transform.SetAsLastSibling(); left.transform.SetAsLastSibling(); }

        Image Piece(Transform parent, string name, float x, float y, float w, float h)
        {
            Image img = UiKit.Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.clear;
            return img;
        }

        Text Num(Transform parent, float cx, float cy, int size, bool big)
        {
            float bw = size * 1.6f;
            Text t = U.Txt(parent, "Edge", cx - bw * 0.5f, cy - size * 0.8f, bw, size * 1.6f, "", size, big ? Color.white : U.Ink, TextAnchor.MiddleCenter);
            if (!big) return t;
            t.resizeTextForBestFit = false; t.fontSize = size; t.fontStyle = FontStyle.Bold; t.alignByGeometry = true; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var rim = t.gameObject.AddComponent<Outline>(); rim.effectColor = new Color(0.12f, 0.08f, 0.04f, 1f); rim.effectDistance = new Vector2(1.6f, -1.6f); // high contrast on any frame colour
            return t;
        }

        /// <summary>`owned` > 0 shows "xN" (also for the convert list: the copies that can still be used); 0 hides it. `dim` = not available (greyed).</summary>
        public void Set(CardDef c, int owned, bool selected, bool dim = false)
        {
            Root.gameObject.SetActive(c != null);
            if (c == null) return;
            Sprite p = GemUiArt.Portrait(c);
            portrait.sprite = p; portrait.preserveAspect = true; portrait.color = p != null ? (dim ? new Color(1f, 1f, 1f, 0.45f) : Color.white) : Color.clear;
            GemUiArt.SetPiece(frame, GemUiArt.CardFrame(c));
            if (fill) frame.preserveAspect = false;
            GemUiArt.SetPiece(badge, GemUiArt.RarityBadge(c));
            top.text = c.Top.ToString(); right.text = c.Right.ToString(); bottom.text = c.Bottom.ToString(); left.text = c.Left.ToString();
            name.text = caption == null && barName ? c.Name : ""; // the frame has a name bar: a card with no caption under it writes the name there
            if (caption != null) caption.text = c.Name;
            GemUiArt.SetPiece(tag, owned > 0 ? GemUiArt.Icon("tag-count") : null);
            count.text = owned > 0 ? "x" + owned : "";
            GemUiArt.SetPiece(select, selected ? "cards/card/select-frame-gold" : null);
            if (fill) select.preserveAspect = false;
            frame.color = dim ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
            unknown.color = Color.clear;
        }

        /// <summary>A card the character never owned (Album): grey frame, the portrait as a dark silhouette with the "?" piece over it, no numbers, no name.</summary>
        public void SetUnknown(CardDef c, bool selected)
        {
            Root.gameObject.SetActive(c != null);
            if (c == null) return;
            Sprite p = GemUiArt.Portrait(c);
            portrait.sprite = p; portrait.preserveAspect = true; portrait.color = p != null ? new Color(0.13f, 0.14f, 0.18f, 0.92f) : Color.clear;
            GemUiArt.SetPiece(frame, "cards/card/card-frame-n");
            frame.color = new Color(0.62f, 0.64f, 0.7f, 1f);
            GemUiArt.SetPiece(badge, null);
            top.text = right.text = bottom.text = left.text = "";
            name.text = caption == null && barName ? "???" : ""; if (caption != null) caption.text = "???";
            GemUiArt.SetPiece(tag, null); count.text = "";
            GemUiArt.SetPiece(unknown, GemUiArt.Icon("icon-unknown"));
            GemUiArt.SetPiece(select, selected ? "cards/card/select-frame-gold" : null);
        }
    }

    /// <summary>A Soul Gem: the shell piece of its kind and rarity with the monster portrait in the round window, an xN tag and the card name under it.</summary>
    public sealed class GemTileView
    {
        public readonly RectTransform Root;
        readonly Image shell, portrait, select, tag, locked;
        readonly Text count, caption;

        public GemTileView(Transform parent, string name, float x, float y, float size, Action onClick, bool showName, bool showCount)
        {
            Root = UiKit.Rect(parent, name, x, y, size, size + (showName ? 30f : 0f));
            select = Mk(Root, "Select", -3f, -3f, size + 6f);
            shell = Mk(Root, "Shell", 0f, 0f, size);
            portrait = Mk(Root, "Portrait", size * 0.27f, size * 0.27f, size * 0.46f); // over the shell: its round window is opaque
            tag = Mk(Root, "Tag", size * 0.45f, size * 0.68f, size * 0.55f); tag.rectTransform.sizeDelta = new Vector2(size * 0.55f, size * 0.27f);
            count = U.Txt(Root, "Count", size * 0.45f, size * 0.68f, size * 0.55f, size * 0.27f, "", Mathf.Max(11, Mathf.RoundToInt(size * 0.18f)), Color.white, TextAnchor.MiddleCenter);
            locked = Mk(Root, "Locked", size * 0.02f, size * 0.66f, size * 0.28f);
            caption = showName ? U.Txt(Root, "Caption", -4f, size + 1f, size + 8f, 28f, "", 12, U.Ink, TextAnchor.UpperCenter) : null;
            if (!showCount) count.gameObject.SetActive(false);
            if (onClick != null) { UiWinKit.MakeButton(shell, onClick); shell.raycastTarget = true; }
        }

        static Image Mk(Transform parent, string name, float x, float y, float s)
        {
            Image img = UiKit.Rect(parent, name, x, y, s, s).gameObject.AddComponent<Image>();
            img.raycastTarget = false; img.color = Color.clear;
            return img;
        }

        /// <summary>`total` = copies shown in the xN tag (0 hides it); `trade` = the gem is trade-locked (small lock).</summary>
        public void Set(CardDef c, GemKind kind, int total, bool selected, bool tradeLocked)
        {
            Root.gameObject.SetActive(c != null);
            if (c == null) return;
            Sprite p = GemUiArt.Portrait(c);
            portrait.sprite = p; portrait.preserveAspect = true; portrait.color = p != null ? Color.white : Color.clear;
            GemUiArt.SetPiece(shell, GemUiArt.GemShell(kind, c.Rarity));
            GemUiArt.SetPiece(select, selected ? "cards/card/select-frame-gold" : null);
            GemUiArt.SetPiece(tag, total > 0 ? GemUiArt.Icon("tag-count") : null);
            GemUiArt.SetPiece(locked, tradeLocked ? GemUiArt.Icon("icon-lock") : null);
            count.text = total > 0 ? "x" + total : "";
            if (caption != null) caption.text = c.Name;
        }
    }
}
