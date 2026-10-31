using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// The party rules (PartyBook) on the wire. All zones of a server run in this one process, so a party simply outlives a portal: nothing here depends on the zone. Requests are applied
    /// as they arrive, the notices they cause go out in the same tick, and every member gets the roster when the party changes or, twice a second, when something on it did (HP, level, map).
    /// A dev host's own hero is not a connected player, so it has no party: remote players can party with each other only.
    /// </summary>
    public sealed class PartyService
    {
        const float RosterEveryMs = 500f;

        public static PartyService Active { get; private set; }

        public readonly PartyBook Book = new PartyBook();

        readonly Dictionary<ushort, NetPlayer> online = new Dictionary<ushort, NetPlayer>();
        readonly Dictionary<int, byte[]> rosterBytes = new Dictionary<int, byte[]>();
        readonly NetDataWriter w = new NetDataWriter();
        readonly NetStats stats;
        float nextRosterMs;

        public PartyService(NetStats netStats) { stats = netStats; Active = this; }

        public void Stop() { if (Active == this) Active = null; }

        public void OnJoin(NetPlayer p)
        {
            online[p.PlayerId] = p;
            Book.AddPlayer(p.PlayerId, p.Name, (byte)p.Hero.Class.Id);
        }

        public void OnLeave(NetPlayer p)
        {
            online.Remove(p.PlayerId);
            Book.RemovePlayer(p.PlayerId);
            Flush();
        }

        public void OnRequest(NetPlayer p, PartyRequestMsg m, float nowMs)
        {
            stats.Add("partyRequests", 1);
            Book.Apply(p.PlayerId, m, (long)nowMs);
            Flush();
        }

        /// <summary>Once per tick: runs out old invites and keeps the panels current.</summary>
        public void Tick(float nowMs)
        {
            Book.Tick((long)nowMs);
            Flush();
            if (!Book.TakeRosterChanged() && nowMs < nextRosterMs) return;
            nextRosterMs = nowMs + RosterEveryMs;
            PushRosters();
        }

        void Flush()
        {
            List<PartyNote> notes = Book.Notes;
            for (int i = 0; i < notes.Count; i++)
            {
                NetPlayer to;
                if (!online.TryGetValue(notes[i].To, out to)) continue; // the player the notice is about has just gone
                notes[i].Msg.Write(w);
                to.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
                stats.Add("partyNotices", 1);
            }
            notes.Clear();
        }

        /// <summary>Sends each player the roster it does not have yet. Compared byte for byte, so an idle party costs nothing on the wire.</summary>
        void PushRosters()
        {
            rosterBytes.Clear();
            foreach (NetPlayer p in online.Values)
            {
                int party = Book.PartyIdOf(p.PlayerId);
                byte[] bytes = null;
                if (party != 0 && !rosterBytes.TryGetValue(party, out bytes))
                {
                    bytes = BuildRoster(p.PlayerId);
                    rosterBytes[party] = bytes;
                }
                if (Same(bytes, p.LastPartyRoster)) continue;
                if (bytes == null)
                {
                    new PartyRosterMsg().Write(w);
                    p.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
                }
                else p.Peer.Send(bytes, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
                p.LastPartyRoster = bytes;
                stats.Add("partyRosters", 1);
            }
        }

        byte[] BuildRoster(ushort anyMember)
        {
            IReadOnlyList<ushort> ids = Book.MembersOf(anyMember);
            var infos = new PartyMemberInfo[ids.Count];
            for (int i = 0; i < infos.Length; i++)
            {
                NetPlayer m = online[ids[i]];
                HeroController h = m.Hero;
                infos[i] = new PartyMemberInfo
                {
                    PlayerId = m.PlayerId, Name = m.Name, HeroClass = (byte)h.Class.Id, Level = (byte)Mathf.Clamp(h.Rpg.Level, 1, 255),
                    Hp = (ushort)Mathf.Clamp(Mathf.CeilToInt(h.Hp), 0, 65535), MaxHp = (ushort)Mathf.Clamp(Mathf.CeilToInt(h.MaxHp), 1, 65535),
                    MapId = h.HomeZone != null && h.HomeZone.Map != null ? h.HomeZone.Map.Id : "",
                };
            }
            new PartyRosterMsg { Members = infos }.Write(w);
            return w.CopyData();
        }

        static bool Same(byte[] a, byte[] b)
        {
            if (a == b) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
