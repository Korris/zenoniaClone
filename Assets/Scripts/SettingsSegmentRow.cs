using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Segment tabs of a Settings row (22/11 perf passes): "Graphics" Low / Medium / High (GraphicsQuality) and "Resolution" Auto / 100 / 85 / 70 %
    /// (RenderScale). The chosen tab is filled (RO Soft: mint TabOn pill, white label; the others white TabOff pill, ink label; classic look: full /
    /// dimmed tab art). A tap applies at once and is kept per device by the setter.
    /// </summary>
    public sealed class SettingsSegmentRow
    {
        public const float TabH = 34f, Gap = 4f;
        readonly Button[] tabs;
        readonly Text[] labels;
        readonly Func<int> current;

        /// <summary>Total width of n tabs of width tabW.</summary>
        public static float WidthOf(int n, float tabW) { return tabW * n + Gap * (n - 1); }

        /// <summary>Tabs at (x, y) inside `row` (top-left based, like the window's other controls); get = index shown as chosen, set = tap.</summary>
        public SettingsSegmentRow(RectTransform row, float x, float y, string name, string[] captions, float tabW, int fontSize, Func<int> get, Action<int> set)
        {
            current = get;
            tabs = new Button[captions.Length];
            labels = new Text[captions.Length];
            for (int i = 0; i < captions.Length; i++)
            {
                int k = i;
                tabs[i] = UiWinKit.ArtButton(row, name + captions[i], "tab-inactive", x + i * (tabW + Gap), y, tabW, TabH, captions[i], fontSize, Color.white,
                    () => { set(k); Refresh(); });
                labels[i] = tabs[i].GetComponentInChildren<Text>();
            }
            Refresh();
        }

        /// <summary>Shows the current choice (the window calls it on open).</summary>
        public void Refresh()
        {
            int on = current();
            for (int i = 0; i < tabs.Length; i++)
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
