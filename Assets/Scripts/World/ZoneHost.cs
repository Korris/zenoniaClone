using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Every zone this server process runs, on one clock. A dedicated server makes one Zone per map in MapRegistry (a new map needs nothing here), each under its own root object, each
    /// with its own monsters, boss, spawner, AOI grid and event list. A dev host keeps the one zone it renders, because the screen can only show one map. A zone with no heroes
    /// is paused: its root is deactivated, so Unity stops updating everything in it and it costs nothing per tick; on wake the time it slept is paid back to the respawn clocks,
    /// the boss timer and the drops (Zone.FastForward). All awake zones read the same time, so ServerMs and a client's clock estimate survive a portal.
    /// </summary>
    public sealed class ZoneHost
    {
        public static ZoneHost Active { get; private set; }

        readonly List<Zone> zones = new List<Zone>();
        readonly Dictionary<string, Zone> byMap = new Dictionary<string, Zone>();

        public IReadOnlyList<Zone> Zones { get { return zones; } }
        /// <summary>Where new heroes begin: Frost Hollow unless the server was started with --map.</summary>
        public Zone Start { get; private set; }
        public float ClockMs { get; private set; }
        public int ZonesAwake { get { int n = 0; for (int i = 0; i < zones.Count; i++) if (!zones[i].Paused) n++; return n; } }
        /// <summary>False on a dev host: one zone and it is the root of everything on screen.</summary>
        public bool MultiZone { get { return zones.Count > 1; } }

        public static void ResetState() { Active = null; }

        /// <summary>Dedicated server: builds every registered map as a zone and fills the hostile ones. The default map stays current between ticks.</summary>
        public static ZoneHost CreateDedicated(Transform world, string startMapName)
        {
            var host = new ZoneHost();
            IGameMap start = MapRegistry.Current;
            if (!string.IsNullOrEmpty(startMapName))
            {
                IGameMap named = MapRegistry.FindLaunch(startMapName);
                if (named != null) start = named;
                else Debug.LogWarning("[ZenoniaProto] --map " + startMapName + " is not a map; heroes start on " + start.Id);
            }
            foreach (IGameMap map in MapRegistry.All)
            {
                var go = new GameObject("Zone_" + map.Id);
                go.transform.SetParent(world, false);
                Zone zone = Zone.CreateHosted(go.transform, map);
                zone.Pump = new ZoneBehaviourPump(go.transform, world);
                host.zones.Add(zone);
                host.byMap[map.Id] = zone;
                Zone.Enter(zone);
                if (map.Hostile) MonsterSpawner.SpawnAll(zone.Root);
            }
            host.CheckPortalTargets();
            host.Start = host.byMap[start.Id];
            Zone.Enter(host.Start);
            Active = host;
            return host;
        }

        /// <summary>Dev host: the zone already on screen, whichever map that is. It never pauses (its root holds the camera and the HUD).</summary>
        public static ZoneHost CreateSingle(Zone main)
        {
            var host = new ZoneHost();
            main.AdoptMap(MapRegistry.Current);
            host.zones.Add(main);
            host.byMap[main.Map.Id] = main;
            host.Start = main;
            host.ClockMs = main.TimeMs;
            Active = host;
            return host;
        }

        /// <summary>Back to plain offline bookkeeping (leaving host mode): the one zone follows MapRegistry again.</summary>
        public void Stop()
        {
            if (Active == this) Active = null;
            if (!MultiZone && zones.Count == 1) zones[0].AdoptMap(null);
        }

        public Zone ZoneOf(string mapId)
        {
            Zone z;
            return byMap.TryGetValue(mapId, out z) ? z : null;
        }

        /// <summary>Called first every frame (GameClock): one clock, every awake zone advanced with its own map current.</summary>
        public void Advance(float dt)
        {
            ClockMs += dt * 1000f;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].Paused) continue;
                Zone.Enter(zones[i]);
                zones[i].Advance(dt);
            }
        }

        /// <summary>Runs the skill objects of every awake zone (after the frame's hero and monster updates).</summary>
        public void TickPumps()
        {
            for (int i = 0; i < zones.Count; i++)
            {
                Zone z = zones[i];
                if (z.Paused || z.Pump == null) continue;
                Zone.Enter(z);
                z.Pump.Tick();
            }
        }

        /// <summary>Wakes a paused zone before a hero is placed in it; the current zone is then this one.</summary>
        public void Wake(Zone z)
        {
            bool wasPaused = z.Paused;
            float slept = z.Idle.Wake(ClockMs);
            if (wasPaused) z.Root.gameObject.SetActive(true);
            Zone.Enter(z);
            if (wasPaused) z.FastForward(slept, ClockMs);
        }

        /// <summary>Once per tick: pauses the zones whose grace ran out. Returns how many paused.</summary>
        public int PauseEmptyZones(float dtMs)
        {
            if (!MultiZone) return 0;
            int paused = 0;
            for (int i = 0; i < zones.Count; i++)
            {
                Zone z = zones[i];
                if (!z.Idle.TickAwake(z.Heroes.Count, dtMs)) continue;
                Zone.Enter(z);
                z.Pump.Adopt(); // moves the buff ticker out if this zone holds it: it serves the other zones too
                ComboProjectile.RetireIn(z);
                z.Idle.MarkPaused(ClockMs);
                z.Root.gameObject.SetActive(false);
                paused++;
            }
            return paused;
        }

        /// <summary>A portal to a map nobody registered, or an arrival inside a wall, would strand a player; a new map gets this check by being registered.</summary>
        void CheckPortalTargets()
        {
            foreach (Zone z in zones)
                foreach (MapPortal p in z.Map.PortalsFor(true))
                {
                    IGameMap target = MapRegistry.Get(p.TargetMapId);
                    if (target == null) Debug.LogError("[ZenoniaProto] " + z.Map.Id + " has a portal to unknown map " + p.TargetMapId);
                    else if (target.IsSolidTile(p.TargetTx, p.TargetTy)) Debug.LogError("[ZenoniaProto] portal " + z.Map.Id + " -> " + p.TargetMapId + " arrives on a solid tile " + p.TargetTx + "," + p.TargetTy);
                }
        }
    }
}
