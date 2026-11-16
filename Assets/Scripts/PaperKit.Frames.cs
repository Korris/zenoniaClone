using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>Panel looks of the style board ("Khung cửa sổ &amp; panel").</summary>
    public enum PaperFrame
    {
        Paper,  // parchment in a wooden frame: main windows
        Pinned, // parchment pinned by nails: detail cards hanging over a scene
        Wood,   // wooden board: shelves, notice boards
        Slate,  // dark slate in a gold-cornered frame: icon / preview wells
        Card,   // small parchment card with a stitched edge: sections inside a window
    }

    /// <summary>
    /// Builders of the Adventure Paper widgets (frames here; buttons, slots, bars and badges in the other PaperKit.*.cs files).
    /// Like UiKit: no prefabs, positions are top-left based in canvas units, every piece falls back to a flat plate until its art is cut (PaperArt).
    /// Widgets that change at run time are small classes (PaperButton, PaperTab, PaperSlot, PaperBar ...) built with new; their Set... methods
    /// only touch the UI when the value changed.
    /// </summary>
    public static partial class PaperKit
    {
        static readonly string[] FramePieces = { PaperPiece.FramePaper, PaperPiece.FramePinned, PaperPiece.FrameWood, PaperPiece.FrameSlate, PaperPiece.FrameCard };

        // Thickness of each frame's drawn rim in canvas units (measured on the cut art): content belongs inside it.
        static readonly float[] FrameInsets = { 24f, 20f, 16f, 14f, 14f };

        /// <summary>The area inside a frame's rim: build the panel's content on this (top-left based) so nothing sits on the wood / torn edge.</summary>
        public static RectTransform Inner(RectTransform frame, PaperFrame kind) { return UiKit.Fill(frame, "Inner", FrameInsets[(int)kind]); }

        /// <summary>A panel. Put content on Inner(frame, kind); only pieces meant to overlap the rim (close button, ribbon) go on the frame itself.</summary>
        public static RectTransform Frame(Transform parent, string name, float x, float y, float w, float h, PaperFrame kind, bool raycast = true)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            Color fill, edge;
            switch (kind)
            {
                case PaperFrame.Wood: fill = PaperTheme.Wood; edge = PaperTheme.WoodDark; break;
                case PaperFrame.Slate: fill = PaperTheme.Slate; edge = PaperTheme.Wood; break;
                case PaperFrame.Pinned: fill = PaperTheme.Paper; edge = PaperTheme.PaperEdge; break;
                case PaperFrame.Card: fill = PaperTheme.Cream; edge = PaperTheme.PaperEdge; break;
                default: fill = PaperTheme.Paper; edge = PaperTheme.Wood; break;
            }
            PaperArt.Draw(rt, FramePieces[(int)kind], fill, edge, kind == PaperFrame.Card || kind == PaperFrame.Slate ? 6f : 10f, raycast);
            return rt;
        }

        /// <summary>Wooden title sign of a screen with laurels ("ARMORY CAROUSEL" + a smaller second line; subtitle null = one line). The art is 3.25 : 1.</summary>
        public static RectTransform TitleBoard(Transform parent, float x, float y, float w, float h, string title, string subtitle = null)
        {
            RectTransform rt = UiKit.Rect(parent, "TitleBoard", x, y, w, h);
            PaperArt.Draw(rt, PaperPiece.TitleBoard, PaperTheme.Wood, PaperTheme.WoodDark, 10f);
            float top = h * 0.14f, body = h * 0.7f; // the plank between the laurels
            Text t = PaperTheme.Title(rt, "Title", 0f, top, w, subtitle != null ? body * 0.6f : body, title, PaperTheme.TitleSize, PaperTheme.TitleGold, PaperInk.Outlined, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow; // one line, centred on the plank
            if (subtitle != null) PaperTheme.Body(rt, "Subtitle", 0f, top + body * 0.56f, w, body * 0.4f, subtitle, PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter, true);
            return rt;
        }

        /// <summary>Red cloth ribbon with a centred caption ("Lv. 60", "LEVEL UP!"). The art is 3.5 : 1 with folded tails.</summary>
        public static Text Ribbon(Transform parent, string name, float x, float y, float w, float h, string text)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            bool art = PaperArt.Draw(rt, PaperPiece.Ribbon, PaperTheme.Red, PaperTheme.BtnRedEdge, h * 0.3f).sprite != PaperArt.Round();
            Text t = PaperTheme.Caption(rt, text, Mathf.Clamp(Mathf.RoundToInt(h * 0.5f), PaperTheme.BodySize, PaperTheme.TitleSize), PaperTheme.TextLight);
            if (art) { t.rectTransform.offsetMin = new Vector2(0f, h * 0.36f); t.rectTransform.offsetMax = new Vector2(0f, -h * 0.1f); } // the band is the upper part, the tails hang below
            return t;
        }

        /// <summary>Leather strip heading a block inside a window ("Compare with Equipped").</summary>
        public static Text SectionStrip(Transform parent, string name, float x, float y, float w, float h, string text)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            PaperArt.Draw(rt, PaperPiece.Strip, PaperTheme.WoodDark, Color.clear, h * 0.3f);
            return PaperTheme.Caption(rt, text, PaperTheme.BodySize, PaperTheme.TextLight);
        }

        /// <summary>Section heading on paper: a small gold diamond (the board's four-point star) + title text.</summary>
        public static Text Heading(Transform parent, string name, float x, float y, float w, string text)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, 22f);
            RectTransform star = UiWinKit.Anchored(rt, "Star", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), 10f, 0f, 10f, 10f);
            PaperArt.Plate(star, PaperTheme.BtnGoldEdge, Color.clear, 2f);
            star.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return PaperTheme.Title(rt, "Text", 24f, 0f, w - 24f, 22f, text, PaperTheme.SectionSize, PaperTheme.Ink);
        }

        /// <summary>Round red close button (X) at a top-left based position.</summary>
        public static Button CloseButton(Transform parent, float x, float y, float size, Action onClick)
        {
            RectTransform rt = UiKit.Rect(parent, "Close", x, y, size, size);
            Image img = PaperArt.Draw(rt, PaperPiece.Close, PaperTheme.BtnRed, PaperTheme.BtnRedEdge, size * 0.5f);
            if (img.sprite == PaperArt.Round()) PaperTheme.Caption(rt, "X", Mathf.RoundToInt(size * 0.5f), PaperTheme.TextLight);
            return UiWinKit.MakeButton(img, onClick);
        }
    }
}
