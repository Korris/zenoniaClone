using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>Decoded hero state at one snapshot (world units).</summary>
    public struct HeroSample
    {
        public float X, Y, Progress;
        public byte FacingFlags, Pose, Step, HpPct;
    }

    /// <summary>Decoded monster state at one snapshot (world units).</summary>
    public struct MonsterSample
    {
        public float X, Y, DeathProgress;
        public byte FacingState, AttackBits, HpPct;
    }

    /// <summary>
    /// Client side entity history: a short ring of timestamped samples per entity plus the server clock estimate. The renderer asks for the state
    /// at (server now - adaptive delay), so there are almost always two samples to blend; positions are interpolated (a jump over 48 units is a
    /// teleport and snaps), discrete fields (facing, pose, state) come from the older sample. Past the newest sample the position is extrapolated
    /// along the last two samples for at most 100 ms (a lost snapshot), then the entity holds its last state.
    /// </summary>
    public sealed class SnapshotBuffer
    {
        public const double MaxExtrapolateMs = 100.0;
        public const float TeleportUnits = 48f;
        const int RingSize = 8;

        sealed class Track<T>
        {
            readonly T[] samples = new T[RingSize];
            readonly double[] times = new double[RingSize];
            int count, head;

            public void Add(double t, T s)
            {
                if (count > 0 && t <= times[head]) return;
                head = (head + 1) % RingSize;
                samples[head] = s; times[head] = t;
                if (count < RingSize) count++;
            }

            public bool Sample(double t, Func<T, T, float, T> lerp, out T result)
            {
                result = default(T);
                if (count == 0) return false;
                int oldest = (head - (count - 1) + RingSize) % RingSize;
                if (t <= times[oldest]) { result = samples[oldest]; return true; }
                for (int k = 0; k < count - 1; k++) // newest first: find the last sample at or before t
                {
                    int ia = (head - k - 1 + RingSize * 2) % RingSize, ib = (head - k + RingSize * 2) % RingSize;
                    if (times[ia] > t) continue;
                    double span = times[ib] - times[ia];
                    double maxU = k == 0 && span > 0.0 ? 1.0 + MaxExtrapolateMs / span : 1.0; // only past the newest sample may u exceed 1
                    result = lerp(samples[ia], samples[ib], span > 0.0 ? (float)Math.Min(maxU, (t - times[ia]) / span) : 1f);
                    return true;
                }
                result = samples[head]; // newer than every sample: hold the latest state
                return true;
            }
        }

        readonly Dictionary<ushort, Track<HeroSample>> heroes = new Dictionary<ushort, Track<HeroSample>>();
        readonly Dictionary<ushort, Track<MonsterSample>> monsters = new Dictionary<ushort, Track<MonsterSample>>();
        readonly AdaptiveInterpolationDelay delay;
        double offset; // server time minus client time, ms
        bool hasClock;
        uint lastSeq;
        bool hasSeq;

        public SnapshotBuffer(double snapshotIntervalMs = 1000.0 / 15.0) { delay = new AdaptiveInterpolationDelay(snapshotIntervalMs); }

        /// <summary>How far behind the server clock other entities are drawn right now (120..250 ms, grows with arrival jitter).</summary>
        public double RenderDelayMs { get { return delay.DelayMs; } }
        public double JitterMs { get { return delay.JitterMs; } }
        public bool HasClock { get { return hasClock; } }
        public uint LastSeq { get { return lastSeq; } }

        /// <summary>Estimated server clock for a client clock reading.</summary>
        public double ServerNow(double clientNowMs) { return clientNowMs + offset; }
        public double RenderTime(double clientNowMs) { return ServerNow(clientNowMs) - delay.DelayMs; }

        /// <summary>Adds a snapshot received at `clientNowMs`; false when it is older than one already applied (unreliable packets reorder).</summary>
        public bool Push(SnapshotMsg m, double clientNowMs)
        {
            if (hasSeq && !NetQuantize.IsNewer(m.Seq, lastSeq)) return false;
            hasSeq = true; lastSeq = m.Seq;
            double sample = m.ServerTimeMs - clientNowMs;
            delay.OnSnapshot(sample);
            if (!hasClock) { offset = sample; hasClock = true; }
            else if (sample < offset) offset = sample; // a faster packet shows the true delay
            else offset += (sample - offset) * 0.02;   // otherwise creep up slowly (clock drift, jitter)
            double t = m.ServerTimeMs;
            for (int i = 0; i < m.Heroes.Count; i++)
            {
                HeroSnap h = m.Heroes[i];
                Track<HeroSample> tr;
                if (!heroes.TryGetValue(h.Id, out tr)) heroes[h.Id] = tr = new Track<HeroSample>();
                tr.Add(t, new HeroSample { X = NetQuantize.Pos(h.X), Y = NetQuantize.Pos(h.Y), FacingFlags = h.FacingFlags, Pose = h.Pose, Step = h.Step, Progress = NetQuantize.Progress(h.Progress), HpPct = h.HpPct });
            }
            for (int i = 0; i < m.Monsters.Count; i++)
            {
                MonsterSnap s = m.Monsters[i];
                Track<MonsterSample> tr;
                if (!monsters.TryGetValue(s.Id, out tr)) monsters[s.Id] = tr = new Track<MonsterSample>();
                tr.Add(t, new MonsterSample { X = NetQuantize.Pos(s.X), Y = NetQuantize.Pos(s.Y), FacingState = s.FacingState, AttackBits = s.AttackBits, HpPct = s.HpPct, DeathProgress = NetQuantize.Progress(s.DeathProgress) });
            }
            return true;
        }

        static float Mix(float a, float b, float u) { return a + (b - a) * u; }

        static readonly Func<HeroSample, HeroSample, float, HeroSample> LerpHero = (a, b, u) =>
        {
            bool jump = Math.Abs(b.X - a.X) > TeleportUnits || Math.Abs(b.Y - a.Y) > TeleportUnits;
            HeroSample r = u > 1f ? b : a; // extrapolating: the newest sample's pose / step win
            r.X = jump ? (u >= 0.5f ? b.X : a.X) : Mix(a.X, b.X, u);
            r.Y = jump ? (u >= 0.5f ? b.Y : a.Y) : Mix(a.Y, b.Y, u);
            if (a.Pose == b.Pose && a.Step == b.Step) r.Progress = Math.Max(0f, Math.Min(1f, Mix(a.Progress, b.Progress, u)));
            if (jump && u >= 0.5f) r.FacingFlags = b.FacingFlags;
            return r;
        };

        static readonly Func<MonsterSample, MonsterSample, float, MonsterSample> LerpMonster = (a, b, u) =>
        {
            bool jump = Math.Abs(b.X - a.X) > TeleportUnits || Math.Abs(b.Y - a.Y) > TeleportUnits;
            MonsterSample r = u > 1f ? b : a;
            r.X = jump ? (u >= 0.5f ? b.X : a.X) : Mix(a.X, b.X, u);
            r.Y = jump ? (u >= 0.5f ? b.Y : a.Y) : Mix(a.Y, b.Y, u);
            r.DeathProgress = Math.Max(0f, Math.Min(1f, Mix(a.DeathProgress, b.DeathProgress, u)));
            r.HpPct = u >= 0.5f ? b.HpPct : a.HpPct;
            return r;
        };

        public bool TryHero(ushort id, double renderTime, out HeroSample s)
        {
            Track<HeroSample> tr;
            s = default(HeroSample);
            return heroes.TryGetValue(id, out tr) && tr.Sample(renderTime, LerpHero, out s);
        }

        public bool TryMonster(ushort id, double renderTime, out MonsterSample s)
        {
            Track<MonsterSample> tr;
            s = default(MonsterSample);
            return monsters.TryGetValue(id, out tr) && tr.Sample(renderTime, LerpMonster, out s);
        }

        public void Forget(ushort id) { heroes.Remove(id); monsters.Remove(id); }
        public void Clear() { heroes.Clear(); monsters.Clear(); hasClock = false; hasSeq = false; }
    }
}
