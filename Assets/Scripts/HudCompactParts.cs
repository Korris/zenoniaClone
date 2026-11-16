using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Small pieces every part of the compact HUD shares (docs/reference/ui-redesign-261116/chatgpt/hud-compact/): thin dark capsules,
    /// round portraits in a thin ring, slim bars. Art comes from Resources/Sprites/ui/paper/hudc/ (PaperPiece.Hc*); a piece that is not
    /// delivered draws flat: a dark see-through capsule with a 1-unit rim, white fills tinted by code.
    /// </summary>
    public static class HudCompact
    {
        /// <summary>The see-through dark of the pills, the thin gold of their rims, the dark outline of the bars.</summary>
        public static readonly Color Dark = new Color(0.05f, 0.06f, 0.09f, 0.8f), Rim = PaperTheme.Hex("#C59A45"), Outline = new Color(0.04f, 0.04f, 0.06f, 0.92f);
        public static readonly Color Track = new Color(0.1f, 0.11f, 0.15f, 0.88f), Veil = new Color(0f, 0f, 0f, 0.38f);

        /// <summary>
        /// Capsule on `rt`: the art of `piece` when delivered, else a plate of `rim` with a face of `fill` one unit inside it (rim alpha 0 =
        /// no rim). Returns the image on `rt` (its colour tints the art, or is the rim of the flat capsule).
        /// </summary>
        public static Image Capsule(RectTransform rt, string piece, Color fill, Color rim, float radius, bool raycast = false)
        {
            Sprite art = piece != null ? PaperArt.Get(piece) : null;
            if (art != null || rim.a <= 0f) return PaperArt.Draw(rt, art != null ? piece : null, fill, Color.clear, radius, raycast);
            Image outer = PaperArt.Plate(rt, rim, Color.clear, radius, raycast);
            PaperArt.Plate(UiKit.Fill(rt, "Inner", 1f), fill, Color.clear, Mathf.Max(1f, radius - 1f));
            return outer;
        }

        /// <summary>
        /// Round portrait: a dark disc that masks the face, in a thin ring. `platePiece` = the ring's window cut on the ring's own canvas
        /// (laid over the same square); without one the disc is flat and starts `hole` inside the square (a share of its size, measured on
        /// the ring art). Returns the face image (set its sprite); `ring` is the image to tint for a state.
        /// </summary>
        public static Image Portrait(Transform parent, string name, float x, float y, float size, string ringPiece, string platePiece, float hole, out Image ring)
        {
            RectTransform root = UiKit.Rect(parent, name, x, y, size, size);
            bool art = PaperArt.Has(ringPiece);
            float inset = !art ? 1.5f : platePiece != null && PaperArt.Has(platePiece) ? 0f : size * hole;
            if (!art) ring = PaperArt.Plate(UiKit.Fill(root, "Ring"), Rim, Color.clear, size * 0.5f); // flat: the ring is a disc under the plate
            else ring = null;
            RectTransform plate = UiKit.Fill(root, "Plate", inset);
            Image back = PaperArt.Draw(plate, inset > 0f ? null : platePiece, PaperWanted.PortraitBack, Color.clear, size * 0.5f);
            back.color = PaperWanted.PortraitBack;
            plate.gameObject.AddComponent<Mask>();
            Image face = UiKit.Fill(plate, "Face", inset > 0f ? 0f : size * hole).gameObject.AddComponent<Image>();
            face.preserveAspect = true;
            face.raycastTarget = false;
            face.enabled = false;
            if (art) ring = PaperArt.Draw(UiKit.Fill(root, "Ring"), ringPiece, Color.clear, Color.clear, size * 0.5f);
            return face;
        }

        /// <summary>Stretches a text over its parent with side padding (bar captions follow the bar's width).</summary>
        public static Text Over(Transform parent, string name, float pad, int size, Color color, TextAnchor align, PaperInk ink = PaperInk.World)
        {
            Text t = PaperTheme.Body(parent, name, 0f, 0f, 10f, 10f, "", size, color, ink, align, true);
            RectTransform rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(pad, 0f); rt.offsetMax = new Vector2(-pad, 0f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        public static void SetAlpha(Graphic g, float a) { Color c = g.color; c.a = a; g.color = c; }
    }

    /// <summary>
    /// Slim bar of the compact HUD: a dark capsule frame, a white fill tinted by code, an optional name at its left end ("HP") and a value at
    /// its right end or centre. Built on any rect (its children stretch with it, so the boss bar can change width). Set only touches the UI
    /// when the value changed.
    /// </summary>
    public sealed class HudCompactBar
    {
        public readonly RectTransform Root, Track;
        public readonly Image Frame;
        public readonly Text Label, Value;
        readonly RectTransform fillRt;
        readonly Image fill;
        float shown = -1f;

        /// <summary>`padX` / `padY` = where the frame's groove starts inside the bar, in units; `valueAlign` null = no value text.</summary>
        public HudCompactBar(RectTransform root, float height, string framePiece, string fillPiece, Color color, float padX, float padY, string label = null, TextAnchor? valueAlign = null)
        {
            Root = root;
            Frame = HudCompact.Capsule(root, framePiece, HudCompact.Track, HudCompact.Outline, height * 0.5f);
            Track = UiKit.Fill(root, "Track");
            Track.offsetMin = new Vector2(padX, padY); Track.offsetMax = new Vector2(-padX, -padY);
            fillRt = UiKit.Fill(Track, "Fill");
            fill = PaperArt.Draw(fillRt, fillPiece, color, Color.clear, (height - padY * 2f) * 0.5f);
            fill.color = color; // the cut fill is white: the tint is the bar's colour
            if (label != null)
            {
                Label = HudCompact.Over(Track, "Name", 5f, PaperTheme.BarSize, PaperTheme.TextLight, TextAnchor.MiddleLeft);
                Label.text = label;
            }
            if (valueAlign.HasValue) Value = HudCompact.Over(Track, "Value", 5f, PaperTheme.BarSize, PaperTheme.TextLight, valueAlign.Value);
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
    }
}
