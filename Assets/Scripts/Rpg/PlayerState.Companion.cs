using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// PlayerState: the hero's pet team (PetTeam, SaveData.petTeam): the Combat slot walks and fights, two Support slots cast at half strength, two Stat slots give a quarter
    /// of their passive. The pets, their levels and stars belong to the account (AccountState, shared by every hero of it). Account is null in bare rule tests: then
    /// every team pet counts as 1 star, level 1.
    /// </summary>
    public sealed partial class PlayerState
    {
        /// <summary>The five slots (PetSlot order), "" = empty. Only pets the account owns once an account is attached.</summary>
        public string[] Team = PetTeam.Empty();
        /// <summary>The account of this hero (shared object, changed in place by AccountState.CopyFrom). Null until attached.</summary>
        public AccountState Account;
        /// <summary>What the team gives (PetTeam.Bonus), recomputed with the stats.</summary>
        public PetBonus PetBonus;

        /// <summary>Id of the combat pet ("" = none): what walks beside the hero.</summary>
        public string PetId { get { return Team[(int)PetSlot.Combat] ?? ""; } set { Team[(int)PetSlot.Combat] = value ?? ""; } }

        /// <summary>The combat pet's definition, null for none.</summary>
        public PetDef Pet { get { return PetConfig.Get(PetId); } }

        /// <summary>Stars / level of a pet: the account's, 1 without an account, 0 when not in the team or unknown.</summary>
        public int PetStars { get { PetDef d = Pet; return d == null ? 0 : Account != null ? System.Math.Max(1, Account.StarsOf(d.Id)) : 1; } }
        public int PetLevel { get { PetDef d = Pet; return d == null ? 0 : Account != null ? System.Math.Max(1, Account.LevelOf(d.Id)) : 1; } }

        /// <summary>The team's passive of this kind (capped sum over slots and Bonds; 0 without pets).</summary>
        public double PetPassive(PetPassive kind) { return PetBonus.Of(kind); }

        /// <summary>The pet in a support slot, null for an empty one.</summary>
        public PetDef SupportPet(int which) { return PetConfig.Get(Team[(int)PetSlot.Support1 + which]); }

        /// <summary>Puts an owned pet in a slot ("" clears it). Recomputes stats.</summary>
        public ActionResult SetTeamSlot(int slot, string id)
        {
            string why = PetTeam.Refusal(Team, slot, id, Level, Account);
            if (why != null) return ActionResult.Fail(why);
            if (Team[slot] == (id ?? "")) return ActionResult.Fail(string.IsNullOrEmpty(id) ? "That slot is empty" : PetConfig.Get(id).Name + " is there already");
            PetTeam.Set(Team, slot, id);
            RecomputeStats();
            PetDef d = PetConfig.Get(id);
            return ActionResult.Success(d != null ? (slot == 0 ? d.Name + " joins you" : d.Name + " set") : "Slot cleared");
        }

        /// <summary>Old call: the combat slot.</summary>
        public ActionResult TakePet(string id) { return SetTeamSlot((int)PetSlot.Combat, id); }

        /// <summary>Binds the account: team pets it does not own (stale save, other device) are dropped. Recomputes stats.</summary>
        public void AttachAccount(AccountState account)
        {
            Account = account;
            PetTeam.Sanitize(Team, Level, account);
            RecomputeStats();
        }

        /// <summary>Called by RecomputeStats before it reads PetPassive.</summary>
        void RecomputePetBonus() { PetBonus = PetTeam.Bonus(Team, Account); }

        /// <summary>Old durability pets (items) still in the bag or the old pet slot: what a save from before Pet Companion carries until AccountPets.Migrate runs.</summary>
        public List<Item> LegacyPets()
        {
            var list = new List<Item>();
            Item worn = GetEquipped(Slots.Pet);
            if (worn != null) list.Add(worn);
            foreach (Item i in Inventory) if (i.slot == Slots.Pet) list.Add(i);
            return list;
        }

        /// <summary>The kind of the old pet worn in the old pet slot, null for none.</summary>
        public PetDef LegacyWornPet { get { return PetConfig.Of(GetEquipped(Slots.Pet)); } }

        /// <summary>Removes every old pet item (after the account has stored what they became).</summary>
        public void RemoveLegacyPets()
        {
            SetEquippedRaw(Slots.Pet, null);
            Inventory.RemoveAll(i => i.slot == Slots.Pet);
            RecomputeStats();
        }
    }
}
