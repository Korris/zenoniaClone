using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>What a hero gets from its whole team (slots + Bond, capped), by passive kind; BossDamage is the Frozen Dominion share (bosses only).</summary>
    public struct PetBonus
    {
        public double MaxHp, Damage, MoveSpeed, DamageTaken, PotionHeal, AttackSpeed, BossDamage;
        public double Of(PetPassive kind)
        {
            switch (kind)
            {
                case PetPassive.MaxHp: return MaxHp;
                case PetPassive.Damage: return Damage;
                case PetPassive.MoveSpeed: return MoveSpeed;
                case PetPassive.DamageTaken: return DamageTaken;
                case PetPassive.PotionHeal: return PotionHeal;
                default: return AttackSpeed;
            }
        }
    }

    /// <summary>
    /// The pet team of one hero (SaveData.petTeam, 5 ids, "" = empty): PetSlot.Combat walks and fights, Support1 / 2 cast their skill at half strength without a body,
    /// Stat1 / 2 give a quarter of their passive. Rules from docs/pet-system-design.md: a slot opens at a hero level, one species sits in one slot, every pet must be owned,
    /// a passive kind adds up from at most 2 support sources, Bond pairs add 2 % when both species are in the team, and the sum per kind is capped (PetRules.Cap).
    /// </summary>
    public static class PetTeam
    {
        public static string[] Empty() { return new[] { "", "", "", "", "" }; }

        /// <summary>Null when `id` may go into `slot` of `team` for a hero at `level` with this account; else why not. "" always clears.</summary>
        public static string Refusal(string[] team, int slot, string id, int level, AccountState account)
        {
            if (slot < 0 || slot >= PetRules.TeamSize) return "No such slot";
            if (level < PetRules.SlotUnlockLevel[slot]) return "Opens at level " + PetRules.SlotUnlockLevel[slot];
            if (string.IsNullOrEmpty(id)) return null;
            PetDef d = PetConfig.Get(id);
            if (d == null) return "No such pet";
            if (account == null || !account.OwnsPet(id)) return "You do not own " + d.Name;
            for (int i = 0; i < team.Length; i++) if (i != slot && team[i] == id) return d.Name + " is already in the team";
            return null;
        }

        /// <summary>Puts `id` in `slot` (or clears it). The caller checked Refusal.</summary>
        public static void Set(string[] team, int slot, string id) { team[slot] = id ?? ""; }

        /// <summary>Drops pets the account no longer owns or slots the level has not opened (a stale save, another device).</summary>
        public static void Sanitize(string[] team, int level, AccountState account)
        {
            for (int i = 0; i < team.Length; i++)
                if (Refusal(team, i, team[i], level, account) != null) team[i] = "";
        }

        public static bool Has(string[] team, string id) { foreach (string t in team) if (t == id) return true; return false; }

        /// <summary>The team's total, capped: combat 100 %, stat slots 25 %, each scaled by the pet's stars; Bond pairs on top. Support slots give no passive.</summary>
        public static PetBonus Bonus(string[] team, AccountState account)
        {
            var sum = new double[6];
            var sources = new int[6];
            double boss = 0;
            for (int i = 0; i < team.Length && i < PetRules.TeamSize; i++)
            {
                PetDef d = PetConfig.Get(team[i]);
                if (d == null || PetRules.SlotPassiveShare[i] <= 0) continue;
                int k = (int)d.Passive;
                if (i != (int)PetSlot.Combat && sources[k] >= 2) continue; // at most two support sources of one kind
                if (i != (int)PetSlot.Combat) sources[k]++;
                int stars = account != null ? account.StarsOf(d.Id) : 1;
                sum[k] += d.PassivePct * PetRules.StarFactor(stars) * PetRules.SlotPassiveShare[i];
            }
            foreach (PetBond b in PetRules.Bonds)
            {
                if (!Has(team, b.PetA) || !Has(team, b.PetB)) continue;
                if (b.BossOnly) boss += b.Pct; else sum[(int)b.Kind] += b.Pct;
            }
            var r = new PetBonus();
            r.MaxHp = Capped(sum, PetPassive.MaxHp); r.Damage = Capped(sum, PetPassive.Damage); r.MoveSpeed = Capped(sum, PetPassive.MoveSpeed);
            r.DamageTaken = Capped(sum, PetPassive.DamageTaken); r.PotionHeal = Capped(sum, PetPassive.PotionHeal); r.AttackSpeed = Capped(sum, PetPassive.AttackSpeed);
            r.BossDamage = System.Math.Min(boss, PetRules.Cap(PetPassive.Damage) - r.Damage > 0 ? PetRules.Cap(PetPassive.Damage) - r.Damage : 0);
            return r;
        }

        static double Capped(double[] sum, PetPassive k) { return System.Math.Min(sum[(int)k], PetRules.Cap(k)); }

        /// <summary>The Bonds the team has on.</summary>
        public static List<PetBond> ActiveBonds(string[] team)
        {
            var list = new List<PetBond>();
            foreach (PetBond b in PetRules.Bonds) if (Has(team, b.PetA) && Has(team, b.PetB)) list.Add(b);
            return list;
        }
    }
}
