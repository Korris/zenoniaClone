using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// PetRoWindow, the right card's tabs. Info: a 2 x 3 grid of what the pet really does at its level and stars (passive, its hit, its skill, attack
    /// interval, skill cooldown, reach; green = what the next star / level adds) and its four skills (basic attack, skill, passive, Bond: locked while
    /// the Bond partner is not in the team). Skills: the same four in words. Bond: the four Bonds, this pet's highlighted, active ones marked.
    /// Pets have no ATK / DEF / Crit of their own, so the mockup's stat names are replaced by these real numbers.
    /// </summary>
    public sealed partial class PetRoWindow
    {
        const float TabY = 168f, BodyY = 212f;
        static readonly string[] DetailTabs = { "Info", "Skills", "Bond" };

        int detailTab;
        readonly Image[] tabSkins = new Image[3];
        readonly Text[] tabLabels = new Text[3];
        readonly RectTransform[] tabBodies = new RectTransform[3];
        readonly Text[] statLabels = new Text[6], statValues = new Text[6];
        readonly Image[] statIcons6 = new Image[6], skillIcons = new Image[4], skillPartner = new Image[1];
        readonly Text[] skillNames = new Text[4], skillValues = new Text[4], skillTexts = new Text[4], bondTexts = new Text[4];
        readonly Image[] bondRows = new Image[4];
        readonly string[] skillFull = new string[4];

        void BuildDetailTabs(RectTransform box)
        {
            float[] xs = { 10f, 140f, 270f }, ws = { 124f, 124f, 121f };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                tabSkins[i] = W.Img(box, "Tab" + DetailTabs[i], xs[i], TabY, ws[i], 38f, "bag-pill-white", true);
                tabLabels[i] = W.Txt(tabSkins[i].transform, "Label", 0f, 0f, ws[i], 36f, DetailTabs[i], 17, W.Ink, TextAnchor.MiddleCenter);
                W.Clickable(tabSkins[i], () => { detailTab = k; Refresh(); });
                tabBodies[i] = UiKit.Rect(box, DetailTabs[i] + "Body", 0f, BodyY, RightW, 250f);
            }
            BuildInfoBody(tabBodies[0]);
            for (int i = 0; i < 4; i++)
            {
                skillTexts[i] = W.Txt(tabBodies[1], "Text" + i, 62f, 4f + i * 62f, RightW - 74f, 58f, "", 13, W.Ink, TextAnchor.MiddleLeft, false);
                skillTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                Image ico = W.Img(tabBodies[1], "Icon" + i, 12f, 8f + i * 62f, 46f, 46f, "pet-skill-basic");
                ico.name = "SkillIcon" + i;
                bondRows[i] = W.Img(tabBodies[2], "Bond" + i, 10f, 4f + i * 60f, RightW - 20f, 54f, "bag-pill-white");
                bondRows[i].pixelsPerUnitMultiplier = 104f / 40f;
                bondTexts[i] = W.Txt(bondRows[i].transform, "Text", 16f, 0f, RightW - 52f, 54f, "", 13, W.Ink, TextAnchor.MiddleLeft, false);
                bondTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            }
        }

        void BuildInfoBody(RectTransform body)
        {
            for (int c = 0; c < 2; c++)
            {
                Image col = W.Img(body, "StatCol" + c, 10f + c * 191f, 0f, 186f, 92f, "bag-pill-white");
                col.pixelsPerUnitMultiplier = 104f / 44f;
                col.color = SoftBox;
                for (int r = 0; r < 3; r++)
                {
                    int i = c * 3 + r;
                    statIcons6[i] = W.Img(col.transform, "Icon" + r, 9f, 7f + r * 28f, 22f, 22f, "bag-stat-atk");
                    statLabels[i] = W.Txt(col.transform, "Label" + r, 35f, 5f + r * 28f, 70f, 26f, "", 16, W.Ink);
                    statValues[i] = W.Txt(col.transform, "Value" + r, 60f, 5f + r * 28f, 118f, 26f, "", 16, W.Ink, TextAnchor.MiddleRight);
                }
            }
            Image sk = W.Img(body, "Skills", 10f, 98f, RightW - 20f, 150f, "bag-pill-white");
            sk.pixelsPerUnitMultiplier = 104f / 44f;
            sk.color = SoftBox;
            W.Img(sk.transform, "Dot", 12f, 9f, 18f, 18f, "pet-el-ice");
            W.Txt(sk.transform, "Title", 36f, 2f, 200f, 30f, "Pet Skills", 18, W.Ink);
            float[] cx = { 51f, 144f, 237f, 330f };
            for (int i = 0; i < 4; i++)
            {
                int k = i;
                skillIcons[i] = W.Img(sk.transform, "Skill" + i, cx[i] - 34f, 32f, 68f, 68f, "pet-skill-basic", true);
                W.Clickable(skillIcons[i], () => Flash(skillFull[k]));
                skillNames[i] = W.Txt(sk.transform, "Name" + i, cx[i] - 46f, 103f, 92f, 20f, "", 14, W.Ink, TextAnchor.MiddleCenter);
                skillValues[i] = W.Txt(sk.transform, "Value" + i, cx[i] - 46f, 122f, 92f, 20f, "", 14, W.Ink, TextAnchor.MiddleCenter);
            }
            skillPartner[0] = UiKit.Rect(skillIcons[3].transform, "Partner", 12f, 12f, 44f, 44f).gameObject.AddComponent<Image>();
            skillPartner[0].preserveAspect = true; skillPartner[0].raycastTarget = false;
        }

        void RefreshDetailTabs(PlayerState p, AccountState a, PetDef d, OwnedPet o)
        {
            for (int i = 0; i < 3; i++)
            {
                bool on = i == detailTab;
                W.SetArt(tabSkins[i], on ? "bag-pill-mint" : "bag-pill-white");
                tabSkins[i].pixelsPerUnitMultiplier = 104f / 44f;
                tabLabels[i].color = on ? Color.white : W.Ink;
                tabBodies[i].gameObject.SetActive(on);
            }
            int level = o != null ? o.level : 1, stars = o != null ? o.stars : 1;
            double passive = PetConfig.PassiveAt(d, stars), nextStar = stars < PetRules.MaxStars ? PetConfig.PassiveAt(d, stars + 1) - passive : 0;
            double mul = PetRules.LevelDamageMul(level), mulNext = level < PetRules.MaxLevel ? PetRules.LevelDamageMul(level + 1) - mul : 0;
            bool attackSkill = PetConfig.IsAttack(d.Skill);
            double skill = attackSkill ? d.SkillPct * mul : d.SkillPct;
            string pk = PassiveShort(d.Passive);
            Stat(0, PassiveIcon(d.Passive), pk, (d.Passive == PetPassive.DamageTaken ? "-" : "+") + PetViewModel.Pct(passive), nextStar);
            Stat(1, "bag-stat-atk", "Pet hit", PetViewModel.Pct(PetConfig.BasicPct * mul), PetConfig.BasicPct * mulNext);
            Stat(2, attackSkill ? "bag-stat-crit" : "bag-stat-def", "Skill", PetViewModel.Pct(skill), attackSkill ? d.SkillPct * mulNext : 0);
            Stat(3, "bag-stat-speed", "Hit every", Sec(PetConfig.BasicAttackMs), 0);
            Stat(4, "pet-ico-attack-speed", "Skill CD", Sec(PetConfig.SkillCooldownMs), 0);
            Stat(5, "pet-ico-power", "Reach", Tiles(d.Melee ? PetConfig.MeleeRangeUnits : PetConfig.RangedRangeUnits), 0);

            PetBond bond = BondOf(d);
            bool bondOn = bond != null && PetTeam.Has(p.Team, bond.PetA) && PetTeam.Has(p.Team, bond.PetB);
            Skill(0, "pet-skill-basic", "Basic Attack", PetViewModel.Pct(PetConfig.BasicPct * mul),
                "Basic attack every " + Sec(PetConfig.BasicAttackMs) + ": " + PetViewModel.Pct(PetConfig.BasicPct * mul) + " of your hit after defence (+0.5% per pet level).");
            Skill(1, SkillPiece(d), d.SkillName, PetViewModel.Pct(skill), PetConfig.SkillText(d) + ". Every " + Sec(PetConfig.SkillCooldownMs) + "; from a Support slot at " + PetViewModel.Pct(PetRules.SupportSkillShare) + ".");
            Skill(2, "pet-skill-passive", "Passive", (d.Passive == PetPassive.DamageTaken ? "-" : "+") + PetViewModel.Pct(passive),
                PetConfig.PassiveText(d, stars) + " at " + stars + " stars (Combat slot 100%, Stat slot 25%; +10% of it per star).");
            PetDef partner = bond == null ? null : PetConfig.Get(bond.PetA == d.Id ? bond.PetB : bond.PetA);
            Skill(3, bondOn ? "bag-slot" : "pet-skill-locked", bond != null ? bond.Name : "No Bond", bond != null ? "+" + PetViewModel.Pct(bond.Pct) : "-",
                bond == null ? "Legacy pets have no Bond." : bond.Name + " with " + partner.Name + ": +" + PetViewModel.Pct(bond.Pct) + " " + BondKind(bond) + " while both are in the team" + (bondOn ? " (active)." : " (put " + partner.Name + " in the team to open it)."));
            skillNames[3].color = skillValues[3].color = bondOn ? W.Ink : W.InkSoft;
            skillPartner[0].enabled = bondOn && partner != null;
            if (partner != null) { skillPartner[0].sprite = ZenoniaProto.PetArt.Icon(partner); skillPartner[0].color = ZenoniaProto.PetArt.TintOf(partner); }

            for (int i = 0; i < 4; i++)
            {
                skillTexts[i].text = i == 1 || i == 3 && bond != null ? skillFull[i] : "<b>" + skillNames[i].text + "</b>  " + skillFull[i]; // skill and Bond texts start with their name
                Image ico = tabBodies[1].Find("SkillIcon" + i).GetComponent<Image>();
                W.SetArt(ico, i == 3 && bondOn ? "pet-skill-passive" : SkillPieceAt(i, d, bondOn));
                PetBond b = PetRules.Bonds[i];
                bool on = PetTeam.Has(p.Team, b.PetA) && PetTeam.Has(p.Team, b.PetB);
                bondRows[i].color = b == bond ? RoSoftTheme.Hex("#FFF2CC") : Color.white;
                bondTexts[i].text = "<b>" + b.Name + "</b>  " + PetConfig.Get(b.PetA).Name + " + " + PetConfig.Get(b.PetB).Name + "\n+" + PetViewModel.Pct(b.Pct) + " " + BondKind(b)
                    + (on ? "   <color=#2E9E57><b>ACTIVE</b></color>" : "   <color=#5F7488>inactive</color>");
            }
        }

        string SkillPieceAt(int i, PetDef d, bool bondOn) { return i == 0 ? "pet-skill-basic" : i == 1 ? SkillPiece(d) : i == 2 ? "pet-skill-passive" : "pet-skill-locked"; }

        void Stat(int i, string icon, string label, string value, double delta)
        {
            W.SetArt(statIcons6[i], icon);
            statLabels[i].text = label;
            statValues[i].text = value + (delta > 0.00001 ? " <color=#2E9E57>(+" + PetViewModel.Pct(delta) + ")</color>" : "");
            statValues[i].fontSize = 16;
            float room = 178f - 35f - statLabels[i].preferredWidth - 8f; // value right-aligned, never under its label
            while (statValues[i].fontSize > 10 && statValues[i].preferredWidth > room) statValues[i].fontSize--;
        }

        void Skill(int i, string icon, string name, string value, string full)
        {
            W.SetArt(skillIcons[i], icon);
            skillNames[i].text = name;
            skillNames[i].fontSize = 14;
            while (skillNames[i].fontSize > 9 && skillNames[i].preferredWidth > 92f) skillNames[i].fontSize--;
            skillValues[i].text = value;
            skillFull[i] = full;
        }

        static PetBond BondOf(PetDef d) { foreach (PetBond b in PetRules.Bonds) if (b.PetA == d.Id || b.PetB == d.Id) return b; return null; }

        static string BondKind(PetBond b) { return b.BossOnly ? "damage to bosses" : b.Kind == PetPassive.MaxHp ? "Max HP" : b.Kind == PetPassive.MoveSpeed ? "move speed" : b.Kind == PetPassive.DamageTaken ? "less damage taken" : "damage"; }

        static string SkillPiece(PetDef d)
        {
            switch (d.Skill)
            {
                case PetSkillKind.Ice: return d.Melee ? "pet-skill-claw" : "pet-skill-ice";
                case PetSkillKind.Fire: return "pet-skill-fire";
                case PetSkillKind.Poison: return "pet-skill-poison";
                case PetSkillKind.Lightning: return "pet-skill-lightning";
                case PetSkillKind.Shield: return "pet-skill-shield";
                case PetSkillKind.Heal: return "pet-skill-heal";
                default: return "pet-skill-strike";
            }
        }

        static string PassiveShort(PetPassive k)
        {
            switch (k) { case PetPassive.MaxHp: return "HP"; case PetPassive.Damage: return "DMG"; case PetPassive.MoveSpeed: return "Speed"; case PetPassive.DamageTaken: return "Taken"; case PetPassive.PotionHeal: return "Potion"; default: return "Atk Spd"; }
        }

        static string PassiveIcon(PetPassive k)
        {
            switch (k) { case PetPassive.MaxHp: return "bag-stat-hp"; case PetPassive.Damage: return "bag-stat-atk"; case PetPassive.MoveSpeed: return "bag-stat-speed"; case PetPassive.DamageTaken: return "bag-stat-def"; case PetPassive.PotionHeal: return "pet-ico-potion"; default: return "pet-ico-attack-speed"; }
        }

        static string Sec(int ms) { return (ms / 1000f).ToString("0.#", CultureInfo.InvariantCulture) + " s"; }
        static string Tiles(float units) { return (units / 16f).ToString("0.#", CultureInfo.InvariantCulture) + " tiles"; }
    }
}
