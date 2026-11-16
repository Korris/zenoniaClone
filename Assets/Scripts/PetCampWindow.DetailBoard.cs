using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow, Team: the pinned paper on the right with the selected pet (picture, rarity, stars, level, melee / ranged, passive at its stars,
    /// skill), the Growth button, the Place / Move button that starts placing it in a nest (Cancel while placing) and the two quick cards: Level Up
    /// (Pet Food) and Star Up (shards of the kind + gold), each sending the same action as the Growth tab. With nothing selected (an account without
    /// pets) it says so and offers the Gacha and the Capture tab.
    /// </summary>
    public sealed partial class PetCampWindow
    {
        const float BoardX = 793f, BoardY = 56f, BoardW = 337f, BoardH = 460f, BoardInner = 297f; // inner = inside the Pinned frame's 20-unit rim

        GameObject boardPet, boardEmpty;
        Image boardIcon;
        Text boardName, boardLevel, boardKind, boardPassive, boardSkillName, boardSkill, levelCost, levelHave, starShards, starGold;
        PetCampBadge boardBadge;
        PaperStars boardStars;
        PaperButton growthButton, placeButton;

        void BuildBoard(RectTransform page)
        {
            RectTransform frame = PaperKit.Frame(page, "DetailBoard", BoardX, BoardY, BoardW, BoardH, PaperFrame.Pinned);
            RectTransform inner = PaperKit.Inner(frame, PaperFrame.Pinned);
            RectTransform pet = UiKit.Fill(inner, "Pet");
            boardPet = pet.gameObject;
            boardIcon = UiKit.Icon(pet, "Icon", 0f, 0f, 108f);
            boardName = PaperTheme.Title(pet, "Name", 112f, 2f, BoardInner - 112f, 24f, "", 19, PaperTheme.Ink);
            boardName.horizontalOverflow = HorizontalWrapMode.Overflow;
            boardBadge = new PetCampBadge(pet, "Rarity", 112f, 30f, 26f);
            boardStars = new PaperStars(pet, "Stars", 156f, 36f, PetRules.MaxStars, 14f);
            boardLevel = PaperTheme.Body(pet, "Level", 112f, 60f, BoardInner - 112f, 18f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            boardKind = PaperTheme.Body(pet, "Kind", 112f, 80f, BoardInner - 112f, 18f, "", PaperTheme.BodySize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            Tag(pet, "PassiveTag", 0f, 116f, PaperPiece.PetTagPassive, PaperTheme.Purple, "Passive");
            boardPassive = PaperTheme.Body(pet, "Passive", 70f, 112f, BoardInner - 70f, 30f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            Tag(pet, "SkillTag", 0f, 152f, PaperPiece.PetTagSkill, PaperTheme.BtnBlue, "Skill");
            boardSkillName = PaperTheme.Title(pet, "SkillName", 70f, 152f, BoardInner - 70f, 22f, "", PaperTheme.SectionSize, PaperTheme.Ink);
            boardSkill = PaperTheme.Body(pet, "Skill", 0f, 178f, BoardInner, 50f, "", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft);
            growthButton = new PaperButton(pet, "Growth", 0f, 236f, 204f, 40f, "Growth", PaperTone.Blue, () => SetTab(Tab.Growth));
            placeButton = new PaperButton(pet, "Place", 210f, 236f, BoardInner - 210f, 40f, "Place", PaperTone.Gold, TogglePlacing);
            levelCost = QuickCard(pet, "LevelUp", 0f, "Level Up", PaperPiece.IconPetFood, InvOp.PetLevelUp, out levelHave);
            starShards = QuickCard(pet, "StarUp", BoardInner * 0.5f + 3f, "Star Up", PaperPiece.IconShard, InvOp.PetStarUp, out starGold);

            RectTransform none = UiKit.Fill(inner, "NoPet");
            boardEmpty = none.gameObject;
            PaperTheme.Title(none, "Text", 20f, 110f, BoardInner - 40f, 80f, "No pet selected -\npick one from\nOwned Pets below.", PaperTheme.SectionSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            PaperTheme.Title(none, "More", 0f, 250f, BoardInner, 22f, "Get more pets", PaperTheme.SectionSize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleCenter);
            new PaperButton(none, "Gacha", 8f, 280f, 136f, 40f, "Gacha", PaperTone.Purple, () => { Close(); UiScreens.Open(UiScreen.Gacha); });
            new PaperButton(none, "Capture", 153f, 280f, 136f, 40f, "Capture", PaperTone.Blue, () => SetTab(Tab.Capture));
        }

        /// <summary>Small coloured tag with a word ("Passive", "Skill").</summary>
        static void Tag(Transform parent, string name, float x, float y, string piece, Color fill, string text)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, 64f, 22f);
            PaperArt.Draw(rt, piece, fill, PaperTheme.Ink, 9f);
            PaperTheme.Caption(rt, text, PaperTheme.BarSize + 1, PaperTheme.TextLight);
        }

        /// <summary>One gold card under the buttons: a title, the material's icon and its amount, and a second line (gold of a star). A tap sends `op`.</summary>
        Text QuickCard(Transform parent, string name, float x, string title, string iconPiece, InvOp op, out Text secondLine)
        {
            float w = BoardInner * 0.5f - 3f;
            RectTransform rt = UiKit.Rect(parent, name, x, 284f, w, 130f);
            Image face = PaperArt.Draw(rt, PaperPiece.PetGrowthCard, PaperTheme.TitleGold, PaperTheme.BtnGoldEdge, 8f);
            UiWinKit.MakeButton(face, () => { if (selected >= 0) PetActions.Send(op, (byte)selected); });
            PaperTheme.Title(rt, "Title", 0f, 12f, w, 22f, title, PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            Image icon = UiKit.Icon(rt, "Icon", 18f, 42f, 30f);
            icon.sprite = PaperArt.Get(iconPiece);
            icon.enabled = icon.sprite != null;
            Text line = PaperTheme.Body(rt, "Line", 52f, 42f, w - 60f, 30f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            secondLine = PaperTheme.Body(rt, "Line2", 8f, 78f, w - 16f, 36f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            return line;
        }

        void TogglePlacing()
        {
            placing = !placing;
            menuSlot = -1;
            Refresh();
        }

        void RefreshBoard(PlayerState p, AccountState a)
        {
            bool any = selected >= 0;
            boardPet.SetActive(any);
            boardEmpty.SetActive(!any);
            if (!any) return;
            PetDef d = PetConfig.All[selected];
            OwnedPet o = a != null ? a.Pet(d.Id) : null;
            int stars = o != null ? o.stars : 1, level = o != null ? o.level : 1;
            boardIcon.sprite = PetArt.Icon(d);
            boardIcon.enabled = boardIcon.sprite != null;
            boardIcon.color = PetArt.TintOf(d);
            boardName.text = d.Name;
            boardBadge.Set(d);
            boardStars.Set(o != null ? stars : 0);
            boardLevel.text = o != null ? "Lv." + level + " / " + PetRules.MaxLevel : "Not owned";
            boardLevel.color = o != null ? PaperTheme.Ink : PaperTheme.Red;
            boardKind.text = (d.Melee ? "Melee" : "Ranged") + (d.Flies ? ", flies" : "");
            boardPassive.text = PetConfig.PassiveText(d, stars);
            string skill = PetConfig.SkillText(d); // "Name: what it does"
            boardSkillName.text = d.SkillName;
            boardSkill.text = Sentence(skill.Substring(skill.IndexOf(':') + 1).Trim()) + " (every " + PetConfig.SkillCooldownMs / 1000 + " s).";
            int inSlot = System.Array.IndexOf(p.Team, d.Id);
            placeButton.Label.text = placing ? "Cancel" : inSlot >= 0 ? "Move" : "Place";
            placeButton.SetEnabled(o != null);
            growthButton.SetEnabled(o != null);

            bool topLevel = level >= PetRules.MaxLevel, heroCap = level >= p.Level;
            int food = PetRules.FoodForLevel(level);
            levelCost.text = o == null ? "-" : topLevel ? "Top level" : heroCap ? "Hero Lv." + p.Level : "x " + food;
            levelCost.color = o != null && !topLevel && !heroCap && a.food < food ? PaperTheme.Red : PaperTheme.Ink;
            levelHave.text = o != null && !topLevel && !heroCap ? "Pet Food: " + a.food : "";
            int needShards = 0, needGold = 0;
            bool canStar = a != null && PetGrowth.NextStarCost(a, d.Id, out needShards, out needGold);
            starShards.text = o == null ? "-" : canStar ? o.shards + " / " + needShards : "Max stars";
            starShards.color = canStar && o.shards < needShards ? PaperTheme.Red : PaperTheme.Ink;
            starGold.text = canStar ? needGold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " G" : "";
            starGold.color = canStar && p.Gold < needGold ? PaperTheme.Red : PaperTheme.Ink;
        }

        static string Sentence(string s) { return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s; }
    }
}
