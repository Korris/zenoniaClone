using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// What the account earns by playing (docs/pet-system-design.md 4-6), counted per ACCOUNT so 5 heroes do not earn 5 times:
    /// - a new account: 10 gacha coins once;
    /// - the account's first Wanted Boss claim of the day: 150 Pet Crystals (at most 4 a week), 10 Pet Food, and one step toward a rare ball (28 claims);
    /// - valid map boss kills (the hero itself dealt 1 % of the boss's max HP; pet hits never count): 3 Pet Food each, and the 5th of the week 150 Crystals;
    /// - the first time any hero of the account reaches level 10 / 20 ... 90: 300 Crystals per milestone;
    /// - normal monsters: never Crystals; 8 % a bag of Pet Food (rolled by the zone, AddFood).
    /// 150 Crystals = 1 pull (PetGacha.BuyCoins). The day is WantedBoss.DayKey (05:00 UTC+7), the week turns Monday 00:00 UTC+7. Nothing here is sold, bought with real money or traded.
    /// </summary>
    public static class AccountRewards
    {
        const long DayMs = 86_400_000L, VietnamShiftMs = 7L * 3_600_000L;

        /// <summary>Week number that turns at Monday 00:00 UTC+7. 1970-01-01 was a Thursday, so day 4 (Monday 5 Jan) starts week 1.</summary>
        public static int WeekKey(long utcMs)
        {
            long day = (long)Math.Floor((utcMs + VietnamShiftMs) / (double)DayMs);
            return (int)Math.Floor((day + 3) / 7.0);
        }

        /// <summary>Moves the day / week counters to now: a new day clears the day flags, a new week the week counters.</summary>
        public static void Roll(AccountState a, long utcMs)
        {
            int day = WantedBoss.DayKey(utcMs), week = WeekKey(utcMs);
            if (a.rewardDay != day) { a.rewardDay = day; a.dayWanted = false; a.dayBossCaught = false; a.dayWantedCaught = false; a.ballsBoughtToday = 0; }
            if (a.rewardWeek != week) { a.rewardWeek = week; a.weekWantedClaims = 0; a.weekBossKills = 0; }
        }

        static int Add(int v, int n) { return Math.Min(AccountState.MaxCounter, v + n); }

        /// <summary>The one-time starter coins. Fails (nothing to store) when they were given already.</summary>
        public static ActionResult GrantStarter(AccountState a)
        {
            if (a.starterGiven) return ActionResult.Fail("");
            a.starterGiven = true;
            a.tickets = Add(a.tickets, PetRules.StarterCoins);
            return ActionResult.Success("+" + PetRules.StarterCoins + " gacha coins (welcome gift)");
        }

        /// <summary>A hero of this account just claimed a Wanted Boss reward. Fails (nothing changed) when the account had its claim today.</summary>
        public static ActionResult OnWantedClaim(AccountState a, long utcMs)
        {
            Roll(a, utcMs);
            if (a.dayWanted) return ActionResult.Fail("");
            a.dayWanted = true;
            a.food = Add(a.food, PetRules.WantedFood);
            string text = "+" + PetRules.WantedFood + " Pet Food";
            if (a.weekWantedClaims < PetRules.WantedPerWeek)
            {
                a.weekWantedClaims++;
                a.crystals = Add(a.crystals, PetRules.WantedCrystals);
                text += ", +" + PetRules.WantedCrystals + " Pet Crystals";
            }
            if (++a.wantedForRareBall >= PetRules.WantedClaimsForRareBall) { a.wantedForRareBall = 0; a.rareBalls = Add(a.rareBalls, 1); text += ", +1 Rare Pet Ball"; }
            return ActionResult.Success(text);
        }

        /// <summary>A hero of this account took part in a map boss kill with a valid share: 3 Pet Food, the 5th of the week 150 Crystals.</summary>
        public static ActionResult OnMapBossKill(AccountState a, long utcMs)
        {
            Roll(a, utcMs);
            a.food = Add(a.food, PetRules.BossFood);
            string text = "+" + PetRules.BossFood + " Pet Food";
            if (a.weekBossKills < 999) a.weekBossKills++;
            if (a.weekBossKills == PetRules.BossKillsForCrystals) { a.crystals = Add(a.crystals, PetRules.BossCrystals); text += ", +" + PetRules.BossCrystals + " Pet Crystals (" + PetRules.BossKillsForCrystals + " bosses this week)"; }
            return ActionResult.Success(text);
        }

        /// <summary>A map boss dropped a ball (the zone rolled PetRules.BossBallChance).</summary>
        public static ActionResult AddBall(AccountState a) { a.balls = Add(a.balls, 1); return ActionResult.Success("+1 Pet Ball"); }

        /// <summary>Pet Food from a normal monster (the zone rolled PetRules.MonsterFoodChance).</summary>
        public static ActionResult AddFood(AccountState a, int n) { a.food = Add(a.food, n); return ActionResult.Success("+" + n + " Pet Food"); }

        /// <summary>A hero of this account is at `level`: pays every milestone it passed that the account was not paid for yet. Fails when there is none.</summary>
        public static ActionResult OnLevel(AccountState a, int level)
        {
            int reached = Math.Min(PetRules.MilestoneMax, level / PetRules.MilestoneStep * PetRules.MilestoneStep);
            if (reached <= a.levelReward) return ActionResult.Fail("");
            int milestones = (reached - a.levelReward) / PetRules.MilestoneStep;
            a.levelReward = reached;
            a.crystals = Add(a.crystals, milestones * PetRules.MilestoneCrystals);
            return ActionResult.Success("+" + milestones * PetRules.MilestoneCrystals + " Pet Crystals (level " + reached + ")");
        }

        /// <summary>At least 1 % of the boss's max HP dealt by the hero itself (a tap-and-leave, or a pet, does not count).</summary>
        public static bool IsValidBossShare(int damage, float bossMaxHp) { return damage > 0 && damage >= Math.Ceiling(bossMaxHp * PetRules.ValidBossDamageShare); }
    }
}
