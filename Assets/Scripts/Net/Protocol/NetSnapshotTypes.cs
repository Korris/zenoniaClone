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

    /// <summary>
    /// Monster snapshot byte bits: attackIdx+1 (bits 0-2), airborne (bit 3), dominant status code (bits 4-6, see StatusCode), marked (bit 7).
    /// The code drives the tint / stars on clients; the mark bit the reticle (a marked monster can also be frozen, burning ...).
    /// </summary>
    public static class MonsterBits
    {
        public const int Airborne = 8, StatusShift = 4, StatusMask = 7 << StatusShift, Marked = 128;

        public static int Status(int attackBits) { return (attackBits & StatusMask) >> StatusShift; }
        public static int WithStatus(int attackBits, int code) { return (attackBits & ~StatusMask) | ((code & 7) << StatusShift); }
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

    /// <summary>10 B on the wire: id, x, y, facing | state, attackIdx | airborne | status | marked, hp percent, death progress.</summary>
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

    /// <summary>
    /// Decoded snapshot (client side, entries are always FULL states: deltas are resolved against the baseline while reading). Lists are reused by
    /// the caller between reads. LastInputSeq is the 16-bit sequence of the last input frame the server simulated for the receiving player.
    /// </summary>
    public sealed class SnapshotMsg
    {
        public uint Seq, ServerTimeMs, BaselineSeq;
        public ushort LastInputSeq;
        public bool HasOwner;
        public OwnerSim Owner;
        public readonly List<HeroSnap> Heroes = new List<HeroSnap>();
        public readonly List<MonsterSnap> Monsters = new List<MonsterSnap>();
    }
}
