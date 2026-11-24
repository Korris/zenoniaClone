using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Tells every client the state of today's Wanted Boss (WantedStatusMsg), whatever map it plays on: all zones of a server run in this one process, so the boss's zone is
    /// simply looked at once per tick. A joining player gets the status at once; everybody gets it again when it is news (the boss died, returned, or the day turned:
    /// WantedStatusRules.IsNews). Read only: who is paid stays with WantedBossCredit. A dev host that does not show the boss's map has no boss anywhere, so it sends nothing.
    /// </summary>
    public sealed class WantedStatusService
    {
        readonly ZoneHost host;
        readonly NetStats stats;
        readonly NetDataWriter w = new NetDataWriter();
        WantedStatusMsg last;
        bool hasLast;

        public WantedStatusService(ZoneHost zoneHost, NetStats netStats) { host = zoneHost; stats = netStats; }

        /// <summary>The status right now, from the zone of today's boss; false when this server does not run that map.</summary>
        bool TryCurrent(out WantedStatusMsg m)
        {
            if (!WantedBoss.Enabled) { m = default(WantedStatusMsg); return false; }
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            WantedBossDef today = WantedBoss.Today(now);
            Zone zone = host.ZoneOf(today.MapId);
            if (zone == null) { m = default(WantedStatusMsg); return false; }
            Monster boss = zone.Wanted;
            bool standing = boss != null && !boss.IsDying && boss.Type == today.BossType;
            m = WantedStatusRules.Build(today.BossType, WantedBoss.DayKey(now), standing, zone.WantedTimer.Active, zone.WantedTimer.LeftMs,
                zone.Idle.SleptMs((float)host.ClockMs), WantedBoss.NextResetUtcMs(now) - now);
            return true;
        }

        /// <summary>After the join: the newcomer learns the status without waiting for the next change.</summary>
        public void SendTo(NetPlayer p)
        {
            WantedStatusMsg m;
            if (!TryCurrent(out m)) return;
            Send(p, m);
        }

        /// <summary>Once per tick: sends the status to everybody when it changed since the last broadcast.</summary>
        public void Tick(List<NetPlayer> players)
        {
            WantedStatusMsg m;
            if (!TryCurrent(out m)) return;
            if (hasLast && !WantedStatusRules.IsNews(last, m)) return;
            last = m; hasLast = true;
            for (int i = 0; i < players.Count; i++) Send(players[i], m);
            stats.Add("wantedStatusBroadcasts", 1);
            Debug.Log("[ZenoniaProto] wanted: status " + m.BossType + " day " + m.DayKey + (m.Alive ? " alive" : " back in " + m.RespawnInMs / 1000u + " s") + " -> " + players.Count + " players");
        }

        void Send(NetPlayer p, WantedStatusMsg m)
        {
            if (p.Peer == null || p.Peer.ConnectionState != ConnectionState.Connected) return;
            m.Write(w);
            p.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }
    }
}
