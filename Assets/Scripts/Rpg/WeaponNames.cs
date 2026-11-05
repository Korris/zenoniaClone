using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The one place that names weapons and off-hands: class -> 10 art tiers (ItemInfo.ArtTier, itemLevel / 10), in the order of the art
    /// (icons weapon-&lt;class&gt;-&lt;tier&gt; / offhand-&lt;class&gt;-&lt;tier&gt; and the held-*.json manifests). Rarity is only a prefix ("Legendary ..."), see FullName.
    /// Edit names here; client and server both read this table, so no protocol change is needed. Keep names short enough for the 40-char item name with the longest prefix.
    /// </summary>
    public static class WeaponNames
    {
        public const int Tiers = 10;

        public static readonly Dictionary<string, string[]> Weapons = new Dictionary<string, string[]>
        {
            { "warrior", new[] { "Lightning Blade", "Star Lance", "Crimson Blade", "Balrog", "Frost Blade", "Thunder Hammer", "Chaos Axe", "Dark Reaper", "Divine Sword", "Dragon Slayer" } },
            { "wizard", new[] { "Apprentice Staff", "Woodland Crescent", "Crystal Lance Staff", "Moonlit Scepter", "Arcane Spire", "Inferno Staff", "Glacier Spire", "Verdant Bloom Staff", "Seraph Wing Staff", "Void Hex Staff" } },
            { "mechanic", new[] { "Scrap Repeater", "Brass Duelist", "Copper Hammerlock", "Plasma Revolver", "Ember Hellfire", "Frost Shardgun", "Volt Arc Pistol", "Gatling Sidearm", "Starfall Magnum", "Dragonfire Twin" } },
            { "assassin", new[] { "Nightfall Kunai", "Gilded Talons", "Amethyst Rapier", "Serpent's Tooth", "Glacier Twinblade", "Inferno Saber", "Stormring Blade", "Skull Sickle", "Crescent Moonblade", "Voidreaver Blade" } },
            { "cleric", new[] { "Acolyte Rod", "Blossom Scepter", "Dawnlight Staff", "Seraph Cross Staff", "Moonlit Crescent Staff", "Radiant Sun Staff", "Halo Wand", "Verdant Laurel Staff", "Celestial Orb Staff", "Archangel Staff" } },
            { "fighter", new[] { "Leather Gloves", "Iron Knuckles", "Bronze Fists", "Tiger Claws", "Flame Gauntlets", "Frost Gauntlets", "Thunder Fists", "Shadow Gauntlets", "Holy Gauntlets", "Dragon Gauntlets" } },
        };

        public static readonly Dictionary<string, string[]> Offhands = new Dictionary<string, string[]>
        {
            { "warrior", new[] { "Lightning Shield", "Star Shield", "Crimson Shield", "Balrog Shield", "Frost Shield", "Thunder Shield", "Chaos Shield", "Reaper Shield", "Divine Shield", "Dragon Shield" } },
            { "wizard", new[] { "Apprentice Tome", "Woodland Codex", "Crystal Grimoire", "Moonlit Almanac", "Arcane Grimoire", "Inferno Codex", "Glacier Tome", "Verdant Herbal", "Holy Scripture", "Void Codex" } },
            { "mechanic", new[] { "Scrap Cannon", "Brass Mortar", "Copper Bombard", "Plasma Thunderer", "Ember Blaster", "Frost Railgun", "Volt Coil Cannon", "Gatling Barrage", "Star Lance Cannon", "Dragon Maw Cannon" } },
            { "assassin", new[] { "Nightfall Dirk", "Gilded Talon", "Amethyst Stiletto", "Serpent Fang", "Glacier Splinter", "Inferno Kukri", "Stormring Chakram", "Skull Scythe", "Moonlit Sickle", "Void Fang" } },
            { "cleric", new[] { "Acolyte Crest", "Blossom Bulwark", "Dawn Aegis", "Seraph Ward", "Moonlit Heartshield", "Radiant Sunshield", "Halo Buckler", "Verdant Laurel Shield", "Celestial Orbguard", "Archangel Aegis" } },
            { "fighter", new[] { "Leather Glove", "Iron Knuckle", "Bronze Fist", "Tiger Claw", "Flame Gauntlet", "Frost Gauntlet", "Thunder Fist", "Shadow Gauntlet", "Holy Gauntlet", "Dragon Gauntlet" } },
        };

        /// <summary>Name of a weapon / offhand without rarity prefix; null for other slots or an unknown class.</summary>
        public static string BaseName(string classId, string slot, int tier)
        {
            Dictionary<string, string[]> table = slot == Slots.Weapon ? Weapons : slot == Slots.Offhand ? Offhands : null;
            string[] names;
            if (table == null || classId == null || !table.TryGetValue(classId, out names)) return null;
            return names[RpgMath.Clamp(tier, 0, Tiers - 1)];
        }

        /// <summary>Full display name ("Legendary Dragonfire Twin"): the rarity prefix is a label, the tier name is the identity. Null when the item has no tiered name.</summary>
        public static string FullName(Item item)
        {
            if (item == null) return null;
            string baseName = BaseName(item.classId, item.slot, ItemInfo.ArtTier(item));
            if (baseName == null) return null;
            RarityDef r = ItemDefs.Rarity(item.rarity);
            return r != null && r.Prefix != null ? r.Prefix + " " + baseName : baseName;
        }
    }
}
