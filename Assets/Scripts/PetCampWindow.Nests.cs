using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetCampWindow, Team: the five nests (PetCampNest; each opens at a hero level, PetRules.SlotUnlockLevel), the "Team Bonus (n/5)" plaque with what
    /// the whole team gives now (PlayerState.PetBonus, capped), the "?" paper with how the slots work and the caps, the hint while a pet is being
    /// placed and the Remove / Details pair of a tapped nest.
    /// </summary>
    public sealed partial class PetCampWindow
    {
        // Middle of each nest in the design frame: Combat in the centre, Support 1-2 beside it, Stat 1-2 in front.
        static readonly Vector2[] NestAt = { new Vector2(495f, 264f), new Vector2(318f, 238f), new Vector2(672f, 238f), new Vector2(289f, 368f), new Vector2(701f, 368f) };
        static readonly string[] RoleIcons = { "combat", "support", "support", "stat", "stat" };

        readonly PetCampNest[] nests = new PetCampNest[PetRules.TeamSize];
        Image bonusPlaque;
        Text bonusTitle, bonusText, hintText, rulesText;
        RectTransform rulesPopup, nestMenu;
        GameObject hint;
        int shownPlaque = -1;

        /// <summary>Share of each slot as the pill shows it: the passive share, or the skill share of the Support slots.</summary>
        static string ShareOf(int slot) { return PetViewModel.Pct(PetRules.SlotPassiveShare[slot] > 0 ? PetRules.SlotPassiveShare[slot] : PetRules.SupportSkillShare); }

        void BuildNests(RectTransform page)
        {
            for (int i = PetRules.TeamSize - 1; i >= 0; i--) // the Combat nest last: its pet may reach over its neighbours
            {
                int slot = i;
                nests[i] = new PetCampNest(page, "Nest" + i, NestAt[i].x, NestAt[i].y, i == 0, PetViewModel.SlotNames[i], ShareOf(i), PaperPiece.PetRoleIconPrefix + RoleIcons[i], () => OnNestTap(slot));
            }
            RectTransform plaque = UiKit.Rect(page, "BonusPlaque", 375f, 350f, 240f, 36f);
            bonusPlaque = PaperArt.Draw(plaque, null, Color.white, Color.clear);
            bonusTitle = PaperTheme.Caption(plaque, "", PaperTheme.SectionSize, PaperTheme.TextLight);
            UiWinKit.MakeButton(bonusPlaque, ToggleRules);
            RectTransform help = UiKit.Rect(page, "RulesButton", 617f, 357f, 22f, 22f);
            PaperTheme.Caption(help, "?", PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain);
            UiWinKit.MakeButton(PaperArt.Plate(help, PaperTheme.BtnGold, PaperTheme.BtnGoldEdge, 11f), ToggleRules);
            RectTransform strip = UiKit.Rect(page, "BonusStrip", 262f, 399f, 466f, 21f);
            PaperArt.Plate(strip, PaperTheme.Cream, PaperTheme.PaperEdge, 8f);
            bonusText = PaperTheme.Body(strip, "Text", 6f, 0f, 454f, 20f, "", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            bonusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void BuildNestOverlays(RectTransform page)
        {
            RectTransform tip = UiKit.Rect(page, "PlaceHint", 365f, 316f, 260f, 26f);
            PaperArt.Plate(tip, PaperTheme.Cream, PaperTheme.BtnGoldEdge, 8f);
            hintText = PaperTheme.Caption(tip, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain);
            hint = tip.gameObject;
            nestMenu = UiKit.Rect(page, "NestMenu", 0f, 0f, 164f, 28f);
            new PaperButton(nestMenu, "Remove", 0f, 0f, 80f, 28f, "Remove", PaperTone.Red, RemoveFromNest);
            new PaperButton(nestMenu, "Details", 84f, 0f, 80f, 28f, "Details", PaperTone.Blue, () => SetTab(Tab.Collection));
            rulesPopup = PaperKit.Frame(page, "Rules", 330f, 150f, 330f, 190f, PaperFrame.Paper);
            RectTransform inner = PaperKit.Inner(rulesPopup, PaperFrame.Paper);
            PaperKit.Heading(inner, "Title", -4f, -2f, 240f, "How the team works");
            rulesText = PaperTheme.Body(inner, "Text", 2f, 24f, 278f, 116f, PetViewModel.RulesText() + "\nBonds: +2% with both pets in the team.", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft);
            UiWinKit.MakeButton(rulesPopup.GetComponent<Image>(), ToggleRules);
            rulesPopup.gameObject.SetActive(false);
        }

        void ToggleRules()
        {
            rulesPopup.gameObject.SetActive(!rulesPopup.gameObject.activeSelf);
            rulesPopup.SetAsLastSibling();
        }

        void RefreshNests(PlayerState p, AccountState a)
        {
            PetDef carried = placing && selected >= 0 ? PetConfig.All[selected] : null;
            int inTeam = 0;
            for (int i = 0; i < PetRules.TeamSize; i++)
            {
                PetDef d = PetConfig.Get(p.Team[i]);
                if (d != null) inTeam++;
                bool open = p.Level >= PetRules.SlotUnlockLevel[i];
                bool lit = open && (carried != null ? d != carried : menuSlot == i || (d != null && selected >= 0 && d == PetConfig.All[selected]));
                nests[i].Set(PetArt.Icon(d), PetArt.TintOf(d), d != null ? "Lv." + (a != null ? a.LevelOf(d.Id) : 1) : "", d != null ? StarsOf(a, d) : 0, lit,
                    open ? null : "Lv " + PetRules.SlotUnlockLevel[i], carried != null, carried != null && open && d != carried ? GivesHere(carried, i, StarsOf(a, carried)) : null);
            }
            bool full = inTeam == PetRules.TeamSize;
            if (shownPlaque != (full ? 1 : 0))
            {
                shownPlaque = full ? 1 : 0;
                PaperArt.Redraw(bonusPlaque, full ? PaperPiece.PetBonusPlaque : PaperPiece.PetBonusPlaqueGrey, full ? PaperTheme.Wood : PaperTheme.Disabled, full ? PaperTheme.BtnGoldEdge : PaperTheme.PlateEdge, 12f);
            }
            bonusTitle.text = "Team Bonus (" + inTeam + "/" + PetRules.TeamSize + ")";
            List<string> parts = PetViewModel.BonusParts(p.PetBonus);
            bonusText.text = parts.Count > 0 ? string.Join("     ", parts.ToArray()) : "Nothing yet: put a pet in a nest.";

            hint.SetActive(carried != null);
            if (carried != null) hintText.text = "Tap a nest to put " + carried.Name + ".";
            bool showMenu = !placing && menuSlot >= 0 && PetConfig.Get(p.Team[menuSlot]) != null;
            nestMenu.gameObject.SetActive(showMenu);
            if (showMenu) nestMenu.anchoredPosition = new Vector2(NestAt[menuSlot].x - 82f, -(NestAt[menuSlot].y + (menuSlot == 0 ? 12f : 3f)));
        }

        /// <summary>What a pet gives in this slot: its passive at the slot's share, or its skill at the Support share.</summary>
        static string GivesHere(PetDef d, int slot, int stars)
        {
            double share = PetRules.SlotPassiveShare[slot];
            if (share <= 0) return d.SkillName + " at " + PetViewModel.Pct(PetRules.SupportSkillShare);
            return PetConfig.PassiveName(d.Passive, PetViewModel.Pct(PetConfig.PassiveAt(d, stars) * share).TrimEnd('%'));
        }
    }
}
