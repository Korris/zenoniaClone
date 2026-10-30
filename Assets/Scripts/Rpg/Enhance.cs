using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Weapon enhancement tuning +0..+15 (web data/weapon-enhance-config.js).</summary>
    public static class EnhanceConfig
    {
        public const int MaxEnhance = 15;
        /// <summary>First level whose held-weapon art / glow switches to the enhanced look.</summary>
        public const int EnhancedArtMinLevel = 7;
        public static readonly int[] VisualBandMins = { 7, 9, 11, 13 };

        /// <summary>
        /// Cumulative fraction of the weapon's base stat at each level (index = level): +1..6 10%, +7..9 20%, +10..12 35%, +13..15 60% per level,
        /// so +15 adds 405% (Unity balance 2026-10-30; the web used 5 / 8 / 12 / 18% = 144%, which made +15 feel like nothing).
        /// </summary>
        public static readonly double[] AtkPct = Build();

        static double[] Build()
        {
            var t = new double[MaxEnhance + 1];
            double total = 0;
            for (int level = 1; level <= MaxEnhance; level++)
            {
                total += level <= 6 ? 0.10 : level <= 9 ? 0.20 : level <= 12 ? 0.35 : 0.60;
                t[level] = Math.Floor(total * 1000 + 0.5) / 1000;
            }
            return t;
        }
    }

    /// <summary>Pure helpers deriving enhanced values; item.stats is never mutated (web systems/weapon-enhance-stats.js).</summary>
    public static class EnhanceStats
    {
        public const double OffhandScale = 0.5; // offhands get half of the weapon % table

        public static bool IsEnhanceable(Item item) { return item != null && (item.slot == Slots.Weapon || item.slot == Slots.Offhand); }

        /// <summary>Stat the bonus scales: atk for weapons, the offhand template's enhanceStat otherwise.</summary>
        public static string StatKey(Item item)
        {
            if (item == null || item.slot != Slots.Offhand) return "atk";
            ItemTemplate t = ItemDefs.Template(item.templateId);
            return t != null ? t.EnhanceStat : "atk";
        }

        public static int Level(Item item) { return IsEnhanceable(item) ? RpgMath.Clamp(item.enhance, 0, EnhanceConfig.MaxEnhance) : 0; }
        public static double Scale(Item item) { return item != null && item.slot == Slots.Offhand ? OffhandScale : 1.0; }

        /// <summary>
        /// Bonus for `level`: % of the base primary stat, at least +1 per level (scaled down for bases under 4, and by pctScale for offhands)
        /// so even the starter weapon visibly gains from every level. A better base weapon still gains far more at high levels.
        /// </summary>
        public static int Bonus(int baseValue, int level, double pctScale = 1.0)
        {
            int lvl = RpgMath.Clamp(level, 0, EnhanceConfig.MaxEnhance);
            if (lvl == 0) return 0;
            int floor = (int)Math.Ceiling(lvl * Math.Min(1.0, baseValue / 4.0) * pctScale);
            return Math.Max(floor, RpgMath.Round(baseValue * EnhanceConfig.AtkPct[lvl] * pctScale));
        }

        /// <summary>Stats including the enhance bonus (new object).</summary>
        public static ItemStats Effective(Item item)
        {
            ItemStats s = item.stats.Copy();
            int level = Level(item);
            if (level > 0)
            {
                string key = StatKey(item);
                int b = s.GetInt(key);
                s.SetInt(key, b + Bonus(b, level, Scale(item)));
            }
            return s;
        }

        public static string DisplayName(Item item) { int l = Level(item); return l > 0 ? item.name + " +" + l : item.name; }

        /// <summary>0 = base look, 1..4 for the +7 / +9 / +11 / +13 thresholds.</summary>
        public static int VisualBand(Item item)
        {
            int level = Level(item), band = 0;
            for (int i = 0; i < EnhanceConfig.VisualBandMins.Length; i++) if (level >= EnhanceConfig.VisualBandMins[i]) band = i + 1;
            return band;
        }
    }

    /// <summary>Swappable enhance cost / chance (web weapon-upgrade-method-stone.js). The only class that knows about stones.</summary>
    public interface IUpgradeMethod
    {
        string Id { get; }
        double SuccessChance(int level);
        bool CanPay(PlayerState player, int level);
        void Pay(PlayerState player);
        string CostLabel { get; }
    }

    public sealed class StoneUpgradeMethod : IUpgradeMethod
    {
        public static readonly StoneUpgradeMethod Instance = new StoneUpgradeMethod();
        /// <summary>Chance to reach the target level (index = target level; +1..+3 guaranteed).</summary>
        public static readonly double[] SuccessByTarget = { 0, 1, 1, 1, 0.9, 0.85, 0.8, 0.7, 0.65, 0.6, 0.5, 0.45, 0.4, 0.3, 0.25, 0.2 };

        public string Id { get { return "upgradeStone"; } }
        public string CostLabel { get { return "1 Upgrade Stone"; } }
        public double SuccessChance(int level) { return SuccessByTarget[RpgMath.Clamp(level + 1, 1, EnhanceConfig.MaxEnhance)]; }
        public bool CanPay(PlayerState p, int level) { return level < EnhanceConfig.MaxEnhance && p.UpgradeStone >= 1; }
        public void Pay(PlayerState p) { p.UpgradeStone -= 1; } // spent on failure too
    }

    public struct EnhanceResult { public bool Ok, Success; public int Level; public string Reason; }

    public static class EnhanceSystem
    {
        /// <summary>One attempt level -> level + 1. Knows nothing about stones: cost and chance come from the method. Ok = an attempt was made.</summary>
        public static EnhanceResult Attempt(PlayerState player, Item item, IUpgradeMethod method = null, Rand rand = null)
        {
            method = method ?? StoneUpgradeMethod.Instance;
            rand = rand ?? RpgRandom.Default;
            int level = EnhanceStats.Level(item);
            Func<string, EnhanceResult> fail = reason => new EnhanceResult { Ok = false, Success = false, Level = level, Reason = reason };
            if (!EnhanceStats.IsEnhanceable(item)) return fail("not-weapon");
            bool equipped = player.GetEquipped(item.slot) == item;
            if (!equipped && !player.Inventory.Contains(item)) return fail("not-owned");
            if (level >= EnhanceConfig.MaxEnhance) return fail("max-level");
            if (!method.CanPay(player, level)) return fail("no-cost");
            method.Pay(player);
            bool success = rand() < method.SuccessChance(level);
            if (success)
            {
                item.enhance = level + 1;
                if (equipped) player.RecomputeStats();
            }
            return new EnhanceResult { Ok = true, Success = success, Level = success ? level + 1 : level, Reason = "" };
        }
    }
}
