using System.Collections.Generic;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// Per-entity delta coding of snapshot entries. An entry is id (2 B) + a field mask (1 B) + the changed fields. Bit 7 of the mask = the changed
    /// position axes are signed bytes (1/8 unit steps, +-15.9 units) instead of shorts. An entry with every field bit set is a FULL entry and needs no
    /// baseline (new entity, baseline too old); a mask of just the id (nothing changed) still tells the client "present, same as before".
    /// </summary>
    public static class EntityDelta
    {
        public const int HeroAll = 0x7F, MonsterAll = 0x3F, SmallPos = 0x80;
        const int HeaderBytes = 3;

        static int Bits(int v) { int n = 0; while (v != 0) { n += v & 1; v >>= 1; } return n; }
        static bool Fits(int d) { return d >= sbyte.MinValue && d <= sbyte.MaxValue; }

        /// <summary>Position bits (X = 1, Y = 2) of a mask plus the small flag when every changed axis fits a signed byte.</summary>
        static int PosMask(int dx, int dy)
        {
            int m = (dx != 0 ? 1 : 0) | (dy != 0 ? 2 : 0);
            return m != 0 && (dx == 0 || Fits(dx)) && (dy == 0 || Fits(dy)) ? m | SmallPos : m;
        }

        // ---- hero ----

        public static int HeroMask(HeroSnap c, bool hasBase, HeroSnap b)
        {
            if (!hasBase) return HeroAll;
            int m = PosMask(c.X - b.X, c.Y - b.Y);
            if (c.FacingFlags != b.FacingFlags) m |= 4;
            if (c.Pose != b.Pose) m |= 8;
            if (c.Step != b.Step) m |= 16;
            if (c.Progress != b.Progress) m |= 32;
            if (c.HpPct != b.HpPct) m |= 64;
            return m;
        }

        public static int HeroBytes(int mask)
        {
            int pos = Bits(mask & 3);
            return HeaderBytes + ((mask & SmallPos) != 0 ? pos : pos * 2) + Bits(mask & 0x7C);
        }

        public static void WriteHero(NetDataWriter w, HeroSnap c, HeroSnap b, int mask)
        {
            w.Put(c.Id); w.Put((byte)mask);
            bool small = (mask & SmallPos) != 0;
            if ((mask & 1) != 0) { if (small) w.Put((sbyte)(c.X - b.X)); else w.Put(c.X); }
            if ((mask & 2) != 0) { if (small) w.Put((sbyte)(c.Y - b.Y)); else w.Put(c.Y); }
            if ((mask & 4) != 0) w.Put(c.FacingFlags);
            if ((mask & 8) != 0) w.Put(c.Pose);
            if ((mask & 16) != 0) w.Put(c.Step);
            if ((mask & 32) != 0) w.Put(c.Progress);
            if ((mask & 64) != 0) w.Put(c.HpPct);
        }

        /// <summary>Reads one entry; `baseline` is the client's earlier state of the same id (null = none: the entry must be full).</summary>
        public static bool TryReadHero(NetDataReader r, Dictionary<ushort, HeroSnap> baseline, out HeroSnap h)
        {
            h = default(HeroSnap);
            if (r.AvailableBytes < HeaderBytes) return false;
            ushort id = r.GetUShort();
            int mask = r.GetByte();
            if (baseline == null || !baseline.TryGetValue(id, out h))
            {
                if ((mask & 0x7F) != 0x7F || (mask & SmallPos) != 0) return false;
                h = default(HeroSnap);
            }
            h.Id = id;
            if (r.AvailableBytes < HeroBytes(mask) - HeaderBytes) return false;
            bool small = (mask & SmallPos) != 0;
            if ((mask & 1) != 0) h.X = small ? (short)(h.X + r.GetSByte()) : r.GetShort();
            if ((mask & 2) != 0) h.Y = small ? (short)(h.Y + r.GetSByte()) : r.GetShort();
            if ((mask & 4) != 0) h.FacingFlags = r.GetByte();
            if ((mask & 8) != 0) h.Pose = r.GetByte();
            if ((mask & 16) != 0) h.Step = r.GetByte();
            if ((mask & 32) != 0) h.Progress = r.GetByte();
            if ((mask & 64) != 0) h.HpPct = r.GetByte();
            return true;
        }

        // ---- monster ----

        public static int MonsterMask(MonsterSnap c, bool hasBase, MonsterSnap b)
        {
            if (!hasBase) return MonsterAll;
            int m = PosMask(c.X - b.X, c.Y - b.Y);
            if (c.FacingState != b.FacingState) m |= 4;
            if (c.AttackBits != b.AttackBits) m |= 8;
            if (c.HpPct != b.HpPct) m |= 16;
            if (c.DeathProgress != b.DeathProgress) m |= 32;
            return m;
        }

        public static int MonsterBytes(int mask)
        {
            int pos = Bits(mask & 3);
            return HeaderBytes + ((mask & SmallPos) != 0 ? pos : pos * 2) + Bits(mask & 0x3C);
        }

        public static void WriteMonster(NetDataWriter w, MonsterSnap c, MonsterSnap b, int mask)
        {
            w.Put(c.Id); w.Put((byte)mask);
            bool small = (mask & SmallPos) != 0;
            if ((mask & 1) != 0) { if (small) w.Put((sbyte)(c.X - b.X)); else w.Put(c.X); }
            if ((mask & 2) != 0) { if (small) w.Put((sbyte)(c.Y - b.Y)); else w.Put(c.Y); }
            if ((mask & 4) != 0) w.Put(c.FacingState);
            if ((mask & 8) != 0) w.Put(c.AttackBits);
            if ((mask & 16) != 0) w.Put(c.HpPct);
            if ((mask & 32) != 0) w.Put(c.DeathProgress);
        }

        public static bool TryReadMonster(NetDataReader r, Dictionary<ushort, MonsterSnap> baseline, out MonsterSnap m)
        {
            m = default(MonsterSnap);
            if (r.AvailableBytes < HeaderBytes) return false;
            ushort id = r.GetUShort();
            int mask = r.GetByte();
            if (baseline == null || !baseline.TryGetValue(id, out m))
            {
                if ((mask & 0x3F) != 0x3F || (mask & SmallPos) != 0) return false;
                m = default(MonsterSnap);
            }
            m.Id = id;
            if (r.AvailableBytes < MonsterBytes(mask) - HeaderBytes) return false;
            bool small = (mask & SmallPos) != 0;
            if ((mask & 1) != 0) m.X = small ? (short)(m.X + r.GetSByte()) : r.GetShort();
            if ((mask & 2) != 0) m.Y = small ? (short)(m.Y + r.GetSByte()) : r.GetShort();
            if ((mask & 4) != 0) m.FacingState = r.GetByte();
            if ((mask & 8) != 0) m.AttackBits = r.GetByte();
            if ((mask & 16) != 0) m.HpPct = r.GetByte();
            if ((mask & 32) != 0) m.DeathProgress = r.GetByte();
            return true;
        }
    }

    /// <summary>
    /// The last 16 snapshots as one side knows them (server: as sent to one client; client: as decoded), keyed by sequence. A delta snapshot names its
    /// baseline sequence; both sides resolve it here, so a baseline the client acked is guaranteed to be on both sides until 16 newer snapshots exist.
    /// </summary>
    public sealed class SnapshotRing
    {
        public const int Size = 16;

        public sealed class Slot
        {
            public uint Seq;
            public bool Valid;
            public readonly List<HeroSnap> Heroes = new List<HeroSnap>();
            public readonly List<MonsterSnap> Monsters = new List<MonsterSnap>();
        }

        readonly Slot[] slots = new Slot[Size];

        public SnapshotRing() { for (int i = 0; i < Size; i++) slots[i] = new Slot(); }

        /// <summary>Claims (and clears) the slot for `seq`; the caller fills Heroes / Monsters.</summary>
        public Slot Begin(uint seq)
        {
            Slot s = slots[seq % Size];
            s.Seq = seq; s.Valid = true;
            s.Heroes.Clear(); s.Monsters.Clear();
            return s;
        }

        public bool TryGet(uint seq, out Slot slot)
        {
            slot = slots[seq % Size];
            return slot.Valid && slot.Seq == seq;
        }

        public void Clear() { for (int i = 0; i < Size; i++) slots[i].Valid = false; }
    }

    /// <summary>Id -> entry maps of one baseline slot (reused: Load clears and refills).</summary>
    public sealed class SnapshotLookup
    {
        public readonly Dictionary<ushort, HeroSnap> Heroes = new Dictionary<ushort, HeroSnap>();
        public readonly Dictionary<ushort, MonsterSnap> Monsters = new Dictionary<ushort, MonsterSnap>();

        public void Load(SnapshotRing.Slot slot)
        {
            Heroes.Clear(); Monsters.Clear();
            if (slot == null) return;
            for (int i = 0; i < slot.Heroes.Count; i++) Heroes[slot.Heroes[i].Id] = slot.Heroes[i];
            for (int i = 0; i < slot.Monsters.Count; i++) Monsters[slot.Monsters[i].Id] = slot.Monsters[i];
        }
    }
}
