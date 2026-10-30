using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>Per-class base stats + growth + offhand type (web data/class-definitions.js and item-definitions OFFHAND_BY_CLASS).</summary>
    public sealed class ClassStatDef
    {
        public string Id, Name, OffhandType, OffhandName, WeaponName;
        public int BaseHp, BaseMp, BaseAtk, BaseDef;
        public float Speed, CritBonus;
        public float GrowHp, GrowMp, GrowAtk, GrowDef;
    }

    public static class RpgClasses
    {
        public const string Default = "warrior";
        public static readonly string[] Ids = { "warrior", "wizard", "mechanic", "assassin", "cleric" };

        static readonly Dictionary<string, ClassStatDef> Table = new Dictionary<string, ClassStatDef>
        {
            { "warrior", new ClassStatDef { Id = "warrior", Name = "Warrior", BaseHp = 100, BaseMp = 40, BaseAtk = 12, BaseDef = 4, Speed = 80f, GrowHp = 12f, GrowMp = 5f, GrowAtk = 2.5f, GrowDef = 1f, OffhandType = "shield", OffhandName = "Kite Shield", WeaponName = "Sword" } },
            { "wizard", new ClassStatDef { Id = "wizard", Name = "Wizard", BaseHp = 80, BaseMp = 70, BaseAtk = 13, BaseDef = 3, Speed = 80f, GrowHp = 9f, GrowMp = 8f, GrowAtk = 2.7f, GrowDef = 0.7f, OffhandType = "spellbook", OffhandName = "Spellbook", WeaponName = "Staff" } },
            { "mechanic", new ClassStatDef { Id = "mechanic", Name = "Mechanic", BaseHp = 90, BaseMp = 55, BaseAtk = 12, BaseDef = 4, Speed = 82f, GrowHp = 10f, GrowMp = 6f, GrowAtk = 2.6f, GrowDef = 0.9f, OffhandType = "robot", OffhandName = "Drone", WeaponName = "Gun" } },
            { "cleric", new ClassStatDef { Id = "cleric", Name = "Cleric", BaseHp = 85, BaseMp = 75, BaseAtk = 11, BaseDef = 4, Speed = 80f, GrowHp = 10f, GrowMp = 8f, GrowAtk = 2.5f, GrowDef = 0.8f, OffhandType = "spirit", OffhandName = "Spirit", WeaponName = "Holy Staff" } },
            { "assassin", new ClassStatDef { Id = "assassin", Name = "Assassin", BaseHp = 88, BaseMp = 50, BaseAtk = 13, BaseDef = 3, Speed = 92f, CritBonus = 0.06f, GrowHp = 10f, GrowMp = 5f, GrowAtk = 2.7f, GrowDef = 0.7f, OffhandType = "dagger", OffhandName = "Off-hand Dagger", WeaponName = "Dagger" } },
        };

        public static bool IsValid(string id) { return id != null && Table.ContainsKey(id); }
        /// <summary>Class by id; unknown ids fall back to the warrior (web getClassDefinition).</summary>
        public static ClassStatDef Get(string id) { ClassStatDef d; return id != null && Table.TryGetValue(id, out d) ? d : Table[Default]; }
        /// <summary>Owner class of an offhand template type ("shield" -> warrior), or null.</summary>
        public static string ClassForOffhand(string type) { foreach (ClassStatDef d in Table.Values) if (d.OffhandType == type) return d.Id; return null; }
    }
}
