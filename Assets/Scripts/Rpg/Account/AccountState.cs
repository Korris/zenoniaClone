using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>One pet the account owns: level and stars (PetRules), the shards of its kind, and what was spent on it so a reset can refund exactly that.</summary>
    [Serializable]
    public class OwnedPet { public string id; public int stars = 1; public int shards; public int level = 1; public int foodSpent, shardsSpent, goldSpent; }

    /// <summary>One gacha pull in the history: when, which pet ("" = shards), duplicate or new, the shards that gave, and the pity counters right after it.</summary>
    [Serializable]
    public class PullRecord { public long at; public string id; public bool dup; public int shards; public int pitySSR, pityUR; }

    /// <summary>
    /// What belongs to the ACCOUNT, not to one hero (docs/pet-system-design.md, ChatGPT Lần 6-8): owned pets with level / stars / kind shards, Pet Crystals and gacha coins,
    /// shared shards, Pet Food, capture balls, the pity counters, the last 200 pulls, the one-time flags and the day / week counters. A hero only stores its team
    /// (SaveData.petTeam). Field names = the server-api `accounts.state` shape (account-validation.js), read by JsonUtility as they are. Online the zone server is the
    /// only writer (ZoneAccountQueue); offline it is a local file (AccountStore). version 2 = the RO rules (version 1 pity counters were 10 / 30 / 60: Sanitize clamps them).
    /// </summary>
    [Serializable]
    public class AccountState
    {
        public const int MaxPets = 16, MaxCounter = 2_000_000_000, Version = 2;

        public int version = Version;
        public List<OwnedPet> pets = new List<OwnedPet>();
        /// <summary>tickets = gacha coins (1 pull each); crystals = Pet Crystals (150 = 1 pull); urShards = UR shards (120 = Frostfang King); sharedShards = shards of no kind (1:1 into a kind).</summary>
        public int tickets, crystals, urShards, sharedShards;
        /// <summary>Pet Food (levels), normal and rare capture balls.</summary>
        public int food, balls, rareBalls;
        /// <summary>Pulls in a row without an SSR or better (0..19) / a UR (0..49). PetGacha resets them.</summary>
        public int pitySSR, pityUR, totalPulls;
        public List<PullRecord> history = new List<PullRecord>(); // newest last, at most PetRules.HistoryMax
        public bool starterGiven;
        /// <summary>Pet kinds already compensated for an old durability pet (once per kind per account).</summary>
        public List<string> compensated = new List<string>();
        /// <summary>Highest level milestone (10, 20 ... 90) already paid, over all heroes of the account.</summary>
        public int levelReward;
        /// <summary>Valid Wanted Boss claims counted toward the next rare ball (28 each), and balls bought today.</summary>
        public int wantedForRareBall, ballsBoughtToday;
        /// <summary>WantedBoss.DayKey of the day flags: the account's first Wanted claim, a boss pet caught, a Wanted pet caught, balls bought.</summary>
        public int rewardDay;
        public bool dayWanted, dayBossCaught, dayWantedCaught;
        /// <summary>AccountRewards.WeekKey these week counters belong to.</summary>
        public int rewardWeek, weekWantedClaims, weekBossKills;

        public OwnedPet Pet(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (OwnedPet p in pets) if (p.id == id) return p;
            return null;
        }

        public bool OwnsPet(string id) { return Pet(id) != null; }
        public int StarsOf(string id) { OwnedPet p = Pet(id); return p != null ? p.stars : 0; }
        public int LevelOf(string id) { OwnedPet p = Pet(id); return p != null ? p.level : 0; }
        /// <summary>Species owned, and the collection level that gives (PetRules.CollectionLevel).</summary>
        public int Species { get { return pets.Count; } }
        public int CollectionLevel { get { return PetRules.CollectionLevel(pets.Count); } }

        /// <summary>A deep copy: every account change is computed on a copy and only adopted once it is stored.</summary>
        public AccountState Clone()
        {
            var c = (AccountState)MemberwiseClone();
            c.pets = new List<OwnedPet>();
            foreach (OwnedPet p in pets) c.pets.Add(new OwnedPet { id = p.id, stars = p.stars, shards = p.shards, level = p.level, foodSpent = p.foodSpent, shardsSpent = p.shardsSpent, goldSpent = p.goldSpent });
            c.history = new List<PullRecord>();
            foreach (PullRecord h in history) c.history.Add(new PullRecord { at = h.at, id = h.id, dup = h.dup, shards = h.shards, pitySSR = h.pitySSR, pityUR = h.pityUR });
            c.compensated = new List<string>(compensated);
            return c;
        }

        /// <summary>Becomes `o` (a stored copy) while keeping this object, so every PlayerState that points at it sees the change.</summary>
        public void CopyFrom(AccountState o)
        {
            AccountState c = o.Clone();
            version = c.version; pets = c.pets; tickets = c.tickets; crystals = c.crystals; urShards = c.urShards; sharedShards = c.sharedShards;
            food = c.food; balls = c.balls; rareBalls = c.rareBalls;
            pitySSR = c.pitySSR; pityUR = c.pityUR; totalPulls = c.totalPulls; history = c.history;
            starterGiven = c.starterGiven; compensated = c.compensated; levelReward = c.levelReward; wantedForRareBall = c.wantedForRareBall; ballsBoughtToday = c.ballsBoughtToday;
            rewardDay = c.rewardDay; dayWanted = c.dayWanted; dayBossCaught = c.dayBossCaught; dayWantedCaught = c.dayWantedCaught;
            rewardWeek = c.rewardWeek; weekWantedClaims = c.weekWantedClaims; weekBossKills = c.weekBossKills;
        }

        /// <summary>Validates untrusted JSON data (save file, network push) with the server-api schema's limits: unknown ids and duplicates go, numbers are clamped.</summary>
        public static AccountState Sanitize(AccountState raw)
        {
            var a = new AccountState();
            if (raw == null) return a;
            foreach (OwnedPet p in raw.pets ?? new List<OwnedPet>())
                if (p != null && PetConfig.Get(p.id) != null && !a.OwnsPet(p.id) && a.pets.Count < MaxPets)
                    a.pets.Add(new OwnedPet { id = p.id, stars = RpgMath.Clamp(p.stars, 1, PetRules.MaxStars), shards = Count(p.shards), level = RpgMath.Clamp(p.level, 1, PetRules.MaxLevel),
                        foodSpent = Count(p.foodSpent), shardsSpent = Count(p.shardsSpent), goldSpent = Count(p.goldSpent) });
            a.tickets = Count(raw.tickets); a.crystals = Count(raw.crystals); a.urShards = Count(raw.urShards); a.sharedShards = Count(raw.sharedShards); a.totalPulls = Count(raw.totalPulls);
            a.food = Count(raw.food); a.balls = Count(raw.balls); a.rareBalls = Count(raw.rareBalls);
            a.pitySSR = RpgMath.Clamp(raw.pitySSR, 0, PetRules.SsrPity - 1);
            a.pityUR = RpgMath.Clamp(raw.pityUR, 0, PetRules.UrPity - 1);
            List<PullRecord> h = raw.history ?? new List<PullRecord>();
            for (int i = Math.Max(0, h.Count - PetRules.HistoryMax); i < h.Count; i++)
                if (h[i] != null && (string.IsNullOrEmpty(h[i].id) || PetConfig.Get(h[i].id) != null))
                    a.history.Add(new PullRecord { at = Math.Max(0L, h[i].at), id = h[i].id ?? "", dup = h[i].dup, shards = RpgMath.Clamp(h[i].shards, 0, 1000),
                        pitySSR = RpgMath.Clamp(h[i].pitySSR, 0, PetRules.SsrPity - 1), pityUR = RpgMath.Clamp(h[i].pityUR, 0, PetRules.UrPity - 1) });
            a.starterGiven = raw.starterGiven;
            foreach (string id in raw.compensated ?? new List<string>())
                if (PetConfig.Get(id) != null && !a.compensated.Contains(id)) a.compensated.Add(id);
            a.levelReward = RpgMath.Clamp(raw.levelReward, 0, PetRules.MilestoneMax);
            a.wantedForRareBall = RpgMath.Clamp(raw.wantedForRareBall, 0, PetRules.WantedClaimsForRareBall - 1);
            a.ballsBoughtToday = RpgMath.Clamp(raw.ballsBoughtToday, 0, PetRules.BallsPerDay);
            a.rewardDay = RpgMath.Clamp(raw.rewardDay, 0, 1_000_000); a.dayWanted = raw.dayWanted; a.dayBossCaught = raw.dayBossCaught; a.dayWantedCaught = raw.dayWantedCaught;
            a.rewardWeek = RpgMath.Clamp(raw.rewardWeek, 0, 1_000_000);
            a.weekWantedClaims = RpgMath.Clamp(raw.weekWantedClaims, 0, PetRules.WantedPerWeek);
            a.weekBossKills = RpgMath.Clamp(raw.weekBossKills, 0, 999);
            return a;
        }

        static int Count(int v) { return RpgMath.Clamp(v, 0, MaxCounter); }
    }
}
