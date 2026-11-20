using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// RO Soft foot label of a normal monster: a small rounded HP bar and a navy "Lv.N Name" tab UNDER ITS FEET, always shown while it is alive
    /// (Ragnarok-style, user 17/11 "hiện máu và tên quái, level ở dưới chân giống Ragnarok"; gold level, white name). The Adventure Paper look
    /// draws the same label with its own art (Monster.PaperOverheadBar.cs); before this file the RO look (the default since 19/11) only had the
    /// plain red quad over the head once hurt, so name and level were gone. Bosses keep nothing here: the top boss bar shows them.
    /// World sprites, not IMGUI: they sit over every monster / hero and under the skill FX, and the RO HUD (overlay canvas) always covers them,
    /// so they can never print over a HUD panel. Pieces: hud-name-hp-track (frame), hud-bar-red (fill), hud-lv-badge (tab), all 9-sliced
    /// as pills (caps = half the art height), so they stay round at any width.
    /// </summary>
    public partial class Monster
    {
        // Box model in world units (view 216 units tall: 1 unit = 5 px at 1080p). Pill rule of docs/ui-layout-rules.md: padding 3 / 12 (ref px).
        const float RoBarW = 17f, RoBarH = 2.8f, RoFillInset = 0.5f, RoFootY = -3.2f;
        const float RoTextH = 3.3f, RoTabPadX = 2.2f, RoTabPadY = 0.75f, RoTabGap = 0.6f;
        /// <summary>Labels draw over every monster and hero, under the skill FX; a monster nearer the camera (lower y) draws its label over the one behind.</summary>
        const int RoLabelBase = FeelConfig.FxOrder - 300;

        static readonly Dictionary<string, Sprite> roPills = new Dictionary<string, Sprite>();

        SpriteRenderer roTrack, roFill, roTab;
        TextMesh roName;
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
            float tabH = RoTextH + RoTabPadY * 2f;
            roTab = RoPart("RoNameTab", "hud-lv-badge", tabH, new Vector2(0.5f, 0.5f));
            var go = new GameObject("RoName");
            go.transform.SetParent(transform, false);
            roName = go.AddComponent<TextMesh>();
            roName.font = RoSoftTheme.Bold;
            roName.fontSize = 40; // glyphs rasterised above their on-screen size at 1080p, sharp at 1440p
            roName.characterSize = RoTextH * 10f / roName.fontSize;
            roName.anchor = TextAnchor.MiddleCenter;
            roName.alignment = TextAlignment.Center;
            roName.richText = true;
            roName.color = Color.white;
            roName.text = "<color=#FDC04E>Lv." + def.ItemLevel + "</color> " + def.Name; // gold level, white name (user 17/11: one colour read as one blur)
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = roName.font.material;
            // Tab = text box + padding (CSS-like box, docs/ui-layout-rules.md section 1), at least a round pill.
            float tabW = Mathf.Max(tabH * 2f, mr.bounds.size.x + RoTabPadX * 2f);
            if (roTab.drawMode == SpriteDrawMode.Sliced) roTab.size = new Vector2(tabW, tabH);
            else roTab.transform.localScale = new Vector3(tabW, tabH, 1f);
            if (roTrack.drawMode == SpriteDrawMode.Sliced) roTrack.size = new Vector2(RoBarW, RoBarH);
            else { roTrack.transform.localScale = new Vector3(RoBarW, RoBarH, 1f); roTrack.color = new Color(0f, 0f, 0f, 0.7f); }
            if (roFill.drawMode != SpriteDrawMode.Sliced) roFill.color = new Color(0.9f, 0.2f, 0.25f, 1f);
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
            roTab.transform.localPosition = new Vector3(0f, tabY, 0f);
            roName.transform.localPosition = new Vector3(0f, tabY + RoTextH * 0.04f, 0f); // Nunito sits a hair low in its line box
            roTab.sortingOrder = o;
            roName.GetComponent<MeshRenderer>().sortingOrder = o + 1;
            roTrack.sortingOrder = o + 2;
            roFill.sortingOrder = o + 3;
            MonsterFootLabelCrowd.Register(this); // crowd / HUD pass after every monster has placed its label
        }

        static readonly Vector3[] roCorner = new Vector3[2];

        /// <summary>GUI-space rects (y down, pixels) of the name tab, the HP bar and the whole label; false when off screen / not shown.</summary>
        public bool RoFootLabelScreenRects(Camera cam, out Rect tab, out Rect bar, out Rect all)
        {
            tab = bar = all = Rect.zero;
            if (!roBuilt || !roTab.enabled) return false;
            tab = GuiRect(cam, roTab.bounds);
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

        /// <summary>MonsterFootLabelCrowd: hides the name tab (crowd) or the whole label (under the HUD) for this frame.</summary>
        public void HideRoFootLabel(bool bar)
        {
            if (!roBuilt) return;
            roTab.enabled = false;
            roName.GetComponent<MeshRenderer>().enabled = false;
            if (bar) { roTrack.enabled = false; roFill.enabled = false; }
        }

        void SetRoFootLabel(bool on)
        {
            if (!roBuilt) return;
            roTrack.enabled = on; roFill.enabled = on; roTab.enabled = on;
            roName.GetComponent<MeshRenderer>().enabled = on;
        }
    }
}
