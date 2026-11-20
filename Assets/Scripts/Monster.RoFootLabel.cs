using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// RO Soft foot label of a normal monster: a small rounded HP bar and "Lv.N Name" text UNDER ITS FEET, always shown while it is alive
    /// (Ragnarok-style, user 17/11 "hiện máu và tên quái, level ở dưới chân giống Ragnarok"; gold level, white name). The text has no backing
    /// pill (user 20/11 "bỏ cái background màu xanh đi, chỉ hiện chữ thôi"): a thin dark outline keeps it readable on snow and dark maps. The Adventure Paper look
    /// draws the same label with its own art (Monster.PaperOverheadBar.cs); before this file the RO look (the default since 19/11) only had the
    /// plain red quad over the head once hurt, so name and level were gone. Bosses keep nothing here: the top boss bar shows them.
    /// World sprites, not IMGUI: they sit over every monster / hero and under the skill FX, and the RO HUD (overlay canvas) always covers them,
    /// so they can never print over a HUD panel. Pieces: hud-name-hp-track (frame), hud-bar-red (fill), both 9-sliced as pills
    /// (caps = half the art height), so they stay round at any width; the name is a TextMesh over 8 dark copies offset by RoOutline.
    /// </summary>
    public partial class Monster
    {
        // Box model in world units (view 216 units tall: 1 unit = 5 px at 1080p). Pill rule of docs/ui-layout-rules.md: padding 3 / 12 (ref px).
        const float RoBarW = 17f, RoBarH = 2.8f, RoFillInset = 0.5f, RoFootY = -3.2f;
        // RoTabPadY / RoTabGap only place the text line where the old pill had it (same spot under the bar); RoOutline = outline width (~1.5 px at 1080p).
        const float RoTextH = 3.3f, RoTabPadY = 0.75f, RoTabGap = 0.6f, RoOutline = 0.3f;
        static readonly Color RoOutlineColor = new Color(0.08f, 0.1f, 0.16f, 0.9f); // dark navy ink, softer than pure black on the pastel maps
        static readonly Vector2[] RoOutlineDirs =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(0.71f, 0.71f), new Vector2(-0.71f, 0.71f), new Vector2(0.71f, -0.71f), new Vector2(-0.71f, -0.71f),
        };
        /// <summary>Labels draw over every monster and hero, under the skill FX; a monster nearer the camera (lower y) draws its label over the one behind.</summary>
        const int RoLabelBase = FeelConfig.FxOrder - 300;

        static readonly Dictionary<string, Sprite> roPills = new Dictionary<string, Sprite>();

        SpriteRenderer roTrack, roFill;
        TextMesh roName;
        MeshRenderer roNameMr;
        readonly MeshRenderer[] roOutlineMr = new MeshRenderer[8];
        bool roBuilt;

        /// <summary>A RO Soft pill piece as a 9-sliced world sprite whose art height is `heightUnits` (caps = half the height, never squashed).</summary>
        static Sprite RoPill(string piece, float heightUnits, Vector2 pivot)
        {
            string key = piece + pivot.x;
            Sprite s;
            if (roPills.TryGetValue(key, out s) && s != null) return s;
            Texture2D tex = SpriteSheet.TryLoad("Sprites/ui/ro-soft/" + piece);
            if (tex == null) return null;
            float cap = Mathf.Floor(tex.height * 0.5f);
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, tex.height / heightUnits, 0, SpriteMeshType.FullRect, new Vector4(cap, 0f, cap, 0f));
            roPills[key] = s;
            return s;
        }

        SpriteRenderer RoPart(string name, string piece, float heightUnits, Vector2 pivot)
        {
            Sprite s = RoPill(piece, heightUnits, pivot);
            SpriteRenderer sr = ProceduralSprites.Child(transform, name, s != null ? s : ProceduralSprites.White(pivot.x == 0f));
            if (s != null) sr.drawMode = SpriteDrawMode.Sliced;
            return sr;
        }

        void BuildRoFootLabel()
        {
            roBuilt = true;
            roTrack = RoPart("RoBarTrack", "hud-name-hp-track", RoBarH, new Vector2(0.5f, 0.5f));
            roFill = RoPart("RoBarFill", "hud-bar-red", RoBarH - RoFillInset * 2f, new Vector2(0f, 0.5f));
            string plain = "Lv." + def.ItemLevel + " " + def.Name;
            // Outline first (8 dark copies, plain text so the rich-text gold does not tint them), then the coloured name on top.
            for (int i = 0; i < roOutlineMr.Length; i++)
                roOutlineMr[i] = RoText("RoNameOutline", plain, RoOutlineColor).GetComponent<MeshRenderer>();
            roName = RoText("RoName", "<color=#FDC04E>Lv." + def.ItemLevel + "</color> " + def.Name, Color.white); // gold level, white name (user 17/11: one colour read as one blur)
            roNameMr = roName.GetComponent<MeshRenderer>();
            if (roTrack.drawMode == SpriteDrawMode.Sliced) roTrack.size = new Vector2(RoBarW, RoBarH);
            else { roTrack.transform.localScale = new Vector3(RoBarW, RoBarH, 1f); roTrack.color = new Color(0f, 0f, 0f, 0.7f); }
            if (roFill.drawMode != SpriteDrawMode.Sliced) roFill.color = new Color(0.9f, 0.2f, 0.25f, 1f);
        }

        TextMesh RoText(string name, string text, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            TextMesh t = go.AddComponent<TextMesh>();
            t.font = RoSoftTheme.Bold;
            t.fontSize = 40; // glyphs rasterised above their on-screen size at 1080p, sharp at 1440p
            t.characterSize = RoTextH * 10f / t.fontSize;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.richText = true;
            t.color = color;
            t.text = text;
            go.GetComponent<MeshRenderer>().sharedMaterial = t.font.material;
            return t;
        }

        /// <summary>Shows / places the label for this frame (called from Render for a normal monster under the RO look).</summary>
        void RenderRoFootLabel()
        {
            if (!roBuilt) BuildRoFootLabel();
            bool show = state != State.Dying;
            SetRoFootLabel(show);
            if (!show) return;
            // Front-to-back by the map row of the feet: up to 48 rows x 4 layers stays under RoLabelBase + 200 (< WorldBuilder's debug overlay).
            int row = Mathf.Clamp(Mathf.RoundToInt((GameConfig.MapH - y) / GameConfig.TileUnits), 0, 49);
            int o = RoLabelBase + row * 4;
            float barY = RoFootY, tabH = RoTextH + RoTabPadY * 2f, tabY = barY - RoBarH * 0.5f - RoTabGap - tabH * 0.5f;
            roTrack.transform.localPosition = new Vector3(0f, barY, 0f);
            float innerW = RoBarW - RoFillInset * 2f, innerH = RoBarH - RoFillInset * 2f, v = Mathf.Clamp01(hp / def.Hp);
            roFill.enabled = v > 0f;
            roFill.transform.localPosition = new Vector3(-innerW * 0.5f, barY, 0f);
            float fillW = Mathf.Max(innerH, innerW * v); // a pill never narrower than its own height (round caps stay round)
            if (roFill.drawMode == SpriteDrawMode.Sliced) roFill.size = new Vector2(fillW, innerH);
            else roFill.transform.localScale = new Vector3(fillW, innerH, 1f);
            float textY = tabY + RoTextH * 0.04f; // Nunito sits a hair low in its line box
            roName.transform.localPosition = new Vector3(0f, textY, 0f);
            for (int i = 0; i < roOutlineMr.Length; i++)
            {
                roOutlineMr[i].transform.localPosition = new Vector3(RoOutlineDirs[i].x * RoOutline, textY + RoOutlineDirs[i].y * RoOutline, 0f);
                roOutlineMr[i].sortingOrder = o;
            }
            roNameMr.sortingOrder = o + 1;
            roTrack.sortingOrder = o + 2;
            roFill.sortingOrder = o + 3;
            MonsterFootLabelCrowd.Register(this); // crowd / HUD pass after every monster has placed its label
        }

        static readonly Vector3[] roCorner = new Vector3[2];

        /// <summary>GUI-space rects (y down, pixels) of the name text (with its outline), the HP bar and the whole label; false when off screen / not shown.</summary>
        public bool RoFootLabelScreenRects(Camera cam, out Rect tab, out Rect bar, out Rect all)
        {
            tab = bar = all = Rect.zero;
            if (!roBuilt || !roNameMr.enabled) return false;
            Bounds nb = roNameMr.bounds;
            nb.Expand(new Vector3(RoOutline * 2f, RoTabPadY * 2f, 0f)); // outline + the old pill's line box, so the crowd spacing stays as before
            tab = GuiRect(cam, nb);
            bar = GuiRect(cam, roTrack.bounds);
            all = Rect.MinMaxRect(Mathf.Min(tab.xMin, bar.xMin), Mathf.Min(tab.yMin, bar.yMin), Mathf.Max(tab.xMax, bar.xMax), Mathf.Max(tab.yMax, bar.yMax));
            return all.xMax > 0f && all.xMin < Screen.width && all.yMax > 0f && all.yMin < Screen.height;
        }

        static Rect GuiRect(Camera cam, Bounds b)
        {
            roCorner[0] = cam.WorldToScreenPoint(b.min);
            roCorner[1] = cam.WorldToScreenPoint(b.max);
            return Rect.MinMaxRect(roCorner[0].x, Screen.height - roCorner[1].y, roCorner[1].x, Screen.height - roCorner[0].y);
        }

        /// <summary>MonsterFootLabelCrowd: hides the name text (crowd) or the whole label (under the HUD) for this frame.</summary>
        public void HideRoFootLabel(bool bar)
        {
            if (!roBuilt) return;
            SetRoName(false);
            if (bar) { roTrack.enabled = false; roFill.enabled = false; }
        }

        void SetRoFootLabel(bool on)
        {
            if (!roBuilt) return;
            roTrack.enabled = on; roFill.enabled = on;
            SetRoName(on);
        }

        void SetRoName(bool on)
        {
            roNameMr.enabled = on;
            foreach (MeshRenderer mr in roOutlineMr) mr.enabled = on;
        }
    }
}
