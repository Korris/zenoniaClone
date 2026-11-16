using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Account-side pet ownership (PetConfig), engine-free. Each works on an AccountState COPY and says what the hero's gold must do once the account is stored
    /// (GoldDelta: negative = pay, positive = compensation / refund), so the account write and the gold change happen together or not at all. Growth (levels, stars,
    /// resets) lives in PetGrowth, the gacha in PetGacha, catching in PetCapture, the team in PetTeam.
    /// </summary>
    public static class AccountPets
    {
        public struct Outcome
        {
            public ActionResult Result;
            public int GoldDelta;
            public static Outcome Fail(string text) { return new Outcome { Result = ActionResult.Fail(text) }; }
            public static Outcome Ok(string text, int gold = 0) { return new Outcome { Result = ActionResult.Success(text), GoldDelta = gold }; }
        }

        /// <summary>Adds a species at 1 star, level 1 (nothing when owned or the account is full).</summary>
        public static void Add(AccountState a, string id) { if (!a.OwnsPet(id) && a.pets.Count < AccountState.MaxPets) a.pets.Add(new OwnedPet { id = id, stars = 1, level = 1 }); }

        /// <summary>Pets unlocked by level (Spirit at 5) for a hero at `level`. Fails when nothing new unlocks.</summary>
        public static Outcome UnlockByLevel(AccountState a, int level)
        {
            var names = new List<string>();
            foreach (PetDef d in PetConfig.All)
                if (d.UnlockLevel > 0 && level >= d.UnlockLevel && !a.OwnsPet(d.Id)) { Add(a, d.Id); names.Add(d.Name); }
            return names.Count > 0 ? Outcome.Ok(string.Join(", ", names.ToArray()) + " unlocked (level " + level + ")") : Outcome.Fail("");
        }

        /// <summary>Buys a pet that is for sale, once per account. Whether this seller may sell it is the caller's check (PetConfig.Refusal).</summary>
        public static Outcome Buy(AccountState a, PetDef d, int gold)
        {
            if (d == null || d.Price <= 0) return Outcome.Fail("Not sold here");
            if (a.OwnsPet(d.Id)) return Outcome.Fail("You already own " + d.Name);
            if (gold < d.Price) return Outcome.Fail("Not enough gold");
            Add(a, d.Id);
            return Outcome.Ok(d.Name + " unlocked for every hero", -d.Price);
        }

        /// <summary>Kept for the old call sites: the next star (PetGrowth).</summary>
        public static bool NextStarCost(AccountState a, string id, out int shards, out int gold) { return PetGrowth.NextStarCost(a, id, out shards, out gold); }
        public static Outcome StarUp(AccountState a, string id, int gold) { return PetGrowth.StarUp(a, id, gold); }

        /// <summary>
        /// Old durability pets become account pets: each kind is unlocked at 1 star (if not yet owned) and, once per kind per account, paid 10 shards of that kind
        /// + 3000 gold (its bonus went down). Fails when there is nothing to migrate. The caller removes the items and puts `worn` in the team after the account is stored.
        /// </summary>
        public static Outcome Migrate(AccountState a, List<Item> legacy)
        {
            int gold = 0;
            var names = new List<string>();
            foreach (Item i in legacy)
            {
                PetDef d = PetConfig.Of(i);
                if (d == null || names.Contains(d.Name)) continue;
                names.Add(d.Name);
                Add(a, d.Id);
                if (a.compensated.Contains(d.Id)) continue;
                a.compensated.Add(d.Id);
                OwnedPet p = a.Pet(d.Id);
                if (p != null) p.shards = Math.Min(AccountState.MaxCounter, p.shards + PetConfig.CompensationShards);
                gold += PetConfig.CompensationGold;
            }
            if (names.Count == 0 && legacy.Count == 0) return Outcome.Fail("");
            string text = names.Count > 0 ? string.Join(", ", names.ToArray()) + " now belong to your account for good" : "Old pets cleared";
            return Outcome.Ok(gold > 0 ? text + " (+" + gold + " G compensation)" : text, gold);
        }
    }
}
