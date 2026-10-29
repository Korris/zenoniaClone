using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>Item generation, drop rolls, starter gear and save sanitising (web systems/equipment-system.js + starter-gear.js).</summary>
    public static class ItemFactory
    {
        public const double NormalDropChance = 0.12;
        public const int MaxItemLevel = 95;

        /// <summary>Weighted rarity roll (weights sum to 1).</summary>
        public static string RollRarity(Rand rand)
        {
            double roll = rand(), cumulative = 0;
            foreach (string id in ItemDefs.RarityOrder)
            {
                cumulative += ItemDefs.Rarity(id).Weight;
                if (roll < cumulative) return id;
            }
            return ItemDefs.RarityOrder[ItemDefs.RarityOrder.Length - 1];
        }

        static string OffhandClass(string classId) { return RpgClasses.IsValid(classId) ? classId : RpgClasses.Default; }

        static ItemTemplate PickTemplate(string slot, int itemLevel, Rand rand, string classId)
        {
            List<ItemTemplate> all = ItemDefs.TemplatesFor(slot);
            string offType = RpgClasses.Get(OffhandClass(classId)).OffhandType;
            List<ItemTemplate> pool = all.FindAll(t => t.MinItemLevel <= itemLevel && (slot != Slots.Offhand || t.Id == offType));
            if (pool.Count == 0) pool = new List<ItemTemplate> { all[0] };
            return pool[(int)Math.Floor(rand() * pool.Count)];
        }

        /// <summary>
        /// One item instance. Stats = round(base * rarityMult * (1 + 0.08 * (itemLevel - 1))). The rand() call order matches the web
        /// (slot?, template, rarity?, uid) so a seeded RNG produces the same picks.
        /// </summary>
        public static Item Generate(Rand rand, int itemLevel, string slot = null, string forceRarity = null, string classId = "warrior")
        {
            string chosen = slot ?? Slots.All[(int)Math.Floor(rand() * Slots.All.Length)];
            ItemTemplate t = PickTemplate(chosen, itemLevel, rand, classId);
            string rarity = forceRarity ?? RollRarity(rand);
            RarityDef r = ItemDefs.Rarity(rarity);
            double scale = r.Mult * (1 + 0.08 * (itemLevel - 1));
            var stats = new ItemStats
            {
                atk = Scaled(t.Base.atk, scale), def = Scaled(t.Base.def, scale), maxHp = Scaled(t.Base.maxHp, scale), maxMp = Scaled(t.Base.maxMp, scale),
                critChance = t.Base.critChance > 0f ? RpgMath.Round3(t.Base.critChance * scale) : 0f,
                speedMult = t.Base.speedMult > 0f ? RpgMath.Round3(t.Base.speedMult * scale) : 0f,
            };
            ClassStatDef cls = RpgClasses.Get(classId);
            string baseName = chosen == Slots.Weapon ? cls.WeaponName
                : chosen == Slots.Offhand ? RpgClasses.Get(OffhandClass(classId)).OffhandName
                : ItemDefs.ArmorTierNames.ContainsKey(chosen) ? ItemDefs.ArmorTierNames[chosen][ItemDefs.EquipmentTier(itemLevel)] : t.Name;
            var item = new Item
            {
                templateId = t.Id, slot = chosen, rarity = rarity, itemLevel = itemLevel, stats = stats,
                name = r.Prefix != null ? r.Prefix + " " + baseName : baseName,
            };
            item.uid = t.Id + "-" + ToBase36((long)Math.Floor(rand() * 1e9));
            if (chosen == Slots.Weapon) item.classId = RpgClasses.IsValid(classId) ? classId : RpgClasses.Default;
            if (chosen == Slots.Offhand) item.classId = OffhandClass(classId);
            return item;
        }

        static int Scaled(int baseValue, double scale) { return baseValue == 0 ? 0 : RpgMath.Round(baseValue * scale); }

        static string ToBase36(long v)
        {
            if (v <= 0) return "0";
            const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            var sb = new System.Text.StringBuilder();
            for (; v > 0; v /= 36) sb.Insert(0, digits[(int)(v % 36)]);
            return sb.ToString();
        }

        /// <summary>
        /// Equipment roll on a monster death: 12% for normal monsters, always for bosses (forced rare/epic). The dropped item belongs to a
        /// random class (tradeable class gear, web rollEquipmentDrop). Null = nothing dropped.
        /// </summary>
        public static Item RollEquipmentDrop(int itemLevel, bool isBoss, Rand rand)
        {
            if (!isBoss && rand() >= NormalDropChance) return null;
            string force = isBoss ? (rand() < 0.5 ? "rare" : "epic") : null;
            string dropClass = RpgClasses.Ids[(int)Math.Floor(rand() * RpgClasses.Ids.Length)];
            return Generate(rand, Math.Max(1, itemLevel), null, force, dropClass);
        }

        /// <summary>Level-1 common +0 starter for 'weapon' | 'offhand' with the stable uid starter-&lt;slot&gt;.</summary>
        public static Item CreateStarterItem(string slot, string classId)
        {
            Item item = Generate(() => 0.0, 1, slot, "common", classId);
            item.uid = "starter-" + slot;
            if (slot == Slots.Weapon) item.name = RpgClasses.Get(classId).WeaponName;
            return item;
        }

        /// <summary>Validates an item from untrusted save JSON; null when it cannot be trusted (unknown template, slot mismatch, bad rarity).</summary>
        public static Item Sanitize(Item raw)
        {
            if (raw == null) return null;
            ItemTemplate t = ItemDefs.Template(raw.templateId);
            if (t == null || raw.slot != t.Slot || ItemDefs.Rarity(raw.rarity) == null) return null;
            ItemStats rs = raw.stats ?? new ItemStats();
            var stats = new ItemStats
            {
                atk = t.Base.atk != 0 ? RpgMath.Clamp(rs.atk, 0, 999) : 0, def = t.Base.def != 0 ? RpgMath.Clamp(rs.def, 0, 999) : 0,
                maxHp = t.Base.maxHp != 0 ? RpgMath.Clamp(rs.maxHp, 0, 999) : 0, maxMp = t.Base.maxMp != 0 ? RpgMath.Clamp(rs.maxMp, 0, 999) : 0,
                critChance = t.Base.critChance > 0f ? ClampF(rs.critChance) : 0f, speedMult = t.Base.speedMult > 0f ? ClampF(rs.speedMult) : 0f,
            };
            var item = new Item
            {
                uid = string.IsNullOrEmpty(raw.uid) ? t.Id + "-" + ToBase36((long)Math.Floor(RpgRandom.Default() * 1e9)) : raw.uid,
                templateId = t.Id, slot = t.Slot, rarity = raw.rarity, stats = stats,
                name = string.IsNullOrEmpty(raw.name) ? t.Name : raw.name.Length > 40 ? raw.name.Substring(0, 40) : raw.name,
                itemLevel = RpgMath.Clamp(raw.itemLevel, 1, MaxItemLevel),
            };
            if (t.Slot == Slots.Weapon && RpgClasses.IsValid(raw.classId)) item.classId = raw.classId;
            if (t.Slot == Slots.Offhand) item.classId = RpgClasses.ClassForOffhand(t.Id) ?? RpgClasses.Default; // the template decides, never the raw tag
            if (t.Slot == Slots.Weapon || t.Slot == Slots.Offhand) item.enhance = RpgMath.Clamp(raw.enhance, 0, EnhanceConfig.MaxEnhance);
            return item;
        }

        static float ClampF(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? 0f : RpgMath.Round3(Math.Max(0f, Math.Min(999f, v))); }
    }
}
