using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto.Rpg
{
    /// <summary>Save file shape (JsonUtility). Equipment is a list of the equipped items (the item's slot field says where it goes).</summary>
    [Serializable]
    public class SaveData
    {
        public int version;
        public long savedAt;
        public string classId, mapId;
        public int level, exp, gold, hpPotion, mpPotion, upgradeStone;
        public float hp, mp, x, y;
        public bool hasPosition, bossDefeated;
        public List<Item> inventory = new List<Item>();
        public List<Item> equipment = new List<Item>();
        public List<int> skillRanks = new List<int>(); // 8 ranks (0-10) by tree index; missing in old saves = nothing learned
        public List<int> skillSlots = new List<int>(); // 8 magazine slots: tree index or -1
        public List<string> defeatedBossMaps = new List<string>(); // map ids whose boss this save killed (the Frostfang King stays in bossDefeated); missing in old saves = none
    }

    /// <summary>
    /// PlayerState &lt;-&gt; JSON, with the web save-serializer's rules: every item is regenerated through ItemFactory.Sanitize (never trust raw JSON),
    /// gear of another class is moved to the bag on load (class lock), empty weapon / offhand slots are refilled with the starter, a dead hero never loads.
    /// </summary>
    public static class SaveSerializer
    {
        public const int SaveVersion = 1;
        public const string MapId = "frostHollow";
        const int MaxBossMaps = 32, MaxMapIdLength = 32; // same limits as the server-api schema, which rejects the whole save otherwise

        public static SaveData Capture(PlayerState p, bool hasPosition, float x, float y, bool bossDefeated, long savedAt = 0, string mapId = null, List<string> defeatedBossMaps = null)
        {
            var d = new SaveData
            {
                version = SaveVersion, savedAt = savedAt != 0 ? savedAt : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                classId = p.ClassId, mapId = string.IsNullOrEmpty(mapId) ? MapId : mapId, level = p.Level, exp = p.Exp, gold = p.Gold, hpPotion = p.HpPotions, mpPotion = p.MpPotions,
                upgradeStone = p.UpgradeStone, hp = p.Hp, mp = p.Mp, hasPosition = hasPosition, x = x, y = y, bossDefeated = bossDefeated,
            };
            d.inventory.AddRange(p.Inventory);
            foreach (string slot in Slots.All) { Item e = p.GetEquipped(slot); if (e != null) d.equipment.Add(e); }
            d.skillRanks = p.Skills.ToList(); d.skillSlots = p.Loadout.ToList();
            if (defeatedBossMaps != null) d.defeatedBossMaps.AddRange(defeatedBossMaps);
            return d;
        }

        public static string ToJson(SaveData d) { return JsonUtility.ToJson(d, true); }

        /// <summary>Parses + validates a save. Null for garbage, unreadable JSON, or a version newer than this build; missing fields default.</summary>
        public static SaveData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            SaveData raw;
            try { raw = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception) { return null; }
            if (raw == null || raw.version < 0 || raw.version > SaveVersion || raw.level < 1) return null;
            var d = new SaveData
            {
                version = SaveVersion, savedAt = raw.savedAt, classId = RpgClasses.IsValid(raw.classId) ? raw.classId : RpgClasses.Default, mapId = string.IsNullOrEmpty(raw.mapId) ? MapId : raw.mapId,
                level = RpgMath.Clamp(raw.level, 1, Leveling.MaxLevel), exp = Math.Max(0, raw.exp), gold = Math.Max(0, raw.gold),
                hpPotion = Math.Max(0, raw.hpPotion), mpPotion = Math.Max(0, raw.mpPotion), upgradeStone = RpgMath.Clamp(raw.upgradeStone, 0, PlayerState.MaxStones),
                hp = Finite(raw.hp), mp = Finite(raw.mp), x = Finite(raw.x), y = Finite(raw.y), hasPosition = raw.hasPosition, bossDefeated = raw.bossDefeated,
            };
            foreach (Item i in raw.inventory ?? new List<Item>())
            {
                Item s = ItemFactory.Sanitize(i);
                if (s != null && d.inventory.Count < PlayerState.InventorySize) d.inventory.Add(s);
            }
            var seen = new HashSet<string>();
            foreach (Item i in raw.equipment ?? new List<Item>())
            {
                Item s = ItemFactory.Sanitize(i);
                if (s != null && seen.Add(s.slot)) d.equipment.Add(s); // one item per slot
            }
            var ranks = new SkillRanks(d.classId); ranks.Load(raw.skillRanks, d.level); // illegal ranks (tampering, class / level change) are trimmed
            var loadout = new SkillLoadout(); loadout.Load(raw.skillSlots, ranks);
            d.skillRanks = ranks.ToList(); d.skillSlots = loadout.ToList();
            foreach (string m in raw.defeatedBossMaps ?? new List<string>())
                if (IsMapId(m) && !d.defeatedBossMaps.Contains(m) && d.defeatedBossMaps.Count < MaxBossMaps) d.defeatedBossMaps.Add(m);
            return d;
        }

        static float Finite(float v) { return float.IsNaN(v) || float.IsInfinity(v) ? 0f : v; }

        /// <summary>The characters the server-api schema allows in a map id, so a hand-edited entry is dropped here and cannot make the server refuse the save.</summary>
        static bool IsMapId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaxMapIdLength) return false;
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || c == '-')) return false;
            return true;
        }

        /// <summary>Builds the live PlayerState from a validated save (class lock, starter repair, hp clamp).</summary>
        public static PlayerState Restore(SaveData d)
        {
            var p = new PlayerState(d.classId) { Level = d.level, Exp = d.exp, Gold = d.gold, HpPotions = d.hpPotion, MpPotions = d.mpPotion, UpgradeStone = d.upgradeStone };
            foreach (Item i in d.inventory) p.Inventory.Add(i);
            foreach (Item e in d.equipment)
            {
                if (p.CanEquip(e)) p.SetEquippedRaw(e.slot, e);
                else if (p.Inventory.Count < PlayerState.InventorySize) p.Inventory.Add(e); // class lock: back to the bag, else dropped
            }
            p.EquipStarterGear();
            p.Skills.Load(d.skillRanks, d.level); p.Loadout.Load(d.skillSlots, p.Skills);
            p.RecomputeStats();
            p.Hp = Math.Min(p.MaxHp, d.hp > 0f ? d.hp : p.MaxHp); // never load a dead hero
            p.Mp = Math.Min(p.MaxMp, d.mp > 0f ? d.mp : p.MaxMp);
            return p;
        }
    }
}
