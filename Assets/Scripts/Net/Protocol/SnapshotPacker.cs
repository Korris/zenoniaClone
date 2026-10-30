using System.Collections.Generic;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    public enum SnapshotReadResult { Ok, Malformed, MissingBaseline }

    /// <summary>
    /// Builds / reads the size-capped, delta coded snapshot. Layout: op, seq (4), server ms (4), last simulated input seq of the receiver (2), baseline
    /// distance (1, 0 = none), flags (1: bit 0 owner sim follows), [owner sim], hero count, hero entries, monster count, monster entries.
    /// Entries come from lists the caller sorted by priority (nearest first); when they do not all fit, each list is written from its cursor and the
    /// cursor advances past the last entry sent, so overflowed entities lead the next snapshot. Entities missing from the baseline are sent full.
    /// </summary>
    public static class SnapshotPacker
    {
        public const int HeaderBytes = 1 + 4 + 4 + 2 + 1 + 1 + 1 + 1;

        public static void Write(NetDataWriter w, uint seq, uint serverMs, ushort lastInputSeq, uint baselineSeq, SnapshotLookup baseline, bool hasOwner, OwnerSim owner,
            IList<HeroSnap> heroes, IList<MonsterSnap> monsters, ref int heroCursor, ref int monsterCursor, List<HeroSnap> sentHeroes, List<MonsterSnap> sentMonsters,
            int capBytes = NetProtocol.SnapshotMaxBytes)
        {
            if (sentHeroes != null) sentHeroes.Clear();
            if (sentMonsters != null) sentMonsters.Clear();
            Dictionary<ushort, HeroSnap> hb = baseline != null ? baseline.Heroes : null;
            Dictionary<ushort, MonsterSnap> mb = baseline != null ? baseline.Monsters : null;
            w.Reset();
            w.Put(NetOp.Snapshot); w.Put(seq); w.Put(serverMs); w.Put(lastInputSeq);
            w.Put((byte)(baselineSeq == 0 ? 0 : seq - baselineSeq));
            w.Put((byte)(hasOwner ? 1 : 0));
            if (hasOwner) owner.Write(w);
            int heroCountPos = w.Length;
            w.Put((byte)0);

            // Monsters get at least half of what is left (or what they need, if less) so a crowd of heroes cannot starve them.
            int mn = System.Math.Min(monsters.Count, NetProtocol.MaxSnapshotEntities), mc = mn == 0 ? 0 : monsterCursor % monsters.Count;
            int wish = 0;
            for (int i = 0; i < mn && wish < capBytes; i++)
            {
                MonsterSnap m = monsters[(mc + i) % monsters.Count], b = default(MonsterSnap);
                wish += EntityDelta.MonsterBytes(EntityDelta.MonsterMask(m, mb != null && mb.TryGetValue(m.Id, out b), b));
            }
            int heroLimit = capBytes - 1 - System.Math.Min(wish, (capBytes - w.Length) / 2);

            int hn = System.Math.Min(heroes.Count, NetProtocol.MaxSnapshotEntities), hc = hn == 0 ? 0 : heroCursor % heroes.Count, sent = 0;
            for (; sent < hn; sent++)
            {
                HeroSnap h = heroes[(hc + sent) % heroes.Count], b = default(HeroSnap);
                int mask = EntityDelta.HeroMask(h, hb != null && hb.TryGetValue(h.Id, out b), b);
                if (w.Length + EntityDelta.HeroBytes(mask) > heroLimit) break;
                EntityDelta.WriteHero(w, h, b, mask);
                if (sentHeroes != null) sentHeroes.Add(h);
            }
            w.Data[heroCountPos] = (byte)sent;
            heroCursor = heroes.Count == 0 || sent >= heroes.Count ? 0 : (hc + sent) % heroes.Count;

            int monsterCountPos = w.Length;
            w.Put((byte)0);
            sent = 0;
            for (; sent < mn; sent++)
            {
                MonsterSnap m = monsters[(mc + sent) % monsters.Count], b = default(MonsterSnap);
                int mask = EntityDelta.MonsterMask(m, mb != null && mb.TryGetValue(m.Id, out b), b);
                if (w.Length + EntityDelta.MonsterBytes(mask) > capBytes) break;
                EntityDelta.WriteMonster(w, m, b, mask);
                if (sentMonsters != null) sentMonsters.Add(m);
            }
            w.Data[monsterCountPos] = (byte)sent;
            monsterCursor = monsters.Count == 0 || sent >= monsters.Count ? 0 : (mc + sent) % monsters.Count;
        }

        /// <summary>
        /// Reads the body after the opcode into `into` (entries resolved to full states). MissingBaseline = a delta against a snapshot the ring no longer
        /// has: the caller must not ack it. `lookup` is scratch space.
        /// </summary>
        public static SnapshotReadResult TryRead(NetDataReader r, SnapshotMsg into, SnapshotRing ring, SnapshotLookup lookup)
        {
            try
            {
                into.Heroes.Clear(); into.Monsters.Clear();
                if (r.AvailableBytes < HeaderBytes - 1) return SnapshotReadResult.Malformed;
                into.Seq = r.GetUInt(); into.ServerTimeMs = r.GetUInt(); into.LastInputSeq = r.GetUShort();
                int dist = r.GetByte();
                into.HasOwner = (r.GetByte() & 1) != 0;
                into.BaselineSeq = dist == 0 ? 0 : into.Seq - (uint)dist;
                SnapshotRing.Slot slot = null;
                if (dist != 0 && !ring.TryGet(into.BaselineSeq, out slot)) return SnapshotReadResult.MissingBaseline;
                lookup.Load(slot);
                if (into.HasOwner) { if (!OwnerSim.TryRead(r, out into.Owner)) return SnapshotReadResult.Malformed; }
                else into.Owner = default(OwnerSim);
                int hn = r.GetByte();
                if (r.AvailableBytes < hn * 3 + 1) return SnapshotReadResult.Malformed;
                for (int i = 0; i < hn; i++)
                {
                    HeroSnap h;
                    if (!EntityDelta.TryReadHero(r, lookup.Heroes, out h)) return SnapshotReadResult.Malformed;
                    into.Heroes.Add(h);
                }
                int mn = r.GetByte();
                if (r.AvailableBytes < mn * 3) return SnapshotReadResult.Malformed;
                for (int i = 0; i < mn; i++)
                {
                    MonsterSnap m;
                    if (!EntityDelta.TryReadMonster(r, lookup.Monsters, out m)) return SnapshotReadResult.Malformed;
                    into.Monsters.Add(m);
                }
                return SnapshotReadResult.Ok;
            }
            catch (System.Exception) { return SnapshotReadResult.Malformed; }
        }

        /// <summary>Stores a decoded snapshot as a baseline candidate for later deltas.</summary>
        public static void Remember(SnapshotRing ring, SnapshotMsg m)
        {
            SnapshotRing.Slot s = ring.Begin(m.Seq);
            s.Heroes.AddRange(m.Heroes); s.Monsters.AddRange(m.Monsters);
        }
    }
}
