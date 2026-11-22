using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// "Graphics" row of the Settings window (22/11 perf pass): three segment tabs Low / Medium / High for GraphicsQuality, the chosen one filled
    /// (RO Soft: mint TabOn pill, white label; the others white TabOff pill, ink label; classic look: full / dimmed tab art). A tap applies at once
    /// (3D map shadows, casters, grass density, MSAA) and is kept per device.
    /// </summary>
    public sealed class SettingsGraphicsRow
    {
        public const float TabW = 68f, TabH = 34f, Gap = 4f; // "Medium" at 14 keeps ~8 units each side
        public const float Width = TabW * 3f + Gap * 2f;
        readonly Button[] tabs = new Button[3];
        readonly Text[] labels = new Text[3];

        /// <summary>Tabs at (x, y) inside `row` (top-left based, like the window's other controls).</summary>
        public SettingsGraphicsRow(RectTransform row, float x, float y)
        {
            for (int i = 0; i < 3; i++)
            {
                var l = (GraphicsQuality.Level)i;
                tabs[i] = UiWinKit.ArtButton(row, "Gfx" + GraphicsQuality.Name(l), "tab-inactive", x + i * (TabW + Gap), y, TabW, TabH, GraphicsQuality.Name(l), 14, Color.white,
                    () => { GraphicsQuality.Set(l); Refresh(); });
                labels[i] = tabs[i].GetComponentInChildren<Text>();
            }
            Refresh();
        }

        /// <summary>Shows the current level (the window calls it on open).</summary>
        public void Refresh()
        {
            int on = (int)GraphicsQuality.Current;
            for (int i = 0; i < 3; i++)
            {
                var face = (Image)tabs[i].targetGraphic;
                if (UiStyle.Ro)
                {
                    face.sprite = RoSoftSprites.Get(i == on ? RoSoftSprites.Shape.TabOn : RoSoftSprites.Shape.TabOff);
                    face.type = Image.Type.Sliced; face.pixelsPerUnitMultiplier = 1f; face.color = Color.white;
                    labels[i].color = i == on ? Color.white : RoSoftTheme.Ink;
                }
                else
                {
                    Sprite s = UiArt.Win(i == on ? "tab-active" : "tab-inactive");
                    if (s != null) { face.sprite = s; face.pixelsPerUnitMultiplier = s.rect.height / TabH; }
                    face.color = i == on ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                    labels[i].color = Color.white;
                }
            }
        }
    }
}
