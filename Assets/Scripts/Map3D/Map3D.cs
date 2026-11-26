using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Real-3D map prototype (22/11/2026, user: "thử làm cho tui map 3D giống Ragnarok 3"; ChatGPT tech-art spec
    /// docs/reference/willowbrook-3d-proto/map-3d-ro3-spec-chatgpt.md; plan plans/261122-1100-map-3d-proto/plan.md).
    /// Behind a flag, visual only: rows, solid tiles, roads, spawns, portals, the server and every position stay the 2D grid. On a 3D map the painted
    /// ground and the 2.5D border are replaced by a terrain mesh (Map3DTerrain: height outside the walkable tiles, 4-layer splat from the 2.5D ground),
    /// trees / rocks / logs / bushes of the prop table become generated meshes with ChatGPT-painted textures (Map3DProps), a real directional light with
    /// soft shadows, trilight ambient and linear fog light them (Map3DLighting), and the camera turns a full 360 deg. Heroes, monsters, pets and the props
    /// that stay sprites (ruins, shed, fence, reeds...) keep standing as billboards (View25D); the kept props become alpha-cutout so they take and cast shadows.
    /// Units on the 2D plane z = 0; world "up" is -z (View25D convention), 1 m = 1 tile = 16 units.
    /// Test builds: only with `--map3d` (2.5D is the game since 26/11/2026; `--map3d-test ...` flags need `--map3d` too). Release builds: off.
    /// </summary>
    public static class Map3D
    {
        /// <summary>Maps that have a 3D version (data: one id per converted map).</summary>
        static readonly string[] Maps = { WillowbrookMeadowsMap.MapId, TauWillowbrookVillageMap.MapId, TauCloverMeadowsMap.MapId }; // + Taurus village / meadows (24/11/2026 try, phase-09)
        static bool parsed, requested;

        static void Parse()
        {
            if (parsed) return;
            parsed = true;
            string[] a = Environment.GetCommandLineArgs();
            // 2.5D is the game (user 26/11: "chốt 2.5D": the 3D try was half-way, the art is 2D); the 3D map only with `--map3d` (was the default since 23/11).
            int i = Array.IndexOf(a, "--map3d");
            bool on = i >= 0 && !(i + 1 < a.Length && a[i + 1] == "off") && Array.IndexOf(a, "--map2d") < 0;
            requested = !ReleaseBuild.IsRelease && on;
        }

        /// <summary>Perf A/B test flags: `--map3d-test noshadow,noborder,lowshadow,noleaves,noimpostor`.</summary>
        public static bool Test(string what)
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--map3d-test");
            return i >= 0 && i + 1 < a.Length && a[i + 1].Contains(what);
        }

        /// <summary>3D maps are on (test build with `--map3d`).</summary>
        public static bool Requested { get { Parse(); return requested; } }

        /// <summary>The map with this id is drawn in 3D (flag on, map converted, 2.5D view available).</summary>
        public static bool IsMap3D(string id)
        {
            return Requested && !string.IsNullOrEmpty(id) && Array.IndexOf(Maps, id) >= 0 && View25D.IsMap25D(id);
        }

        /// <summary>The map on screen is drawn in 3D now.</summary>
        public static bool Active { get { return MapRegistry.Current != null && IsMap3D(MapRegistry.Current.Id); } }

        /// <summary>World units per metre (one tile).</summary>
        public const float M = GameConfig.TileUnits;

        /// <summary>World position of a ground point at tile coords (tx, ty) (y down like the rows, fractional) lifted h metres: up is -z.</summary>
        public static Vector3 World(float tx, float ty, float h)
        {
            return new Vector3(tx * M, GameConfig.MapH - ty * M, -h * M);
        }

        /// <summary>Builds the 3D scene of the map (terrain, water, light; the props come through MapScenery). Called by WorldBuilder instead of the ground sprites.</summary>
        public static void Build(Transform mapRoot, IGameMap map)
        {
            float t0 = Time.realtimeSinceStartup;
            Map3DBatcher.Clear();
            Map3DImpostors.Clear();
            Map3DHeightField field = Map3DHeightField.For(map);
            Map3DTerrain.Build(mapRoot, map, field);
            Map3DWater.Build(mapRoot, map, field);
            Map3DCreekDressing.Build(mapRoot, map, field); // Clover sunken creek: bank rocks + reeds (RO3 loop round 2)
            Map3DLakeWall.Build(map, field);               // village lake: stone embankment into deep water (25/11)
            Map3DLighting.Build(mapRoot);
            if (!Test("noborder")) Map3DBorderForest.Build(mapRoot, map, field);
            if (!Test("nograss")) Map3DGrass.Build(map, field);
            Debug.Log("[ZenoniaProto] Map3D " + map.Id + ": terrain + border built in " + Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms");
        }
    
        /// <summary>After MapScenery queued the prop meshes: merge them (Map3DBatcher).</summary>
        public static void Finish(Transform mapRoot)
        {
            Map3DBatcher.Flush(mapRoot);
            Map3DImpostors.Build(mapRoot);     // distant trees as baked billboards (22/11 perf pass 2)
            Map3DBakedShadow.Attach(mapRoot);  // + the sun shadow baked once (Medium; High too for the impostors)
        }
    }
}
