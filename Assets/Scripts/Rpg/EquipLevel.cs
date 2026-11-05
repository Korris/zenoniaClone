namespace ZenoniaProto.Rpg
{
    /// <summary>Hero level needed to wear a weapon / off-hand, by art tier (itemLevel / 10). Armor and pets have no requirement. Enhancement does not raise it.</summary>
    public static class EquipLevel
    {
        public static readonly int[] ByTier = { 1, 5, 10, 15, 20, 30, 35, 40, 50, 60 };

        /// <summary>Weapons and off-hands carry a requirement (even tier 0, Lv 1); everything else is free to wear.</summary>
        public static bool Applies(Item item) { return item != null && (item.slot == Slots.Weapon || item.slot == Slots.Offhand); }

        /// <summary>Required hero level of the item (1 = none).</summary>
        public static int Required(Item item) { return Applies(item) ? ByTier[ItemInfo.ArtTier(item)] : 1; }

        public static bool Meets(Item item, int heroLevel) { return heroLevel >= Required(item); }

        /// <summary>"Req. Lv 30" (empty for gear without a requirement).</summary>
        public static string Label(Item item) { return Applies(item) ? "Req. Lv " + Required(item) : ""; }
    }
}
