using System;
using UnityEngine;
using UnityEngine.UI;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// Small boxes shared by the My Cards and Monster Album pages. Same rules as GemUiWidgets: every picture is a card-kit piece, text centred both ways,
    /// the pill takes its shape from the 9-slice art.
    /// </summary>
    public static class CardPageWidgets
    {
        /// <summary>A pill that shows "Label: value" and steps to the next value on every tap (stand-in for the mockup dropdowns: the kit has no dropdown art).</summary>
        public sealed class CyclePill
        {
            public readonly RectTransform Root;
            readonly Text label;

            public CyclePill(Transform parent, string name, float x, float y, float w, Action onTap)
            {
                Root = UiKit.Rect(parent, name, x, y, w, U.ChipH);
                Image skin = Root.gameObject.AddComponent<Image>();
                GemUiArt.SetPiece(skin, GemUiArt.Frame("filter-pill"));
                UiWinKit.MakeButton(skin, onTap);
                label = U.Txt(Root, "Label", 12f, 0f, w - 24f, U.ChipH, "", U.TextSmall + 1, U.Ink, TextAnchor.MiddleCenter);
            }

            public void Set(string text) { label.text = text; }
        }

        /// <summary>Text-only tab (region tabs): region-tab art normal / selected, label centred.</summary>
        public sealed class TextTab
        {
            public readonly RectTransform Root;
            readonly Image skin;
            readonly Text label;

            public TextTab(Transform parent, string name, float x, float y, float w, string text, Action onTap)
            {
                Root = UiKit.Rect(parent, name, x, y, w, U.ChipH);
                skin = Root.gameObject.AddComponent<Image>();
                UiWinKit.MakeButton(skin, onTap);
                label = U.Txt(Root, "Label", 4f, 0f, w - 8f, U.ChipH, text, U.TextSmall - 1, U.Ink, TextAnchor.MiddleCenter);
                Set(false);
            }

            public void Set(bool on) { GemUiArt.SetPiece(skin, GemUiArt.Frame(on ? "region-tab-selected" : "region-tab")); label.color = on ? U.OnButton : U.Ink; }
        }

        /// <summary>Page arrows with "n / m" between them, centred on `cx`.</summary>
        public sealed class Pager
        {
            readonly Text text;
            readonly Action<int> onStep;

            public Pager(Transform parent, float cx, float y, Action<int> onStep)
            {
                this.onStep = onStep;
                text = U.Txt(parent, "Page", cx - 40f, y, 80f, 30f, "1 / 1", U.TextNormal, U.Ink, TextAnchor.MiddleCenter);
                Arrow(parent, cx - 96f, y, "btn-page-left", -1); Arrow(parent, cx + 56f, y, "btn-page-right", 1);
            }

            void Arrow(Transform parent, float x, float y, string piece, int dir)
            {
                Image a = GemUiArt.Img(parent, "Arrow" + dir, x, y, 40f, 30f, GemUiArt.Frame(piece), true);
                UiWinKit.MakeButton(a, () => onStep(dir));
            }

            public void Set(int page, int pages) { text.text = (page + 1) + " / " + pages; }
        }

        public static string ShortRegion(string id)
        {
            switch (id)
            {
                case "willowbrookMeadows": return "Willowbrook";
                case "bramblewoodForest": return "Bramblewood";
                case "rootburrow": return "Rootburrow";
                case "frostfangDen": return "Frostfang";
                case "emberfenMarsh": return "Emberfen";
                case "sunkenReliquary": return "Reliquary";
                case "sunbreakFoothills": return "Sunbreak";
                case "scorpionGorge": return "Scorpion";
                default: return GemUiModel.RegionName(id);
            }
        }
    }
}
