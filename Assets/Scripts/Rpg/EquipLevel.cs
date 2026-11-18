namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Hero level needed to wear a piece of gear, by art tier (itemLevel / 10): T0 Lv 1, then T(n) = n * 10 up to T9 Lv 90 = Leveling.MaxLevel
    /// (user decision 16/11/2026; until then weapon / off-hand only, 1/5/10/15/20/30/35/40/50/60). Every equipment slot carries it, pets do not.
    /// Enhancement never raises it. Mirrored by server-api rpg-constants.js EQUIP_LEVEL_BY_TIER.
    /// </summary>
    public static class EquipLevel
    {
        public static readonly int[] ByTier = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90 };

        /// <summary>Weapon, off-hand, helmet, armor, gloves and boots carry a requirement (even tier 0, Lv 1); pets are free to wear.</summary>
        public static bool Applies(Item item) { return item != null && item.slot != Slots.Pet && item.slot != Slots.Headgear && Slots.Index(item.slot) >= 0; } // hats: cosmetic, any level

        /// <summary>Required hero level of the item (1 = none).</summary>
        public static int Required(Item item) { return Applies(item) ? ByTier[ItemInfo.ArtTier(item)] : 1; }

        public static bool Meets(Item item, int heroLevel) { return heroLevel >= Required(item); }

        /// <summary>"Req. Lv 30" (empty for gear without a requirement).</summary>
        public static string Label(Item item) { return Applies(item) ? "Req. Lv " + Required(item) : ""; }

        /// <summary>
        /// Save load (SaveSerializer.Restore, offline and zone server alike): worn gear above the hero's level goes back to the bag, so a save from
        /// before the 16/11/2026 table never keeps wearing what it could not equip now. Nothing is ever lost: with a full bag the piece stays worn.
        /// Empty weapon / off-hand slots are refilled with the starter right after (EquipStarterGear). Returns how many pieces moved.
        /// </summary>
        public static int UnequipAboveLevel(PlayerState p)
        {
            int moved = 0;
            foreach (string slot in Slots.All)
            {
                Item worn = p.GetEquipped(slot);
                if (worn != null && !p.MeetsLevel(worn) && p.Unequip(slot).Ok) moved++;
            }
            return moved;
        }
    }
}
