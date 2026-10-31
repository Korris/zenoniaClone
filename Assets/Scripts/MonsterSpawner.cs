using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Monster spawner of the current map (web monster-spawner.js; MapSpawnTable picks the zones by map id), owned by the Zone: fills every spawn zone on
    /// load, schedules a replacement per death (zone.respawnMs) at a random walkable tile at least 60 units from every hero, and places the map's boss
    /// (MapSpawnTable.BossFor) once. Offline a boss never returns in a session (the save decides); online the zone's BossRespawnTimer calls SpawnBoss
    /// 30 minutes after the kill.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        const float MinSpawnDistanceFromHero = 60f, RetryMs = 500f;
        string mapId;
        Zone homeZone;
        FrostHollowMap.Zone[] zones;
        List<float>[] pending;

        public static void SpawnAll(Transform parent)
        {
            var go = new GameObject("MonsterSpawner");
            go.transform.SetParent(parent, false);
            MonsterSpawner spawner = go.AddComponent<MonsterSpawner>();
            spawner.mapId = MapRegistry.Current.Id;
            spawner.homeZone = Zone.Main;
            spawner.zones = MapSpawnTable.ZonesFor(spawner.mapId);
            spawner.pending = new List<float>[spawner.zones.Length];
            if (Zone.Main != null) Zone.Main.Spawner = spawner;
            for (int z = 0; z < spawner.zones.Length; z++)
            {
                spawner.pending[z] = new List<float>();
                FrostHollowMap.Zone zone = spawner.zones[z];
                for (int i = 0; i < zone.Count; i++)
                    if (!spawner.SpawnInZone(z, 0f)) Debug.LogWarning("[ZenoniaProto] no free spot for a " + zone.Type + " in " + spawner.mapId + " zone " + z + " (the rect may not fit the map's tiles)");
            }
            spawner.SpawnBoss();
        }

        /// <summary>Places the map's boss at its home tile (no-op while a living boss exists or on a map without a boss).</summary>
        public void SpawnBoss()
        {
            MonsterDef def = MapSpawnTable.BossFor(mapId);
            if (def == null) return;
            if (Monster.Boss != null && !Monster.Boss.IsDying) return;
            Vector2Int tile = MapSpawnTable.BossTileFor(mapId);
            Vector2 boss = FrostHollowMap.TileFeet(tile.x, tile.y, 0f);
            Monster.Create(transform.parent, def, boss, -1);
        }

        /// <summary>Compat for the old static call: schedules the replacement of a zone monster that just died.</summary>
        public static void NotifyDeath(int zone)
        {
            if (Zone.Main != null && Zone.Main.Spawner != null) Zone.Main.Spawner.OnDeath(zone);
        }

        /// <summary>Schedules the replacement of a zone monster that just died (boss = zone -1 is ignored).</summary>
        public void OnDeath(int zone)
        {
            if (zone < 0 || zone >= pending.Length) return;
            pending[zone].Add(zones[zone].RespawnMs);
        }

        /// <summary>The zone slept for `ms`: every scheduled respawn comes that much closer, the ones already due are placed on the next tick.</summary>
        public void FastForward(float ms)
        {
            for (int z = 0; z < pending.Length; z++) ZenoniaProto.Net.RespawnClock.FastForward(pending[z], ms);
        }

        bool SpawnInZone(int z, float minDist)
        {
            FrostHollowMap.Zone zone = zones[z];
            Vector2 feet = Vector2.zero;
            bool found = false;
            for (int attempt = 0; attempt < 30 && !found; attempt++) // same 30 random tries as before, now rejecting spots near ANY hero
            {
                if (!FrostHollowMap.TryZoneSpot(zone, Vector2.zero, 0f, out feet)) continue;
                found = !MapSpawnTable.NearEntrance(MapRegistry.Current, feet) && (minDist <= 0f || Zone.Main == null || !Zone.Main.Heroes.AnyWithin(feet, minDist));
            }
            if (!found) return false;
            Monster.Create(transform.parent, MonsterDef.ByType(zone.Type), feet, z);
            return true;
        }

        void Update()
        {
            Zone.Enter(homeZone);
            float dtMs = GameClock.Dt * 1000f;
            for (int z = 0; z < pending.Length; z++)
            {
                List<float> timers = pending[z];
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    timers[i] -= dtMs;
                    if (timers[i] > 0f) continue;
                    if (SpawnInZone(z, MinSpawnDistanceFromHero)) timers.RemoveAt(i);
                    else timers[i] = RetryMs; // no free spot right now: retry shortly
                }
            }
        }

        void OnDestroy() { if (homeZone != null && homeZone.Spawner == this) homeZone.Spawner = null; }
    }
}
