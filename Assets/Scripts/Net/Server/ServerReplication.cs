using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Server -> client replication, run per player from ZoneServer: area-of-interest membership (Spawn / Despawn messages), the prioritised
    /// size-capped Snapshot of everything the player sees, and the AOI filtered event batches. All sends go through here so NetStats can count them.
    /// </summary>
    public sealed class ServerReplication
    {
        const int EventBatchMax = 40;
        struct DistKey<T> { public float D; public T S; }

        readonly NetDataWriter w = new NetDataWriter();
        readonly InterestGrid grid = new InterestGrid(GameConfig.MapW, GameConfig.MapH);
        readonly List<ushort> entered = new List<ushort>(32), left = new List<ushort>(32), gone = new List<ushort>(8);
        readonly List<DistKey<HeroSnap>> heroKeys = new List<DistKey<HeroSnap>>(64);
        readonly List<DistKey<MonsterSnap>> monsterKeys = new List<DistKey<MonsterSnap>>(64);
        readonly List<HeroSnap> heroSnaps = new List<HeroSnap>(64);
        readonly List<MonsterSnap> monsterSnaps = new List<MonsterSnap>(64);
        readonly NetStats stats;
        int[] cellX = new int[64], cellY = new int[64];

        public ServerReplication(NetStats netStats) { stats = netStats; }

        /// <summary>Rebuilds the cell index from every entity of the zone (a few hundred inserts per tick).</summary>
        public void RebuildGrid(Zone zone)
        {
            grid.Clear();
            IReadOnlyList<HeroController> heroes = zone.Heroes.All;
            for (int i = 0; i < heroes.Count; i++) grid.Set(heroes[i].NetId, heroes[i].Position.x, heroes[i].Position.y);
            for (int i = 0; i < zone.Monsters.Count; i++) { Vector2 f = zone.Monsters[i].Feet; grid.Set(zone.Monsters[i].NetId, f.x, f.y); }
            for (int i = 0; i < zone.Loot.Count; i++) { Vector2 f = zone.Loot[i].Target; grid.Set(zone.Loot[i].NetId, f.x, f.y); }
        }

        public bool InInterest(NetPlayer p, Vector2 at)
        {
            int cx, cy;
            if (p.Hero == null || !grid.TryCellOf(p.Hero.NetId, out cx, out cy)) return false;
            return System.Math.Max(System.Math.Abs(grid.CellX(at.x) - cx), System.Math.Abs(grid.CellY(at.y) - cy)) <= InterestSet.DropRadius;
        }

        // ---- Spawn / Despawn ----

        public void UpdateInterest(Zone zone, NetPlayer p)
        {
            int cx, cy;
            if (p.Hero == null || !grid.TryCellOf(p.Hero.NetId, out cx, out cy)) return;
            p.Interest.Update(grid, cx, cy, entered, left, gone);
            for (int i = 0; i < entered.Count; i++)
            {
                SpawnMsg m;
                if (TryBuildSpawn(zone.Ids.Get(entered[i]), entered[i], out m)) { m.Write(w); Send(p, true); }
            }
            for (int i = 0; i < left.Count; i++) SendDespawn(p, left[i], DespawnCause.LeftAoi);
            for (int i = 0; i < gone.Count; i++) SendDespawn(p, gone[i], DespawnCause.Removed);
        }

        void SendDespawn(NetPlayer p, ushort id, DespawnCause cause)
        {
            NetMessageWriter.Despawn(w, new DespawnMsg { Id = id, Cause = cause });
            Send(p, true);
        }

        /// <summary>Spawn description of a hero / monster / drop (false for an id that no longer exists).</summary>
        public static bool TryBuildSpawn(object entity, ushort id, out SpawnMsg m)
        {
            m = default(SpawnMsg);
            m.Id = id;
            HeroController h = entity as HeroController;
            if (h != null)
            {
                Vector2 p = h.Position;
                m.Kind = EntityKind.Hero; m.Type = (byte)h.Class.Id; m.X = NetQuantize.Pos(p.x); m.Y = NetQuantize.Pos(p.y); m.Facing = (byte)h.Facing;
                m.PlayerId = h.PlayerId; m.Name = h.PlayerName; m.Level = (byte)Mathf.Clamp(h.Rpg.Level, 1, 255); m.Look = NetEventBus.AppearanceOf(h);
                return true;
            }
            Monster mo = entity as Monster;
            if (mo != null)
            {
                Vector2 p = mo.Feet;
                m.Kind = EntityKind.Monster; m.Type = mo.DefIndex; m.X = NetQuantize.Pos(p.x); m.Y = NetQuantize.Pos(p.y); m.Facing = 0;
                m.Hp = (ushort)Mathf.Clamp(Mathf.CeilToInt(mo.Hp), 0, 65535); m.MaxHp = (ushort)Mathf.Clamp(Mathf.CeilToInt(mo.MaxHp), 1, 65535); m.State = mo.NetStateCode;
                return true;
            }
            LootDrop l = entity as LootDrop;
            if (l != null)
            {
                Vector2 p = l.Target;
                NetLootCodec.Fill(ref m, l.Entry);
                m.X = NetQuantize.Pos(p.x); m.Y = NetQuantize.Pos(p.y);
                m.OwnerPlayerId = l.OwnerId; // the owning hero's NetId (0 = free for all)
                m.FreeAtMs = (uint)Mathf.Max(0f, l.FreeAtMs);
                return true;
            }
            return false;
        }

        // ---- Snapshot ----

        public void SendSnapshot(Zone zone, NetPlayer p, uint serverMs)
        {
            if (p.Hero == null) return;
            Vector2 me = p.Hero.Position;
            heroKeys.Clear(); monsterKeys.Clear();
            foreach (ushort id in p.Interest.Visible)
            {
                object o = zone.Ids.Get(id);
                HeroController h = o as HeroController;
                if (h != null) { heroKeys.Add(new DistKey<HeroSnap> { D = id == p.Hero.NetId ? -1f : (h.Position - me).sqrMagnitude, S = h.ToSnap() }); continue; }
                Monster m = o as Monster;
                if (m != null) monsterKeys.Add(new DistKey<MonsterSnap> { D = (m.Feet - me).sqrMagnitude, S = m.ToSnap() });
            }
            heroKeys.Sort((a, b) => a.D.CompareTo(b.D));
            monsterKeys.Sort((a, b) => a.D.CompareTo(b.D));
            heroSnaps.Clear(); monsterSnaps.Clear();
            for (int i = 0; i < heroKeys.Count; i++) heroSnaps.Add(heroKeys[i].S);
            for (int i = 0; i < monsterKeys.Count; i++) monsterSnaps.Add(monsterKeys[i].S);
            // Delta against the newest snapshot the client acked, while the ring of sent snapshots still has it (otherwise a full snapshot).
            uint seq = ++p.SnapSeq, baseSeq = 0;
            SnapshotRing.Slot baseSlot;
            if (!NetLaunchArgs.NoDelta && p.AckSnapSeq != 0 && seq - p.AckSnapSeq < SnapshotRing.Size - 2 && p.SentRing.TryGet(p.AckSnapSeq, out baseSlot))
            {
                baseSeq = p.AckSnapSeq;
                p.BaselineLookup.Load(baseSlot);
            }
            SnapshotRing.Slot sent = p.SentRing.Begin(seq);
            SnapshotPacker.Write(w, seq, serverMs, (ushort)p.Input.State.LastSeq, baseSeq, baseSeq != 0 ? p.BaselineLookup : null, true, p.Hero.CaptureSim(),
                heroSnaps, monsterSnaps, ref p.HeroCursor, ref p.MonsterCursor, sent.Heroes, sent.Monsters);
            stats.OnSnapshotSent(w.Length);
            if (baseSeq == 0) stats.Add("snapshotsFull", 1);
            Send(p, false);
        }

        // ---- Events ----

        public void RouteEvents(IReadOnlyList<NetPlayer> players)
        {
            List<NetEventBus.Queued> pending = NetEventBus.Pending;
            if (pending.Count == 0) return;
            if (cellX.Length < players.Count) { cellX = new int[players.Count * 2]; cellY = new int[players.Count * 2]; }
            for (int pi = 0; pi < players.Count; pi++)
            {
                int cx = -1000, cy = -1000;
                if (players[pi].Hero != null) grid.TryCellOf(players[pi].Hero.NetId, out cx, out cy);
                cellX[pi] = cx; cellY[pi] = cy;
            }
            for (int i = 0; i < pending.Count; i++)
            {
                NetEventBus.Queued it = pending[i];
                int ex = grid.CellX(it.At.x), ey = grid.CellY(it.At.y);
                for (int pi = 0; pi < players.Count; pi++)
                {
                    NetPlayer p = players[pi];
                    if (p.Hero == null) continue;
                    bool take = it.ZoneWide || (it.OwnerHero != 0 ? it.OwnerHero == p.Hero.NetId : System.Math.Max(System.Math.Abs(ex - cellX[pi]), System.Math.Abs(ey - cellY[pi])) <= InterestSet.DropRadius);
                    if (take) p.OutEvents.Add(it.Ev);
                }
            }
            pending.Clear();
        }

        public void FlushEvents(NetPlayer p, uint serverMs)
        {
            int sent = 0;
            while (sent < p.OutEvents.Count)
            {
                int n = NetMessageWriter.EventBatch(w, serverMs, p.OutEvents, sent, EventBatchMax);
                if (n <= 0) break;
                stats.OnEventBatchSent(w.Length, n);
                Send(p, true);
                sent += n;
            }
            p.OutEvents.Clear();
        }

        // ---- plain messages ----

        public void SendWelcome(NetPlayer p, uint serverMs, int tickRate, int snapDiv)
        {
            NetMessageWriter.Welcome(w, new WelcomeMsg { PlayerId = p.PlayerId, HeroNetId = p.Hero.NetId, ZoneId = 0, MapId = 0, ServerTimeMs = serverMs, TickRate = (byte)tickRate, SnapDiv = (byte)snapDiv });
            Send(p, true);
        }

        public void SendSelfState(NetPlayer p)
        {
            HeroController h = p.Hero;
            var m = new SelfStateMsg
            {
                Hp = (ushort)Mathf.Clamp(Mathf.CeilToInt(h.Hp), 0, 65535), MaxHp = (ushort)Mathf.Clamp(Mathf.CeilToInt(h.MaxHp), 1, 65535),
                Mp = (ushort)Mathf.Clamp(Mathf.CeilToInt(h.Mp), 0, 65535), MaxMp = (ushort)Mathf.Clamp(Mathf.CeilToInt(h.MaxMp), 1, 65535),
                Exp = (uint)Mathf.Max(0, h.Rpg.Exp), Level = (byte)Mathf.Clamp(h.Rpg.Level, 1, 255), SkillCdLeft = h.SkillCooldownLeftMs,
            };
            NetMessageWriter.SelfState(w, m);
            p.LastSelf = m;
            Send(p, true);
        }

        public void SendRpgState(NetPlayer p, uint reqId, bool ok, string text)
        {
            HeroController h = p.Hero;
            var save = ZenoniaProto.Rpg.SaveSerializer.Capture(h.Rpg, true, h.Position.x, h.Position.y, false);
            NetMessageWriter.RpgState(w, new RpgStateMsg { ReqId = reqId, Result = (byte)(ok ? 1 : 0), Text = text, SaveJson = JsonUtility.ToJson(save) });
            Send(p, true);
        }

        void Send(NetPlayer p, bool reliable)
        {
            if (p.Peer == null || p.Peer.ConnectionState != ConnectionState.Connected) return;
            p.Peer.Send(w, reliable ? NetProtocol.ChannelReliable : NetProtocol.ChannelUnreliable, reliable ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);
        }

        public void SendReject(NetPeer peer, NetManager net, RejectReason reason, string text)
        {
            NetMessageWriter.Reject(w, new RejectMsg { Reason = reason, Text = text });
            net.DisconnectPeer(peer, w);
        }
    }
}
