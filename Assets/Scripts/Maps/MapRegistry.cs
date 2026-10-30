using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// All maps by id and the one the local sim currently runs. Frost Hollow is the default (the online zone server, the headless bench and the Editor
    /// tests only know it); an offline game starts in the village unless `--map frost-hollow` (or a debug capture flag) says otherwise.
    /// Switching the current map only changes the data; MapTransfer rebuilds the scene objects around it.
    /// </summary>
    public static class MapRegistry
    {
        static readonly IGameMap[] Maps = { new FrostHollowLayout(), new OakhavenVillageMap() };

        public static IGameMap Current { get; private set; }
        /// <summary>True when the launch fixed the start map (`--map`, or a capture run): saved maps and "new characters start in the village" do not override it.</summary>
        public static bool Forced { get; private set; }
        /// <summary>Editor tests only: when set, ParseLaunch starts this map (as if `--map` said so) so the smoke test gets Frost Hollow's monsters.</summary>
        public static string TestStartMap;

        static MapRegistry() { Current = Maps[0]; }

        public static IGameMap Get(string id)
        {
            foreach (IGameMap m in Maps) if (m.Id == id) return m;
            return null;
        }

        /// <summary>Launch-arg lookup: case and dashes are ignored, so "frost-hollow", "frostHollow" and "frost" all find Frost Hollow; "village" finds Oakhaven.</summary>
        static IGameMap Find(string name)
        {
            string n = name.Replace("-", "").ToLowerInvariant();
            foreach (IGameMap m in Maps) if (m.Id.ToLowerInvariant() == n || m.Id.ToLowerInvariant().StartsWith(n) && n.Length >= 5) return m;
            return n == "village" ? Get(OakhavenVillageMap.MapId) : null;
        }

        /// <summary>Makes `id` the current map (unknown id: logs and keeps the current one). Returns the map now current.</summary>
        public static IGameMap SetCurrent(string id)
        {
            IGameMap m = Get(id);
            if (m == null) Debug.LogError("[ZenoniaProto] unknown map '" + id + "'");
            else Current = m;
            return Current;
        }

        /// <summary>Static state survives play sessions when Domain Reload is off: back to the default and no forced map.</summary>
        public static void ResetState() { Current = Maps[0]; Forced = false; }

        /// <summary>
        /// Picks the start map from the command line: `--map frost-hollow|oakhaven-village` (also "village" / "frost") wins; otherwise an offline
        /// normal start (no debug capture flags) begins in the village, everything else (server, host, client, captures) in Frost Hollow.
        /// </summary>
        public static void ParseLaunch(string[] args, bool captureRun)
        {
            ResetState();
            if (!NetMode.IsOffline) return; // the zone server only knows Frost Hollow for now
            IGameMap test = TestStartMap != null ? Get(TestStartMap) : null;
            if (test != null) { Current = test; Forced = true; return; }
            int i = Array.IndexOf(args, "--map");
            if (i >= 0 && i + 1 < args.Length)
            {
                IGameMap named = Find(args[i + 1]);
                if (named != null) { Current = named; Forced = true; return; }
                Debug.LogWarning("[ZenoniaProto] --map " + args[i + 1] + " is not a map; using the default");
            }
            if (captureRun) Forced = true; // capture / bot scripts are written for Frost Hollow's tiles: no save-driven map switch either
            else Current = Get(OakhavenVillageMap.MapId);
        }
    }
}
