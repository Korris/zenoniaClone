using System;

namespace ZenoniaProto.Net
{
    /// <summary>Held-key bits of an input frame.</summary>
    public static class InputBits
    {
        public const byte Up = 1, Down = 2, Left = 4, Right = 8, Attack = 16, Dash = 32, Skill = 64;
    }

    /// <summary>
    /// One client input sample. Held keys are a bitmask; one-shot presses are wrapping counters so a lost packet cannot drop a press
    /// (the next packet carries the higher counter) and a duplicated packet cannot fire it twice (the server remembers the last counter).
    /// </summary>
    public struct InputFrame
    {
        public uint Seq;
        public byte Held, AttackCnt, DashCnt, SkillCnt, HpPotCnt, MpPotCnt;
        /// <summary>0..3 facing hint, 255 = none.</summary>
        public byte Facing;

        public const int WireBytes = 11;
        public bool HeldBit(byte bit) { return (Held & bit) != 0; }
    }

    /// <summary>Client side: remembers the last 3 frames so every packet repeats them (input redundancy, no ack needed).</summary>
    public sealed class InputRedundancy
    {
        readonly InputFrame[] ring = new InputFrame[NetProtocol.MaxInputFrames];
        int count, head;

        public uint NextSeq { get; private set; } = 1;

        /// <summary>Stamps the frame with the next sequence number and stores it.</summary>
        public InputFrame Push(InputFrame frame)
        {
            frame.Seq = NextSeq++;
            head = (head + 1) % ring.Length;
            ring[head] = frame;
            if (count < ring.Length) count++;
            return frame;
        }

        /// <summary>Copies the stored frames, oldest first, into `into`; returns how many (1..3, 0 before the first Push).</summary>
        public int Fill(InputFrame[] into)
        {
            int n = Math.Min(count, into.Length);
            for (int i = 0; i < n; i++) into[i] = ring[(head - (n - 1 - i) + ring.Length * 2) % ring.Length];
            return n;
        }
    }

    public enum PressKind { Attack, Dash, Skill, HpPotion, MpPotion }

    /// <summary>
    /// Server side: folds incoming (possibly duplicated / reordered) frames into the hero's input. Held bits = the newest frame; presses =
    /// counter deltas (clamped to 4 per frame against spam) drained one per tick; no frame for 250 ms releases every key.
    /// </summary>
    public sealed class ServerInputState
    {
        const int Kinds = 5;
        readonly byte[] lastCnt = new byte[Kinds];
        readonly byte[] cnt = new byte[Kinds];
        readonly int[] pending = new int[Kinds];
        bool hasBaseline;
        uint lastRecvMs;

        public uint LastSeq { get; private set; }
        public byte Held { get; private set; }
        public byte Facing { get; private set; } = 255;
        public bool HasInput { get { return hasBaseline; } }

        /// <summary>Applies the frames of one packet; returns how many were new (older / duplicate sequence numbers are skipped).</summary>
        public int Accept(InputFrame[] frames, int count, uint nowMs)
        {
            int applied = 0;
            for (int i = 0; i < count; i++)
            {
                InputFrame f = frames[i];
                if (hasBaseline && !NetQuantize.IsNewer(f.Seq, LastSeq)) continue;
                Apply(f);
                applied++;
            }
            if (applied > 0) lastRecvMs = nowMs;
            return applied;
        }

        void Apply(InputFrame f)
        {
            cnt[0] = f.AttackCnt; cnt[1] = f.DashCnt; cnt[2] = f.SkillCnt; cnt[3] = f.HpPotCnt; cnt[4] = f.MpPotCnt;
            if (hasBaseline)
                for (int k = 0; k < Kinds; k++)
                    pending[k] += Math.Min((int)(byte)(cnt[k] - lastCnt[k]), NetProtocol.MaxPressesPerFrame); // wrapping byte delta
            for (int k = 0; k < Kinds; k++) lastCnt[k] = cnt[k];
            hasBaseline = true;
            LastSeq = f.Seq;
            Held = (byte)(f.Held & 0x7F);
            Facing = f.Facing;
        }

        /// <summary>Per tick: releases held keys when the client went quiet.</summary>
        public void Tick(uint nowMs)
        {
            if (hasBaseline && nowMs - lastRecvMs > NetProtocol.InputStaleMs) Held = 0;
        }

        public bool HeldBit(byte bit) { return (Held & bit) != 0; }

        /// <summary>True once per queued press; call at most once per kind per tick so a burst plays out one press per tick.</summary>
        public bool TakePress(PressKind kind)
        {
            if (pending[(int)kind] <= 0) return false;
            pending[(int)kind]--;
            return true;
        }
    }
}
