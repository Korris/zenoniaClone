namespace ZenoniaProto.Rpg
{
    /// <summary>The five team slots (ChatGPT Lần 8, RO 1 + 2 + 5 shrunk to 1 + 2 + 2). Index = SaveData.petTeam position.</summary>
    public enum PetSlot { Combat = 0, Support1 = 1, Support2 = 2, Stat1 = 3, Stat2 = 4 }

    /// <summary>One Bond: both species in the team (any slots) give the bonus once.</summary>
    public sealed class PetBond
    {
        public string Name, PetA, PetB;
        public PetPassive Kind;
        public double Pct;
        /// <summary>DamageToBoss bonds raise damage on bosses only (Frozen Dominion).</summary>
        public bool BossOnly;
    }

    /// <summary>
    /// The V2 pet rules beside PetConfig (docs/pet-system-design.md, ChatGPT Lần 8): team slots and their share, Bond, caps, level and star growth, gacha prices and rates,
    /// capture balls and odds, the collection levels. Engine-free, shared with the zone server; the server-api mirrors the limits in account-validation.js.
    /// </summary>
    public static class PetRules
    {
        // ---- team ----
        public const int TeamSize = 5;
        /// <summary>InvOp.PetEquip's Slot byte: team slot x 16 + PetConfig index; index ClearIndex empties the slot.</summary>
        public const int ClearIndex = 15;
        public static byte EncodeSlot(int teamSlot, int petIndex) { return (byte)(teamSlot * 16 + (petIndex < 0 ? ClearIndex : petIndex)); }
        public static void DecodeSlot(byte code, out int teamSlot, out int petIndex) { teamSlot = code / 16; petIndex = code % 16; if (petIndex == ClearIndex) petIndex = -1; }
        /// <summary>Hero level that opens each slot: Combat 5, Support 20 / 30, Stat 35 / 45.</summary>
        public static readonly int[] SlotUnlockLevel = { 5, 20, 30, 35, 45 };
        /// <summary>Share of the pet's passive a slot gives: combat 100 %, stat slots 25 %, support slots nothing (they give their skill at SupportSkillShare).</summary>
        public static readonly double[] SlotPassiveShare = { 1.0, 0.0, 0.0, 0.25, 0.25 };
        public const double SupportSkillShare = 0.5;
        /// <summary>Caps on the sum of every pet source (slots + Bond) per passive kind.</summary>
        public static double Cap(PetPassive kind)
        {
            switch (kind)
            {
                case PetPassive.MaxHp: return 0.12;
                case PetPassive.Damage: return 0.10;
                case PetPassive.DamageTaken: return 0.10;
                case PetPassive.MoveSpeed: case PetPassive.AttackSpeed: return 0.10;
                default: return 0.10; // potion healing: same ceiling
            }
        }
        public static readonly PetBond[] Bonds =
        {
            new PetBond { Name = "Frost Pack", PetA = "frostSlime", PetB = "iceGoblin", Kind = PetPassive.DamageTaken, Pct = 0.02 },
            new PetBond { Name = "Wild Hunt", PetA = "hornRabbit", PetB = "spectralWolf", Kind = PetPassive.MoveSpeed, Pct = 0.02 },
            new PetBond { Name = "Swamp Guard", PetA = "thornBoar", PetB = "marshStalker", Kind = PetPassive.MaxHp, Pct = 0.02 },
            new PetBond { Name = "Frozen Dominion", PetA = "crystalGolem", PetB = "frostfangKing", Kind = PetPassive.Damage, Pct = 0.02, BossOnly = true },
        };
        /// <summary>Seconds out of combat before the team may change outside a village.</summary>
        public const int TeamChangeCalmMs = 10000;

        // ---- growth ----
        public const int MaxLevel = 60, MaxStars = 5;
        /// <summary>Pet Food to go from level L to L + 1: 2 + floor(L / 10). Always succeeds.</summary>
        public static int FoodForLevel(int level) { return 2 + level / 10; }
        /// <summary>Damage the pet itself deals (basic attack, attack skills) grows 0.5 % per level over level 1.</summary>
        public static double LevelDamageMul(int level) { return 1.0 + 0.005 * (RpgMath.Clamp(level, 1, MaxLevel) - 1); }
        /// <summary>Shards of the kind + gold for the next star, by current stars - 1: 10 + 5000, 20 + 10000, 30 + 20000, 40 + 30000.</summary>
        public static readonly int[] StarShards = { 10, 20, 30, 40 }, StarGold = { 5000, 10000, 20000, 30000 };
        /// <summary>The passive at these stars: 100 / 110 / 120 / 130 / 140 % of the 1-star value. Skills do not grow with stars.</summary>
        public static double StarFactor(int stars) { return 1.0 + 0.1 * (RpgMath.Clamp(stars, 1, MaxStars) - 1); }
        public const int DuplicateShards = 10, SharedShardsPerPull = 5;

        // ---- gacha (RO rates) ----
        public const int CrystalsPerPull = 150, MaxPullsAtOnce = 10;
        /// <summary>By PetRarity, then the shared-shard result: N 12, R 24, SR 36, SSR 6.25, UR 2.5, shards 19.25 %.</summary>
        public static readonly double[] GachaRates = { 0.12, 0.24, 0.36, 0.0625, 0.025 };
        public const double GachaShardRate = 0.1925;
        public const int SsrPity = 20, UrPity = 50, HistoryMax = 200, UrExchangeCost = 120;
        public const string UrExchangePet = "frostfangKing";

        // ---- rewards (per account) ----
        public const int StarterCoins = 10, WantedCrystals = 150, WantedPerWeek = 4, BossKillsForCrystals = 5, BossCrystals = 150, MilestoneCrystals = 300, MilestoneStep = 10, MilestoneMax = 90;
        public const int WantedFood = 10, BossFood = 3;
        public const double MonsterFoodChance = 0.08, BossBallChance = 0.10, ValidBossDamageShare = 0.01;

        // ---- capture ----
        public const int BallPrice = 500, BallsPerDay = 5, WantedClaimsForRareBall = 28;
        public const double CaptureHpLine = 0.50, CaptureLowHp = 0.25;
        public const float CaptureRangeUnits = 4f * 16f;
        public const int CaptureHitMemoryMs = 10000, BossCorpseMs = 30000, WantedCorpseMs = 45000;
        /// <summary>Normal-ball odds on a living monster by rarity: HP 26-50 % / 1-25 %. UR never while alive.</summary>
        public static double LiveCaptureChance(PetRarity r, double hpShare)
        {
            bool low = hpShare <= CaptureLowHp;
            switch (r)
            {
                case PetRarity.N: return low ? 0.60 : 0.40;
                case PetRarity.R: return low ? 0.40 : 0.25;
                case PetRarity.SR: return low ? 0.20 : 0.12;
                case PetRarity.SSR: return low ? 0.08 : 0.04;
                default: return 0.0;
            }
        }
        /// <summary>Normal-ball odds on a fallen boss: map boss SSR 5 % / UR 1 %; Wanted Boss SSR 3 % / UR 0.5 %.</summary>
        public static double BossCaptureChance(PetRarity r, bool wanted)
        {
            if (r == PetRarity.SSR) return wanted ? 0.03 : 0.05;
            if (r == PetRarity.UR) return wanted ? 0.005 : 0.01;
            return 0.0;
        }

        // ---- collection ----
        /// <summary>Species owned for collection level 2 .. 5 (level 1 = default). The level is the highest rarity a ball may catch: 1 N/R, 2 SR, 3 SSR, 4 UR, 5 title.</summary>
        public static readonly int[] CollectionSpecies = { 3, 5, 7, 10 };
        public static int CollectionLevel(int species) { int lvl = 1; foreach (int n in CollectionSpecies) if (species >= n) lvl++; return lvl; }
        public static int CollectionLevelFor(PetRarity r) { return r <= PetRarity.R ? 1 : r == PetRarity.SR ? 2 : r == PetRarity.SSR ? 3 : 4; }
    }
}
