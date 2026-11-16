using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Big button of Lumi's counter (the cut gacha/btn-buy-gold, btn-pull-blue, btn-pull-purple; a flat plate for a piece that is not cut):
    /// a caption and, with an icon piece, a second line "icon + amount" under it ("Pull x1" over "coin x1", "Buy 1 Coin" over "crystal 150").
    /// SetLit(false) shows the kit's grey button but keeps the click: a pull without coins must still open the "not enough coins" paper.
    /// </summary>
    public sealed class PaperGachaButton
    {
        public readonly RectTransform Root;
        public readonly Text Title, Sub;
        readonly Image face, icon;
        readonly Button click;
        readonly string piece;
        readonly Color fill, edge, textColor;
        readonly float w, h;
        int lit = -1;

        public PaperGachaButton(Transform parent, string name, float x, float y, float w, float h, string piece, Color fill, Color edge, Color textColor, string title, Action onClick, string iconPiece = null)
        {
            this.piece = piece; this.fill = fill; this.edge = edge; this.textColor = textColor; this.w = w; this.h = h;
            Root = UiKit.Rect(parent, name, x, y, w, h);
            face = PaperArt.Draw(Root, null, Color.white, Color.clear, h * 0.3f);
            click = UiWinKit.MakeButton(face, onClick);
            bool two = iconPiece != null;
            PaperInk ink = textColor == PaperTheme.Ink ? PaperInk.Plain : PaperInk.Outlined;
            int size = Mathf.Clamp(Mathf.RoundToInt(h * (two ? 0.27f : 0.36f)), PaperTheme.BodySize, PaperTheme.TitleSize);
            Title = PaperTheme.Title(Root, "Title", 0f, two ? h * 0.1f : 0f, w, two ? h * 0.42f : h * 0.94f, title, size, textColor, ink, TextAnchor.MiddleCenter);
            Title.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (two)
            {
                icon = UiKit.Icon(Root, "Icon", 0f, h * 0.5f, h * 0.3f);
                icon.sprite = PaperArt.Get(iconPiece);
                icon.enabled = icon.sprite != null;
                Sub = PaperTheme.Title(Root, "Sub", 0f, h * 0.47f, w, h * 0.36f, "", size - 1, textColor, ink, TextAnchor.MiddleLeft);
                Sub.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            SetLit(true);
        }

        /// <summary>The amount beside the icon; icon and text are centred together.</summary>
        public void SetSub(string text)
        {
            if (Sub == null || Sub.text == text) return;
            Sub.text = text;
            float iw = icon.enabled ? icon.rectTransform.sizeDelta.x + 3f : 0f, x0 = (w - iw - Sub.preferredWidth) * 0.5f;
            icon.rectTransform.anchoredPosition = new Vector2(x0, -h * 0.5f);
            Sub.rectTransform.anchoredPosition = new Vector2(x0 + iw, -h * 0.47f);
        }

        public void SetLit(bool on)
        {
            if (lit == (on ? 1 : 0)) return;
            lit = on ? 1 : 0;
            bool art = on ? PaperArt.Redraw(face, piece, fill, edge, h * 0.3f) : PaperArt.Redraw(face, PaperPiece.BtnGrey, PaperTheme.Disabled, PaperTheme.PlateEdge, h * 0.3f);
            Sprite pressed = on && art ? PaperArt.Get(piece + PaperPiece.Pressed) : null;
            click.transition = pressed != null ? Selectable.Transition.SpriteSwap : Selectable.Transition.ColorTint;
            if (pressed != null) click.spriteState = new SpriteState { pressedSprite = pressed };
            Paint(Title, on);
            if (Sub != null) Paint(Sub, on);
        }

        void Paint(Text t, bool on)
        {
            t.color = on ? textColor : PaperTheme.DisabledText;
            Outline o = t.GetComponent<Outline>();
            if (o != null) o.enabled = on;
        }
    }
}
