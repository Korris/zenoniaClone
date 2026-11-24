using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// All maps by id and the one the local sim runs. Taurus V4 (24/11/2026): <see cref="Live"/> holds the Taurus world (new heroes begin on <see cref="StartMapId"/>);
    /// <see cref="Legacy"/> keeps the pre-Taurus maps compiled but OFF the live game: they only exist in <see cref="All"/> / Get / FindLaunch when
    /// WorldFlags.LegacyEnabled is set (`--legacy-maps`, the dotnet tests, old capture / bot runs, which then still default to Frost Hollow).
    /// Switching the current map only changes the data; MapTransfer rebuilds the scene objects around it. On a zone server Current follows the running zone (Zone.Enter).
    /// </summary>
    public static class MapRegistry
    {
        /// <summary>The Taurus world: Willowbrook Village + Clover Meadows (phase 02, greybox); the Ancient Root Shrine joins in phase 06.</summary>
        static readonly IGameMap[] LiveMaps = { new TauWillowbrookVillageMap(), new TauCloverMeadowsMap(), new TauAncientRootShrineMap() };
        static readonly IGameMap[] LegacyMaps = { new FrostHollowLayout(), new OakhavenVillageMap(), new SunspireOasisMap(), new AmberDunesMap(), new FrostfangDenMap(), new EmberfenMarshMap(), new SunkenReliquaryMap(), new ThatchVillageMap(), new WillowbrookMeadowsMap(), new BramblewoodForestMap(), new RootburrowMap(), new RootburrowB2Map(), new RootburrowB3Map(), new ThornheartChamberMap(), new SunbreakFoothillsMap(), new ScorpionGorgeMap() };
        static IGameMap[] allWithLegacy;

        /// <summary>The map id new characters start on (offline, host and server).</summary>
        public const string StartMapId = WorldFlags.StartMapId;
        public static System.Collections.Generic.IReadOnlyList<IGameMap> Live { get { return LiveMaps; } }
        public static System.Collections.Generic.IReadOnlyList<IGameMap> Legacy { get { return LegacyMaps; } }
        /// <summary>The pre-Taurus maps take part (WorldFlags.LegacyEnabled).</summary>
        public static bool IncludeLegacy { get { return WorldFlags.LegacyEnabled; } }

        public static IGameMap Current { get; private set; }
        /// <summary>Every playable map: Live, plus Legacy when IncludeLegacy. The zone server builds one zone per entry, so registering a map is all it takes to play it online.</summary>
        public static System.Collections.Generic.IReadOnlyList<IGameMap> All
        {
            get
            {
                if (!IncludeLegacy) return LiveMaps;
                if (allWithLegacy == null) { allWithLegacy = new IGameMap[LiveMaps.Length + LegacyMaps.Length]; LiveMaps.CopyTo(allWithLegacy, 0); LegacyMaps.CopyTo(allWithLegacy, LiveMaps.Length); }
                return allWithLegacy;
            }
        }
        /// <summary>True when the launch fixed the start map (`--map`, or a capture run): saved maps and "new characters start in the village" do not override it.</summary>
        public static bool Forced { get; private set; }
        /// <summary>Editor tests only: when set, ParseLaunch starts this map (as if `--map` said so) so the smoke test gets Frost Hollow's monsters.</summary>
        public static string TestStartMap;

        /// <summary>The map before anything picks one: Frost Hollow with legacy on (headless bench, Editor tests, old captures rely on it), else the Taurus start map.</summary>
        static IGameMap Default { get { return IncludeLegacy ? LegacyMaps[0] : LiveMaps[0]; } }

        static MapRegistry() { Current = Default; }

        public static IGameMap Get(string id)
        {
            foreach (IGameMap m in All) if (m.Id == id) return m;
            return null;
        }

        /// <summary>Launch-arg lookup: case and dashes are ignored, so "frost-hollow", "frostHollow" and "frost" all find Frost Hollow; "village" finds the start village.</summary>
        public static IGameMap FindLaunch(string name)
        {
            string n = name.Replace("-", "").ToLowerInvariant();
            foreach (IGameMap m in All) if (m.Id.ToLowerInvariant() == n || m.Id.ToLowerInvariant().StartsWith(n) && n.Length >= 5) return m;
            return n == "village" ? Get(StartMapId) : null;
        }

        /// <summary>Makes `id` the current map (unknown id: logs and keeps the current one). Returns the map now current.</summary>
        public static IGameMap SetCurrent(string id)
        {
            IGameMap m = Get(id);
            if (m == null) Debug.LogError("[ZenoniaProto] unknown map '" + id + "'");
            else Current = m;
            return Current;
        }

        /// <summary>Server only (Zone.Enter): the zone whose code runs next collides with its own map.</summary>
        public static void UseZoneMap(IGameMap map) { Current = map; }

        /// <summary>Static state survives play sessions when Domain Reload is off: back to the default and no forced map.</summary>
        public static void ResetState() { Current = Default; Forced = false; }

        /// <summary>
        /// Picks the start map from the command line: `--map <id>` (also "village"; the old ids need --legacy-maps) wins; otherwise an offline normal start
        /// (no debug capture flags) begins on StartMapId, everything else (server, host, client, captures) on Default (Frost Hollow with legacy on).
        /// </summary>
        public static void ParseLaunch(string[] args, bool captureRun)
        {
            ResetState();
            if (!NetMode.IsOffline) return; // online the server says where a hero is (Welcome); ZoneHost reads a server's --map
            IGameMap test = TestStartMap != null ? Get(TestStartMap) : null;
            if (test != null) { Current = test; Forced = true; return; }
            int i = Array.IndexOf(args, "--map");
            if (i >= 0 && i + 1 < args.Length)
            {
                IGameMap named = FindLaunch(args[i + 1]);
                if (named != null) { Current = named; Forced = true; return; }
                Debug.LogWarning("[ZenoniaProto] --map " + args[i + 1] + " is not a map; using the default");
            }
            if (captureRun) Forced = true; // capture / bot scripts are written for Frost Hollow's tiles: no save-driven map switch either
            else Current = Get(StartMapId);
        }
    }
}
