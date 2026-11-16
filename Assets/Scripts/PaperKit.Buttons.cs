using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>Button colours of the style board: Gold = the main action, Blue = secondary, Red = destructive, Purple = premium (gacha x10, respec), Grey = cannot be used.</summary>
    public enum PaperTone { Gold, Blue, Red, Purple, Grey }

    /// <summary>Tab looks: leather tab on top of a window, wooden plank of a side menu, rounded filter chip of a filter row.</summary>
    public enum PaperTabStyle { Leather, Plank, Chip }

    public static partial class PaperKit
    {
        internal static readonly string[] TonePieces = { PaperPiece.BtnGold, PaperPiece.BtnBlue, PaperPiece.BtnRed, PaperPiece.BtnPurple, PaperPiece.BtnGrey };
        static readonly Color[] ToneFills = { PaperTheme.BtnGold, PaperTheme.BtnBlue, PaperTheme.BtnRed, PaperTheme.Purple, PaperTheme.Disabled };
        static readonly Color[] ToneEdges = { PaperTheme.BtnGoldEdge, PaperTheme.BtnBlueEdge, PaperTheme.BtnRedEdge, PaperTheme.BtnPurpleEdge, PaperTheme.PlateEdge };

        internal static Color ToneFill(PaperTone t) { return ToneFills[(int)t]; }
        internal static Color ToneEdge(PaperTone t) { return ToneEdges[(int)t]; }
        internal static Color ToneText(PaperTone t) { return t == PaperTone.Gold ? PaperTheme.Ink : t == PaperTone.Grey ? PaperTheme.DisabledText : PaperTheme.TextLight; }

        /// <summary>Round wooden page arrow of a carousel (left = previous).</summary>
        public static Button PageArrow(Transform parent, string name, float x, float y, float size, bool left, Action onClick)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, size, size);
            Image img = PaperArt.Draw(rt, left ? PaperPiece.ArrowLeft : PaperPiece.ArrowRight, PaperTheme.Wood, PaperTheme.BtnGoldEdge, size * 0.5f);
            if (img.sprite == PaperArt.Round()) PaperTheme.Caption(rt, left ? "<" : ">", Mathf.RoundToInt(size * 0.55f), PaperTheme.TitleGold);
            return UiWinKit.MakeButton(img, onClick);
        }
    }

    /// <summary>Pill button with a centred caption. Pressed = its "-pressed" art (a darker tint until cut); SetEnabled(false) = the grey look, not clickable.</summary>
    public sealed class PaperButton
    {
        public readonly RectTransform Root;
        public readonly Text Label;
        public readonly Button Click;
        readonly Image face;
        readonly Outline outline;
        readonly PaperTone tone;
        readonly float radius;
        bool enabled = true;

        public PaperButton(Transform parent, string name, float x, float y, float w, float h, string text, PaperTone tone, Action onClick)
        {
            this.tone = tone;
            radius = h * 0.38f;
            Root = UiKit.Rect(parent, name, x, y, w, h);
            face = PaperArt.Draw(Root, null, Color.white, Color.clear, radius);
            Label = PaperTheme.Caption(Root, text, Mathf.Clamp(Mathf.RoundToInt(h * 0.42f), PaperTheme.BodySize, PaperTheme.TitleSize), PaperKit.ToneText(tone));
            Label.rectTransform.offsetMin = new Vector2(0f, h * 0.06f); // the art's face sits a little above its drop edge
            outline = Label.GetComponent<Outline>();
            Click = UiWinKit.MakeButton(face, onClick);
            ColorBlock cb = Click.colors;
            cb.disabledColor = Color.white; // the grey look comes from the piece / plate colour, not from a tint
            Click.colors = cb;
            Show(tone);
            if (tone == PaperTone.Grey) { enabled = false; Click.interactable = false; }
        }

        public void SetEnabled(bool on)
        {
            if (on == enabled || tone == PaperTone.Grey) return;
            enabled = on;
            Click.interactable = on;
            Show(on ? tone : PaperTone.Grey);
        }

        void Show(PaperTone t)
        {
            string piece = PaperKit.TonePieces[(int)t];
            bool art = PaperArt.Redraw(face, piece, PaperKit.ToneFill(t), PaperKit.ToneEdge(t), radius);
            Sprite pressed = art && t != PaperTone.Grey ? PaperArt.Get(piece + PaperPiece.Pressed) : null;
            Click.transition = pressed != null ? Selectable.Transition.SpriteSwap : Selectable.Transition.ColorTint;
            if (pressed != null) Click.spriteState = new SpriteState { pressedSprite = pressed };
            Label.color = PaperKit.ToneText(t);
            outline.enabled = t != PaperTone.Gold && t != PaperTone.Grey; // light captions only: dark text on gold needs no edge
        }
    }

    /// <summary>One tab (leather tab, side-menu plank or filter chip). SetActive picks the lit look.</summary>
    public sealed class PaperTab
    {
        public readonly RectTransform Root;
        public readonly Text Label;
        readonly Image face;
        readonly Outline outline;
        readonly PaperTabStyle style;
        int shown = -1;

        public PaperTab(Transform parent, string name, float x, float y, float w, float h, string text, PaperTabStyle style, Action onClick)
        {
            this.style = style;
            Root = UiKit.Rect(parent, name, x, y, w, h);
            face = PaperArt.Draw(Root, null, Color.white, Color.clear);
            Label = PaperTheme.Caption(Root, text, style == PaperTabStyle.Chip ? PaperTheme.BodySize : PaperTheme.SectionSize, PaperTheme.TextLight);
            outline = Label.GetComponent<Outline>();
            UiWinKit.MakeButton(face, onClick);
            SetActive(false);
        }

        public void SetActive(bool on)
        {
            if (shown == (on ? 1 : 0)) return;
            shown = on ? 1 : 0;
            bool lightText = !on && style != PaperTabStyle.Chip; // inactive leather / plank are dark; chips are gold (on) or cream (off)
            float r = style == PaperTabStyle.Chip ? Root.sizeDelta.y * 0.5f : 8f;
            switch (style)
            {
                case PaperTabStyle.Plank: PaperArt.Redraw(face, on ? PaperPiece.PlankActive : PaperPiece.PlankInactive, on ? PaperTheme.BtnGold : PaperTheme.Wood, on ? PaperTheme.BtnGoldEdge : PaperTheme.WoodDark, r); break;
                case PaperTabStyle.Chip: PaperArt.Redraw(face, on ? PaperPiece.ChipActive : PaperPiece.ChipInactive, on ? PaperTheme.BtnGold : PaperTheme.Cream, on ? PaperTheme.BtnGoldEdge : PaperTheme.PaperEdge, r); break;
                default: PaperArt.Redraw(face, on ? PaperPiece.TabActive : PaperPiece.TabInactive, on ? PaperTheme.Paper : PaperTheme.WoodDark, on ? PaperTheme.BtnGoldEdge : PaperTheme.PlateEdge, r); break;
            }
            Label.color = lightText ? PaperTheme.TextLight : PaperTheme.Ink;
            outline.enabled = lightText;
        }
    }

    /// <summary>Row of page dots ("Page 1/3"): the current one gold, the rest dark.</summary>
    public sealed class PaperDots
    {
        public readonly RectTransform Root;
        readonly Image[] dots;
        readonly float size;
        int shown = -1;

        public PaperDots(Transform parent, string name, float x, float y, int count, float size = 10f)
        {
            this.size = size;
            Root = UiKit.Rect(parent, name, x, y, count * size * 1.5f, size);
            dots = new Image[count];
            for (int i = 0; i < count; i++) dots[i] = PaperArt.Draw(UiKit.Rect(Root, "Dot" + i, i * size * 1.5f, 0f, size, size), null, Color.white, Color.clear);
            Set(0);
        }

        public void Set(int index)
        {
            if (index == shown) return;
            shown = index;
            for (int i = 0; i < dots.Length; i++) PaperArt.Redraw(dots[i], i == index ? PaperPiece.DotOn : PaperPiece.DotOff, i == index ? PaperTheme.Gold : PaperTheme.Plate, Color.clear, size * 0.5f);
        }
    }
}
