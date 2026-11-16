using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Raising a pet (docs/pet-system-design.md 5): levels 1-60 with Pet Food (2 + level / 10 a level, never above the account's highest hero level), stars 1-5 with shards of
    /// the kind + gold (10 / 20 / 30 / 40 shards, 5k / 10k / 20k / 30k gold), always successful; shared shards turn into a kind's shards 1 : 1; a reset gives back exactly what the
    /// ledger on the pet says was spent (food, shards, gold), free, any number of times. Gold moves through AccountPets.Outcome.GoldDelta once the account is stored.
    /// </summary>
    public static class PetGrowth
    {
        /// <summary>One level with Pet Food. `heroTop` = the account's highest hero level, the pet's ceiling.</summary>
        public static AccountPets.Outcome LevelUp(AccountState a, string id, int heroTop)
        {
            OwnedPet p = a.Pet(id);
            PetDef d = PetConfig.Get(id);
            if (p == null || d == null) return AccountPets.Outcome.Fail("You do not own that pet");
            if (p.level >= PetRules.MaxLevel) return AccountPets.Outcome.Fail(d.Name + " is at the top level");
            if (p.level >= heroTop) return AccountPets.Outcome.Fail("A pet cannot outlevel your best hero (" + heroTop + ")");
            int food = PetRules.FoodForLevel(p.level);
            if (a.food < food) return AccountPets.Outcome.Fail("Needs " + food + " Pet Food");
            a.food -= food;
            p.foodSpent = Math.Min(AccountState.MaxCounter, p.foodSpent + food);
            p.level++;
            return AccountPets.Outcome.Ok(d.Name + " reached level " + p.level);
        }

        /// <summary>Shards and gold the next star costs; false at 5 stars or not owned.</summary>
        public static bool NextStarCost(AccountState a, string id, out int shards, out int gold)
        {
            shards = gold = 0;
            OwnedPet p = a.Pet(id);
            if (p == null || p.stars >= PetRules.MaxStars) return false;
            shards = PetRules.StarShards[p.stars - 1];
            gold = PetRules.StarGold[p.stars - 1];
            return true;
        }

        /// <summary>One star up, always succeeds when paid: shards of the same kind from the account, gold from the hero.</summary>
        public static AccountPets.Outcome StarUp(AccountState a, string id, int heroGold)
        {
            PetDef d = PetConfig.Get(id);
            OwnedPet p = a.Pet(id);
            if (d == null || p == null) return AccountPets.Outcome.Fail("You do not own that pet");
            int shards, cost;
            if (!NextStarCost(a, id, out shards, out cost)) return AccountPets.Outcome.Fail(d.Name + " has the most stars");
            if (p.shards < shards) return AccountPets.Outcome.Fail("Needs " + shards + " " + d.Name + " shards");
            if (heroGold < cost) return AccountPets.Outcome.Fail("Needs " + cost + " gold");
            p.shards -= shards;
            p.shardsSpent = Math.Min(AccountState.MaxCounter, p.shardsSpent + shards);
            p.goldSpent = Math.Min(AccountState.MaxCounter, p.goldSpent + cost);
            p.stars++;
            return AccountPets.Outcome.Ok(d.Name + " is now " + p.stars + " stars", -cost);
        }

        /// <summary>Shared shards (from the gacha's shard result) become `n` shards of one owned kind.</summary>
        public static AccountPets.Outcome Convert(AccountState a, string id, int n)
        {
            OwnedPet p = a.Pet(id);
            PetDef d = PetConfig.Get(id);
            if (p == null || d == null) return AccountPets.Outcome.Fail("You do not own that pet");
            if (n <= 0 || a.sharedShards < n) return AccountPets.Outcome.Fail("Needs " + Math.Max(1, n) + " shared shards");
            a.sharedShards -= n;
            p.shards = Math.Min(AccountState.MaxCounter, p.shards + n);
            return AccountPets.Outcome.Ok("+" + n + " " + d.Name + " shards");
        }

        /// <summary>Back to level 1, every Pet Food spent returned.</summary>
        public static AccountPets.Outcome ResetLevel(AccountState a, string id)
        {
            OwnedPet p = a.Pet(id);
            PetDef d = PetConfig.Get(id);
            if (p == null || d == null) return AccountPets.Outcome.Fail("You do not own that pet");
            if (p.level <= 1) return AccountPets.Outcome.Fail(d.Name + " is level 1 already");
            int back = p.foodSpent;
            a.food = Math.Min(AccountState.MaxCounter, a.food + back);
            p.foodSpent = 0; p.level = 1;
            return AccountPets.Outcome.Ok(d.Name + " reset to level 1, +" + back + " Pet Food back");
        }

        /// <summary>Back to 1 star, every shard and all the gold spent returned.</summary>
        public static AccountPets.Outcome ResetStars(AccountState a, string id)
        {
            OwnedPet p = a.Pet(id);
            PetDef d = PetConfig.Get(id);
            if (p == null || d == null) return AccountPets.Outcome.Fail("You do not own that pet");
            if (p.stars <= 1) return AccountPets.Outcome.Fail(d.Name + " is 1 star already");
            int shards = p.shardsSpent, gold = p.goldSpent;
            p.shards = Math.Min(AccountState.MaxCounter, p.shards + shards);
            p.shardsSpent = 0; p.goldSpent = 0; p.stars = 1;
            return AccountPets.Outcome.Ok(d.Name + " reset to 1 star, +" + shards + " shards and +" + gold + " G back", gold);
        }
    }
}
