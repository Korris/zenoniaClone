using System.Collections.Generic;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    public enum HeroPose : byte { Idle = 0, Walk = 1, Dash = 2, Attack = 3, Hurt = 4, Death = 5, Cast = 6 }

    /// <summary>Hero snapshot flag bits (above the 2 facing bits of FacingFlags).</summary>
    public static class HeroFlags
    {
        public const int Dead = 1, Dash = 2, Moving = 4, Invuln = 8, Hurt = 16;
    }

    /// <summary>Monster snapshot byte bits: AirChill = attackIdx+1 (bits 0-2), airborne (bit 3), chilled (bit 4).</summary>
    public static class MonsterBits
    {
        public const int Airborne = 8, Chilled = 16;
    }

    /// <summary>11 B on the wire: id, x, y (1/8 unit), facing | flags, pose, combo step, pose progress, hp percent.</summary>
    public struct HeroSnap
    {
        public ushort Id;
        public short X, Y;
        public byte FacingFlags, Pose, Step, Progress, HpPct;
        public const int WireBytes = 11;

        public void Write(NetDataWriter w)
        {
            w.Put(Id); w.Put(X); w.Put(Y); w.Put(FacingFlags); w.Put(Pose); w.Put(Step); w.Put(Progress); w.Put(HpPct);
        }

        public static HeroSnap Read(NetDataReader r)
        {
            return new HeroSnap { Id = r.GetUShort(), X = r.GetShort(), Y = r.GetShort(), FacingFlags = r.GetByte(), Pose = r.GetByte(), Step = r.GetByte(), Progress = r.GetByte(), HpPct = r.GetByte() };
        }
    }

    /// <summary>10 B on the wire: id, x, y, facing | state, attackIdx | airborne | chilled, hp percent, death progress.</summary>
    public struct MonsterSnap
    {
        public ushort Id;
        public short X, Y;
        public byte FacingState, AttackBits, HpPct, DeathProgress;
        public const int WireBytes = 10;

        public void Write(NetDataWriter w)
        {
            w.Put(Id); w.Put(X); w.Put(Y); w.Put(FacingState); w.Put(AttackBits); w.Put(HpPct); w.Put(DeathProgress);
        }

        public static MonsterSnap Read(NetDataReader r)
        {
            return new MonsterSnap { Id = r.GetUShort(), X = r.GetShort(), Y = r.GetShort(), FacingState = r.GetByte(), AttackBits = r.GetByte(), HpPct = r.GetByte(), DeathProgress = r.GetByte() };
        }
    }

    /// <summary>Decoded snapshot (client side). Lists are reused by the caller between reads.</summary>
    public sealed class SnapshotMsg
    {
        public uint Seq, ServerTimeMs, LastInputSeq;
        public readonly List<HeroSnap> Heroes = new List<HeroSnap>();
        public readonly List<MonsterSnap> Monsters = new List<MonsterSnap>();
    }

    /// <summary>
    /// Builds a size-capped snapshot from entity lists the caller already sorted by priority (nearest first). When everything does not fit,
    /// each list is written starting at its cursor and the cursor advances past the last entry sent, so overflowed entities lead the next snapshot.
    /// </summary>
    public static class SnapshotPacker
    {
        public const int HeaderBytes = 1 + 4 + 4 + 4 + 1 + 1;

        public static void Write(NetDataWriter w, uint seq, uint serverTimeMs, uint lastInputSeq, IList<HeroSnap> heroes, IList<MonsterSnap> monsters,
            ref int heroCursor, ref int monsterCursor, int capBytes = NetProtocol.SnapshotMaxBytes)
        {
            int budget = capBytes - HeaderBytes;
            int monsterWish = System.Math.Min(monsters.Count, NetProtocol.MaxSnapshotEntities) * MonsterSnap.WireBytes;
            int heroN = System.Math.Min(System.Math.Min(heroes.Count, NetProtocol.MaxSnapshotEntities), (budget - System.Math.Min(monsterWish, budget / 2)) / HeroSnap.WireBytes);
            int monsterN = System.Math.Min(System.Math.Min(monsters.Count, NetProtocol.MaxSnapshotEntities), (budget - heroN * HeroSnap.WireBytes) / MonsterSnap.WireBytes);
            if (heroN < 0) heroN = 0;
            if (monsterN < 0) monsterN = 0;

            w.Reset();
            w.Put(NetOp.Snapshot); w.Put(seq); w.Put(serverTimeMs); w.Put(lastInputSeq);
            w.Put((byte)heroN);
            int hc = heroes.Count == 0 ? 0 : heroCursor % heroes.Count;
            for (int i = 0; i < heroN; i++) heroes[(hc + i) % heroes.Count].Write(w);
            heroCursor = heroN >= heroes.Count ? 0 : (hc + heroN) % heroes.Count;
            w.Put((byte)monsterN);
            int mc = monsters.Count == 0 ? 0 : monsterCursor % monsters.Count;
            for (int i = 0; i < monsterN; i++) monsters[(mc + i) % monsters.Count].Write(w);
            monsterCursor = monsterN >= monsters.Count ? 0 : (mc + monsterN) % monsters.Count;
        }

        /// <summary>Reads the body after the opcode. Returns false on truncated / oversized data.</summary>
        public static bool TryRead(NetDataReader r, SnapshotMsg into)
        {
            try
            {
                into.Heroes.Clear(); into.Monsters.Clear();
                into.Seq = r.GetUInt(); into.ServerTimeMs = r.GetUInt(); into.LastInputSeq = r.GetUInt();
                int hn = r.GetByte();
                if (r.AvailableBytes < hn * HeroSnap.WireBytes + 1) return false;
                for (int i = 0; i < hn; i++) into.Heroes.Add(HeroSnap.Read(r));
                int mn = r.GetByte();
                if (r.AvailableBytes < mn * MonsterSnap.WireBytes) return false;
                for (int i = 0; i < mn; i++) into.Monsters.Add(MonsterSnap.Read(r));
                return true;
            }
            catch (System.Exception) { return false; }
        }
    }
}
