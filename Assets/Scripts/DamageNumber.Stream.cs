using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ladder of your own hits on one monster (RO style, 21/11 clean-up of the multi-hit numbers): every number pops at the SAME point above the
    /// monster and pushes the older ones of that monster up one step (eased), so the newest is always at the bottom, big and on top. Only
    /// MaxVisible numbers per monster stay: the one pushed past them fades out in CullFadeMs. Stream numbers rise little (StreamRisePx) since the
    /// push gives the spacing; a split part that is not the last one fades fast (DamageNumber.MultiHit.cs).
    /// Monsters standing together get side-by-side lanes (LaneW apart on screen, at most 3: centre, then each side) so their ladders never
    /// overlap; a 4th monster in the same spot shares the nearest ladder (NewStack). Before 21/11 14:50 up to 8 numbers per monster climbed a
    /// 6-slot cascade of 17 px, and three monsters in one spot printed three crossing columns over the skill FX.
    /// </summary>
    public partial class DamageNumber
    {
        const int MaxVisible = 3;                // newest + 2 older per monster
        const float CullFadeMs = 80f, StepMul = 0.86f, LiftEaseMs = 45f;
        const float StreamRisePx = 22f, StreamCritRisePx = 30f;
        const float LaneW = 58f, LaneNearY = 90f; // 720p px: lane spacing, vertical reach of a ladder for the neighbour check
        const float LaneGraceMs = 1000f;

        /// <summary>Per monster hit by you: its ladder (Ladder = the monster itself, or a neighbour's when merged), where the ladder stands
        /// (world anchor + lane offset), until when it is live, and the biggest real hit (tiny-hit chip threshold).</summary>
        struct Stack { public float LiveUntilMs, AnchorX, AnchorY, LaneX, WX, WY; public int Peak; public object Ladder; }
        static readonly Dictionary<object, Stack> Stacks = new Dictionary<object, Stack>();

        static float StackNowMs { get { return Time.unscaledTime * 1000f; } }

        /// <summary>Puts a number of your own on its monster's ladder (lane offset, ladder anchor, short rise) and keeps the ladder alive while
        /// it shows. The lane stays LaneGraceMs after the last number so burn chips and the next hit reuse it instead of jumping lanes.</summary>
        static void StreamPlace(ref Entry e, object key)
        {
            if (e.RisePx <= 0f) e.RisePx = e.Kind == DamageNumberKind.Crit ? StreamCritRisePx : StreamRisePx;
            float now = StackNowMs, appear = now + Mathf.Max(0f, -e.AgeMs);
            Stack st;
            if (!Stacks.TryGetValue(key, out st) || now > st.LiveUntilMs + LaneGraceMs)
            {
                if (Stacks.Count > 64) PruneStacks(now);
                st = NewStack(key, e.X, e.Y, now);
            }
            st.LiveUntilMs = Mathf.Max(st.LiveUntilMs, appear + MotionOf(ref e).LifeMs);
            Stacks[key] = st;
            if (!ReferenceEquals(st.Ladder, key)) TouchLadder(st.Ladder, st.LiveUntilMs);
            e.Key = st.Ladder;
            e.X = st.WX; e.Y = st.WY; // the ladder stays where it started, even if the monster is knocked back
            e.OffX = st.LaneX;
        }

        /// <summary>A merged monster keeps the ladder it joined alive (so a third monster does not pick the same spot).</summary>
        static void TouchLadder(object ladder, float until)
        {
            Stack o;
            if (ladder == null || !Stacks.TryGetValue(ladder, out o) || o.LiveUntilMs >= until) return;
            o.LiveUntilMs = until;
            Stacks[ladder] = o;
        }

        /// <summary>Biggest real hit of the monster's live ladder (tiny hits below 1/8 of it become a chip), 0 when idle.</summary>
        static int PeakFor(object key)
        {
            Stack st;
            return key != null && Stacks.TryGetValue(key, out st) && StackNowMs <= st.LiveUntilMs + LaneGraceMs ? st.Peak : 0;
        }

        static void NotePeak(object key, int dmg)
        {
            Stack st;
            if (key == null || !Stacks.TryGetValue(key, out st) || dmg <= st.Peak) return;
            st.Peak = dmg;
            Stacks[key] = st;
        }

        static readonly float[] LaneSteps = { 0f, 1f, -1f };

        /// <summary>A monster starting a ladder: lane 0 unless a live ladder of another monster is within LaneW on screen, then one lane to
        /// the free side, then the other; when the three lanes are all taken it merges into the nearest neighbour's ladder (same spot, shared cap).</summary>
        static Stack NewStack(object key, float x, float y, float now)
        {
            var st = new Stack { Ladder = key, WX = x, WY = y };
            Camera c = instance != null && instance.cam != null ? instance.cam : Camera.main;
            if (c == null) return st;
            Vector2 a = GuiPoint(c, x, y) / (Screen.height / 720f);
            st.AnchorX = a.x; st.AnchorY = a.y;
            float side = 0f, nearest = float.MaxValue;
            bool any = false;
            Stack join = default(Stack);
            foreach (KeyValuePair<object, Stack> kv in Stacks)
            {
                Stack o = kv.Value;
                if (ReferenceEquals(kv.Key, key) || !ReferenceEquals(o.Ladder, kv.Key) || now > o.LiveUntilMs + LaneGraceMs || Mathf.Abs(o.AnchorY - a.y) > LaneNearY) continue;
                float d = Mathf.Abs(o.AnchorX + o.LaneX - a.x);
                if (d >= LaneW * 2f) continue;
                any = true;
                side += a.x - (o.AnchorX + o.LaneX);
                if (d < nearest) { nearest = d; join = o; join.Ladder = kv.Key; }
            }
            if (!any) return st;
            float dir = side < 0f ? -1f : 1f;
            for (int s = 0; s < LaneSteps.Length; s++)
            {
                float off = LaneSteps[s] * dir * LaneW;
                if (LaneFree(key, a.x + off, a.y, now)) { st.LaneX = off; return st; }
            }
            st.Ladder = join.Ladder; st.WX = join.WX; st.WY = join.WY; st.LaneX = join.LaneX; st.AnchorX = join.AnchorX; st.AnchorY = join.AnchorY;
            return st;
        }

        /// <summary>True when no live ladder of another monster stands within LaneW of screen x (720p px).</summary>
        static bool LaneFree(object key, float x, float y, float now)
        {
            foreach (KeyValuePair<object, Stack> kv in Stacks)
            {
                Stack o = kv.Value;
                if (ReferenceEquals(kv.Key, key) || !ReferenceEquals(o.Ladder, kv.Key) || now > o.LiveUntilMs + LaneGraceMs || Mathf.Abs(o.AnchorY - y) > LaneNearY) continue;
                if (Mathf.Abs(o.AnchorX + o.LaneX - x) < LaneW - 0.5f) return false;
            }
            return true;
        }

        /// <summary>Each frame: an older number's push-up target is the sum of the steps of the newer shown numbers of its monster; past
        /// MaxVisible it fades out fast. Chips and numbers not shown yet (negative age) are not on the ladder.</summary>
        static void UpdateLadders(float dtMs)
        {
            float ease = 1f - Mathf.Exp(-dtMs / LiftEaseMs);
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active || Pool[i].Key == null || Pool[i].Chip || Pool[i].AgeMs < 0f) continue;
                float target = 0f;
                int newer = 0;
                for (int j = 0; j < Capacity; j++)
                {
                    if (j == i || !Pool[j].Active || Pool[j].Chip || Pool[j].AgeMs < 0f || !ReferenceEquals(Pool[j].Key, Pool[i].Key)) continue;
                    if (Pool[j].AgeMs > Pool[i].AgeMs || (Pool[j].AgeMs == Pool[i].AgeMs && Order(j) < Order(i))) continue;
                    newer++;
                    target += DamageNumberMotion.Of(Pool[j].Kind).HeightPx * (Pool[j].SizeMul > 0f ? Pool[j].SizeMul : 1f) * StepMul;
                }
                Pool[i].Lift += (target - Pool[i].Lift) * ease;
                if (newer < MaxVisible) continue;
                float life = MotionOf(ref Pool[i]).LifeMs;
                if (Pool[i].AgeMs < life - CullFadeMs) Pool[i].AgeMs = life - CullFadeMs; // fade out fast instead of popping away
            }
        }

        /// <summary>Spawn order of a pool slot (bigger = newer), for numbers that appear on the same frame.</summary>
        static int Order(int i) { return (i - cursor - 1 + Capacity) % Capacity; }

        static readonly List<object> StaleKeys = new List<object>();

        static void PruneStacks(float now)
        {
            StaleKeys.Clear();
            foreach (KeyValuePair<object, Stack> kv in Stacks) if (now - kv.Value.LiveUntilMs > LaneGraceMs + 2000f) StaleKeys.Add(kv.Key);
            for (int i = 0; i < StaleKeys.Count; i++) Stacks.Remove(StaleKeys[i]);
        }
    }
}
