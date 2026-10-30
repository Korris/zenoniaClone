using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    public sealed class RarityDef { public string Id, Prefix, Color; public double Mult, Weight; }

    public sealed class ItemTemplate
    {
        public string Id, Slot, Name, EnhanceStat = "atk";
        public int MinItemLevel;
        public ItemStats Base;
    }

    /// <summary>Slots, rarities and drop templates (web data/item-definitions.js). Template order matters: it feeds the seeded pool pick.</summary>
    public static class ItemDefs
    {
        public static readonly string[] RarityOrder = { "common", "uncommon", "rare", "epic" };
        static readonly Dictionary<string, RarityDef> Rarities = new Dictionary<string, RarityDef>
        {
            { "common", new RarityDef { Id = "common", Color = "#ffffff", Mult = 1.0, Weight = 0.70, Prefix = null } },
            { "uncommon", new RarityDef { Id = "uncommon", Color = "#8fff8f", Mult = 1.25, Weight = 0.20, Prefix = "Fine" } },
            { "rare", new RarityDef { Id = "rare", Color = "#6ba8ff", Mult = 1.6, Weight = 0.08, Prefix = "Superior" } },
            { "epic", new RarityDef { Id = "epic", Color = "#e0a0ff", Mult = 2.1, Weight = 0.02, Prefix = "Legendary" } },
        };

        public static RarityDef Rarity(string id) { RarityDef r; return id != null && Rarities.TryGetValue(id, out r) ? r : null; }

        /// <summary>Shared art tier: itemLevel 1/5 -> 0, 10/15 -> 1 ... 90/95 -> 9.</summary>
        public static int EquipmentTier(int itemLevel) { return RpgMath.Clamp(itemLevel / 10, 0, 9); }

        public static readonly Dictionary<string, string[]> ArmorTierNames = new Dictionary<string, string[]>
        {
            { Slots.Helmet, new[] { "Leather Cap", "Steel Helm", "Bronze Helm", "Frost Helm", "Flame Helm", "Royal Helm", "Verdant Helm", "Arcane Helm", "Celestial Helm", "Dragon Helm" } },
            { Slots.Armor, new[] { "Leather Vest", "Steel Plate", "Bronze Plate", "Frost Plate", "Flame Plate", "Royal Plate", "Verdant Armor", "Arcane Armor", "Celestial Armor", "Dragon Armor" } },
            { Slots.Gloves, new[] { "Cloth Gloves", "Steel Gauntlets", "Bronze Gauntlets", "Frost Gauntlets", "Flame Gauntlets", "Royal Gauntlets", "Verdant Gloves", "Arcane Gauntlets", "Celestial Gauntlets", "Dragon Gauntlets" } },
            { Slots.Boots, new[] { "Leather Boots", "Steel Boots", "Bronze Boots", "Frost Boots", "Flame Boots", "Royal Boots", "Verdant Boots", "Arcane Boots", "Celestial Boots", "Dragon Boots" } },
        };

        static readonly List<ItemTemplate> All = new List<ItemTemplate>();
        static readonly Dictionary<string, ItemTemplate> ById = new Dictionary<string, ItemTemplate>();

        static ItemDefs()
        {
            W("sword", "Sword", 4, 1); W("sword2", "Short Sword", 6, 1); W("rapier", "Rapier", 7, 3); W("club", "Club", 8, 4);
            W("katana", "Katana", 9, 6); W("axe", "Axe", 10, 6); W("lance", "Lance", 11, 8); W("hammer", "Hammer", 13, 11); W("bigSword", "Big Sword", 15, 14);
            G(Slots.Helmet, "leatherCap", "Leather Cap", 1, new ItemStats { def = 1, maxHp = 5 });
            G(Slots.Helmet, "ironHelm", "Iron Helm", 5, new ItemStats { def = 3, maxHp = 12 });
            G(Slots.Helmet, "knightHelm", "Knight Helm", 10, new ItemStats { def = 6, maxHp = 22 });
            G(Slots.Armor, "leatherVest", "Leather Vest", 1, new ItemStats { def = 2, maxHp = 10 });
            G(Slots.Armor, "ironMail", "Iron Mail", 5, new ItemStats { def = 5, maxHp = 25 });
            G(Slots.Armor, "knightPlate", "Knight Plate", 10, new ItemStats { def = 9, maxHp = 45 });
            G(Slots.Gloves, "clothGloves", "Cloth Gloves", 1, new ItemStats { atk = 1, critChance = 0.02f });
            G(Slots.Gloves, "leatherGloves", "Leather Gloves", 5, new ItemStats { atk = 2, critChance = 0.04f });
            G(Slots.Gloves, "ironGauntlets", "Iron Gauntlets", 10, new ItemStats { atk = 4, critChance = 0.07f });
            G(Slots.Boots, "leatherBoots", "Leather Boots", 1, new ItemStats { def = 1, speedMult = 0.03f });
            G(Slots.Boots, "swiftBoots", "Swift Boots", 5, new ItemStats { def = 2, speedMult = 0.06f });
            G(Slots.Boots, "knightGreaves", "Knight Greaves", 10, new ItemStats { def = 4, speedMult = 0.09f });
            // Offhands: one type per class; `enhanceStat` is legacy (offhand +N now feeds SecondWeapon; display names only changed, ids kept for saves).
            O("shield", "Kite Shield", "def", new ItemStats { def = 3, maxHp = 10 });
            O("dagger", "Off-hand Dagger", "atk", new ItemStats { atk = 2, critChance = 0.02f });
            O("robot", "Drone", "atk", new ItemStats { atk = 3 });
            O("spirit", "Spirit", "maxHp", new ItemStats { maxHp = 12, maxMp = 8 });
            O("spellbook", "Spellbook", "atk", new ItemStats { atk = 2, maxMp = 10 });
        }

        static void Add(ItemTemplate t) { All.Add(t); ById[t.Id] = t; }
        static void W(string id, string name, int atk, int minLevel) { Add(new ItemTemplate { Id = id, Slot = Slots.Weapon, Name = name, MinItemLevel = minLevel, Base = new ItemStats { atk = atk } }); }
        static void G(string slot, string id, string name, int minLevel, ItemStats b) { Add(new ItemTemplate { Id = id, Slot = slot, Name = name, MinItemLevel = minLevel, Base = b }); }
        static void O(string id, string name, string enhanceStat, ItemStats b) { Add(new ItemTemplate { Id = id, Slot = Slots.Offhand, Name = name, MinItemLevel = 1, EnhanceStat = enhanceStat, Base = b }); }

        public static ItemTemplate Template(string id) { ItemTemplate t; return id != null && ById.TryGetValue(id, out t) ? t : null; }

        /// <summary>Templates of a slot in definition order.</summary>
        public static List<ItemTemplate> TemplatesFor(string slot) { return All.FindAll(t => t.Slot == slot); }
    }
}
