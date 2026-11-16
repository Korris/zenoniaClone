using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Progress bar of the style board ("Thanh tiến độ"): gold-capped frame (the long piece from 20 units high, the short one below;
    /// their groove is about half the bar height, so a bar with text wants 26 units or more),
    /// one white fill piece tinted by code (HP red, MP blue, EXP green ... PaperTheme.Bar*), an optional dark tag on the left ("EXP")
    /// and a value text. Set / SetText only touch the UI when the value changed, so they are safe to call every frame.
    /// </summary>
    public sealed class PaperBar
    {
        public readonly RectTransform Root;
        public readonly Text Value;
        readonly RectTransform fillRt;
        readonly Image fill;
        float shown = -1f;

        public PaperBar(Transform parent, string name, float x, float y, float w, float h, Color color, string tag = null)
        {
            Root = UiKit.Rect(parent, name, x, y, w, h);
            bool art = PaperArt.Draw(Root, h >= 20f ? PaperPiece.BarLarge : PaperPiece.BarSmall, PaperTheme.Plate, PaperTheme.PlateEdge, h * 0.5f).sprite != PaperArt.Round();
            // The track (dark groove) inside the frame: the cut frames have gold end caps 0.46 h wide and a groove about half the height.
            float flat = h >= 20f ? 3f : 2f;
            float side = art ? h * 0.46f : flat, top = art ? h * 0.29f : flat, bottom = art ? h * 0.25f : flat + 1f, trackH = h - top - bottom;
            float tagW = tag != null ? Mathf.Max(trackH * 2.4f, 30f) : 0f;
            RectTransform track = UiKit.Fill(Root, "Track");
            track.offsetMin = new Vector2(side + tagW, bottom); track.offsetMax = new Vector2(-side, -top);
            fillRt = UiKit.Fill(track, "Fill");
            fill = PaperArt.Draw(fillRt, PaperPiece.BarFill, color, Color.clear, trackH * 0.5f);
            fill.color = color; // the cut fill is white: the tint is the bar's colour
            int size = Mathf.Clamp(Mathf.RoundToInt(trackH * 0.95f), PaperTheme.BarSize, PaperTheme.BodySize);
            if (tag != null) PaperTheme.Title(Root, "Tag", side, top, tagW, trackH, tag, size, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            Value = PaperTheme.Body(Root, "Value", side + tagW, top, w - side * 2f - tagW - 4f, trackH, "", size, PaperTheme.TextLight, PaperInk.World, tag != null ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter, true);
            Set(1f);
        }

        public void Set(float frac)
        {
            frac = Mathf.Clamp01(frac);
            if (Mathf.Approximately(frac, shown)) return;
            shown = frac;
            fillRt.anchorMax = new Vector2(frac, 1f);
            fill.enabled = frac > 0f;
        }

        public void SetColor(Color color) { if (fill.color != color) fill.color = color; }

        /// <summary>Value caption ("512 / 818"). Build the string only when the numbers changed (see RpgHud): this only skips the redraw.</summary>
        public void SetText(string text) { Value.text = text; }
    }

    /// <summary>
    /// Resource chip ("3,000 G", "x20"): dark pill, icon sitting on its left end, value text and an optional gold "+" (buy / top up).
    /// The caller sets Icon.sprite from the game's own art (LootSprites, potion icons ...).
    /// </summary>
    public sealed class PaperChip
    {
        public readonly RectTransform Root;
        public readonly Image Icon;
        public readonly Text Value;

        public PaperChip(Transform parent, string name, float x, float y, float w, float h, Sprite icon, string text, Action onPlus = null)
        {
            Root = UiKit.Rect(parent, name, x, y, w, h);
            PaperArt.Draw(Root, PaperPiece.Chip, PaperTheme.Plate, PaperTheme.PlateEdge, h * 0.5f);
            float iconSize = h + 4f, plusSize = onPlus != null ? h - 4f : 0f;
            Icon = UiKit.Icon(Root, "Icon", -2f, -2f, iconSize);
            Icon.sprite = icon;
            Icon.enabled = icon != null;
            Value = PaperTheme.Body(Root, "Value", iconSize, 0f, w - iconSize - plusSize - 6f, h, text, PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.World, TextAnchor.MiddleCenter, true);
            Value.horizontalOverflow = HorizontalWrapMode.Overflow; // one line: a long amount grows sideways instead of wrapping
            if (onPlus == null) return;
            RectTransform plus = UiKit.Rect(Root, "Plus", w - plusSize - 2f, 2f, plusSize, plusSize);
            Image face = PaperArt.Draw(plus, PaperPiece.Plus, PaperTheme.BtnGold, PaperTheme.BtnGoldEdge, 4f);
            if (face.sprite == PaperArt.Round()) PaperTheme.Caption(plus, "+", Mathf.RoundToInt(plusSize * 0.8f), PaperTheme.TextLight);
            UiWinKit.MakeButton(face, onPlus);
        }

        public void SetText(string text) { Value.text = text; }
    }
}
