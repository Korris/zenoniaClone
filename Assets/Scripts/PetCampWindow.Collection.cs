using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow, Collection (pet-camp-collection-growth-capture-tabs.png, panel 1; docs/pet-system-design.md 7): the book header with the
    /// account's collection level, every species by rarity (N, R, SR, SSR, UR and the two Legacy pets; not owned = the "?" card) and, on the right,
    /// the selected species: rarity, stars and level or "Not owned", passive now and at five stars, skill, its monster and how to get it.
    /// </summary>
    public sealed partial class PetCampWindow
    {
        const float BookX = 176f, BookW = 952f, GridRowH = 100f, DetailX = 456f, DetailW = 448f;
        static readonly PetRarity[] RarityRows = { PetRarity.N, PetRarity.R, PetRarity.SR, PetRarity.SSR, PetRarity.UR };

        readonly PetCampCard[] speciesCards = new PetCampCard[PetConfig.All.Length];
        Text bookTitle, bookLevel, colName, colOwned, colKind, colPassive, colSkillName, colSkill, colSource;
        Image colIcon;
        PetCampBadge colBadge;
        PaperStars colStars;

        void BuildCollection(RectTransform page)
        {
            RectTransform header = UiKit.Rect(page, "Header", BookX, 56f, BookW, 46f);
            PaperArt.Draw(header, PaperPiece.PetCollectionHeader, PaperTheme.Wood, PaperTheme.BtnGoldEdge, 10f);
            bookTitle = PaperTheme.Caption(header, "", PaperTheme.SectionSize + 1, PaperTheme.TextLight);
            RectTransform inner = PaperKit.Inner(PaperKit.Frame(page, "Book", BookX, 104f, BookW, 530f, PaperFrame.Paper), PaperFrame.Paper);
            PaperTheme.Body(inner, "Gate", 0f, -2f, 440f, 20f, "Balls may catch:  <color=#4E5A66>Lv1 N-R</color>,  <color=#1C61B4>Lv2 SR</color>,  <color=#C05A00>Lv3 SSR</color>,  <color=#7A2FC0>Lv4 UR</color>,  Lv5 title <color=#B02A20>Beast Tamer</color>",
                PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            int[] inRow = new int[RarityRows.Length + 1];
            for (int row = 0; row <= RarityRows.Length; row++) // five rarities, then Legacy; three rows a column
                Row(inner, row).Set(FirstOfRow(row));
            for (int i = 0; i < speciesCards.Length; i++)
            {
                int index = i, r = RowOf(PetConfig.All[i]);
                speciesCards[i] = PetCampCard.Create(inner, "Species" + i, RowX(r) + 48f + inRow[r]++ * 86f, RowY(r), 1f, () => { selected = index; Refresh(); });
            }
            RectTransform note = PaperKit.Frame(inner, "LevelNote", 0f, 334f, 440f, 144f, PaperFrame.Card, false);
            bookLevel = PaperTheme.Body(PaperKit.Inner(note, PaperFrame.Card), "Text", 2f, 0f, 408f, 114f, "", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft);

            colIcon = UiKit.Icon(inner, "Icon", DetailX, 0f, 120f);
            colName = PaperTheme.Title(inner, "Name", DetailX + 130f, 2f, DetailW - 130f, 28f, "", PaperTheme.TitleSize, PaperTheme.Ink);
            colBadge = new PetCampBadge(inner, "Rarity", DetailX + 130f, 36f, 26f);
            colStars = new PaperStars(inner, "Stars", DetailX + 176f, 42f, PetRules.MaxStars, 14f);
            colOwned = PaperTheme.Body(inner, "Owned", DetailX + 130f, 66f, DetailW - 130f, 20f, "", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            colKind = PaperTheme.Body(inner, "Kind", DetailX + 130f, 88f, DetailW - 130f, 20f, "", PaperTheme.BodySize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            Tag(inner, "PassiveTag", DetailX, 132f, PaperPiece.PetTagPassive, PaperTheme.Purple, "Passive");
            colPassive = PaperTheme.Body(inner, "Passive", DetailX + 72f, 126f, DetailW - 72f, 34f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            Tag(inner, "SkillTag", DetailX, 172f, PaperPiece.PetTagSkill, PaperTheme.BtnBlue, "Skill");
            colSkillName = PaperTheme.Title(inner, "SkillName", DetailX + 72f, 172f, DetailW - 72f, 22f, "", PaperTheme.SectionSize, PaperTheme.Ink);
            colSkill = PaperTheme.Body(inner, "Skill", DetailX, 198f, DetailW, 36f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft);
            PaperKit.Heading(inner, "HowLabel", DetailX - 4f, 246f, 200f, "How to get");
            colSource = PaperTheme.Body(inner, "Source", DetailX, 276f, DetailW, 200f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft);
        }

        static int RowOf(PetDef d) { return d.Legacy ? RarityRows.Length : (int)d.Rarity; }
        static float RowX(int row) { return row < 3 ? 0f : 222f; }
        static float RowY(int row) { return 24f + (row % 3) * GridRowH; }
        static PetDef FirstOfRow(int row) { foreach (PetDef d in PetConfig.All) if (RowOf(d) == row) return d; return PetConfig.All[0]; }
        static PetCampBadge Row(Transform parent, int row) { return new PetCampBadge(parent, "Row" + row, RowX(row), RowY(row) + 34f, 28f); }

        void RefreshCollection(PlayerState p, AccountState a)
        {
            int species = a != null ? a.Species : 0, level = a != null ? a.CollectionLevel : 1;
            string next = level <= PetRules.CollectionSpecies.Length ? "next at " + PetRules.CollectionSpecies[level - 1] : "max";
            bookTitle.text = "Collection Lv. " + level + "   -   " + species + " / " + PetConfig.All.Length + " species   -   " + next;
            bookLevel.text = "<b>Collection level " + level + "</b>: a ball may catch up to " + (level >= 4 ? "UR" : level == 3 ? "SSR" : level == 2 ? "SR" : "R") + " pets.\n"
                + "Every species counts once; stars and levels do not.\nLevels at " + string.Join(", ", System.Array.ConvertAll(PetRules.CollectionSpecies, n => n.ToString())) + " species.\n"
                + "A pet belongs to the account: every hero can take it along.";
            PetDef d = Shown;
            for (int i = 0; i < speciesCards.Length; i++)
            {
                PetDef s = PetConfig.All[i];
                speciesCards[i].Set(s, a != null ? a.Pet(s.Id) : null, s == d, System.Array.IndexOf(p.Team, s.Id));
            }
            OwnedPet o = a != null ? a.Pet(d.Id) : null;
            int stars = o != null ? o.stars : 1;
            colIcon.sprite = PetArt.Icon(d);
            colIcon.enabled = colIcon.sprite != null;
            colIcon.color = PetArt.TintOf(d);
            colName.text = d.Name;
            colBadge.Set(d);
            colStars.Set(o != null ? o.stars : 0);
            colOwned.text = o != null ? "Lv." + o.level + " / " + PetRules.MaxLevel : "Not owned";
            colOwned.color = o != null ? PaperTheme.Ink : PaperTheme.Red;
            colKind.text = (d.Melee ? "Melee" : "Ranged") + (d.Flies ? ", flies" : "") + (d.MonsterType != null ? "   -   Monster: " + d.MonsterType : "");
            colPassive.text = PetConfig.PassiveText(d, stars) + "\n<color=#7A5A45>At 5 stars: " + PetConfig.PassiveText(d, PetRules.MaxStars) + "</color>";
            string skill = PetConfig.SkillText(d);
            colSkillName.text = d.SkillName;
            colSkill.text = Sentence(skill.Substring(skill.IndexOf(':') + 1).Trim()) + ", every " + PetConfig.SkillCooldownMs / 1000 + " s.";
            colSource.text = o != null ? "Owned by your account: every hero can take it along." : "-  " + PetViewModel.HowToGet(d, a).Replace(", or ", "\n-  ").Replace(". Needs", ".\n\nNeeds");
        }
    }
}
