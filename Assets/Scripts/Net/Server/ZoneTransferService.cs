using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Server-authoritative portals. After the sim of each tick it looks at where every hero's feet are, with the same tile rule as offline; the client never asks. A hero on a portal
    /// tile that its own save allows (a boss gate is per hero: PortalsFor) leaves the old zone, joins the target zone on the arrival tile with a new NetId and is told with a reliable
    /// ZoneChanged. Everything tied to the old zone is dropped in the same step: what the player could see, the delta baselines, its queued input and the skills it left behind.
    /// </summary>
    public sealed class ZoneTransferService
    {
        readonly ZoneHost host;
        readonly ServerReplication repl;
        readonly NetStats stats;

        /// <summary>False with --no-portals (load tests keep every bot on one zone) and on a host with a single zone. Clients are told, so they draw no dead portals.</summary>
        public bool PortalsLive { get; private set; }

        public ZoneTransferService(ZoneHost zoneHost, ServerReplication replication, NetStats netStats)
        {
            host = zoneHost; repl = replication; stats = netStats;
            PortalsLive = host.MultiZone && !NetLaunchArgs.NoPortals;
        }

        /// <summary>Once per tick, after the sim: moves every hero that walked onto a portal it may use.</summary>
        public void CheckPortals(float dtMs)
        {
            if (!PortalsLive) return;
            IReadOnlyList<Zone> zones = host.Zones;
            for (int z = 0; z < zones.Count; z++)
            {
                Zone zone = zones[z];
                if (zone.Paused || zone.Players.Count == 0) continue;
                IGameMap map = zone.Map;
                bool ownBoss = MapSpawnTable.HasOwnBoss(map.Id);
                for (int i = zone.Players.Count - 1; i >= 0; i--) // backward: a transfer takes the player out of this list
                {
                    NetPlayer p = zone.Players[i];
                    HeroController h = p.Hero;
                    if (h == null) continue;
                    MapPortal portal;
                    Vector2 feet = h.Position;
                    bool onTrigger = MapPortals.TryFind(map.PortalsFor(p.Bosses.IsDefeatedOn(map.Id, ownBoss)), feet.x, feet.y, out portal) && !h.IsDead;
                    if (p.Portals.Tick(onTrigger, dtMs)) Move(p, zone, portal);
                }
            }
        }

        void Move(NetPlayer p, Zone from, MapPortal portal)
        {
            Zone to = host.ZoneOf(portal.TargetMapId);
            if (to == null || to == from) { p.Portals.Disarm(); return; } // ZoneHost already logged a portal to nowhere
            HeroController hero = p.Hero;

            Zone.Enter(from);
            from.Pump.DestroyOwnedBy(hero);
            ComboProjectile.RetireFor(hero);
            from.Players.Remove(p);
            from.Heroes.Remove(hero);

            host.Wake(to); // also makes `to` the current zone
            hero.transform.SetParent(to.Root, false);
            hero.JoinZone(to);
            hero.ArriveInZone(FrostHollowMap.TileFeet(portal.TargetTx, portal.TargetTy, 2f), 0);
            to.Players.Add(p);
            p.OnZoneTransfer();

            // Reliable and ordered: the client has the new map id before the first Spawn of the new zone, and every Spawn / Despawn of the old zone before it.
            repl.SendZoneChanged(p, to.Map.Id);
            repl.RebuildGrid(to);
            repl.UpdateInterest(to, p);
            repl.SendSelfState(p);
            p.RpgDirty = true; // the mirrored save names the map the hero is on
            stats.Add("zoneTransfers", 1);
            Debug.Log("[ZenoniaProto] net: " + p.Name + " " + from.Map.Id + " -> " + to.Map.Id + " (epoch " + p.Epoch + ")");
        }
    }
}
