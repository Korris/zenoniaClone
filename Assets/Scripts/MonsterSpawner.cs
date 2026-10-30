using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Hollow spawner (web monster-spawner.js), owned by the Zone: fills every spawn zone on load, schedules a replacement per death
    /// (zone.respawnMs) at a random walkable tile at least 60 units from every hero, and places the Frostfang King once. Offline the king never
    /// returns in a session (the save flag decides); online the zone's BossRespawnTimer calls SpawnBoss 30 minutes after the kill.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        const float MinSpawnDistanceFromHero = 60f, RetryMs = 500f;
        readonly List<float>[] pending = new List<float>[FrostHollowMap.Zones.Length];

        public static void SpawnAll(Transform parent)
        {
            var go = new GameObject("MonsterSpawner");
            go.transform.SetParent(parent, false);
            MonsterSpawner spawner = go.AddComponent<MonsterSpawner>();
            if (Zone.Main != null) Zone.Main.Spawner = spawner;
            for (int z = 0; z < FrostHollowMap.Zones.Length; z++)
            {
                spawner.pending[z] = new List<float>();
                FrostHollowMap.Zone zone = FrostHollowMap.Zones[z];
                for (int i = 0; i < zone.Count; i++) spawner.SpawnInZone(z, 0f);
            }
            spawner.SpawnBoss();
        }

        /// <summary>Places the Frostfang King at its home tile (no-op while a living king exists).</summary>
        public void SpawnBoss()
        {
            if (Monster.Boss != null && !Monster.Boss.IsDying) return;
            Vector2 boss = FrostHollowMap.TileFeet(FrostHollowMap.BossTile.x, FrostHollowMap.BossTile.y, 0f);
            Monster.Create(transform.parent, MonsterDef.FrostfangKing, boss, -1);
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
            pending[zone].Add(FrostHollowMap.Zones[zone].RespawnMs);
        }

        bool SpawnInZone(int z, float minDist)
        {
            FrostHollowMap.Zone zone = FrostHollowMap.Zones[z];
            Vector2 feet = Vector2.zero;
            bool found = false;
            for (int attempt = 0; attempt < 30 && !found; attempt++) // same 30 random tries as before, now rejecting spots near ANY hero
            {
                if (!FrostHollowMap.TryZoneSpot(zone, Vector2.zero, 0f, out feet)) continue;
                found = minDist <= 0f || Zone.Main == null || !Zone.Main.Heroes.AnyWithin(feet, minDist);
            }
            if (!found) return false;
            Monster.Create(transform.parent, MonsterDef.ByType(zone.Type), feet, z);
            return true;
        }

        void Update()
        {
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

        void OnDestroy() { if (Zone.Main != null && Zone.Main.Spawner == this) Zone.Main.Spawner = null; }
    }
}
