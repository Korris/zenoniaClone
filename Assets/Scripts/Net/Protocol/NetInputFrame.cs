using System;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// Bits of one input frame (one client sim step, 1/30 s). Up..Right are the held direction keys; Attack..MpPotion are PRESSES that happened
    /// during this step (edge, not held), so a frame is self-contained and a replay of the same frames gives the same result.
    /// </summary>
    public static class InputBits
    {
        public const ushort Up = 1, Down = 2, Left = 4, Right = 8, Attack = 16, Dash = 32, Skill = 64, HpPotion = 128, MpPotion = 256;
        public const ushort MoveMask = Up | Down | Left | Right;
        public const ushort AllMask = 511;
    }

    /// <summary>One client input sample: the client's step sequence number and its bits. On the wire only the low 16 bits of Seq travel.</summary>
    public struct InputFrame
    {
        public uint Seq;
        public ushort Bits;

        public const int WireBytes = 2;
        public bool Has(ushort bit) { return (Bits & bit) != 0; }
    }

    /// <summary>Client side: remembers the last 3 frames so every packet repeats them (input redundancy, no ack needed).</summary>
    public sealed class InputRedundancy
    {
        readonly InputFrame[] ring = new InputFrame[NetProtocol.MaxInputFrames];
        int count, head;

        public uint NextSeq { get; private set; } = 1;

        /// <summary>Stamps a frame with the next sequence number and stores it.</summary>
        public InputFrame Push(ushort bits)
        {
            var frame = new InputFrame { Seq = NextSeq++, Bits = (ushort)(bits & InputBits.AllMask) };
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

    /// <summary>
    /// Server side: a small jitter buffer of a player's input frames. Packets (possibly duplicated, reordered or lossy) are folded into one in-order
    /// queue; the hero consumes EXACTLY one frame per sim step, so the server hero walks the same steps the client predicted (Seq is what a snapshot
    /// acks). An empty queue freezes the hero (late frame) until the client has been silent for InputStaleMs, then it idles. Every frozen tick earns
    /// one tick of catch-up credit, and so does every tick the SERVER loop itself lost (a hitch, AddCredit): when a backlog of frames exists the hero
    /// may take two steps in one tick (TakeExtra) until the credit is spent, so jitter and server hitches cost no movement while a client can never
    /// gain more steps than real time passed. A queue longer than MaxQueue (a burst beyond the credit) is cut back to KeepAfterDrop: the cut frames
    /// lose their movement but not their presses.
    /// </summary>
    public sealed class ServerInputState
    {
        public const int MaxQueue = 8, KeepAfterDrop = 4, MaxGapFill = 10, Capacity = 16, MaxCredit = 8;
        readonly InputFrame[] queue = new InputFrame[Capacity];
        int qHead, qCount;
        uint newest;               // newest sequence ever queued
        ushort newestBits;
        bool hasNewest;
        uint lastRecvMs;
        ushort carry;              // press bits of dropped frames, delivered with the next applied frame
        int potionHp, potionMp;
        int credit;                // frozen ticks not yet repaid with a catch-up step

        /// <summary>Sequence of the last frame the hero consumed (what snapshots report back to the client).</summary>
        public uint LastSeq { get; private set; }
        /// <summary>Bits of the frame being simulated this step (0 when the hero idles on a silent client).</summary>
        public ushort Current { get; private set; }
        public bool HasInput { get { return hasNewest; } }
        public int Queued { get { return qCount; } }
        public int Dropped { get; private set; }
        public int Starved { get; private set; }
        public int CatchUps { get; private set; }

        /// <summary>Expands the 16-bit wire sequence to the 32-bit counter nearest to `reference`.</summary>
        public static uint Expand(ushort wire, uint reference) { return reference + (uint)(short)(wire - (ushort)reference); }

        /// <summary>Queues the frames of one packet (Seq = low 16 bits); returns how many were new.</summary>
        public int Accept(InputFrame[] frames, int count, uint nowMs)
        {
            int applied = 0;
            for (int i = 0; i < count; i++)
            {
                uint seq = hasNewest ? Expand((ushort)frames[i].Seq, newest) : frames[i].Seq;
                if (hasNewest && !NetQuantize.IsNewer(seq, newest)) continue;
                ushort bits = (ushort)(frames[i].Bits & InputBits.AllMask);
                if (hasNewest)
                {
                    uint gap = seq - newest - 1;
                    if (gap > MaxGapFill) { qCount = 0; carry = 0; }             // long outage: restart the stream, no catch-up
                    else
                    {
                        ushort held = (ushort)(newestBits & InputBits.MoveMask);
                        for (uint g = 0; g < gap; g++) Enqueue(newest + 1 + g, held); // lost frames repeat the last held direction, no presses
                    }
                }
                Enqueue(seq, bits);
                newest = seq; newestBits = bits; hasNewest = true;
                applied++;
            }
            if (applied > 0) lastRecvMs = nowMs;
            return applied;
        }

        void Enqueue(uint seq, ushort bits)
        {
            if (qCount == Capacity) { carry |= (ushort)(queue[qHead].Bits & ~InputBits.MoveMask); qHead = (qHead + 1) % Capacity; qCount--; Dropped++; }
            queue[(qHead + qCount) % Capacity] = new InputFrame { Seq = seq, Bits = bits };
            qCount++;
        }

        /// <summary>
        /// Called once per hero sim step. Returns true when the hero should simulate (Current holds the frame), false when it must freeze
        /// because the next frame has not arrived yet.
        /// </summary>
        public bool Advance(uint nowMs)
        {
            Current = 0;
            if (qCount > MaxQueue)
                while (qCount > KeepAfterDrop)
                {
                    carry |= (ushort)(queue[qHead].Bits & ~InputBits.MoveMask);
                    qHead = (qHead + 1) % Capacity; qCount--; Dropped++;
                }
            if (qCount > 0) { Consume(); return true; }
            if (!hasNewest || nowMs - lastRecvMs > NetProtocol.InputStaleMs) return true; // silent client: idle so timers still finish
            Starved++;
            if (credit < MaxCredit) credit++;
            return false;
        }

        void Consume()
        {
            InputFrame f = queue[qHead];
            qHead = (qHead + 1) % Capacity; qCount--;
            Current = (ushort)(f.Bits | carry);
            carry = 0;
            LastSeq = f.Seq;
            if ((Current & InputBits.HpPotion) != 0) potionHp++;
            if ((Current & InputBits.MpPotion) != 0) potionMp++;
        }

        /// <summary>The server loop skipped `ticks` ticks (frame hitch): the hero may repay them with extra steps once frames are queued.</summary>
        public void AddCredit(int ticks) { credit = Math.Min(MaxCredit, credit + Math.Max(0, ticks)); }

        /// <summary>
        /// Asked after a step in the same tick: true (and Current = the next frame) when a backlog exists and an earlier frozen tick can be repaid.
        /// </summary>
        public bool TakeExtra()
        {
            if (credit <= 0 || qCount < 2) return false;
            credit--; CatchUps++;
            Consume();
            return true;
        }
        public bool Has(ushort bit) { return (Current & bit) != 0; }

        /// <summary>True once per consumed potion press (potions are applied by the server loop, not by a sim step).</summary>
        public bool TakePotion(bool hp)
        {
            if (hp) { if (potionHp <= 0) return false; potionHp--; return true; }
            if (potionMp <= 0) return false;
            potionMp--; return true;
        }

        /// <summary>Drops queued potion presses beyond `max` (a long burst must not drain potions later).</summary>
        public void ClampPotions(int max) { if (potionHp > max) potionHp = max; if (potionMp > max) potionMp = max; }
    }
}
