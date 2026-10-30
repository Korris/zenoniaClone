namespace ZenoniaProto.Rpg
{
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

        // ---- consumables ----

        /// <summary>Drinks a potion only when it can restore something. Heals 30% of max.</summary>
        public ActionResult UsePotion(bool hp)
        {
            int count = hp ? HpPotions : MpPotions;
            if (count <= 0) return ActionResult.Fail("No potion");
            float cur = hp ? Hp : Mp, max = hp ? MaxHp : MaxMp;
            if (cur >= max) return ActionResult.Fail(hp ? "HP full" : "MP full");
            float v = System.Math.Min(max, cur + RpgMath.Round(max * PotionHealFraction));
            if (hp) { Hp = v; HpPotions--; } else { Mp = v; MpPotions--; }
            return ActionResult.Success((hp ? "HP +" : "MP +") + RpgMath.Round(max * PotionHealFraction));
        }

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
