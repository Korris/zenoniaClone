using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// What the big map window (WorldMapWindow) shows of a map, read from data the game already has, nothing sent: the level band (MapSpawnTable zones),
    /// the monster camps (zones on the same tile rect merged, so "mantis + wolf" in one cave is one camp Lv6-7), and one exit per neighbour map
    /// (a wide road's pair of trigger tiles and several doors to the same map close together count once). Positions are tile rects / tiles (row 0 = top).
    /// </summary>
    public static class WorldMapData
    {
        public struct Camp { public RectInt Tiles; public int MinLv, MaxLv; }
        public struct Exit { public int Tx, Ty; public string TargetName; }

        const int ExitMergeTiles = 4; // portals to the same map this close are one door on the big map

        /// <summary>"Lv. 6-9" from the map's camps (the boss's level when it has no camps, "Safe Zone" for a town).</summary>
        public static string LevelText(IGameMap map)
        {
            if (map == null || !map.Hostile) return "Safe Zone";
            int min = int.MaxValue, max = 0;
            foreach (FrostHollowMap.Zone z in MapSpawnTable.ZonesFor(map.Id))
            {
                MonsterDef d = MonsterDef.ByType(z.Type);
                if (d == null) continue;
                min = Mathf.Min(min, d.ItemLevel); max = Mathf.Max(max, d.ItemLevel);
            }
            if (max == 0)
            {
                MonsterDef boss = MapSpawnTable.BossFor(map.Id);
                if (boss == null) return "";
                min = max = boss.ItemLevel;
            }
            return min == max ? "Lv. " + min : "Lv. " + min + "-" + max;
        }

        /// <summary>Monster camps of a hostile map (none in a town).</summary>
        public static List<Camp> Camps(IGameMap map)
        {
            var camps = new List<Camp>();
            if (map == null || !map.Hostile) return camps;
            foreach (FrostHollowMap.Zone z in MapSpawnTable.ZonesFor(map.Id))
            {
                MonsterDef d = MonsterDef.ByType(z.Type);
                if (d == null) continue;
                var rect = new RectInt(z.Tx, z.Ty, z.W, z.H);
                int i = camps.FindIndex(c => c.Tiles.Equals(rect));
                if (i < 0) { camps.Add(new Camp { Tiles = rect, MinLv = d.ItemLevel, MaxLv = d.ItemLevel }); continue; }
                Camp c0 = camps[i];
                c0.MinLv = Mathf.Min(c0.MinLv, d.ItemLevel); c0.MaxLv = Mathf.Max(c0.MaxLv, d.ItemLevel);
                camps[i] = c0;
            }
            return camps;
        }

        /// <summary>One exit per door: portals to the same map within ExitMergeTiles of each other are merged (their middle tile).</summary>
        public static List<Exit> Exits(IGameMap map)
        {
            var exits = new List<Exit>();
            var counts = new List<int>();
            var sums = new List<Vector2>();
            if (map == null) return exits;
            foreach (MapPortal p in map.Portals)
            {
                IGameMap target = MapRegistry.Get(p.TargetMapId);
                string name = target != null ? target.Name : p.TargetMapId;
                int i = exits.FindIndex(e => e.TargetName == name && Mathf.Abs(e.Tx - p.Tx) <= ExitMergeTiles && Mathf.Abs(e.Ty - p.Ty) <= ExitMergeTiles);
                if (i < 0) { exits.Add(new Exit { Tx = p.Tx, Ty = p.Ty, TargetName = name }); counts.Add(1); sums.Add(new Vector2(p.Tx, p.Ty)); continue; }
                counts[i]++; sums[i] += new Vector2(p.Tx, p.Ty);
                Vector2 mid = sums[i] / counts[i];
                exits[i] = new Exit { Tx = Mathf.RoundToInt(mid.x), Ty = Mathf.RoundToInt(mid.y), TargetName = name };
            }
            return exits;
        }

        /// <summary>Is the villager a shop keeper (gear or potions)?</summary>
        public static bool IsShop(string npcId) { return Rpg.GearShops.IsSeller(npcId) || Rpg.PotionShops.IsSeller(npcId); }
    }
}
