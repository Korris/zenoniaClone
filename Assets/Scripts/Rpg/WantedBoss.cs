using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>One wanted boss and its fixed reward (paid once per character per day). PotionSize is a PotionCatalog size.</summary>
    public sealed class WantedBossDef
    {
        public readonly string BossType, BossName, MapId;
        public readonly int BossLevel, Gold, Stones, HpPotions, MpPotions, PotionSize;
        /// <summary>The one fixed tile (tx, ty from the map's top-left) the boss stands on all day, in open ground: off every monster zone, path, portal, bridge and stair (docs/map-design-rules.md).</summary>
        public readonly int SpawnTx, SpawnTy;

        public WantedBossDef(string bossType, string bossName, string mapId, int bossLevel, int gold, int stones, int hpPotions, int mpPotions, int potionSize, int spawnTx, int spawnTy)
        {
            BossType = bossType; BossName = bossName; MapId = mapId; BossLevel = bossLevel;
            SpawnTx = spawnTx; SpawnTy = spawnTy; Gold = gold; Stones = stones; HpPotions = hpPotions; MpPotions = mpPotions; PotionSize = potionSize;
        }
    }

    /// <summary>
    /// Daily "Wanted Boss": which of the three bosses is wanted today and what killing it pays. Pure data and day math, no state: the zone decides
    /// from its own clock, the client calls the same functions only to show the board. The claim itself lives in PlayerState.WantedDay.
    /// </summary>
    public static class WantedBoss
    {
        public const long DayMs = 86_400_000L;
        /// <summary>The day turns at 05:00 Vietnam time (UTC+7) = 22:00 UTC: shifting the clock by 7h - 5h = 2h makes that instant a multiple of DayMs. The one place to change the reset hour.</summary>
        public const long ResetShiftMs = 2L * 3_600_000L;

        /// <summary>Rotation order = the day cycle. Each boss comes every third day. Rewards are mostly Upgrade Stones (trade cannot move them).</summary>
        public static readonly WantedBossDef[] Rotation =
        {
            new WantedBossDef("rimeclawTyrant", "Rimeclaw Tyrant", "frostHollow", 10, 150, 3, 3, 3, PotionCatalog.Small, 64, 29),
            new WantedBossDef("glacialColossus", "Glacial Colossus", "frostfangDen", 25, 600, 5, 3, 3, PotionCatalog.Medium, 28, 3),
            new WantedBossDef("mireHydraQueen", "Mire Hydra Queen", "sunkenReliquary", 40, 1000, 8, 3, 3, PotionCatalog.Large, 33, 10),
        };

        /// <summary>Launch-arg test hook (--wanted-boss): when set, this boss is wanted every day. Null in production.</summary>
        public static string Override;

        public static WantedBossDef Find(string bossType)
        {
            if (string.IsNullOrEmpty(bossType)) return null;
            for (int i = 0; i < Rotation.Length; i++) if (Rotation[i].BossType == bossType) return Rotation[i];
            return null;
        }

        /// <summary>Day number counted from 05:00 UTC+7. Always above 0 for any real clock, so 0 can mean "never claimed".</summary>
        public static int DayKey(long utcMs) { return (int)Math.Floor((utcMs + ResetShiftMs) / (double)DayMs); }

        public static WantedBossDef For(int dayKey)
        {
            int n = Rotation.Length;
            return Rotation[((dayKey % n) + n) % n];
        }

        /// <summary>The wanted boss at this instant: the override when one is set, else today's rotation entry.</summary>
        public static WantedBossDef Today(long utcMs)
        {
            WantedBossDef o = Find(Override);
            return o ?? For(DayKey(utcMs));
        }

        /// <summary>When today's boss changes (display only: a countdown on the board).</summary>
        public static long NextResetUtcMs(long utcMs) { return (DayKey(utcMs) + 1L) * DayMs - ResetShiftMs; }

        /// <summary>"+600 G, +5 Upgrade Stones, +3 HP / +3 MP Medium Potions": the server toast and the client board read the same text.</summary>
        public static string RewardText(WantedBossDef d)
        {
            string size = PotionCatalog.Name(true, d.PotionSize).Split(' ')[0];
            string pots = "+" + d.HpPotions + " HP / +" + d.MpPotions + " MP " + size + " Potions";
            return "+" + d.Gold + " G, +" + d.Stones + " Upgrade Stone" + (d.Stones > 1 ? "s" : "") + ", " + pots;
        }

        /// <summary>The same reward in about 30 characters, for the notification toast ("+150 G, +3 stones, +3/+3 potions").</summary>
        public static string ShortText(WantedBossDef d) { return "+" + d.Gold + " G, +" + d.Stones + " stone" + (d.Stones > 1 ? "s" : "") + ", +" + d.HpPotions + "/+" + d.MpPotions + " potions"; }
    }
}
