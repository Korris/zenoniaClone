using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetWindow, Team tab (docs/pet-system-design.md 2): the five slots in a row (Combat walks and fights; Support 1-2 cast their skill at half strength; Stat 1-2 give a
    /// quarter of their passive), each showing its pet or the hero level that opens it. Tap a slot to put the selected species there (tap again to take it out); the
    /// server checks ownership, level, one species per team (PetTeam.Refusal). Below: what the whole team gives the hero now (PetBonus, capped) and the active Bonds.
    /// </summary>
    public sealed partial class PetWindow
    {
        const float SlotW = 100f, SlotH = 92f;
        readonly Image[] slotFrames = new Image[PetRules.TeamSize], slotIcons = new Image[PetRules.TeamSize];
        readonly Text[] slotNames = new Text[PetRules.TeamSize], slotPets = new Text[PetRules.TeamSize];
        Text teamBonusText, teamHintText;

        void BuildTeam()
        {
            for (int i = 0; i < PetRules.TeamSize; i++)
            {
                int slot = i;
                RectTransform rt = UiKit.Rect(teamTab, "Slot" + i, 14f + i * (SlotW + 12f), 10f, SlotW, SlotH);
                slotFrames[i] = UiWinKit.Sliced(rt, "slot", SlotW / 160f, true);
                UiWinKit.MakeButton(slotFrames[i], () => OnSlot(slot));
                slotNames[i] = UiKit.Label(rt, "Name", -6f, 2f, SlotW + 12f, 16f, SlotNames[i] + " " + SlotShare[i], 11, UiWinKit.Dim, TextAnchor.MiddleCenter, true); // ChatGPT Lần 9: what each slot gives, under its name
                slotIcons[i] = UiKit.Icon(rt, "Icon", (SlotW - 44f) / 2f, 18f, 44f);
                slotPets[i] = UiKit.Label(rt, "Pet", -10f, SlotH - 26f, SlotW + 20f, 22f, "", 10, Color.white, TextAnchor.MiddleCenter);
            }
            teamHintText = UiKit.Label(teamTab, "Hint", 14f, SlotH + 16f, W - 90f, 20f, "", 13, new Color(0.75f, 0.9f, 1f));
            teamBonusText = UiKit.Label(teamTab, "Bonus", 14f, SlotH + 38f, W - 90f, BodyH - SlotH - 44f, "", 13, UiWinKit.Cream, TextAnchor.UpperLeft);
        }

        /// <summary>The selected species goes into this slot; if it is there already it leaves.</summary>
        void OnSlot(int slot)
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            PetDef d = PetConfig.All[selected];
            bool there = p.Team[slot] == d.Id;
            if (!there && p.Account != null)
            {
                string why = PetTeam.Refusal(p.Team, slot, d.Id, p.Level, p.Account);
                if (why != null) { RpgHud.Toast(why, new Color(1f, 0.7f, 0.5f)); return; } // the server would say the same; save the round trip
            }
            PetActions.Send(InvOp.PetEquip, PetRules.EncodeSlot(slot, there ? -1 : selected));
        }

        void FillTeam(PetDef d, AccountState a, PlayerState p)
        {
            for (int i = 0; i < PetRules.TeamSize; i++)
            {
                PetDef inSlot = PetConfig.Get(p.Team[i]);
                bool openSlot = p.Level >= PetRules.SlotUnlockLevel[i];
                slotIcons[i].sprite = PetArt.Icon(inSlot);
                slotIcons[i].enabled = slotIcons[i].sprite != null;
                slotIcons[i].color = inSlot != null ? PetArt.TintOf(inSlot) : Color.white;
                slotFrames[i].color = inSlot != null && inSlot == d ? new Color(1f, 0.95f, 0.6f) : openSlot ? Color.white : new Color(0.6f, 0.6f, 0.65f, 0.8f);
                slotPets[i].text = inSlot != null ? inSlot.Name + " " + PetArt.Stars(a != null ? a.StarsOf(inSlot.Id) : 1) + " L" + (a != null ? a.LevelOf(inSlot.Id) : 1)
                    : openSlot ? "<color=#8a96a4>empty</color>" : "<color=#8a96a4>Lv " + PetRules.SlotUnlockLevel[i] + "</color>";
            }
            int sel = System.Array.IndexOf(p.Team, d.Id);
            bool owned = a != null && a.OwnsPet(d.Id);
            teamHintText.text = sel >= 0 ? d.Name + " is in " + SlotNames[sel] + ": tap that slot to take it out, another to move it."
                : owned ? "Tap a slot to put " + d.Name + " there." : d.Name + " is not yours yet (Collection tab tells how).";
            teamBonusText.text = BonusText(p);
        }

        static string Pct(double v) { return (v * 100.0).ToString("0.#", CultureInfo.InvariantCulture) + "%"; }

        static string BonusText(PlayerState p)
        {
            PetBonus b = p.PetBonus;
            var s = new StringBuilder("<b>Your team gives</b>   ");
            int n = 0;
            if (b.Damage > 0) { s.Append("+").Append(Pct(b.Damage)).Append(" damage   "); n++; }
            if (b.MaxHp > 0) { s.Append("+").Append(Pct(b.MaxHp)).Append(" Max HP   "); n++; }
            if (b.MoveSpeed > 0) { s.Append("+").Append(Pct(b.MoveSpeed)).Append(" speed   "); n++; }
            if (b.DamageTaken > 0) { s.Append("-").Append(Pct(b.DamageTaken)).Append(" damage taken   "); n++; }
            if (b.PotionHeal > 0) { s.Append("+").Append(Pct(b.PotionHeal)).Append(" potion healing   "); n++; }
            if (b.AttackSpeed > 0) { s.Append("+").Append(Pct(b.AttackSpeed)).Append(" attack speed   "); n++; }
            if (b.BossDamage > 0) { s.Append("+").Append(Pct(b.BossDamage)).Append(" vs bosses   "); n++; }
            if (n == 0) s.Append("nothing yet: put a pet in a slot.");
            s.Append("\nCombat: full passive, fights beside you (hit every ").Append((PetConfig.BasicAttackMs / 1000f).ToString("0.#", CultureInfo.InvariantCulture)).Append(" s, skill every ").Append(PetConfig.SkillCooldownMs / 1000).Append(" s). Support: skill at ")
             .Append(Pct(PetRules.SupportSkillShare)).Append(". Stat: ").Append(Pct(PetRules.SlotPassiveShare[3])).Append(" of the passive. Caps: +").Append(Pct(PetRules.Cap(PetPassive.Damage))).Append(" damage, +")
             .Append(Pct(PetRules.Cap(PetPassive.MaxHp))).Append(" HP, +").Append(Pct(PetRules.Cap(PetPassive.MoveSpeed))).Append(" speed, -").Append(Pct(PetRules.Cap(PetPassive.DamageTaken))).Append(" taken.\n<b>Bonds</b> (+2% with both in the team)\n");
            List<PetBond> active = PetTeam.ActiveBonds(p.Team);
            foreach (PetBond bond in PetRules.Bonds)
            {
                bool on = active.Contains(bond);
                s.Append(on ? "<color=#9fff9f>" : "<color=#8a96a4>").Append(bond.Name).Append(": ").Append(PetConfig.Get(bond.PetA).Name).Append(" + ").Append(PetConfig.Get(bond.PetB).Name).Append("  ")
                 .Append(PetConfig.PassiveName(bond.Kind, Pct(bond.Pct).TrimEnd('%'))).Append(bond.BossOnly ? " (bosses only)" : "").Append(on ? "  active" : "").Append("</color>\n");
            }
            return s.ToString();
        }
    }
}
