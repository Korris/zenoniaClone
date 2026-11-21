using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>One chest's refill time in a save: unix ms (real time, so neither a relog nor another map resets it).</summary>
    [Serializable]
    public class ChestCooldown
    {
        public string id;
        public long at;
    }

    /// <summary>
    /// Dungeon chests and warp crystals of one character (Rootburrow V5, 21/11/2026, DungeonPoints): when each chest refills, which crystals are attuned.
    /// Saved with the character, mirrored to the client in every RpgState. Only the side that owns the state changes it (zone server online, the game offline);
    /// the caller checks the distance (it knows the tiles).
    /// </summary>
    public sealed partial class PlayerState
    {
        readonly Dictionary<string, long> chestReadyAt = new Dictionary<string, long>();
        readonly List<string> waypoints = new List<string>();

        public static long UnixNowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }

        /// <summary>The chest is still empty for this character at `nowMs` (unix ms).</summary>
        public bool ChestEmpty(string chestId, long nowMs)
        {
            long at;
            return chestReadyAt.TryGetValue(chestId, out at) && at > nowMs;
        }

        public bool HasWaypoint(string id) { return waypoints.Contains(id); }

        /// <summary>The attuned crystals, in the order they were touched.</summary>
        public IReadOnlyList<string> Waypoints { get { return waypoints; } }

        /// <summary>Opens a chest: rolls ChestLoot, puts everything in the bag (never on the ground), starts the per-character refill timer. One call, one payment.</summary>
        public ActionResult OpenChest(string chestId, long nowMs, Rand rand)
        {
            ChestDef chest = DungeonPoints.Chest(chestId);
            if (chest == null) return ActionResult.Fail("No such chest");
            if (ChestEmpty(chestId, nowMs))
            {
                long mins = Math.Max(1, (chestReadyAt[chestId] - nowMs + 59999) / 60000);
                return ActionResult.Fail("The chest is empty. It refills in " + (mins >= 60 ? (mins / 60) + " h " + (mins % 60) + " min" : mins + " min"));
            }
            if (Inventory.Count >= InventorySize) return ActionResult.Fail("Inventory full");
            List<LootEntry> loot = ChestLoot.Roll(chest, ClassId, rand);
            foreach (LootEntry e in loot) ApplyPickup(e);
            chestReadyAt[chestId] = nowMs + chest.CooldownMs;
            return ActionResult.Success(ChestLoot.Describe(loot));
        }

        /// <summary>Attunes a crystal for good. False when unknown or already attuned. Counts for a "reach" quest (QuestDef.WaypointId).</summary>
        public bool UnlockWaypoint(string id)
        {
            if (DungeonPoints.Waypoint(id) == null || waypoints.Contains(id)) return false;
            waypoints.Add(id);
            Quests.CountReach(id);
            return true;
        }

        /// <summary>For the save: every chest still empty (a refilled one needs no entry).</summary>
        public List<ChestCooldown> ChestCooldownsToList(long nowMs)
        {
            var list = new List<ChestCooldown>();
            foreach (KeyValuePair<string, long> e in chestReadyAt) if (e.Value > nowMs) list.Add(new ChestCooldown { id = e.Key, at = e.Value });
            return list;
        }

        public void LoadDungeon(List<ChestCooldown> chests, List<string> attuned)
        {
            chestReadyAt.Clear(); waypoints.Clear();
            foreach (ChestCooldown c in SanitizeChests(chests)) chestReadyAt[c.id] = c.at;
            foreach (string w in SanitizeWaypoints(attuned)) waypoints.Add(w);
        }

        /// <summary>Never trust raw JSON: unknown chests, repeats and negative times are dropped; a refill is never further away than the chest's own cooldown from now.</summary>
        public static List<ChestCooldown> SanitizeChests(List<ChestCooldown> raw, long nowMs = 0)
        {
            var clean = new List<ChestCooldown>();
            if (raw == null) return clean;
            if (nowMs == 0) nowMs = UnixNowMs;
            foreach (ChestCooldown c in raw)
            {
                ChestDef def = c != null ? DungeonPoints.Chest(c.id) : null;
                if (def == null || c.at <= 0 || clean.Exists(x => x.id == c.id)) continue;
                clean.Add(new ChestCooldown { id = c.id, at = Math.Min(c.at, nowMs + def.CooldownMs) });
            }
            return clean;
        }

        public static List<string> SanitizeWaypoints(List<string> raw)
        {
            var clean = new List<string>();
            if (raw == null) return clean;
            foreach (string w in raw) if (DungeonPoints.Waypoint(w) != null && !clean.Contains(w)) clean.Add(w);
            return clean;
        }
    }
}
