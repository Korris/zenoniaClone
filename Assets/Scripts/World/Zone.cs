using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// One shared map instance: its heroes, monsters (+ the boss and kill counter), ground loot, the spawner, the world-boss respawn clock, its AOI grid and its events.
    /// Replaces the old Monster / LootDrop / MonsterSpawner statics: their getters forward to Zone.Main, the zone the running code belongs to. Offline and on a client that is
    /// the only zone; on a server (ZoneHost) every entity selects its own zone at the top of its Update, so most call sites stay unchanged. See ZoneHost and ZoneBehaviourPump.
    /// </summary>
    public sealed class Zone
    {
        public static Zone Main { get; private set; }

        public readonly Transform Root;
        /// <summary>Shared NetId space of this zone (heroes, monsters, loot) and id -> entity lookup.</summary>
        public readonly NetEntityRegistry Ids = new NetEntityRegistry();
        public readonly ZoneHeroes Heroes;
        public readonly List<Monster> Monsters = new List<Monster>();
        public readonly List<LootDrop> Loot = new List<LootDrop>();
        public readonly BossRespawnTimer BossTimer = new BossRespawnTimer();
        /// <summary>Server only: who plays here, what happened this tick (NetEventBus) and the cell index of this zone's entities.</summary>
        public readonly List<NetPlayer> Players = new List<NetPlayer>();
        public readonly List<NetEventBus.Queued> Events = new List<NetEventBus.Queued>(128);
        public readonly InterestGrid Grid = new InterestGrid(GameConfig.MapW, GameConfig.MapH);
        public readonly ZoneIdleClock Idle = new ZoneIdleClock();
        /// <summary>Multi-zone server only: runs this zone's skill objects with the zone current.</summary>
        public ZoneBehaviourPump Pump;
        public MonsterSpawner Spawner;
        /// <summary>The living boss of this map (HUD boss bar), or null.</summary>
        public Monster Boss;
        public int Kills;
        /// <summary>Zone sim clock in ms (advances with GameClock.Dt, so it pauses with hit-stop offline); loot priority windows run on it.</summary>
        public float TimeMs { get; private set; }
        /// <summary>The map of a server zone; null offline and on a client, where MapRegistry.Current is simply the map on screen.</summary>
        public IGameMap Map { get; private set; }
        public bool Paused { get { return Idle.Paused; } }
        /// <summary>Raised when the boss dies (online: zone-wide notification; offline: the save flag).</summary>
        public event Action<Monster> BossDied;

        Zone(Transform root) { Root = root; Heroes = new ZoneHeroes(Ids, this); }

        public static Zone CreateMain(Transform root) { Main = new Zone(root); return Main; }
        public static void ResetMain() { Main = null; }

        /// <summary>A zone of a multi-zone server; the caller makes it current with Enter.</summary>
        public static Zone CreateHosted(Transform root, IGameMap map) { return new Zone(root) { Map = map }; }

        /// <summary>Host mode: this zone now stands for `map` (null when going back offline).</summary>
        public void AdoptMap(IGameMap map) { Map = map; }

        /// <summary>Makes `z` the zone (and its map the map) that the running code sees. A no-op offline and on a client, where only one zone exists.</summary>
        public static void Enter(Zone z)
        {
            if (z == null || z == Main) return;
            Main = z;
            if (z.Map != null) MapRegistry.UseZoneMap(z.Map);
        }

        /// <summary>Called first every frame (GameClock, or ZoneHost for every awake zone): advances the zone clock and the boss respawn timer.</summary>
        public void Advance(float dt)
        {
            float ms = dt * 1000f;
            TimeMs += ms;
            Ids.Tick(TimeMs);
            if (BossTimer.Tick(ms) && Spawner != null) Spawner.SpawnBoss();
        }

        /// <summary>Waking from a pause: everything that runs on time gets the slept `ms` at once, so a respawn or the boss comes due exactly as if the zone had kept running.</summary>
        public void FastForward(float ms, float nowMs)
        {
            TimeMs = nowMs;
            Ids.Tick(TimeMs);
            if (BossTimer.Tick(ms) && Spawner != null) Spawner.SpawnBoss();
            if (Spawner != null) Spawner.FastForward(ms);
            for (int i = 0; i < Loot.Count; i++) Loot[i].FastForward(ms);
            for (int i = 0; i < Monsters.Count; i++) Monsters[i].FastForwardDeath(ms);
        }

        /// <summary>A monster just died: bookkeeping, spawner replacement, rewards, and the boss timer online.</summary>
        public void OnMonsterDied(Monster m, int spawnZone)
        {
            Kills++;
            if (Boss == m) Boss = null;
            if (Spawner != null) Spawner.OnDeath(spawnZone);
            KillRewards.Resolve(this, m);
            if (!m.IsBoss) return;
            if (!NetMode.IsOffline && NetMode.IsAuthority) BossTimer.Start();
            NetEventBus.BossDied(m);
            if (BossDied != null) BossDied(m);
        }
    }
}
