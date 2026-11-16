using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What both looks of the pet screen share (the old PetWindow and the rebuilt PetCampWindow): the names of the five team slots, the fingerprint of
    /// everything they show, the texts built from the rules (team totals, where a species comes from) and the two actions that need a check before
    /// they are sent. Every change still goes through PetActions.Send, so offline and online run the same rules.
    /// </summary>
    public static class PetViewModel
    {
        public static readonly string[] SlotNames = { "Combat", "Support 1", "Support 2", "Stat 1", "Stat 2" };
        public static readonly string[] SlotShare = { "100%", "skill 50%", "skill 50%", "passive 25%", "passive 25%" };
        public static readonly string[] SlotShort = { "out", "sup", "sup", "stat", "stat" };

        public static string Pct(double v) { return (v * 100.0).ToString("0.#", CultureInfo.InvariantCulture) + "%"; }

        /// <summary>A cheap fingerprint of the hero's and the account's pet state, so a view only rebuilds its texts when something changed.</summary>
        public static int StateKey(PlayerState p, AccountState a)
        {
            unchecked
            {
                int k = p.Gold * 31 + p.Level * 11;
                foreach (string t in p.Team) k = k * 13 + (t ?? "").GetHashCode();
                if (a == null) return k;
                k = k * 17 + a.tickets + a.crystals * 3 + a.food * 5 + a.balls * 7 + a.rareBalls * 11 + a.sharedShards * 19 + a.ballsBoughtToday * 23 + (a.dayBossCaught ? 29 : 0) + (a.dayWantedCaught ? 37 : 0);
                k = k * 17 + a.urShards + a.wantedForRareBall * 41;
                foreach (OwnedPet o in a.pets) k = k * 13 + o.id.GetHashCode() + o.stars * 5 + o.shards + o.level * 101;
                return k;
            }
        }

        /// <summary>What the whole team gives the hero now, one entry per kind that is above zero ("+0.9% damage", "-4.8% damage taken" ...).</summary>
        public static List<string> BonusParts(PetBonus b)
        {
            var list = new List<string>();
            if (b.Damage > 0) list.Add("+" + Pct(b.Damage) + " damage");
            if (b.MaxHp > 0) list.Add("+" + Pct(b.MaxHp) + " Max HP");
            if (b.MoveSpeed > 0) list.Add("+" + Pct(b.MoveSpeed) + " speed");
            if (b.DamageTaken > 0) list.Add("-" + Pct(b.DamageTaken) + " damage taken");
            if (b.PotionHeal > 0) list.Add("+" + Pct(b.PotionHeal) + " potion healing");
            if (b.AttackSpeed > 0) list.Add("+" + Pct(b.AttackSpeed) + " attack speed");
            if (b.BossDamage > 0) list.Add("+" + Pct(b.BossDamage) + " vs bosses");
            return list;
        }

        /// <summary>How the slots work and the caps on the team's total, from the rules.</summary>
        public static string RulesText()
        {
            return "Combat: full passive, fights beside you (hit every " + (PetConfig.BasicAttackMs / 1000f).ToString("0.#", CultureInfo.InvariantCulture) + " s, skill every " + PetConfig.SkillCooldownMs / 1000
                + " s). Support: skill at " + Pct(PetRules.SupportSkillShare) + ". Stat: " + Pct(PetRules.SlotPassiveShare[3]) + " of the passive. Caps: +" + Pct(PetRules.Cap(PetPassive.Damage)) + " damage, +"
                + Pct(PetRules.Cap(PetPassive.MaxHp)) + " HP, +" + Pct(PetRules.Cap(PetPassive.MoveSpeed)) + " speed, -" + Pct(PetRules.Cap(PetPassive.DamageTaken)) + " taken.";
        }

        /// <summary>Where a species the account does not own comes from: legacy unlock, gacha rate, UR shards, a ball on its monster or its fallen boss.</summary>
        public static string HowToGet(PetDef d, AccountState a)
        {
            if (d.UnlockLevel > 0) return "Legacy pet: free when any of your heroes reaches level " + d.UnlockLevel + ".";
            if (d.Price > 0) return "Legacy pet: buy at a merchant for " + d.Price + " G.";
            string s = "From the Pet Gacha (" + ((PetGacha.PetRate(d) * 100).ToString("0.##", CultureInfo.InvariantCulture)) + "% a pull)";
            if (d.Id == PetRules.UrExchangePet) s += ", or for " + PetRules.UrExchangeCost + " UR shards (" + (a != null ? a.urShards : 0) + " now)";
            if (d.Rarity <= PetRarity.SSR && d.MonsterType != null) s += ", or a Pet Ball on a " + d.Name + " at half HP (" + Pct(PetRules.LiveCaptureChance(d.Rarity, 0.5)) + ", " + Pct(PetRules.LiveCaptureChance(d.Rarity, 0.25)) + " under a quarter)";
            if (d.Rarity >= PetRarity.SSR) s += ", or a ball on its fallen boss (" + Pct(PetRules.BossCaptureChance(d.Rarity, false)) + ")";
            return s + ". Needs collection level " + PetRules.CollectionLevelFor(d.Rarity) + " to catch.";
        }

        /// <summary>The old window's slot tap: species `petIndex` goes into this slot; if it is there already it leaves.</summary>
        public static void ToggleSlot(int slot, int petIndex)
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            PetDef d = PetConfig.All[petIndex];
            bool there = p.Team[slot] == d.Id;
            if (!there && p.Account != null)
            {
                string why = PetTeam.Refusal(p.Team, slot, d.Id, p.Level, p.Account);
                if (why != null) { RpgHud.Toast(why, new Color(1f, 0.7f, 0.5f)); return; } // the server would say the same; save the round trip
            }
            PetActions.Send(InvOp.PetEquip, PetRules.EncodeSlot(slot, there ? -1 : petIndex));
        }

        /// <summary>`n` shared shards (all of them when n is negative) become shards of species `petIndex`.</summary>
        public static void Convert(int petIndex, int n)
        {
            AccountState a = PetActions.Account;
            if (a == null) return;
            if (n < 0) n = a.sharedShards;
            PetActions.Send(InvOp.PetConvertShards, (byte)petIndex, Mathf.Min(n, a.sharedShards).ToString());
        }
    }
}
