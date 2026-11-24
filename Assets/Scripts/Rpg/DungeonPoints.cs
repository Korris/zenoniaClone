using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>A treasure chest of a dungeon floor (Rootburrow V5, 21/11/2026): stands on the solid tile at the end of a dead-end alcove, opened from the floor tile in front.</summary>
    public sealed class ChestDef
    {
        public readonly string Id, MapId;
        /// <summary>The solid tile the chest stands on (x from the west, y from the north, like MapProp).</summary>
        public readonly int Tx, Ty;
        /// <summary>Dungeon floor 1-3: the loot table (ChestLoot) grows with it.</summary>
        public readonly int Floor;
        /// <summary>Rare chest: double gold and item chances, 24 h instead of 6 h (ChatGPT V5).</summary>
        public readonly bool Rare;
        public ChestDef(string id, string mapId, int tx, int ty, int floor, bool rare) { Id = id; MapId = mapId; Tx = tx; Ty = ty; Floor = floor; Rare = rare; }
        /// <summary>Per-character wait before this chest refills, real time.</summary>
        public long CooldownMs { get { return Rare ? DungeonPoints.RareChestCooldownMs : DungeonPoints.ChestCooldownMs; } }
    }

    /// <summary>A warp crystal (RO-style waypoint): unlocked for good by walking up to it, then any unlocked crystal of the dungeon teleports to the others.</summary>
    public sealed class WaypointDef
    {
        public readonly string Id, MapId, Name;
        /// <summary>The solid tile the crystal stands on.</summary>
        public readonly int Tx, Ty;
        /// <summary>The floor tile a warp to this crystal lands on (next to it).</summary>
        public readonly int ArriveTx, ArriveTy;
        /// <summary>A touch point for a quest only (Taurus rune stone / Ancient Roots, phase 05): walking up to it attunes it like a crystal and counts for a WaypointId quest, but it is no warp destination (WaypointWindow skips it).</summary>
        public readonly bool QuestOnly;
        public WaypointDef(string id, string mapId, string name, int tx, int ty, int arriveTx, int arriveTy, bool questOnly = false) { Id = id; MapId = mapId; Name = name; Tx = tx; Ty = ty; ArriveTx = arriveTx; ArriveTy = arriveTy; QuestOnly = questOnly; }
    }

    /// <summary>
    /// Chests and warp crystals of the dungeons (first: Rootburrow 1F-3F, ChatGPT V5 docs/reference/rootburrow-dark-25d/floors-purpose-chatgpt.md): 3 chests per floor in the
    /// dead-end alcoves (the third one rare), one crystal per floor next to where the floor is entered. Shared by the client, the offline game and the zone server,
    /// so the tiles are plain numbers (no map classes in this assembly). Tools/place-map-25d-dungeon-props.py keeps these tiles clear of other props (RESERVED),
    /// Tools/preview-map-25d-dungeon.py checks them against the rows (solid tile, floor next to it, warp arrival walkable: PROBLEM lines).
    /// </summary>
    public static class DungeonPoints
    {
        public const long ChestCooldownMs = 6L * 3600 * 1000, RareChestCooldownMs = 24L * 3600 * 1000;
        /// <summary>World units from the chest's foot / the crystal's foot a hero may stand to use it (ChatGPT: 1.5 m = 24 units; the foot is half a tile inside the wall).</summary>
        public const float ReachUnits = 30f;
        /// <summary>The same check on the zone server, with slack for movement it saw last (like QuestCatalog.ReachUnits).</summary>
        public const float ServerReachUnits = 44f;
        /// <summary>Walking this close to a crystal unlocks it.</summary>
        public const float UnlockUnits = 36f;
        /// <summary>Hold time to open a chest, channel time of a warp, and how long since the last hit a warp needs (ChatGPT V5).</summary>
        public const float ChestOpenMs = 1200f, WarpChannelMs = 3000f, WarpCombatMs = 5000f;
        /// <summary>Id of the "Return to Bramblewood" entry of the warp list: always allowed, lands just outside Rootburrow's gate.</summary>
        public const string ExitId = "rootburrow-exit";
        public const string ExitMapId = "bramblewoodForest";
        public const int ExitTx = 4, ExitTy = 13;
        public const string ExitName = "Bramblewood Forest (outside)";

        public static readonly ChestDef[] Chests =
        {
            new ChestDef("rb1-a", "rootburrow", 57, 43, 1, false), new ChestDef("rb1-b", "rootburrow", 38, 2, 1, false), new ChestDef("rb1-c", "rootburrow", 15, 5, 1, true),
            new ChestDef("rb2-a", "rootburrow2", 55, 42, 2, false), new ChestDef("rb2-b", "rootburrow2", 33, 6, 2, false), new ChestDef("rb2-c", "rootburrow2", 23, 5, 2, true),
            new ChestDef("rb3-a", "rootburrow3", 54, 40, 3, false), new ChestDef("rb3-b", "rootburrow3", 45, 6, 3, false), new ChestDef("rb3-c", "rootburrow3", 24, 40, 3, true),
        };

        public static readonly WaypointDef[] Waypoints =
        {
            new WaypointDef("rootburrow1", "rootburrow", "Rootburrow 1F (Entrance)", 68, 21, 67, 23),
            new WaypointDef("rootburrow2", "rootburrow2", "Rootburrow 2F", 68, 9, 66, 9),
            new WaypointDef("rootburrow3", "rootburrow3", "Rootburrow 3F", 69, 37, 67, 37),
            // Taurus quest touch points (T-M03 / T-M04): solid greybox tiles of Clover Meadows (Tools/build-tau-greybox-rows.py RUNESTONE / ANCIENT_ROOTS), reached from the free tile beside them.
            new WaypointDef(TauRunestoneId, "tauCloverMeadows", "Brook Runestone", 38, 23, 38, 22, true),
            new WaypointDef(TauAncientRootsId, "tauCloverMeadows", "Ancient Roots", 53, 9, 55, 9, true),
            // Ancient Root Shrine (phase 06): the entrance crystal and the checkpoint crystal in front of the boss hall (a hero who falls wakes at the entrance and warps back in one touch), plus the Memory Amber (quest touch point).
            new WaypointDef(TauShrineGateId, "tauAncientRootShrine", "Ancient Root Shrine (Entrance)", 2, 41, 3, 41),
            new WaypointDef(TauShrineDeepId, "tauAncientRootShrine", "Ancient Root Shrine (Boss Hall)", 62, 45, 62, 46),
            new WaypointDef(TauMemoryAmberId, "tauAncientRootShrine", "Memory Amber", 43, 48, 43, 49, true),
        };
        public const string TauRunestoneId = "tauBrookRunestone", TauAncientRootsId = "tauAncientRoots", TauShrineGateId = "tauShrineGate", TauShrineDeepId = "tauShrineDeep", TauMemoryAmberId = "tauMemoryAmber";

        /// <summary>"Return to Clover Meadows": the exit row of the shrine crystals, always allowed, lands on the corridor of the dungeon door (Clover Meadows DoorArrival).</summary>
        public const string ShrineExitId = "shrine-exit", ShrineExitMapId = "tauCloverMeadows", ShrineExitName = "Clover Meadows (outside)";
        public const int ShrineExitTx = 57, ShrineExitTy = 6;
        public const string ShrineMapId = "tauAncientRootShrine";

        /// <summary>The exit row id of a crystal: the shrine's own, or Rootburrow's.</summary>
        public static string ExitFor(WaypointDef from) { return from != null && from.MapId == ShrineMapId ? ShrineExitId : ExitId; }
        public static string ExitNameFor(string exitId) { return exitId == ShrineExitId ? ShrineExitName : ExitName; }

        /// <summary>Crystals listed in the warp window of crystal `from`: the same dungeon only (a shrine crystal never offers Rootburrow's, nor the reverse).</summary>
        public static bool SameDungeon(WaypointDef a, WaypointDef b) { return a != null && b != null && (a.MapId == ShrineMapId) == (b.MapId == ShrineMapId); }

        public static ChestDef Chest(string id)
        {
            foreach (ChestDef c in Chests) if (c.Id == id) return c;
            return null;
        }

        public static WaypointDef Waypoint(string id)
        {
            foreach (WaypointDef w in Waypoints) if (w.Id == id) return w;
            return null;
        }

        public static IEnumerable<ChestDef> ChestsOn(string mapId)
        {
            foreach (ChestDef c in Chests) if (c.MapId == mapId) yield return c;
        }

        public static IEnumerable<WaypointDef> WaypointsOn(string mapId)
        {
            foreach (WaypointDef w in Waypoints) if (w.MapId == mapId) yield return w;
        }

        /// <summary>
        /// Null when a warp from crystal `fromId` (the one the hero stands at) to `toId` may go: both crystals unlocked by this character (the exit is always
        /// open), not the same crystal, the hero not hit in the last 5 s. The distance to the crystal is checked by the caller (it knows the tile size).
        /// </summary>
        public static string WarpRefusal(PlayerState p, string fromId, string toId, float msSinceHurt)
        {
            if (Waypoint(fromId) == null || Waypoint(fromId).QuestOnly || !p.HasWaypoint(fromId)) return "Touch the crystal first";
            if (toId == fromId) return "You are already here";
            if (toId != ExitId && toId != ShrineExitId && (Waypoint(toId) == null || Waypoint(toId).QuestOnly || !p.HasWaypoint(toId))) return "That crystal is not attuned yet";
            if (msSinceHurt < WarpCombatMs) return "Cannot warp during combat";
            return null;
        }

        /// <summary>Where a warp lands: map id + floor tile. False for an unknown id.</summary>
        public static bool Target(string toId, out string mapId, out int tx, out int ty)
        {
            if (toId == ExitId) { mapId = ExitMapId; tx = ExitTx; ty = ExitTy; return true; }
            if (toId == ShrineExitId) { mapId = ShrineExitMapId; tx = ShrineExitTx; ty = ShrineExitTy; return true; }
            WaypointDef w = Waypoint(toId);
            if (w == null) { mapId = null; tx = ty = 0; return false; }
            mapId = w.MapId; tx = w.ArriveTx; ty = w.ArriveTy;
            return true;
        }
    }
}
