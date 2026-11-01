namespace ZenoniaProto.Rpg
{
    /// <summary>What one drain did to the worn pet: nothing to tell, it just crossed the warning line, or it is gone.</summary>
    public enum PetWear { None, Low, WornOut }

    /// <summary>PlayerState: bag, equipment slots, starter gear, consumables and pickups.</summary>
    public sealed partial class PlayerState
    {
        // ---- bag + equipment ----

        /// <summary>Class gear (weapon / offhand carry classId) only fits its own class; armor fits everyone.</summary>
        public bool CanEquip(Item item) { return string.IsNullOrEmpty(item.classId) || item.classId == ClassId; }

        public bool AddToInventory(Item item)
        {
            if (Inventory.Count >= InventorySize) return false;
            Inventory.Add(item);
            return true;
        }

        public bool RemoveFromInventory(string uid)
        {
            int i = Inventory.FindIndex(x => x.uid == uid);
            if (i < 0) return false;
            Inventory.RemoveAt(i);
            return true;
        }

        /// <summary>Equips an item (from the bag or fresh), swapping the previous one into the bag. Recomputes stats on success.</summary>
        public ActionResult Equip(Item item)
        {
            int slot = Slots.Index(item.slot);
            if (slot < 0) return ActionResult.Fail("Invalid slot");
            if (!CanEquip(item)) return ActionResult.Fail("Wrong class");
            bool inBag = Inventory.Exists(i => i.uid == item.uid);
            Item previous = equipment[slot];
            int after = Inventory.Count - (inBag ? 1 : 0) + (previous != null ? 1 : 0);
            if (after > InventorySize) return ActionResult.Fail("Inventory full");
            if (inBag) RemoveFromInventory(item.uid);
            equipment[slot] = item;
            if (previous != null) Inventory.Add(previous);
            RecomputeStats();
            return ActionResult.Success();
        }

        /// <summary>Unequips a slot back into the bag (fails when the bag is full).</summary>
        public ActionResult Unequip(string slot)
        {
            int i = Slots.Index(slot);
            if (i < 0 || equipment[i] == null) return ActionResult.Fail("Empty slot");
            if (Inventory.Count >= InventorySize) return ActionResult.Fail("Inventory full");
            Inventory.Add(equipment[i]);
            equipment[i] = null;
            RecomputeStats();
            return ActionResult.Success();
        }

        /// <summary>Fills EMPTY weapon / offhand slots with the class starter (never replaces gear, never duplicates a starter in the bag).</summary>
        public bool EquipStarterGear()
        {
            bool changed = false;
            foreach (string slot in new[] { Slots.Weapon, Slots.Offhand })
            {
                if (GetEquipped(slot) != null || Inventory.Exists(i => i.uid == "starter-" + slot)) continue;
                SetEquippedRaw(slot, ItemFactory.CreateStarterItem(slot, ClassId));
                changed = true;
            }
            if (changed) RecomputeStats();
            return changed;
        }

        long petAttackWindowEndsMs;

        /// <summary>A monster hit this hero, who survives it. Only a pet that drains on hits taken (the spirit) loses anything. The caller checks the hero is alive.</summary>
        public PetWear PetHitTaken()
        {
            PetDef pet = PetConfig.Of(GetEquipped(Slots.Pet));
            return pet != null ? DrainPet(pet.DrainPerHitTaken) : PetWear.None;
        }

        /// <summary>This hero landed a hit. Only a pet that drains on attacks (the imp) loses anything, and only once per attack event (PetConfig.AttackWindowMs). The caller checks the hero is alive.</summary>
        public PetWear PetAttackLanded(long nowMs)
        {
            PetDef pet = PetConfig.Of(GetEquipped(Slots.Pet));
            if (pet == null || pet.DrainPerAttack <= 0 || nowMs < petAttackWindowEndsMs) return PetWear.None;
            petAttackWindowEndsMs = nowMs + PetConfig.AttackWindowMs;
            return DrainPet(pet.DrainPerAttack);
        }

        PetWear DrainPet(int amount)
        {
            if (amount <= 0) return PetWear.None;
            Item worn = GetEquipped(Slots.Pet);
            int before = worn.durability;
            worn.durability = System.Math.Max(0, before - amount);
            if (worn.durability > 0) return PetConfig.IsLow(worn.durability) && !PetConfig.IsLow(before) ? PetWear.Low : PetWear.None;
            SetEquippedRaw(Slots.Pet, null); // gone for good: it is in no list any more, so no save can bring it back
            RecomputeStats(); // the bonus stops now, and HP comes down to the new maximum
            return PetWear.WornOut;
        }

        /// <summary>How many of this pet the hero has: in the bag and worn.</summary>
        public int PetCount(PetDef def)
        {
            int n = PetConfig.Of(GetEquipped(Slots.Pet)) == def ? 1 : 0;
            foreach (Item i in Inventory) if (PetConfig.Of(i) == def) n++;
            return n;
        }

        /// <summary>Pays and puts a new pet in the bag. Whether this seller may sell it is the caller's check (PetConfig.Refusal).</summary>
        public ActionResult BuyPet(PetDef def, Rand rand = null)
        {
            if (def == null) return ActionResult.Fail("Not sold here");
            if (Inventory.Count >= InventorySize) return ActionResult.Fail("Inventory full");
            if (Gold < def.Price) return ActionResult.Fail("Not enough gold");
            Item pet;
            do pet = ItemFactory.CreatePet(def, rand ?? RpgRandom.Default);
            while (Inventory.Exists(i => i.uid == pet.uid) || (GetEquipped(Slots.Pet) != null && GetEquipped(Slots.Pet).uid == pet.uid));
            Gold -= def.Price;
            Inventory.Add(pet);
            return ActionResult.Success("Bought " + def.Name);
        }

        // ---- consumables ----

        /// <summary>Applies a picked-up drop (web applyPickup). Equipment fails with "Inventory full" when the bag has no room.</summary>
        public ActionResult ApplyPickup(LootEntry drop)
        {
            switch (drop.Kind)
            {
                case LootKind.Coin: Gold += drop.Value; return ActionResult.Success("+" + drop.Value + " G");
                case LootKind.HpPotion: HpPotions++; return ActionResult.Success("+1 HP Potion");
                case LootKind.MpPotion: MpPotions++; return ActionResult.Success("+1 MP Potion");
                case LootKind.UpgradeStone:
                    int n = System.Math.Max(1, drop.Value);
                    UpgradeStone = System.Math.Min(MaxStones, UpgradeStone + n);
                    return ActionResult.Success("+" + n + " Upgrade Stone" + (n > 1 ? "s" : ""));
                case LootKind.Equipment:
                    if (drop.Item == null) return ActionResult.Fail("");
                    return AddToInventory(drop.Item) ? ActionResult.Success(EnhanceStats.DisplayName(drop.Item)) : ActionResult.Fail("Inventory full");
                default: return ActionResult.Fail("");
            }
        }
    }
}
