using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: the top bar (title sign, what the hero carries: gold, upgrade stones, HP / MP potions of every size, the Skill and
    /// Close buttons) and the Stats popup. The game has no gems and nothing to buy here, so the mockup's gem chip and "+" buttons are left out.
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        PaperChip goldChip, stoneChip, hpChip, mpChip;
        RectTransform statsPopup;
        readonly Text[] statValues = new Text[BagText.StatusLabels.Length];

        void BuildTopBar()
        {
            RectTransform sign = PaperKit.TitleBoard(content, 2f, -3f, 286f, 88f, "ARMORY CAROUSEL", "Inventory");
            sign.Find("Title").GetComponent<Text>().fontSize = 19; // fifteen letters between the laurels
            goldChip = new PaperChip(content, "Gold", 392f, 10f, 132f, 28f, LootSprites.Coin(), "");
            stoneChip = new PaperChip(content, "Stones", 534f, 10f, 104f, 28f, LootSprites.Stone(), "");
            hpChip = new PaperChip(content, "HpPotions", 648f, 10f, 84f, 28f, LootSprites.Potion(true), "");
            mpChip = new PaperChip(content, "MpPotions", 742f, 10f, 84f, 28f, LootSprites.Potion(false), "");
            new PaperButton(content, "Skill", 872f, 6f, 128f, 40f, "Skill", PaperTone.Blue, () => { Close(); UiScreens.Open(UiScreen.Skill); });
            new PaperButton(content, "Close", 1010f, 6f, 120f, 40f, "Close", PaperTone.Red, Close);
            BuildStatsPopup();
        }

        /// <summary>The eight hero stats of the old window's Stats popup, on paper, over the left of the carousel.</summary>
        void BuildStatsPopup()
        {
            statsPopup = PaperKit.Frame(content, "StatsPopup", 14f, 84f, 252f, 238f, PaperFrame.Paper);
            RectTransform inner = PaperKit.Inner(statsPopup, PaperFrame.Paper);
            PaperKit.Heading(inner, "Title", -4f, -2f, 200f, "Stats");
            for (int i = 0; i < statValues.Length; i++)
            {
                float y = 22f + i * 21f;
                PaperTheme.Body(inner, "Label" + i, 4f, y, 120f, 20f, BagText.StatusLabels[i], PaperTheme.BodySize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleLeft, true);
                statValues[i] = PaperTheme.Body(inner, "Value" + i, 100f, y, 100f, 20f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleRight, true);
            }
            statsPopup.gameObject.SetActive(false);
        }

        void ToggleStats()
        {
            statsPopup.gameObject.SetActive(!statsPopup.gameObject.activeSelf);
            statsPopup.SetAsLastSibling();
        }

        void RefreshTopBar(PlayerState p)
        {
            int hp = 0, mp = 0;
            for (int s = 0; s < PotionCatalog.Sizes; s++) { hp += p.PotionCount(true, s); mp += p.PotionCount(false, s); }
            goldChip.SetText(p.Gold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
            stoneChip.SetText(p.UpgradeStone.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
            hpChip.SetText("x" + hp);
            mpChip.SetText("x" + mp);
            string[] values = BagText.StatusValues(p);
            for (int i = 0; i < values.Length; i++) statValues[i].text = values[i];
        }
    }
}
