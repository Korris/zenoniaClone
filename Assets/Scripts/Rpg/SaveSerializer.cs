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
        public int hpPotionMedium, hpPotionLarge, mpPotionMedium, mpPotionLarge, hpPotionPick, mpPotionPick; // hpPotion / mpPotion are the Small stacks; missing in old saves = none, Small picked
        public float hp, mp, x, y;
        public bool hasPosition, bossDefeated;
        public List<Item> inventory = new List<Item>();
        public List<Item> equipment = new List<Item>();
        public List<int> skillRanks = new List<int>(); // 8 ranks (0-10) by tree index; missing in old saves = nothing learned
        public List<int> skillSlots = new List<int>(); // 8 magazine slots: tree index or -1
        public List<string> defeatedBossMaps = new List<string>(); // map ids whose boss this save killed (the Frostfang King stays in bossDefeated); missing in old saves = none
        public List<QuestEntry> quests = new List<QuestEntry>(); // quests taken so far (QuestLog); missing in old saves = none taken
        public int wantedDay; // WantedBoss.DayKey of the last Wanted Boss claim (PlayerState.WantedDay); missing in old saves = 0 = never
        public string petId; // v4: the combat pet (kept so a v4 save loads into petTeam[0]); missing = none. Old saves carry pet ITEMS instead, which AccountPets.Migrate converts
        public List<string> petTeam = new List<string>(); // v5: the five team slots (PetSlot order, "" = empty); missing = petId in the combat slot
        public string name; // offline hero name (HeroNameRules); missing / empty in older saves = the class name (SaveStore.DisplayName). Online the server keeps the name, this stays empty
        public List<ChestCooldown> chests = new List<ChestCooldown>(); // dungeon chests still empty for this character, refill time in unix ms (PlayerState.Dungeon); missing in old saves = all full
        public List<string> waypoints = new List<string>(); // attuned dungeon warp crystals (DungeonPoints); missing in old saves = none
        // v6: monster cards (CardCollection). All missing in old saves = nothing owned.
        public List<CardEntry> cards = new List<CardEntry>(); // copies per card id
        public List<GemEntry> gems = new List<GemEntry>();    // Soul Gem stacks
        public List<int> album = new List<int>();             // CardTable indexes ever owned
        public int albumClaimed;                              // bit per Album milestone claimed
        public List<string> cardDeck = new List<string>();    // duel deck (phase 05)
        public CardDailyData cardDaily = new CardDailyData(); // daily duel counters (phase 05 / 06)
        public int cardNpcFirstWins;                          // bit per duel NPC beaten once (phase 05)
        public CardRankData cardRank = new CardRankData();    // Ranked season (phase 07)
        public CharacterAppearanceData appearance; // look (hair style / colours / headgear); missing or stored = false in older saves = not chosen (no version bump needed)
    }

    /// <summary>
    /// PlayerState &lt;-&gt; JSON, with the web save-serializer's rules: every item is regenerated through ItemFactory.Sanitize (never trust raw JSON),
    /// gear of another class is moved to the bag on load (class lock), empty weapon / offhand slots are refilled with the starter, a dead hero never loads.
    /// </summary>
    public static class SaveSerializer
    {
        public const int SaveVersion = 6; // 6: monster cards, Soul Gems, Album, deck, item sockets (an older save simply owns no cards). 5: the pet team (petTeam, 5 slots). 4: pets belong to the account (petId). 2: pets carry durability. 3: Frost Hollow grew to 48 rows, so older frostHollow positions are shifted by Parse (= FrostHollowGrownVersion).
        /// <summary>Saves older than this hold Frost Hollow positions measured on the 24-row map; Parse moves them up by the rows the map has gained since.</summary>
        public const int FrostHollowGrownVersion = 3;
        /// <summary>Frost Hollow's row count before it grew (36x24). Callers pass FrostHollowLayout.RowCount; the default means "not grown", so a caller that forgets never shifts.</summary>
        public const int LegacyFrostHollowRows = 24;
        const float TileUnits = 16f;
        public const string MapId = "frostHollow";
        const int MaxBossMaps = 32, MaxMapIdLength = 32, MaxWantedDay = 1_000_000; // same limits as the server-api schema, which rejects the whole save otherwise

        public static SaveData Capture(PlayerState p, bool hasPosition, float x, float y, bool bossDefeated, long savedAt = 0, string mapId = null, List<string> defeatedBossMaps = null)
        {
            var d = new SaveData
            {
                version = SaveVersion, savedAt = savedAt != 0 ? savedAt : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                classId = p.ClassId, mapId = string.IsNullOrEmpty(mapId) ? MapId : mapId, level = p.Level, exp = p.Exp, gold = p.Gold, hpPotion = p.HpPotions, mpPotion = p.MpPotions,
                hpPotionMedium = p.HpPotionsMedium, hpPotionLarge = p.HpPotionsLarge, mpPotionMedium = p.MpPotionsMedium, mpPotionLarge = p.MpPotionsLarge, hpPotionPick = p.HpPotionPick, mpPotionPick = p.MpPotionPick,
                upgradeStone = p.UpgradeStone, hp = p.Hp, mp = p.Mp, hasPosition = hasPosition, x = x, y = y, bossDefeated = bossDefeated,
            };
            d.inventory.AddRange(p.Inventory);
            foreach (string slot in Slots.All) { Item e = p.GetEquipped(slot); if (e != null) d.equipment.Add(e); }
            d.skillRanks = p.Skills.ToList(); d.skillSlots = p.Loadout.ToList();
            if (defeatedBossMaps != null) d.defeatedBossMaps.AddRange(defeatedBossMaps);
            d.quests = p.Quests.ToList();
            d.wantedDay = p.WantedDay;
            d.petId = p.PetId ?? "";
            d.petTeam = new List<string>(p.Team);
            d.appearance = CharacterAppearanceData.Sanitize(p.Appearance, p.ClassId);
            d.chests = p.ChestCooldownsToList(d.savedAt);
            d.waypoints = new List<string>(p.Waypoints);
            p.Cards.ToSave(d);
            return d;
        }

        public static string ToJson(SaveData d) { return JsonUtility.ToJson(d, true); }

        /// <summary>Parses + validates a save. Null for garbage, unreadable JSON, or a version newer than this build; missing fields default.</summary>
        /// <param name="frostHollowRows">Row count of this build's Frost Hollow. A frostHollow save older than FrostHollowGrownVersion moves up (rows - 24) * 16 units (y points up and the map grew downward in the painting), so the hero keeps the same tile. A 24-row build moves nothing.</param>
        public static SaveData Parse(string json, int frostHollowRows = LegacyFrostHollowRows)
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
                hpPotion = Math.Max(0, raw.hpPotion), mpPotion = Math.Max(0, raw.mpPotion),
                hpPotionMedium = Math.Max(0, raw.hpPotionMedium), hpPotionLarge = Math.Max(0, raw.hpPotionLarge), mpPotionMedium = Math.Max(0, raw.mpPotionMedium), mpPotionLarge = Math.Max(0, raw.mpPotionLarge),
                hpPotionPick = RpgMath.Clamp(raw.hpPotionPick, 0, PotionCatalog.Sizes - 1), mpPotionPick = RpgMath.Clamp(raw.mpPotionPick, 0, PotionCatalog.Sizes - 1),
                upgradeStone = RpgMath.Clamp(raw.upgradeStone, 0, PlayerState.MaxStones),
                hp = Finite(raw.hp), mp = Finite(raw.mp), x = Finite(raw.x), y = Finite(raw.y), hasPosition = raw.hasPosition, bossDefeated = raw.bossDefeated,
                name = HeroNameRules.Problem(raw.name) == null ? raw.name.Trim() : "", // a broken name falls back to the class name
            };
            if (raw.version < FrostHollowGrownVersion && d.mapId == MapId && d.hasPosition && frostHollowRows > LegacyFrostHollowRows)
                d.y += (frostHollowRows - LegacyFrostHollowRows) * TileUnits;
            foreach (Item i in raw.inventory ?? new List<Item>())
            {
                Item s = ItemFactory.Sanitize(i, raw.version < 2);
                if (s != null && d.inventory.Count < PlayerState.InventorySize) d.inventory.Add(s);
            }
            var seen = new HashSet<string>();
            foreach (Item i in raw.equipment ?? new List<Item>())
            {
                Item s = ItemFactory.Sanitize(i, raw.version < 2);
                if (s != null && seen.Add(s.slot)) d.equipment.Add(s); // one item per slot
            }
            var ranks = new SkillRanks(d.classId); ranks.Load(raw.skillRanks, d.level); // illegal ranks (tampering, class / level change) are trimmed
            var loadout = new SkillLoadout(); loadout.Load(raw.skillSlots, ranks);
            d.skillRanks = ranks.ToList(); d.skillSlots = loadout.ToList();
            foreach (string m in raw.defeatedBossMaps ?? new List<string>())
                if (IsMapId(m) && !d.defeatedBossMaps.Contains(m) && d.defeatedBossMaps.Count < MaxBossMaps) d.defeatedBossMaps.Add(m);
            d.quests = QuestLog.Sanitize(raw.quests);
            d.wantedDay = RpgMath.Clamp(raw.wantedDay, 0, MaxWantedDay);
            d.petId = PetConfig.Get(raw.petId) != null ? raw.petId : ""; // ownership is checked when the account is attached (PlayerState.AttachAccount)
            string[] team = PetTeam.Empty();
            if (raw.petTeam != null && raw.petTeam.Count > 0) { for (int i = 0; i < team.Length && i < raw.petTeam.Count; i++) team[i] = PetConfig.Get(raw.petTeam[i]) != null ? raw.petTeam[i] : ""; }
            else team[0] = d.petId;
            for (int i = 0; i < team.Length; i++) for (int j = 0; j < i; j++) if (team[i] != "" && team[i] == team[j]) team[i] = ""; // one species, one slot
            d.petTeam = new List<string>(team); d.petId = team[0];
            d.appearance = CharacterAppearanceData.Sanitize(raw.appearance, d.classId);
            d.chests = PlayerState.SanitizeChests(raw.chests);
            d.waypoints = PlayerState.SanitizeWaypoints(raw.waypoints);
            new CardCollection().Load(raw).ToSave(d); // sanitised copy of every card field
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
            var p = new PlayerState(d.classId) { Level = d.level, Exp = d.exp, Gold = d.gold, HpPotions = d.hpPotion, MpPotions = d.mpPotion, UpgradeStone = d.upgradeStone,
                HpPotionsMedium = d.hpPotionMedium, HpPotionsLarge = d.hpPotionLarge, MpPotionsMedium = d.mpPotionMedium, MpPotionsLarge = d.mpPotionLarge, HpPotionPick = d.hpPotionPick, MpPotionPick = d.mpPotionPick };
            foreach (Item i in d.inventory) p.Inventory.Add(i);
            foreach (Item e in d.equipment)
            {
                if (p.CanEquip(e)) p.SetEquippedRaw(e.slot, e);
                else if (p.Inventory.Count < PlayerState.InventorySize) p.Inventory.Add(e); // class lock: back to the bag, else dropped
            }
            EquipLevel.UnequipAboveLevel(p); // gear above the level (old save, 16/11/2026 tier table) back to the bag, never lost
            p.EquipStarterGear();
            p.Skills.Load(d.skillRanks, d.level); p.Loadout.Load(d.skillSlots, p.Skills);
            p.Quests.Load(d.quests);
            p.WantedDay = d.wantedDay;
            p.LoadDungeon(d.chests, d.waypoints); // after the quests: nothing here moves a quest
            p.Appearance = CharacterAppearanceData.Sanitize(d.appearance, p.ClassId);
            p.Cards.Load(d);
            for (int i = 0; i < p.Team.Length; i++) p.Team[i] = d.petTeam != null && i < d.petTeam.Count && d.petTeam[i] != null ? d.petTeam[i] : i == 0 ? d.petId ?? "" : "";
            p.RecomputeStats();
            p.Hp = Math.Min(p.MaxHp, d.hp > 0f ? d.hp : p.MaxHp); // never load a dead hero
            p.Mp = Math.Min(p.MaxMp, d.mp > 0f ? d.mp : p.MaxMp);
            return p;
        }
    }
}
