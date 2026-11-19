using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Small RO Soft pieces shared by the Wanted notices, the bounty panel and the RO board (mockup docs/reference/ro-soft-hud/wanted-notices-mockup-chatgpt-1.png,
    /// pieces sheet F ro-hud-f-wanted): the boss's round portrait in the gold ring, the four bounty cells, a centred label in a ribbon / pill.
    /// </summary>
    public static class RoWantedWidgets
    {
        /// <summary>Round boss portrait: sky backing masked round, the face, the gold ring on top (crown on its top-left when `crown`). Returns the face.</summary>
        public static Image Portrait(Transform parent, string name, float x, float y, float size, bool crown)
        {
            RectTransform box = UiKit.Rect(parent, name, x, y, size, size);
            RectTransform m = UiKit.Fill(box, "Mask", size * 0.07f);
            Image mask = RoHud.Art(m, "hud-portrait-mask");
            mask.preserveAspect = false;
            mask.color = RoSoftTheme.SkyMist;
            m.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Image face = UiKit.Fill(m, "Face").gameObject.AddComponent<Image>();
            face.raycastTarget = false;
            face.preserveAspect = true;
            RoHud.Art(UiKit.Fill(box, "Ring"), "hud-portrait-ring");
            if (crown) RoHud.Art(UiKit.Rect(box, "Crown", -size * 0.04f, -size * 0.1f, size * 0.34f, size * 0.34f), "hud-icon-crown");
            return face;
        }

        /// <summary>A label filling `rt` minus padding px / py, centred both ways (ui-layout-rules: text never touches the border).</summary>
        public static Text Centred(RectTransform rt, string text, int size, Color color, float px, float py, bool outline = false)
        {
            RectTransform t = UiKit.Fill(rt, "Text");
            t.offsetMin = new Vector2(px, py); t.offsetMax = new Vector2(-px, -py);
            Text l = RoHud.Label(t, text, size, color, TextAnchor.MiddleCenter, true, outline);
            l.horizontalOverflow = HorizontalWrapMode.Wrap;
            l.resizeTextForBestFit = true; l.resizeTextMinSize = Mathf.Max(9, size - 4); l.resizeTextMaxSize = size;
            return l;
        }

        /// <summary>A sliced RO piece at (x, y) with its art height mapped onto `h` (pills keep round ends).</summary>
        public static Image Plate(Transform parent, string name, float x, float y, float w, float h, string piece, float artH, bool raycast = false)
        {
            Image i = RoHud.Art(UiKit.Rect(parent, name, x, y, w, h), piece, raycast);
            if (i.type == Image.Type.Sliced) i.pixelsPerUnitMultiplier = artH / h;
            return i;
        }
    }

    /// <summary>The fixed daily bounty as four ivory cells (gold, upgrade stones, HP and MP potions), the amount at the bottom right of each.</summary>
    public sealed class RoWantedRewards
    {
        readonly Image[] icons = new Image[4];
        readonly Text[] amounts = new Text[4];
        WantedBossDef shown;

        public RoWantedRewards(Transform parent, float x, float y, float cell, float gap)
        {
            for (int i = 0; i < 4; i++)
            {
                RectTransform c = UiKit.Rect(parent, "Reward" + i, x + i * (cell + gap), y, cell, cell);
                RoHud.Art(c, "slot");
                icons[i] = UiKit.Fill(c, "Icon", cell * 0.14f).gameObject.AddComponent<Image>();
                icons[i].preserveAspect = true; icons[i].raycastTarget = false;
                RectTransform a = UiKit.Fill(c, "Amount");
                a.offsetMin = new Vector2(3f, 2f); a.offsetMax = new Vector2(-4f, -cell * 0.55f);
                amounts[i] = RoHud.Label(a, "", Mathf.RoundToInt(Mathf.Clamp(cell * 0.26f, 10f, 13f)), Color.white, TextAnchor.LowerRight, true, true);
            }
        }

        public void Set(WantedBossDef d)
        {
            if (d == null || d == shown) return;
            shown = d;
            icons[0].sprite = LootSprites.Coin(); amounts[0].text = d.Gold >= 10000 ? (d.Gold / 1000) + "k" : d.Gold.ToString();
            icons[1].sprite = LootSprites.Stone(); amounts[1].text = "x" + d.Stones;
            icons[2].sprite = LootSprites.Potion(true, d.PotionSize); amounts[2].text = "x" + d.HpPotions;
            icons[3].sprite = LootSprites.Potion(false, d.PotionSize); amounts[3].text = "x" + d.MpPotions;
        }
    }
}
