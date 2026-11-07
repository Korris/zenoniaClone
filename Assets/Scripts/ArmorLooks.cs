namespace ZenoniaProto
{
    /// <summary>
    /// Outfit of a hero, from the worn body armour (designer's rule: the look comes only from the gear): one full body per armour art tier,
    /// each a matching set with the sword and shield of that tier and its own themed helmet (T0 = the class's own body, no helmet; T1..T9 =
    /// body/set-t1..set-t9; design: docs/reference/warrior-sets-10-tiers-themed-helmets.png).
    /// Each outfit is a repaint of the Lv1 rows with the same poses (Tools/build-warrior-outfit-sheets.py), so gear anchors and fists are shared;
    /// rows not painted yet are the Lv1 rows. Warrior only for now; tiers above the last painted outfit wear the last one.
    /// </summary>
    public static class ArmorLooks
    {
        // Index = armour art tier (ItemInfo.ArtTier = item level / 10); null = the class's own body.
        static readonly string[] WarriorOutfits =
        {
            null, "classes/warrior/body/set-t1", "classes/warrior/body/set-t2", "classes/warrior/body/set-t3", "classes/warrior/body/set-t4",
            "classes/warrior/body/set-t5", "classes/warrior/body/set-t6", "classes/warrior/body/set-t7", "classes/warrior/body/set-t8", "classes/warrior/body/set-t9",
        };

        static string[] Of(HeroClassDef cls) { return cls != null && cls.Id == HeroClass.Warrior ? WarriorOutfits : null; }

        /// <summary>Outfit index for a worn armour art tier (-1 = nothing worn): 0 = the class's own body.</summary>
        public static int OutfitIndex(HeroClassDef cls, int armorTier)
        {
            string[] o = Of(cls);
            if (o == null || armorTier <= 0) return 0;
            return armorTier < o.Length ? armorTier : o.Length - 1;
        }

        /// <summary>Resources folder (under Sprites/) holding bare/ and hand/ of that outfit, null for the class's own body.</summary>
        public static string OutfitFolder(HeroClassDef cls, int index)
        {
            string[] o = Of(cls);
            return o != null && index > 0 && index < o.Length ? o[index] : null;
        }

        /// <summary>Art tier of a worn piece, -1 when the slot is empty.</summary>
        public static int TierOf(Rpg.Item item) { return item != null ? Rpg.ItemInfo.ArtTier(item) : -1; }

        /// <summary>Dresses a paper doll in the outfit of a player's worn body armour.</summary>
        public static void FromWorn(HeroRenderer view, Rpg.PlayerState p) { view.SetOutfit(TierOf(p.GetEquipped(Rpg.Slots.Armor))); }
    }
}
