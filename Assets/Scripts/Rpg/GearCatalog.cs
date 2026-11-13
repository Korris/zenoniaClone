using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Who sells basic gear and where (same shape as PotionShops / PetConfig): a blacksmith's tier range, map and reach check.
    /// One catalog entry = (tier, slot), encoded as tier * Slots.Generated.Length + slotIndex -- the byte InvOp.GearBuy's Slot carries, the same
    /// way PotionBuy packs a size and PetBuy an index. World map step 1 (plans/261113-0106-world-map-step1): smithBram (Oakhaven) T0-T1,
    /// smithHolt (Thatchwick) T2-T3, blacksmithKhaled (Sunspire) T4-T5. Later villages (docs/world-map-2-design.md) add higher tiers the same way.
    /// </summary>
    public static class GearShops
    {
        public const int Tiers = 10; // ItemDefs.EquipmentTier range (0..9)
        public const int EntryCount = Tiers * 6; // 6 = Slots.Generated.Length, pinned so a slot reorder cannot silently shift the wire format

        /// <summary>The map a seller works on; null = this villager sells no gear.</summary>
        public static string MapOf(string npcId)
        {
            switch (npcId)
            {
                case "smithBram": return PotionShops.Oakhaven;
                case "smithHolt": return PotionShops.Thatchwick;
                case "blacksmithKhaled": return PotionShops.Sunspire;
                default: return null;
            }
        }

        /// <summary>Inclusive tier range this seller stocks (every Slots.Generated piece at each of those tiers).</summary>
        public static bool TierRange(string npcId, out int minTier, out int maxTier)
        {
            switch (npcId)
            {
                case "smithBram": minTier = 0; maxTier = 1; return true;
                case "smithHolt": minTier = 2; maxTier = 3; return true;
                case "blacksmithKhaled": minTier = 4; maxTier = 5; return true;
                default: minTier = maxTier = 0; return false;
            }
        }

        public static bool IsSeller(string npcId) { return MapOf(npcId) != null; }

        /// <summary>entryIndex -> (tier, slotIndex); false when the index is out of range (nothing was decoded).</summary>
        public static bool Decode(int entryIndex, out int tier, out int slotIndex)
        {
            if (entryIndex < 0 || entryIndex >= EntryCount) { tier = slotIndex = 0; return false; }
            tier = entryIndex / 6;
            slotIndex = entryIndex % 6;
            return true;
        }

        public static int Encode(int tier, int slotIndex) { return tier * 6 + slotIndex; }

        public static bool Sells(string npcId, int entryIndex)
        {
            int tier, slotIndex, minTier, maxTier;
            return Decode(entryIndex, out tier, out slotIndex) && TierRange(npcId, out minTier, out maxTier) && tier >= minTier && tier <= maxTier;
        }

        /// <summary>Null when this hero may buy: the seller stocks this entry, works on the hero's map and stands in reach.</summary>
        public static string Refusal(string npcId, string mapId, int entryIndex, float distance)
        {
            if (!IsSeller(npcId) || MapOf(npcId) != mapId) return "Nobody sells that here";
            if (!Sells(npcId, entryIndex)) return "Not sold here";
            if (distance > PotionShops.ReachUnits) return "Too far from the shop";
            return null;
        }
    }

    /// <summary>
    /// Generates and prices one basic-gear entry (tier, slot): always +0, common rarity, the tier's lowest item level, so a farmed or traded
    /// piece (enhanced, a better rarity, a higher roll) always keeps an edge over what the shop sells.
    /// </summary>
    public static class GearCatalog
    {
        /// <summary>Lowest itemLevel of an art tier (ItemDefs.EquipmentTier: itemLevel / 10): 1, 10, 20 ... 90.</summary>
        public static int FloorItemLevel(int tier) { return tier <= 0 ? 1 : tier * 10; }

        // Gold price: a per-tier base (quadratic, gentle at low tiers where starter gear already covers weapon/offhand) times a per-slot
        // weight (weapon costliest, jewellery/gloves/boots cheapest). Tuned against the existing economy (LootTable gold drops, PotionCatalog
        // prices); always increases with tier, revisit once higher villages (docs/world-map-2-design.md) need real numbers.
        static readonly double[] SlotWeight = { 1.0, 0.4, 0.5, 0.35, 0.35, 0.85 }; // Weapon, Helmet, Armor, Gloves, Boots, Offhand (Slots.Generated order)

        public static int Price(int tier, int slotIndex)
        {
            double basePrice = 20.0 * (tier + 1) * (tier + 1);
            return Math.Max(5, RpgMath.Round(basePrice * SlotWeight[slotIndex]));
        }

        /// <summary>
        /// The item this entry sells for `classId` (only weapon / offhand carry a class, same as ItemFactory.CreateStarterItem). Deterministic:
        /// the same tier + slot + class always names and rolls the same piece, so the shop row and the purchase never disagree.
        /// </summary>
        public static Item Preview(int tier, int slotIndex, string classId)
        {
            string slot = Slots.Generated[slotIndex];
            return ItemFactory.Generate(() => 0.0, FloorItemLevel(tier), slot, "common", classId);
        }

        /// <summary>A fresh instance of Preview() with its own uid (rand only touches the uid, never the template or stats).</summary>
        public static Item Buy(int tier, int slotIndex, string classId, Rand rand)
        {
            Item item = Preview(tier, slotIndex, classId);
            item.uid = item.templateId + "-shop" + (long)Math.Floor(rand() * 1e9);
            return item;
        }
    }
}
