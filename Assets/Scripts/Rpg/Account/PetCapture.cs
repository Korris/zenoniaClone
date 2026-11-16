using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>What a capture request found: refused (nothing spent), a ball thrown and missed, or the pet caught.</summary>
    public enum CaptureOutcome { Refused, Missed, Caught }

    /// <summary>What the zone / offline sim tells the rules about the target (the rules never see a Monster).</summary>
    public struct CaptureTarget
    {
        public PetDef Pet;         // the species this monster becomes (null = not a pet)
        public bool Boss, Wanted;  // a fallen map boss (30 s window) / Wanted Boss (45 s window)
        public bool Alive;         // a living normal monster
        public double HpShare;     // HP left / max, for a living monster
        public bool HitRecently;   // the hero damaged it in the last 10 s (living) or had a valid share of the kill (boss)
        public bool InRange;       // within 4 tiles
    }

    /// <summary>
    /// Catching monsters as pets (docs/pet-system-design.md 6-7). Normal balls: 500 gold at a merchant, 5 a day per account, 10 % from map bosses. Rare balls: one per 28 valid
    /// Wanted Boss claims, 100 % when the target is valid. A living monster is caught at 50 % HP or less, within 4 tiles, by someone who hit it in the last 10 s, with the odds
    /// of PetRules.LiveCaptureChance (UR never alive); a fallen boss in its window by someone with a valid share, one try per account per death, one boss pet a day; the
    /// collection level must allow the rarity. A miss costs the ball; a refusal costs nothing. Gold for balls moves through Outcome.GoldDelta once the account is stored.
    /// </summary>
    public static class PetCapture
    {
        public static AccountPets.Outcome BuyBalls(AccountState a, int n, int heroGold, long utcMs)
        {
            AccountRewards.Roll(a, utcMs);
            if (n <= 0) return AccountPets.Outcome.Fail("How many?");
            if (a.ballsBoughtToday + n > PetRules.BallsPerDay) return AccountPets.Outcome.Fail("Only " + (PetRules.BallsPerDay - a.ballsBoughtToday) + " more ball" + (PetRules.BallsPerDay - a.ballsBoughtToday == 1 ? "" : "s") + " today");
            int cost = n * PetRules.BallPrice;
            if (heroGold < cost) return AccountPets.Outcome.Fail("Needs " + cost + " gold");
            a.ballsBoughtToday += n;
            a.balls = Math.Min(AccountState.MaxCounter, a.balls + n);
            return AccountPets.Outcome.Ok("+" + n + " Pet Ball" + (n > 1 ? "s" : ""), -cost);
        }

        /// <summary>The chance a normal ball has on this target (0 = not catchable with one).</summary>
        public static double Chance(CaptureTarget t)
        {
            if (t.Pet == null || t.Pet.Legacy) return 0.0;
            if (t.Boss) return PetRules.BossCaptureChance(t.Pet.Rarity, t.Wanted);
            return t.Alive ? PetRules.LiveCaptureChance(t.Pet.Rarity, t.HpShare) : 0.0;
        }

        /// <summary>Null when a ball may be thrown now; else why not (nothing is spent on a refusal).</summary>
        public static string Refusal(AccountState a, CaptureTarget t, bool rare)
        {
            if (t.Pet == null || t.Pet.Legacy) return "That cannot become a pet";
            if (rare ? a.rareBalls <= 0 : a.balls <= 0) return rare ? "No Rare Pet Ball" : "No Pet Ball";
            if (!t.Boss && !t.Alive) return "Too late, it is gone";
            if (!t.InRange) return "Too far away";
            if (!t.HitRecently) return t.Boss ? "You had no part in that kill" : "Hit it first";
            if (!t.Boss && t.HpShare > PetRules.CaptureHpLine) return "Weaken it to half its HP first";
            if (a.CollectionLevel < PetRules.CollectionLevelFor(t.Pet.Rarity)) return "Your pet collection must reach level " + PetRules.CollectionLevelFor(t.Pet.Rarity) + " for " + t.Pet.Rarity;
            if (t.Boss && (t.Wanted ? a.dayWantedCaught : a.dayBossCaught)) return "One boss pet a day";
            if (!rare && Chance(t) <= 0.0) return "A Rare Pet Ball is needed for that";
            return null;
        }

        /// <summary>Throws a ball: the caller checked Refusal and rolled `roll` in [0, 1). A miss spends the ball; a catch also marks the day for bosses.</summary>
        public static CaptureOutcome Throw(AccountState a, CaptureTarget t, bool rare, double roll, long utcMs)
        {
            AccountRewards.Roll(a, utcMs);
            if (Refusal(a, t, rare) != null) return CaptureOutcome.Refused;
            if (rare) a.rareBalls--; else a.balls--;
            bool caught = rare || roll < Chance(t);
            if (!caught) return CaptureOutcome.Missed;
            if (!a.OwnsPet(t.Pet.Id) && a.pets.Count < AccountState.MaxPets) a.pets.Add(new OwnedPet { id = t.Pet.Id, stars = 1, level = 1 });
            else { OwnedPet p = a.Pet(t.Pet.Id); if (p != null) p.shards = Math.Min(AccountState.MaxCounter, p.shards + PetRules.DuplicateShards); }
            if (t.Boss) { if (t.Wanted) a.dayWantedCaught = true; else a.dayBossCaught = true; }
            return CaptureOutcome.Caught;
        }

        /// <summary>"Frost Slime caught!" / "+10 Frost Slime shards (already owned)".</summary>
        public static string CaughtText(AccountState a, PetDef d, bool wasOwned) { return wasOwned ? "+" + PetRules.DuplicateShards + " " + d.Name + " shards (already owned)" : d.Name + " caught! It joins your collection"; }
    }
}
