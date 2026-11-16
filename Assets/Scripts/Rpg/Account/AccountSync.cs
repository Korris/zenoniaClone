using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The account changes due when a hero arrives or levels up, gathered in one write: old pets migrated (+ compensation), the starter tickets, pets unlocked by level
    /// (Spirit at 5), level milestone tickets. Works on an AccountState COPY; GoldDelta goes to the hero once stored, and WearLegacy says which migrated pet it wears then.
    /// </summary>
    public static class AccountSync
    {
        public struct Plan
        {
            public bool Changed;
            public int GoldDelta;
            public string Text;
            /// <summary>The old pet worn in the old pet slot: the hero takes this account pet along after the write.</summary>
            public string WearLegacy;
            /// <summary>Old pet items to remove from the hero after the write.</summary>
            public bool RemoveLegacy;
        }

        public static Plan OnJoin(AccountState a, PlayerState p, long nowMs)
        {
            var plan = new Plan();
            var texts = new List<string>();
            List<Item> legacy = p.LegacyPets();
            if (legacy.Count > 0)
            {
                PetDef worn = p.LegacyWornPet;
                Note(AccountPets.Migrate(a, legacy), ref plan, texts);
                plan.RemoveLegacy = true;
                plan.Changed = true;
                plan.WearLegacy = worn != null ? worn.Id : null;
            }
            Note(AccountRewards.GrantStarter(a), ref plan, texts);
            Collect(OnLevel(a, p.Level), ref plan, texts);
            int day = a.rewardDay, week = a.rewardWeek;
            AccountRewards.Roll(a, nowMs);
            if (a.rewardDay != day || a.rewardWeek != week) plan.Changed = true;
            plan.Text = string.Join(". ", texts.ToArray());
            return plan;
        }

        /// <summary>Level reached (also on join): Spirit at 5, 2 tickets per new milestone of 10.</summary>
        public static Plan OnLevel(AccountState a, int level)
        {
            var plan = new Plan();
            var texts = new List<string>();
            Note(AccountPets.UnlockByLevel(a, level), ref plan, texts);
            Note(AccountRewards.OnLevel(a, level), ref plan, texts);
            plan.Text = string.Join(". ", texts.ToArray());
            return plan;
        }

        static void Note(AccountPets.Outcome o, ref Plan plan, List<string> texts)
        {
            if (!o.Result.Ok) return;
            plan.Changed = true;
            plan.GoldDelta += o.GoldDelta;
            if (!string.IsNullOrEmpty(o.Result.Text)) texts.Add(o.Result.Text);
        }

        static void Note(ActionResult r, ref Plan plan, List<string> texts) { Note(new AccountPets.Outcome { Result = r }, ref plan, texts); }

        static void Collect(Plan inner, ref Plan plan, List<string> texts)
        {
            if (!inner.Changed) return;
            plan.Changed = true;
            plan.GoldDelta += inner.GoldDelta;
            if (!string.IsNullOrEmpty(inner.Text)) texts.Add(inner.Text);
        }
    }
}
