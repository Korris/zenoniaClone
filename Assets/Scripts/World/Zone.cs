using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One shared map instance: its heroes, monsters (+ the boss and kill counter), ground loot, the spawner and the world-boss respawn clock.
    /// Replaces the old Monster / LootDrop / MonsterSpawner statics (their getters forward to Zone.Main so most call sites are unchanged).
    /// Phase 1 runs a single zone (Main); several per process come with the multi-zone phase.
    /// </summary>
    public sealed class Zone
    {
        public static Zone Main { get; private set; }

        public readonly Transform Root;
        public readonly ZoneHeroes Heroes = new ZoneHeroes();
        public readonly List<Monster> Monsters = new List<Monster>();
        public readonly List<LootDrop> Loot = new List<LootDrop>();
        public readonly BossRespawnTimer BossTimer = new BossRespawnTimer();
        public MonsterSpawner Spawner;
        /// <summary>The living Frostfang King (HUD boss bar), or null.</summary>
        public Monster Boss;
        public int Kills;
        /// <summary>Zone sim clock in ms (advances with GameClock.Dt, so it pauses with hit-stop offline); loot priority windows run on it.</summary>
        public float TimeMs { get; private set; }
        /// <summary>Raised when the boss dies (online: zone-wide notification; offline: the save flag).</summary>
        public event Action<Monster> BossDied;

        Zone(Transform root) { Root = root; }

        public static Zone CreateMain(Transform root) { Main = new Zone(root); return Main; }
        public static void ResetMain() { Main = null; }

        /// <summary>Called first every frame (GameClock): advances the zone clock and the boss respawn timer.</summary>
        public void Advance(float dt)
        {
            float ms = dt * 1000f;
            TimeMs += ms;
            if (BossTimer.Tick(ms) && Spawner != null) Spawner.SpawnBoss();
        }

        /// <summary>A monster just died: bookkeeping, spawner replacement, rewards, and the boss timer online.</summary>
        public void OnMonsterDied(Monster m, int spawnZone)
        {
            Kills++;
            if (Boss == m) Boss = null;
            if (Spawner != null) Spawner.OnDeath(spawnZone);
            KillRewards.Resolve(this, m);
            if (!m.IsBoss) return;
            if (!NetMode.IsOffline) BossTimer.Start();
            if (BossDied != null) BossDied(m);
        }
    }
}
