using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// TrainingHallWindow: the top bar (title sign "WARRIOR SKILLS", class + level, skill points free / total, gold, Respec with its real
    /// price, Inventory, Close), the tier planks on the left and the hero on the hall's rug. The game has no gems, so the mockup's gem
    /// chip and "+" buttons are left out. The skill data has three tiers by unlock level (SkillDef.Tier: Basic, Advanced, Ultimate), not
    /// the mockup's two boards of four: there is one plank per tier with its real count; a tap selects the first statue of that tier and
    /// the plank of the selected statue's tier is lit. All eight statues stay on the shelf.
    /// </summary>
    public sealed partial class TrainingHallWindow
    {
        static readonly string[] TierIcons = { PaperPiece.HallIconBasic, PaperPiece.HallIconAdvanced, PaperPiece.StarFull };
        static readonly int TierCount = System.Enum.GetValues(typeof(SkillTier)).Length;

        Text titleText, respecLabel;
        PaperChip classChip, spChip, goldChip;
        PaperButton respecButton;
        readonly PaperTab[] tierTabs = new PaperTab[TierCount];
        HeroPortraitView heroView;
        int shownTierCounts = -1;

        void BuildHero()
        {
            // The portrait's own 336 x 264 box at 0.75: the feet land on the middle of the rug painted in the hall (109, 430 in design units).
            RectTransform box = UiKit.Rect(content, "Hero", 109f - 126f, 430f - 155f, 252f, 198f);
            var host = new GameObject("TrainingHallHeroPortrait"); // in the world, not on the canvas: the portrait films a real HeroRenderer
            host.transform.SetParent(UiScreens.Root, false);
            heroView = host.AddComponent<HeroPortraitView>();
            heroView.Init(box.gameObject.AddComponent<RawImage>());
        }

        void BuildTopBar()
        {
            RectTransform sign = PaperKit.TitleBoard(content, 2f, -3f, 286f, 88f, "", "Training Hall");
            titleText = sign.Find("Title").GetComponent<Text>();
            titleText.fontSize = 19; // "MECHANIC SKILLS": fifteen letters between the laurels
            classChip = new PaperChip(content, "Class", 300f, 10f, 156f, 28f, PaperArt.Get(PaperPiece.HallIconBasic), "");
            spChip = new PaperChip(content, "SkillPoints", 468f, 10f, 122f, 28f, PaperArt.Get(PaperPiece.HallIconSp), "");
            goldChip = new PaperChip(content, "Gold", 602f, 10f, 132f, 28f, LootSprites.Coin(), "");
            respecButton = new PaperButton(content, "Respec", 806f, 6f, 134f, 40f, "", PaperTone.Purple, OnRespec);
            respecLabel = respecButton.Label;
            respecLabel.fontSize = PaperTheme.BarSize + 1;
            respecLabel.lineSpacing = 0.8f;
            respecLabel.rectTransform.offsetMin = new Vector2(30f, 3f); // the two arrows sit on the left end
            PaperSkillParts.Pic(respecButton.Root, "Icon", 12f, 9f, 22f, 22f, PaperPiece.HallIconRespec);
            new PaperButton(content, "Inventory", 948f, 6f, 130f, 40f, "Inventory", PaperTone.Blue, () => { Close(); UiScreens.Open(UiScreen.Bag); });
            PaperKit.CloseButton(content, 1088f, 4f, 44f, Close);
            for (int i = 0; i < TierCount; i++)
            {
                int tier = i;
                tierTabs[i] = new PaperTab(content, "Tier" + (SkillTier)i, 12f, 112f + i * 52f, 136f, 48f, "", PaperTabStyle.Plank, () => OnTier(tier));
                tierTabs[i].Label.rectTransform.offsetMin = new Vector2(34f, 0f); // the tier's emblem sits on the left end
                PaperSkillParts.Pic(tierTabs[i].Root, "Icon", 12f, 11f, 26f, 26f, TierIcons[i]);
            }
        }

        void RefreshTopBar(PlayerState p, HeroController hero, SkillDef d)
        {
            titleText.text = hero.Class.Name.ToUpperInvariant() + " SKILLS";
            classChip.SetText("Lv. " + p.Level + " " + hero.Class.Name);
            spChip.SetText(p.SkillPointsFree + " / " + (p.Level + SkillRanks.DevBonus)); // free / all the points the hero has earned
            goldChip.SetText(p.Gold.ToString("N0", CultureInfo.InvariantCulture));
            respecLabel.text = "Respec\n" + SkillRespec.Cost(p.Level).ToString("N0", CultureInfo.InvariantCulture) + " G";
            Enable(respecButton, p.Skills.Sum > 0, PaperTheme.TextLight); // nothing to reset: greyed, as the old window's button
            heroView.Show(hero.Class, p.GetEquipped(Slots.Weapon), p.GetEquipped(Slots.Offhand), p);

            var counts = new int[TierCount];
            foreach (SkillDef s in SkillTable.ForClass(p.ClassId)) counts[(int)s.Tier]++;
            int key = counts[0] * 100 + counts[1] * 10 + counts[2];
            if (key != shownTierCounts)
            {
                shownTierCounts = key;
                for (int i = 0; i < TierCount; i++) tierTabs[i].Label.text = (SkillTier)i + " (" + counts[i] + ")";
            }
            for (int i = 0; i < TierCount; i++) tierTabs[i].SetActive(d != null && (int)d.Tier == i);
        }

        /// <summary>A tier plank: the first statue of that tier (tree order) is selected.</summary>
        void OnTier(int tier)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            foreach (SkillDef s in SkillTable.ForClass(p.ClassId))
                if ((int)s.Tier == tier) { OnStatue(s.Index); return; }
        }
    }
}
