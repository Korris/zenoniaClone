using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>One pull's outcome. Pet null = the shared-shard result.</summary>
    public struct PullResult { public PetDef Pet; public bool Duplicate; public int Shards; public bool ByPity; }

    /// <summary>
    /// The pet gacha with Ragnarok: Pocket Saga's table (docs/pet-system-design.md 4): a pull costs 1 coin; coins are bought with Pet Crystals (150 each, earned in play,
    /// never with real money) or come straight from rewards. Results: N 12 / R 24 / SR 36 / SSR 6.25 / UR 2.5 % and 19.25 % "5 shared shards"; pets of one rarity share its
    /// rate evenly; Legacy pets are never pulled. Pity per account: the 20th pull without an SSR or better is SSR+ (SSR 71.43 : UR 28.57), the 50th without a UR is a UR;
    /// an SSR+ resets the SSR counter, a UR both. New pet = 1 star; a duplicate = 10 shards of its kind. Every pull that is not a UR adds a UR shard; 120 buy Frostfang King.
    /// Online only the zone server rolls (RpgRandom.Secure). version 1 accounts (pity 10 / 30 / 60) keep their counters, clamped.
    /// </summary>
    public static class PetGacha
    {
        public static readonly double[] Rates = PetRules.GachaRates;

        /// <summary>
        /// Taurus V4 (24/11/2026): the old 8-pet pool is OFF in the live game (Pull / ExchangeUr refuse), only WorldFlags.LegacyEnabled opens it.
        /// TODO phase 08: replace the pool with the two Taurus pets only (Horn Rabbit N, Bramble Boar R: new `tau` ids, PetConfig defs + art) and drop this gate.
        /// Class companions (Spirit / Imp, PetDef.Legacy) are not part of the gacha and stay as they are.
        /// </summary>
        public static bool Open { get { return WorldFlags.LegacyEnabled; } }
        public const string ClosedText = "The pet gacha is closed until the Taurus pets arrive";

        /// <summary>The rarity of the next pull from the pity counters and one roll in [0, 1); null = the shard result.</summary>
        public static PetRarity? RollRarity(int pitySSR, int pityUR, double roll, out bool byPity)
        {
            byPity = true;
            if (pityUR >= PetRules.UrPity - 1) return PetRarity.UR;
            if (pitySSR >= PetRules.SsrPity - 1) return roll < Rates[4] / (Rates[3] + Rates[4]) ? PetRarity.UR : PetRarity.SSR;
            byPity = false;
            double acc = 0;
            for (int r = Rates.Length - 1; r >= 0; r--) { acc += Rates[r]; if (roll < acc) return (PetRarity)r; } // the rarest band sits at the bottom of the roll
            return null; // the remaining 19.25 %: shared shards
        }

        public static int CountOf(PetRarity r) { int n = 0; foreach (PetDef d in PetConfig.All) if (!d.Legacy && d.Rarity == r) n++; return n; }

        public static PetDef NthOf(PetRarity r, int i)
        {
            foreach (PetDef d in PetConfig.All) if (!d.Legacy && d.Rarity == r && i-- == 0) return d;
            return null;
        }

        /// <summary>The exact chance of one pet on a normal pull, for the published table (0 for Legacy).</summary>
        public static double PetRate(PetDef d) { return d.Legacy ? 0.0 : Rates[(int)d.Rarity] / CountOf(d.Rarity); }
        public static int PullsToSsr(AccountState a) { return PetRules.SsrPity - a.pitySSR; }
        public static int PullsToUr(AccountState a) { return PetRules.UrPity - a.pityUR; }

        /// <summary>Pet Crystals into coins: `n` coins cost n x 150.</summary>
        public static ActionResult BuyCoins(AccountState a, int n)
        {
            if (n <= 0 || n > 100) return ActionResult.Fail("1 to 100 coins");
            int cost = n * PetRules.CrystalsPerPull;
            if (a.crystals < cost) return ActionResult.Fail("Needs " + cost + " Pet Crystals");
            a.crystals -= cost;
            a.tickets = Math.Min(AccountState.MaxCounter, a.tickets + n);
            return ActionResult.Success("+" + n + " gacha coin" + (n > 1 ? "s" : ""));
        }

        /// <summary>One pull on `a` (no coin check: Pull does that). Two rolls: the band, then the pet in it.</summary>
        public static PullResult PullOne(AccountState a, Rand rng, long nowMs)
        {
            bool byPity;
            PetRarity? rarity = RollRarity(a.pitySSR, a.pityUR, rng(), out byPity);
            var result = new PullResult { ByPity = byPity };
            a.pitySSR = rarity.HasValue && rarity.Value >= PetRarity.SSR ? 0 : a.pitySSR + 1;
            a.pityUR = rarity.HasValue && rarity.Value == PetRarity.UR ? 0 : a.pityUR + 1;
            if (!rarity.HasValue || rarity.Value != PetRarity.UR) a.urShards = Math.Min(AccountState.MaxCounter, a.urShards + 1);
            a.totalPulls = Math.Min(AccountState.MaxCounter, a.totalPulls + 1);
            string id = "";
            if (!rarity.HasValue)
            {
                result.Shards = PetRules.SharedShardsPerPull;
                a.sharedShards = Math.Min(AccountState.MaxCounter, a.sharedShards + result.Shards);
            }
            else
            {
                int n = CountOf(rarity.Value);
                PetDef pet = NthOf(rarity.Value, Math.Min(n - 1, (int)(rng() * n)));
                result.Pet = pet; id = pet.Id;
                OwnedPet owned = a.Pet(pet.Id);
                if (owned != null) { result.Duplicate = true; result.Shards = PetRules.DuplicateShards; owned.shards = Math.Min(AccountState.MaxCounter, owned.shards + result.Shards); }
                else a.pets.Add(new OwnedPet { id = pet.Id, stars = 1, level = 1 });
            }
            a.history.Add(new PullRecord { at = nowMs, id = id, dup = result.Duplicate, shards = result.Shards, pitySSR = a.pitySSR, pityUR = a.pityUR });
            if (a.history.Count > PetRules.HistoryMax) a.history.RemoveRange(0, a.history.Count - PetRules.HistoryMax);
            return result;
        }

        /// <summary>x1 or x10: spends the coins, then pulls one by one (pity per pull). Fails and changes nothing without enough coins.</summary>
        public static ActionResult Pull(AccountState a, int count, Rand rng, long nowMs, List<PullResult> results)
        {
            if (!Open) return ActionResult.Fail(ClosedText);
            if (count != 1 && count != PetRules.MaxPullsAtOnce) return ActionResult.Fail("Pull 1 or 10");
            if (a.tickets < count) return ActionResult.Fail("Not enough gacha coins");
            a.tickets -= count;
            for (int i = 0; i < count; i++) results.Add(PullOne(a, rng, nowMs));
            return ActionResult.Success(Summary(results));
        }

        /// <summary>"Got Phoenix (SSR, new)" / "5 shared shards" for one, "10 pulls: 3 new, best SR" for ten. Under NetProtocol.MaxTextLength.</summary>
        public static string Summary(List<PullResult> results)
        {
            if (results.Count == 1)
            {
                PullResult r = results[0];
                if (r.Pet == null) return "+" + r.Shards + " shared pet shards";
                return "Got " + r.Pet.Name + " (" + r.Pet.Rarity + (r.Duplicate ? ", +" + r.Shards + " shards" : ", new") + ")";
            }
            int fresh = 0, shards = 0;
            PetRarity best = PetRarity.N;
            bool any = false;
            foreach (PullResult r in results)
            {
                if (r.Pet == null) { shards += r.Shards; continue; }
                any = true;
                if (!r.Duplicate) fresh++;
                if (r.Pet.Rarity > best) best = r.Pet.Rarity;
            }
            return results.Count + " pulls: " + fresh + " new" + (any ? ", best " + best : "") + (shards > 0 ? ", +" + shards + " shards" : "");
        }

        /// <summary>120 UR shards for Frostfang King, when the account does not own it.</summary>
        public static ActionResult ExchangeUr(AccountState a)
        {
            if (!Open) return ActionResult.Fail(ClosedText);
            PetDef d = PetConfig.Get(PetRules.UrExchangePet);
            if (a.OwnsPet(d.Id)) return ActionResult.Fail("Already owned");
            if (a.urShards < PetRules.UrExchangeCost) return ActionResult.Fail("Needs " + PetRules.UrExchangeCost + " UR shards");
            a.urShards -= PetRules.UrExchangeCost;
            a.pets.Add(new OwnedPet { id = d.Id, stars = 1, level = 1 });
            return ActionResult.Success("Exchanged for " + d.Name);
        }
    }
}
