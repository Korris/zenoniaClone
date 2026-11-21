using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Big map: the painting under the markers and the legend bar. The painting is the minimap's source (Minimap.MinimapSource: the map's
    /// "-minimap" picture, else its single ground painting); a big source (a whole ground painting, 64 px per tile) is shrunk on the GPU by halving
    /// first, like Minimap.Bake, so it does not alias without mipmaps. A small one (the -minimap pictures, 6-12 px per tile) is shown as it is.
    /// </summary>
    public sealed partial class WorldMapWindow
    {
        const int MaxGroundPx = 1100; // ~1.5x the widest map rect at 1080p: sharp enough, small in memory
        Texture2D bakedGround;

        void SetGround(IGameMap map)
        {
            ReleaseGround();
            Texture2D src = Minimap.MinimapSource(map);
            ground.color = src != null ? Color.white : new Color(0.1f, 0.16f, 0.24f);
            if (src == null) { ground.texture = null; return; }
            if (src.width <= MaxGroundPx) { ground.texture = src; return; }
            bakedGround = Shrink(src);
            ground.texture = bakedGround;
        }

        void ReleaseGround()
        {
            if (bakedGround != null) Destroy(bakedGround);
            bakedGround = null;
        }

        /// <summary>Halves `src` on the GPU until it is at most MaxGroundPx wide, then copies it into a Texture2D (a RenderTexture can lose its contents).</summary>
        static Texture2D Shrink(Texture2D src)
        {
            RenderTexture previous = RenderTexture.active, step = null;
            int w = src.width, h = src.height;
            while (w > MaxGroundPx)
            {
                w /= 2; h /= 2;
                RenderTexture next = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                next.filterMode = FilterMode.Bilinear;
                if (step == null) Graphics.Blit(src, next);
                else { Graphics.Blit(step, next); RenderTexture.ReleaseTemporary(step); }
                step = next;
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            RenderTexture.active = step;
            tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
            tex.Apply(false, true);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(step);
            return tex;
        }

        // ---- legend: [icon gap word] items in one centred row on the ivory bar ----
        const float LegendIcon = 20f, LegendIconGap = 5f, LegendItemGap = 18f;
        const int LegendFont = 13;
        static readonly Color LegendCampTint = RoSoftTheme.Hex("#A98BD0");
        RectTransform legend;

        void BuildLegend()
        {
            legend = UiKit.Rect(window, "Legend", PadX, 0f, 400f, LegendH);
            legend.anchorMin = legend.anchorMax = legend.pivot = new Vector2(0.5f, 0f);
            legend.anchoredPosition = new Vector2(0f, PadBottom);
            RoHud.Art(legend, "wm-legend").pixelsPerUnitMultiplier = 100f / (2f * LegendH);
            var row = legend.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(20, 20, 4, 4);
            row.spacing = LegendItemGap;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            LegendItem("wm-arrow", "You", Color.white);
            LegendItem("wm-party", "Party", Color.white);
            LegendItem("wm-portal", "Portal", Color.white);
            LegendItem("wm-npc", "NPC", Color.white);
            LegendItem("wm-boss", "Boss", Color.white);
            LegendItem("wm-wanted-face", "Wanted", Color.white);
            LegendItem("wm-camp-ring", "Camp", LegendCampTint); // the white ring would vanish on the ivory bar
        }

        void LegendItem(string icon, string word, Color tint)
        {
            RectTransform item = UiKit.Rect(legend, word, 0f, 0f, 60f, LegendIcon);
            var h = item.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = LegendIconGap; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            RectTransform ic = UiKit.Rect(item, "Icon", 0f, 0f, LegendIcon, LegendIcon);
            RoHud.Art(ic, icon).color = tint;
            Fixed(ic, LegendIcon, LegendIcon);
            Text t = RoHud.Label(UiKit.Rect(item, "Text", 0f, 0f, 40f, LegendIcon), word, LegendFont, RoHud.Ink, TextAnchor.MiddleLeft);
            Fixed(t.rectTransform, -1f, LegendIcon);
        }

        void LayoutLegend(float windowW) { legend.sizeDelta = new Vector2(windowW - PadX * 2f, LegendH); }
    }
}
