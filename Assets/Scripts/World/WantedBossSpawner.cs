using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Places the daily Wanted Boss (an event raid boss, MonsterDef.IsWanted) in the zone whose map it stands on: exactly one, at its fixed tile (WantedBossDef.SpawnTx / SpawnTy), present all day.
    /// Zone authority only (dedicated server and host; offline and clients never get one: clients only see the proxy). Dying starts Zone.WantedTimer (RespawnMs, 30 min by default,
    /// --wanted-respawn-sec shortens it for tests); when the day changes the old boss is removed without any reward and the new one stands on its own map. Sits on the MonsterSpawner's object.
    /// </summary>
    public sealed class WantedBossSpawner : MonoBehaviour
    {
        /// <summary>Delay between the kill and the next boss (ms). --wanted-respawn-sec overrides it.</summary>
        public static float RespawnMs = BossRespawnTimer.DefaultRespawnMs;
        const float CheckEverySeconds = 5f;

        Zone homeZone;
        string mapId;
        float nextCheck;

        public static void Attach(GameObject spawnerObject, Zone zone, string mapId)
        {
            if (zone == null || NetMode.IsOffline || !NetMode.IsAuthority) return;
            WantedBossSpawner w = spawnerObject.AddComponent<WantedBossSpawner>();
            w.homeZone = zone; w.mapId = mapId;
            zone.WantedSpawner = w;
            w.Refresh();
        }

        static long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }

        /// <summary>Makes the zone match today's wanted boss: removes a boss of another day, then places today's when this is its map and nothing stands or waits to respawn.</summary>
        public void Refresh()
        {
            Zone.Enter(homeZone);
            WantedBossDef today = WantedBoss.Today(NowMs);
            Monster alive = homeZone.Wanted;
            if (alive != null && alive.IsDying) alive = null; // its respawn clock is running
            if (alive != null && alive.Type != today.BossType) { homeZone.Wanted = null; Destroy(alive.gameObject); homeZone.WantedTimer.Cancel(); alive = null; }
            if (today.MapId != mapId) { homeZone.WantedTimer.Cancel(); return; }
            if (alive != null || homeZone.WantedTimer.Active) return;
            if (homeZone.Wanted != null && !homeZone.Wanted.IsDying) return;
            MonsterDef def = MonsterDef.ByType(today.BossType);
            if (def == null) { Debug.LogError("[ZenoniaProto] wanted boss " + today.BossType + " has no MonsterDef"); return; }
            Monster.Create(transform.parent, def, FrostHollowMap.TileFeet(today.SpawnTx, today.SpawnTy, 0f), -1);
            Debug.Log("[ZenoniaProto] wanted boss " + def.Name + " stands at tile " + today.SpawnTx + "," + today.SpawnTy + " on " + mapId);
        }

        void Update()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + CheckEverySeconds; // the day turning is noticed within a few seconds
            Refresh();
        }

        void OnDestroy() { if (homeZone != null && homeZone.WantedSpawner == this) homeZone.WantedSpawner = null; }
    }
}
