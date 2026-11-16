using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Shared look of the compact HUD controls (hud-compact-parts-board-states-v2.png): flat dark semi-transparent plates with a thin
    /// light rim, so the eye stays on the hero. Every piece is cut art from Resources/Sprites/ui/paper/hudc/ when it is delivered;
    /// until then the plate below is drawn, which is already the compact look (no wood, no leather, no gold frames).
    /// </summary>
    public static class PaperHudCompact
    {
        public static readonly Color Dark = new Color(0.06f, 0.07f, 0.1f, 0.62f), DarkPressed = new Color(0.2f, 0.17f, 0.1f, 0.8f);
        public static readonly Color Rim = new Color(1f, 0.95f, 0.84f, 0.5f), RimPressed = new Color(1f, 0.82f, 0.4f, 0.95f);
        /// <summary>Behind a skill / potion icon, and the cooldown sweep over it.</summary>
        public static readonly Color Well = new Color(0.05f, 0.06f, 0.09f, 0.82f), Sweep = new Color(0.02f, 0.02f, 0.03f, 0.72f);
        public static readonly Color NoMpRed = new Color(1f, 0.42f, 0.37f);
        static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

        /// <summary>Round (radius = half the width) or rounded plate filling `rt`: the art of `piece`, else the dark plate with its rim.</summary>
        public static Image Plate(RectTransform rt, string piece, float radius, bool raycast = false)
        {
            return PaperArt.Draw(rt, piece, Dark, Rim, radius, raycast);
        }

        /// <summary>Pressed / released look of a plate made by Plate (the art's "-pressed" piece when it is cut).</summary>
        public static void Press(Image plate, string piece, float radius, bool down)
        {
            PaperArt.Redraw(plate, down ? piece + PaperPiece.Pressed : piece, down ? DarkPressed : Dark, down ? RimPressed : Rim, radius);
        }

        /// <summary>
        /// A control = an invisible touch cell of `cell` units centred on (x, y) from `corner`, with the visible plate of `size` units
        /// in its middle (child "Art"). The cell is what a finger hits: never smaller than 44 units for the small icons.
        /// </summary>
        public static RectTransform Cell(Transform parent, string name, Vector2 corner, float x, float y, float cell, float size, out RectTransform art)
        {
            RectTransform root = TouchControls.Place(parent, name, corner, x, y, cell);
            TouchControls.Img(root, null, Color.clear, true);
            art = TouchControls.Place(root, "Art", Mid, 0f, 0f, size);
            return root;
        }

        /// <summary>
        /// Window of a ring / slot: a dark well, `inset` units inside `face`, that masks the icon and the cooldown sweep put in it.
        /// Without art the thin rim is drawn under the well (a flat plate cannot be hollow); a cut ring is laid over the icon by the
        /// caller, a cut hotbar slot (it has its own dark middle) under the well.
        /// </summary>
        public static RectTransform MaskedWell(RectTransform face, float radius, bool rimUnder, float inset = 2f)
        {
            if (rimUnder) PaperArt.Plate(UiKit.Fill(face, "Rim"), Rim, Color.clear, radius);
            RectTransform well = UiKit.Fill(face, "Well", inset);
            PaperArt.Plate(well, Well, Color.clear, Mathf.Max(1f, radius - inset));
            well.gameObject.AddComponent<Mask>();
            return well;
        }

        /// <summary>Radial cooldown sweep filling `well` (hidden until enabled). The cut disc of the round slots is 7/8 of its canvas.</summary>
        public static Image CooldownSweep(RectTransform well, float size, bool square)
        {
            Sprite disc = square ? null : PaperArt.Get(PaperPiece.CSkillDim);
            Image sweep = TouchControls.Child(well, "Cooldown", disc != null ? disc : PaperArt.Round(), disc != null ? size * 1.1f : size, Sweep);
            sweep.type = Image.Type.Filled; sweep.fillMethod = Image.FillMethod.Radial360; sweep.fillOrigin = (int)Image.Origin360.Top; sweep.fillClockwise = true;
            sweep.enabled = false;
            return sweep;
        }

        /// <summary>Small key label of the PC layout ("B", "Shift", "LMB"), centred on (x, y) from `corner`: light text on a dark cap.</summary>
        public static void KeyLabel(Transform parent, Vector2 corner, float x, float y, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            const float H = HudAnchors.IconKeyHeight;
            bool one = text.Length == 1;
            RectTransform k = TouchControls.Place(parent, "Key" + text, corner, x, y, H);
            k.sizeDelta = new Vector2(one ? H + 3f : 9f + text.Length * 5.6f, H);
            Image cap = PaperArt.Draw(k, one ? PaperPiece.CKeycap : PaperPiece.CKeycapWide, new Color(0.04f, 0.05f, 0.08f, 0.8f), Color.clear, 4f);
            if (one && cap.sprite != PaperArt.Round()) cap.type = Image.Type.Simple; // drawn whole: its corners are larger than a cap this small
            PaperTheme.Caption(k, text, PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Plain);
        }

        /// <summary>Swaps a kit badge / mark for its compact piece when that is cut.</summary>
        public static void Reskin(GameObject mark, string piece)
        {
            Sprite s = PaperArt.Get(piece);
            if (s == null) return;
            PaperArt.Apply(mark.GetComponent<Image>(), s);
            Transform face = mark.transform.Find("Face"); // the flat fallback's inner plate
            if (face != null) face.gameObject.SetActive(false);
        }
    }
}
