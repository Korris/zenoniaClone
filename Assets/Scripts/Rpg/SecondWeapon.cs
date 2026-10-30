using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Second-weapon (offhand) table: one shared +0..+15 curve and per-class caps (plans/261030-1630-second-weapon-enhance). Every stat is
    /// cap * F(level); signature combos run base + (cap - base) * F and are active from +0 while the offhand is worn.
    /// </summary>
    public static class SecondWeapon
    {
        /// <summary>F by level: per-level weight 1 (+1..6), 2 (+7..9), 3 (+10..12), 4 (+13..15), cumulative / 33.</summary>
        public static readonly double[] Curve = BuildCurve();

        static double[] BuildCurve()
        {
            var t = new double[EnhanceConfig.MaxEnhance + 1];
            int sum = 0;
            for (int l = 1; l <= EnhanceConfig.MaxEnhance; l++) { sum += l <= 6 ? 1 : l <= 9 ? 2 : l <= 12 ? 3 : 4; t[l] = sum; }
            for (int l = 1; l < t.Length; l++) t[l] /= sum;
            return t;
        }

        public static double F(int level) { return Curve[RpgMath.Clamp(level, 0, EnhanceConfig.MaxEnhance)]; }

        /// <summary>base + (cap - base) * F(level) for a combo parameter.</summary>
        public static double Combo(double baseValue, double cap, int level) { return baseValue + (cap - baseValue) * F(level); }

        // Combo parameters (base at +0 -> cap at +15).
        public static double ComboEarthShieldPct(int level) { return Combo(0.04, 0.12, level); }   // Earth Break >=3 hit: shield % MaxHP, 3 s
        public static double ComboChillSeconds(int level) { return Combo(1.0, 3.0, level); }        // Ice Storm chill duration
        public static double ComboVolleyMul(int level) { return Combo(0.4, 1.0, level); }           // Plasma Barrage volley: 3 shards x 55% x this x (1+RobotDmg)
        public static double ComboNextCritPts(int level) { return Combo(0.30, 0.80, level); }       // Shadow Step: next-hit crit points, 3 s, 1 charge
        public static double ComboHealShieldPct(int level) { return Combo(0.15, 0.40, level); }     // Heal: spirit shield % of healed amount, 4 s

        /// <summary>Stats of an equipped second weapon of `classId` at `level`; <see cref="SecondWeaponStats.Zero"/> never comes from here.</summary>
        public static SecondWeaponStats For(string classId, int level)
        {
            level = RpgMath.Clamp(level, 0, EnhanceConfig.MaxEnhance);
            double f = F(level);
            var s = new SecondWeaponStats { Equipped = true, Level = level, F = f };
            switch (RpgClasses.Get(classId).Id)
            {
                case "warrior": s.DefPct = 0.30 * f; s.HpPct = 0.20 * f; s.DmgReduction = 0.10 * f; break;
                case "wizard": s.MpPct = 0.30 * f; s.CastSpeed = 0.25 * f; s.MpCostCut = 0.20 * f; break;
                case "mechanic": s.RobotDmg = 0.60 * f; s.RobotRate = 0.40 * f; break;
                case "assassin": s.CritRate = 0.10 * f; s.CritDmg = 0.50 * f; break;
                case "cleric": s.HealPower = 0.40 * f; s.ShieldPower = 0.40 * f; break;
            }
            return s;
        }
    }
}
