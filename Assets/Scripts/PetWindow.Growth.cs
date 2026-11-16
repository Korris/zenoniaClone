using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetWindow, Collection and Growth tabs. Collection (design 7): the selected species, its rarity, passive and skill, where it comes from (gacha, a ball on its monster,
    /// UR shards), and the account's collection level with the rarity gate it opens. Growth (design 3): level with Pet Food (never above your best hero), stars with shards
    /// of the kind + gold, shared shards from the gacha turned into this kind, and the two resets that give everything back.
    /// </summary>
    public sealed partial class PetWindow
    {
        Image bigIcon;
        Text colNameText, colInfoText, colSourceText, colLevelText;
        Text growHeadText, growLevelText, growStarText, growShardText, levelLabel, starLabel, convertLabel;

        void BuildCollection()
        {
            bigIcon = UiKit.Icon(collectionTab, "Icon", 12f, 12f, 76f);
            colNameText = UiKit.Label(collectionTab, "Name", 100f, 8f, W - 170f, 26f, "", 18, Color.white, TextAnchor.MiddleLeft, true);
            colInfoText = UiKit.Label(collectionTab, "Info", 100f, 34f, W - 170f, 70f, "", 12, UiWinKit.Cream, TextAnchor.UpperLeft);
            colSourceText = UiKit.Label(collectionTab, "Source", 12f, 104f, W - 90f, 40f, "", 12, new Color(0.75f, 0.9f, 1f), TextAnchor.UpperLeft);
            colLevelText = UiKit.Label(collectionTab, "Level", 12f, 146f, W - 90f, BodyH - 150f, "", 12, UiWinKit.Dim, TextAnchor.UpperLeft);
        }

        void FillCollection(PetDef d, AccountState a, PlayerState p)
        {
            OwnedPet o = a != null ? a.Pet(d.Id) : null;
            bigIcon.sprite = PetArt.Icon(d);
            bigIcon.enabled = bigIcon.sprite != null;
            bigIcon.color = o != null ? PetArt.TintOf(d) : new Color(0.3f, 0.3f, 0.33f, 0.6f);
            colNameText.text = d.Name + "   " + PetArt.Label(d) + (o != null ? "   <size=13><color=#ffd27a>" + PetArt.Stars(o.stars) + "  Lv " + o.level + "</color></size>" : "   <size=13><color=#8a96a4>not owned</color></size>");
            int stars = o != null ? o.stars : 1;
            colInfoText.text = "Passive: <color=#b3ffb3>" + PetConfig.PassiveText(d, stars) + "</color>  (5 stars: " + PetConfig.PassiveText(d, PetRules.MaxStars) + ")\nSkill: " + PetConfig.SkillText(d) + ", every " + PetConfig.SkillCooldownMs / 1000 + " s\n"
                + (d.Melee ? "Melee" : "Ranged") + (d.Flies ? ", flies" : "") + (d.MonsterType != null ? "   Monster: " + d.MonsterType : "");
            colSourceText.text = o != null ? "Owned by your account: every hero can take it along." : HowToGet(d, a);
            int species = a != null ? a.Species : 0, level = a != null ? a.CollectionLevel : 1;
            string next = level <= PetRules.CollectionSpecies.Length ? "   next at " + PetRules.CollectionSpecies[level - 1] + " species" : "   (max)";
            colLevelText.text = "<b>Collection level " + level + "</b>   " + species + " / " + PetConfig.All.Length + " species" + next
                + "\nBalls may catch: level 1 N and R, 2 SR, 3 SSR, 4 UR, 5 the title Beast Tamer.\nEvery species counts once; stars and levels do not.";
        }

        static string HowToGet(PetDef d, AccountState a)
        {
            if (d.UnlockLevel > 0) return "Legacy pet: free when any of your heroes reaches level " + d.UnlockLevel + ".";
            if (d.Price > 0) return "Legacy pet: buy at a merchant for " + d.Price + " G.";
            string s = "From the Pet Gacha (" + ((PetGacha.PetRate(d) * 100).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) + "% a pull)";
            if (d.Id == PetRules.UrExchangePet) s += ", or for " + PetRules.UrExchangeCost + " UR shards (" + (a != null ? a.urShards : 0) + " now)";
            if (d.Rarity <= PetRarity.SSR && d.MonsterType != null) s += ", or a Pet Ball on a " + d.Name + " at half HP (" + Pct(PetRules.LiveCaptureChance(d.Rarity, 0.5)) + ", " + Pct(PetRules.LiveCaptureChance(d.Rarity, 0.25)) + " under a quarter)";
            if (d.Rarity >= PetRarity.SSR) s += ", or a ball on its fallen boss (" + Pct(PetRules.BossCaptureChance(d.Rarity, false)) + ")";
            return s + ". Needs collection level " + PetRules.CollectionLevelFor(d.Rarity) + " to catch.";
        }

        void BuildGrowth()
        {
            growHeadText = UiKit.Label(growthTab, "Head", 14f, 8f, W - 90f, 24f, "", 16, Color.white, TextAnchor.MiddleLeft, true);
            growLevelText = UiKit.Label(growthTab, "Level", 14f, 36f, W - 90f, 20f, "", 12, UiWinKit.Cream);
            Button lv = UiWinKit.ArtButton(growthTab, "LevelUp", "btn-gold", 14f, 58f, 200f, 32f, "Level up", 13, new Color(0.25f, 0.12f, 0.02f), () => Send(InvOp.PetLevelUp));
            levelLabel = lv.GetComponentInChildren<Text>();
            UiWinKit.ArtButton(growthTab, "ResetLevel", "btn-navy", 224f, 58f, 150f, 32f, "Reset level", 12, Color.white, () => Send(InvOp.PetResetLevel));
            growStarText = UiKit.Label(growthTab, "Stars", 14f, 98f, W - 90f, 20f, "", 12, UiWinKit.Cream);
            Button st = UiWinKit.ArtButton(growthTab, "StarUp", "btn-gold", 14f, 120f, 200f, 32f, "Star up", 13, new Color(0.25f, 0.12f, 0.02f), () => Send(InvOp.PetStarUp));
            starLabel = st.GetComponentInChildren<Text>();
            UiWinKit.ArtButton(growthTab, "ResetStars", "btn-navy", 224f, 120f, 150f, 32f, "Reset stars", 12, Color.white, () => Send(InvOp.PetResetStars));
            growShardText = UiKit.Label(growthTab, "Shards", 14f, 160f, W - 90f, 20f, "", 12, UiWinKit.Cream);
            Button cv = UiWinKit.ArtButton(growthTab, "Convert", "btn-navy", 14f, 182f, 200f, 30f, "Convert 10 shared", 12, Color.white, () => Convert(10));
            convertLabel = cv.GetComponentInChildren<Text>();
            UiWinKit.ArtButton(growthTab, "ConvertAll", "btn-navy", 224f, 182f, 150f, 30f, "Convert all", 12, Color.white, () => Convert(-1));
        }

        void Send(InvOp op) { PetActions.Send(op, (byte)selected); }

        void Convert(int n)
        {
            AccountState a = PetActions.Account;
            if (a == null) return;
            if (n < 0) n = a.sharedShards;
            PetActions.Send(InvOp.PetConvertShards, (byte)selected, Mathf.Min(n, a.sharedShards).ToString());
        }

        void FillGrowth(PetDef d, AccountState a, PlayerState p)
        {
            OwnedPet o = a != null ? a.Pet(d.Id) : null;
            growHeadText.text = d.Name + "   " + PetArt.Label(d) + (o == null ? "   <size=12><color=#8a96a4>not owned: nothing to grow</color></size>" : "");
            int level = o != null ? o.level : 1, stars = o != null ? o.stars : 1;
            int food = PetRules.FoodForLevel(level);
            bool topLevel = level >= PetRules.MaxLevel, heroCap = level >= p.Level;
            growLevelText.text = "Level " + level + " / " + PetRules.MaxLevel + "   damage x" + PetRules.LevelDamageMul(level).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)
                + "   (+0.5% a level; never above your best hero, " + p.Level + ")" + (o != null && o.foodSpent > 0 ? "   food spent " + o.foodSpent : "");
            levelLabel.text = o == null ? "Level up" : topLevel ? "Top level" : heroCap ? "Hero is level " + p.Level : "Level up: " + food + " food (" + (a != null ? a.food : 0) + ")";
            int needShards = 0, needGold = 0;
            bool canStar = a != null && PetGrowth.NextStarCost(a, d.Id, out needShards, out needGold);
            growStarText.text = PetArt.Stars(stars) + " / " + PetRules.MaxStars + "   passive " + PetConfig.PassiveText(d, stars) + (canStar ? "   (next: " + PetConfig.PassiveText(d, stars + 1) + ")" : "")
                + "   shards " + (o != null ? o.shards : 0) + (canStar ? " / " + needShards : "");
            starLabel.text = o == null ? "Star up" : canStar ? "Star up: " + needShards + " sh + " + needGold + " G" : "Max stars";
            int shared = a != null ? a.sharedShards : 0;
            growShardText.text = "Shared shards " + shared + "   (the gacha's " + Pct(PetRules.GachaShardRate) + " result: " + PetRules.SharedShardsPerPull + " a pull; a duplicate pet gives " + PetRules.DuplicateShards + " of its kind)";
            convertLabel.text = "Convert 10 shared";
        }
    }
}
