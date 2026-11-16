using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow, Growth (pet-camp-collection-growth-capture-tabs.png, panel 2; docs/pet-system-design.md 5): the pet on a nest between two arrows
    /// (the owned pets in turn), the Level station (Pet Food, never above the best hero, Reset level) and the Stars station (shards of the kind +
    /// gold, Reset stars), and the shared shards from the gacha turned into this kind (Convert 10 / all). Costs and limits are PetRules / PetGrowth;
    /// the buttons send the old window's InvOps.
    /// </summary>
    public sealed partial class PetCampWindow
    {
        const float StageX = 330f, StationY = 80f, StationW = 302f, StationH = 344f, LevelX = 500f, StarsX = 816f;

        Image growIcon;
        Text growName, growStatus, growLevel, growDamage, growLevelNote, growStarCount, growPassive, sharedText;
        PetCampBadge growBadge;
        PaperStars growStars, growStarsBig;
        PaperBar shardBar;
        PaperButton levelUp, resetLevel, starUp, resetStars, convertTen, convertAll;
        GameObject growStatusPlate;

        void BuildGrowth(RectTransform page)
        {
            PaperArt.Draw(UiKit.Rect(page, "Nest", StageX - 104f, 276f, 208f, 89f), PaperPiece.PetNestLarge, PaperTheme.Wood, PaperTheme.WoodDark, 20f);
            growIcon = UiKit.Icon(page, "Pet", StageX - 78f, 172f, 156f);
            growName = Sign(page, "NameSign", StageX - 100f, 62f, 200f, 42f, "");
            growBadge = new PetCampBadge(page, "Rarity", StageX - 62f, 110f, 26f);
            growStars = new PaperStars(page, "Stars", StageX - 18f, 115f, PetRules.MaxStars, 16f);
            PaperKit.PageArrow(page, "Prev", StageX - 150f, 220f, 40f, true, () => StepOwned(-1));
            PaperKit.PageArrow(page, "Next", StageX + 110f, 220f, 40f, false, () => StepOwned(1));
            RectTransform status = UiKit.Rect(page, "Status", StageX - 130f, 378f, 260f, 30f);
            PaperArt.Draw(status, PaperPiece.BtnGrey, PaperTheme.Disabled, PaperTheme.PlateEdge, 12f);
            growStatus = PaperTheme.Caption(status, "", PaperTheme.BodySize, PaperTheme.TextLight);
            growStatusPlate = status.gameObject;

            RectTransform level = Station(page, "Level", LevelX, PaperPiece.PetFoodTrough);
            growLevel = PaperTheme.Title(level, "Level", 0f, 118f, StationW, 24f, "", 18, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            growDamage = PaperTheme.Body(level, "Damage", 0f, 144f, StationW, 20f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            growLevelNote = PaperTheme.Body(level, "Note", 24f, 168f, StationW - 48f, 54f, "", PaperTheme.BarSize + 1, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.UpperCenter, true);
            levelUp = TwoLineButton(level, "LevelUp", () => SendGrowth(InvOp.PetLevelUp));
            resetLevel = new PaperButton(level, "ResetLevel", 76f, 290f, 150f, 30f, "Reset level", PaperTone.Blue, () => SendGrowth(InvOp.PetResetLevel));

            RectTransform star = Station(page, "Stars", StarsX, PaperPiece.PetCrystalAltar);
            growStarsBig = new PaperStars(star, "StarRow", 76f, 120f, PetRules.MaxStars, 20f);
            growStarCount = PaperTheme.Body(star, "Count", 180f, 120f, 70f, 20f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            growPassive = PaperTheme.Body(star, "Passive", 20f, 146f, StationW - 40f, 40f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            PaperTheme.Body(star, "ShardLabel", 28f, 190f, 60f, 26f, "Shards", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            shardBar = new PaperBar(star, "ShardBar", 84f, 190f, 192f, 26f, PaperTheme.BarMp);
            starUp = TwoLineButton(star, "StarUp", () => SendGrowth(InvOp.PetStarUp));
            resetStars = new PaperButton(star, "ResetStars", 76f, 290f, 150f, 30f, "Reset stars", PaperTone.Blue, () => SendGrowth(InvOp.PetResetStars));

            RectTransform shared = PaperKit.Frame(page, "Shared", LevelX, 436f, StarsX + StationW - LevelX, 62f, PaperFrame.Wood, false);
            Image shard = UiKit.Icon(shared, "Icon", 22f, 14f, 34f);
            shard.sprite = PaperArt.Get(PaperPiece.IconShard);
            shard.enabled = shard.sprite != null;
            sharedText = PaperTheme.Title(shared, "Text", 62f, 0f, 250f, 62f, "", PaperTheme.SectionSize, PaperTheme.TextLight, PaperInk.Outlined);
            convertTen = new PaperButton(shared, "Convert10", 322f, 13f, 132f, 36f, "Convert 10", PaperTone.Gold, () => PetViewModel.Convert(PetConfig.IndexOf(Shown), 10));
            convertAll = new PaperButton(shared, "ConvertAll", 464f, 13f, 132f, 36f, "Convert all", PaperTone.Gold, () => PetViewModel.Convert(PetConfig.IndexOf(Shown), -1));
            RectTransform note = UiKit.Rect(page, "SharedNote", LevelX, 504f, StarsX + StationW - LevelX, 24f);
            PaperArt.Plate(note, PaperTheme.Cream, PaperTheme.PaperEdge, 8f);
            PaperTheme.Caption(note, "Shared shards: the gacha's " + PetViewModel.Pct(PetRules.GachaShardRate) + " result, " + PetRules.SharedShardsPerPull + " a pull. A duplicate pet gives " + PetRules.DuplicateShards + " shards of its kind.",
                PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain);
        }

        /// <summary>Wooden sign with a caption (the pet's name, a station's title).</summary>
        static Text Sign(Transform parent, string name, float x, float y, float w, float h, string text)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            PaperArt.Draw(rt, PaperPiece.PetStationSign, PaperTheme.Wood, PaperTheme.BtnGoldEdge, 10f);
            return PaperTheme.Caption(rt, text, PaperTheme.SectionSize + 1, PaperTheme.TextLight);
        }

        /// <summary>One gold station card with its sign on the top edge and its picture (food trough / crystal altar).</summary>
        static RectTransform Station(Transform page, string title, float x, string picture)
        {
            RectTransform rt = UiKit.Rect(page, title + "Station", x, StationY, StationW, StationH);
            PaperArt.Draw(rt, PaperPiece.PetGrowthCard, PaperTheme.TitleGold, PaperTheme.BtnGoldEdge, 10f, true);
            Sign(rt, "Sign", StationW * 0.5f - 76f, -18f, 152f, 38f, title);
            Image img = UiKit.Icon(rt, "Picture", StationW * 0.5f - 52f, 22f, 104f);
            img.sprite = PaperArt.Get(picture);
            img.enabled = img.sprite != null;
            return rt;
        }

        static PaperButton TwoLineButton(Transform parent, string name, System.Action onClick)
        {
            var b = new PaperButton(parent, name, 26f, 232f, 250f, 50f, "", PaperTone.Gold, onClick);
            b.Label.fontSize = PaperTheme.SectionSize;
            b.Label.lineSpacing = 0.8f;
            return b;
        }

        /// <summary>A cost on a gold button: dark red when the account cannot pay it (the press still asks, and the answer says what is missing).</summary>
        static string Cost(string text, bool enough) { return enough ? text : "<color=#A01810>" + text + "</color>"; }

        void SendGrowth(InvOp op) { PetActions.Send(op, (byte)PetConfig.IndexOf(Shown)); }

        void RefreshGrowth(PlayerState p, AccountState a)
        {
            PetDef d = Shown;
            OwnedPet o = a != null ? a.Pet(d.Id) : null;
            int level = o != null ? o.level : 1, stars = o != null ? o.stars : 1, food = PetRules.FoodForLevel(level);
            bool topLevel = level >= PetRules.MaxLevel, heroCap = level >= p.Level;
            growIcon.sprite = PetArt.Icon(d);
            growIcon.enabled = growIcon.sprite != null;
            growIcon.color = o != null ? PetArt.TintOf(d) : new Color(0.3f, 0.3f, 0.33f, 0.7f);
            growName.text = d.Name;
            growBadge.Set(d);
            growStars.Set(o != null ? stars : 0);
            string status = o == null ? "Not owned: nothing to grow" : topLevel ? "Top level" : heroCap ? "Hero is level " + p.Level : null;
            growStatusPlate.SetActive(status != null);
            growStatus.text = status ?? "";

            growLevel.text = "Lv. " + level + " / " + PetRules.MaxLevel;
            growDamage.text = "Damage x" + PetRules.LevelDamageMul(level).ToString("0.000", CultureInfo.InvariantCulture);
            growLevelNote.text = "+0.5% a level; never above your best hero (" + p.Level + ")." + (o != null && o.foodSpent > 0 ? "\nFood spent: " + o.foodSpent : "");
            levelUp.Label.text = o == null ? "Level Up" : topLevel ? "Top level" : heroCap ? "Hero is level " + p.Level : "Level Up\n<size=12>" + Cost("- " + food + " food (" + a.food + ")", a.food >= food) + "</size>";
            levelUp.SetEnabled(o != null && !topLevel && !heroCap);
            resetLevel.SetEnabled(o != null && level > 1);

            int needShards = 0, needGold = 0;
            bool canStar = a != null && PetGrowth.NextStarCost(a, d.Id, out needShards, out needGold);
            growStarsBig.Set(o != null ? stars : 0);
            growStarCount.text = "(" + stars + " / " + PetRules.MaxStars + ")";
            growPassive.text = "Passive: " + PetConfig.PassiveText(d, stars) + (canStar ? "\nNext: " + PetConfig.PassiveText(d, stars + 1) : "");
            int shards = o != null ? o.shards : 0;
            shardBar.Set(canStar ? (float)shards / needShards : 1f);
            shardBar.SetText(canStar ? shards + " / " + needShards : shards.ToString());
            starUp.Label.text = o == null ? "Star Up" : canStar ? "Star Up\n<size=12>" + Cost("- " + needShards + " shards", shards >= needShards) + " " + Cost("+ " + needGold.ToString("N0", CultureInfo.InvariantCulture) + " G", p.Gold >= needGold) + "</size>" : "Max stars";
            starUp.SetEnabled(canStar);
            resetStars.SetEnabled(o != null && stars > 1);

            int sharedShards = a != null ? a.sharedShards : 0;
            sharedText.text = "Shared shards   " + sharedShards;
            convertTen.SetEnabled(o != null && sharedShards > 0);
            convertAll.SetEnabled(o != null && sharedShards > 0);
        }
    }
}
