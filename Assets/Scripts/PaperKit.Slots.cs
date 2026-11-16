using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Square item slot of the style board ("Ô vật phẩm"): frame in the rarity colour (tier 0-4, PaperTheme.Rarity; -1 = empty), icon,
    /// gold glow when selected; locked = the chained slot art covers it (a dark veil + padlock until cut).
    /// </summary>
    public sealed class PaperSlot
    {
        public readonly RectTransform Root;
        public readonly Image Icon;
        readonly Image frame;
        readonly GameObject glow, lockLayer;
        readonly float radius;
        int shownTier = int.MinValue;

        public PaperSlot(Transform parent, string name, float x, float y, float size, Action onClick = null)
        {
            radius = size * 0.16f;
            Root = UiKit.Rect(parent, name, x, y, size, size);
            RectTransform g = UiKit.Fill(Root, "Glow", -3f);
            PaperArt.Draw(g, PaperPiece.SlotGlow, PaperTheme.Glow, Color.clear, radius + 3f);
            glow = g.gameObject;
            frame = PaperArt.Draw(UiKit.Fill(Root, "Frame"), null, Color.white, Color.clear);
            Icon = UiKit.Icon(Root, "Icon", size * 0.19f, size * 0.19f, size * 0.62f);
            RectTransform l = UiKit.Fill(Root, "Lock");
            if (!PaperArt.Redraw(l.gameObject.AddComponent<Image>(), PaperPiece.SlotLocked, new Color(0.1f, 0.09f, 0.1f, 0.72f), Color.clear, radius)) PaperKit.Padlock(l, size * 0.4f);
            l.GetComponent<Image>().raycastTarget = false;
            lockLayer = l.gameObject;
            glow.SetActive(false); lockLayer.SetActive(false); Icon.enabled = false;
            if (onClick != null) UiWinKit.MakeButton(frame, onClick);
            Set(-1, null);
        }

        public void Set(int tier, Sprite icon, bool selected = false, bool locked = false)
        {
            if (tier != shownTier)
            {
                shownTier = tier;
                if (tier < 0) PaperArt.Redraw(frame, PaperPiece.SlotEmpty, PaperTheme.Plate, PaperTheme.PlateEdge, radius);
                else PaperArt.Redraw(frame, PaperPiece.SlotPrefix + PaperTheme.RarityArt(tier), PaperTheme.RarityCell(tier), PaperTheme.Rarity(tier), radius);
            }
            if (Icon.sprite != icon) { Icon.sprite = icon; Icon.enabled = icon != null; }
            if (glow.activeSelf != selected) glow.SetActive(selected);
            if (lockLayer.activeSelf != locked) lockLayer.SetActive(locked);
        }
    }

    /// <summary>
    /// Standing item card of the carousel shelf (88 x 112 units, the cut cards' shape): picture well in the rarity colour, star row,
    /// name (up to two lines) and one small line ("Lv. 60", "x 20"); gold frame when selected; locked = dark well with a padlock and
    /// the reason in red on the small line ("Lv. 70 Required").
    /// </summary>
    public sealed class PaperCard
    {
        public const float W = 88f, H = 112f;
        const float WellX = 7f, WellY = 11f, WellH = 48f; // the picture well painted on the cut cards, in card units
        public readonly RectTransform Root;
        public readonly Image Icon;
        public readonly Text Name, Sub;
        public readonly PaperStars Stars;
        readonly Image frame, well;
        readonly GameObject glow, lockLayer;
        int shownTier = int.MinValue;

        public PaperCard(Transform parent, string name, float x, float y, Action onClick = null)
        {
            Root = UiKit.Rect(parent, name, x, y, W, H);
            frame = PaperArt.Draw(UiKit.Fill(Root, "Frame"), null, Color.white, Color.clear);
            well = PaperArt.Plate(UiKit.Rect(Root, "Well", WellX, WellY, W - WellX * 2f, WellH), Color.white, Color.clear, 5f);
            Icon = UiKit.Icon(Root, "Icon", (W - 40f) * 0.5f, WellY + 2f, 40f);
            RectTransform l = UiKit.Rect(Root, "Lock", WellX, WellY, W - WellX * 2f, WellH);
            PaperArt.Plate(l, new Color(0.1f, 0.09f, 0.1f, 0.72f), Color.clear, 5f);
            PaperKit.Padlock(l, 22f);
            lockLayer = l.gameObject;
            Stars = new PaperStars(Root, "Stars", (W - 5 * 10f) * 0.5f, 54f, 5, 10f); // straddling the lower edge of the well, like the mockup
            Name = PaperTheme.Body(Root, "Name", 6f, 65f, W - 12f, 22f, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            Name.lineSpacing = 0.8f; // two lines of 11 fill these 22 units: the box starts under the star row (ends at 64) and ends above the small line
            Sub = PaperTheme.Body(Root, "Sub", 6f, 89f, W - 12f, 13f, "", PaperTheme.BarSize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleCenter);
            RectTransform g = UiKit.Fill(Root, "Glow", -3f); // the cut selection frame is a hollow gold frame: drawn over the card
            Image glowImage = g.gameObject.AddComponent<Image>();
            glowImage.raycastTarget = false;
            if (!PaperArt.Redraw(glowImage, PaperPiece.CardGlow, PaperTheme.Glow, Color.clear, 11f)) g.SetAsFirstSibling(); // the flat glow is solid: behind the card
            glow = g.gameObject;
            glow.SetActive(false); lockLayer.SetActive(false); Icon.enabled = false;
            if (onClick != null) UiWinKit.MakeButton(frame, onClick);
        }

        public void Set(int tier, Sprite icon, string name, string sub, int stars, bool selected = false, bool locked = false)
        {
            if (tier != shownTier)
            {
                shownTier = tier;
                bool art = PaperArt.Redraw(frame, PaperPiece.CardPrefix + PaperTheme.RarityArt(tier), PaperTheme.Paper, PaperTheme.Rarity(tier), 8f);
                well.enabled = !art; // the cut cards carry their own picture well
                well.color = PaperTheme.RarityCell(tier);
            }
            if (Icon.sprite != icon) { Icon.sprite = icon; Icon.enabled = icon != null; }
            Name.text = name; // Text.text ignores an unchanged string
            Sub.text = sub;
            Sub.color = locked ? PaperTheme.Red : PaperTheme.InkSoft;
            Stars.Set(stars);
            if (glow.activeSelf != selected) glow.SetActive(selected);
            if (lockLayer.activeSelf != locked) lockLayer.SetActive(locked);
        }
    }

    /// <summary>Row of stars (gold / dark diamonds until cut). Set(n) lights the first n.</summary>
    public sealed class PaperStars
    {
        public readonly RectTransform Root;
        readonly Image[] stars;
        int shown = -1;

        public PaperStars(Transform parent, string name, float x, float y, int max, float size)
        {
            Root = UiKit.Rect(parent, name, x, y, max * size, size);
            stars = new Image[max];
            bool art = PaperArt.Has(PaperPiece.StarEmpty) && PaperArt.Has(PaperPiece.StarFull);
            for (int i = 0; i < max; i++)
            {
                float s = art ? size : size * 0.62f;
                RectTransform rt = UiWinKit.Anchored(Root, "Star" + i, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), (i + 0.5f) * size, 0f, s, s);
                stars[i] = PaperArt.Draw(rt, null, Color.white, Color.clear);
                if (!art) rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            Set(0);
        }

        public void Set(int count)
        {
            if (count == shown) return;
            shown = count;
            for (int i = 0; i < stars.Length; i++) PaperArt.Redraw(stars[i], i < count ? PaperPiece.StarFull : PaperPiece.StarEmpty, i < count ? PaperTheme.Gold : PaperTheme.Plate, Color.clear, 1.5f);
        }
    }

    public static partial class PaperKit
    {
        /// <summary>Padlock centred on a lock veil: the steel lock badge, or the old bag's padlock glyph until it is cut.</summary>
        internal static void Padlock(RectTransform veil, float size)
        {
            Image pad = UiWinKit.Anchored(veil, "Padlock", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, size, size).gameObject.AddComponent<Image>();
            pad.raycastTarget = false;
            pad.preserveAspect = true;
            Sprite s = PaperArt.Get(PaperPiece.MarkLock);
            pad.sprite = s != null ? s : BagSprites.Lock();
            pad.color = s != null ? Color.white : PaperTheme.TextLight;
        }
    }
}
