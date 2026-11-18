using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Stat bundle of an item or template. Zero = stat not present (web items only carry the template's stats).</summary>
    [Serializable]
    public class ItemStats
    {
        public int atk, def, maxHp, maxMp;
        public float critChance, speedMult;

        public ItemStats Copy() { return (ItemStats)MemberwiseClone(); }
        public bool IsZero { get { return atk == 0 && def == 0 && maxHp == 0 && maxMp == 0 && critChance == 0f && speedMult == 0f; } }

        public int GetInt(string key)
        {
            switch (key) { case "atk": return atk; case "def": return def; case "maxHp": return maxHp; case "maxMp": return maxMp; default: return 0; }
        }

        public void SetInt(string key, int v)
        {
            switch (key) { case "atk": atk = v; break; case "def": def = v; break; case "maxHp": maxHp = v; break; case "maxMp": maxMp = v; break; }
        }

        /// <summary>Sum of all stat values, the web isUpgrade() power heuristic.</summary>
        public double Total { get { return atk + def + maxHp + maxMp + critChance + speedMult; } }
    }

    /// <summary>Item instance: web shape { uid, templateId, slot, name, rarity, itemLevel, stats, classId?, enhance? }. JsonUtility friendly.</summary>
    [Serializable]
    public class Item
    {
        public string uid, templateId, slot, name, rarity, classId;
        public int itemLevel, enhance;
        public int durability; // OLD pet items only (ItemFactory.LegacyPetDurability), read from saves before Pet Companion; 0 on everything else
        public ItemStats stats = new ItemStats();
    }

    public static class Slots
    {
        public const string Weapon = "weapon", Helmet = "helmet", Armor = "armor", Gloves = "gloves", Boots = "boots", Offhand = "offhand", Pet = "pet";
        /// <summary>Cosmetic hat (HeadgearCatalog): no stats, sets CharacterAppearanceData.equippedHeadgearId. Appended last: index 7 still fits NetLootCodec's 3 slot bits.</summary>
        public const string Headgear = "headgear";
        public static readonly string[] All = { Weapon, Helmet, Armor, Gloves, Boots, Offhand, Pet, Headgear };
        /// <summary>What monsters drop and the level-scaled generator makes. Pet is only the OLD pet slot (Pet Companion pets are account data, PlayerState.PetId).</summary>
        public static readonly string[] Generated = { Weapon, Helmet, Armor, Gloves, Boots, Offhand };
        public static int Index(string slot) { return Array.IndexOf(All, slot); }
    }
}
