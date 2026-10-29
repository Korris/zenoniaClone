using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Hollow spawner (web monster-spawner.js): fills every spawn zone on load, schedules a replacement per death
    /// (zone.respawnMs) at a random walkable tile at least 60 units from the hero, and places the Frostfang King once (never respawns).
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        const float MinSpawnDistanceFromHero = 60f, RetryMs = 500f;
        static MonsterSpawner instance;
        readonly List<float>[] pending = new List<float>[FrostHollowMap.Zones.Length];

        public static void SpawnAll(Transform parent)
        {
            var go = new GameObject("MonsterSpawner");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<MonsterSpawner>();
            for (int z = 0; z < FrostHollowMap.Zones.Length; z++)
            {
                instance.pending[z] = new List<float>();
                FrostHollowMap.Zone zone = FrostHollowMap.Zones[z];
                for (int i = 0; i < zone.Count; i++) instance.SpawnInZone(z, Vector2.zero, 0f);
            }
            Vector2 boss = FrostHollowMap.TileFeet(FrostHollowMap.BossTile.x, FrostHollowMap.BossTile.y, 0f);
            Monster.Create(parent, MonsterDef.FrostfangKing, boss, -1);
        }

        /// <summary>Schedules the replacement of a zone monster that just died (boss = zone -1 is ignored).</summary>
        public static void NotifyDeath(int zone)
        {
            if (instance == null || zone < 0 || zone >= instance.pending.Length) return;
            instance.pending[zone].Add(FrostHollowMap.Zones[zone].RespawnMs);
        }

        bool SpawnInZone(int z, Vector2 avoid, float minDist)
        {
            FrostHollowMap.Zone zone = FrostHollowMap.Zones[z];
            Vector2 feet;
            if (!FrostHollowMap.TryZoneSpot(zone, avoid, minDist, out feet)) return false;
            Monster.Create(transform.parent, MonsterDef.ByType(zone.Type), feet, z);
            return true;
        }

        void Update()
        {
            float dtMs = GameClock.Dt * 1000f;
            HeroController hero = HeroController.Instance;
            Vector2 heroPos = hero != null ? hero.Position : Vector2.zero;
            for (int z = 0; z < pending.Length; z++)
            {
                List<float> timers = pending[z];
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    timers[i] -= dtMs;
                    if (timers[i] > 0f) continue;
                    if (SpawnInZone(z, heroPos, MinSpawnDistanceFromHero)) timers.RemoveAt(i);
                    else timers[i] = RetryMs; // no free spot right now: retry shortly
                }
            }
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
