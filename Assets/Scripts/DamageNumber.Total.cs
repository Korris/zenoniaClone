using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// RO-style multi-hit TOTAL (user spec 22/11): the white numbers of your own hits on one monster's ladder that pop less than ChainGapMs apart
    /// form a chain (the split parts of one hit, a skill's real hits landing close together, a double attack). Right after the last part of a
    /// hit, when the chain holds 2+ numbers, a TOTAL shows above the white cluster: bright yellow, 150 %, thick black outline (Kind Total, own
    /// atlas). Later hits of the same chain add into it and bump it again. It sums exactly the numbers already shown (burn chips stay apart).
    /// A chain with a crit makes the total the crit look instead (red-orange on the spiky burst): the crit wins over yellow.
    /// A new chain on the ladder fades the old total out at once, so a total never stands over numbers it does not count.
    /// </summary>
    public partial class DamageNumber
    {
        const float ChainGapMs = 320f, TotalDelayMs = 90f, TotalBumpAgeMs = 80f, TotalLifeMs = 1100f, TotalGapPx = 2f;

        struct Chain { public int Sum, Count, Slot; public bool Crit; public float LastMs; }
        static readonly Dictionary<object, Chain> Chains = new Dictionary<object, Chain>();
        static readonly Dictionary<object, float> ClusterTop = new Dictionary<object, float>(); // per ladder: top of its visible numbers (720p px)

        /// <summary>A number of your own just popped (Update): add it into its ladder's chain and show / bump the TOTAL after a last part.</summary>
        static void OnShown(int i)
        {
            if (Pool[i].Value <= 0 || Pool[i].Key == null || Pool[i].Chip || Pool[i].Total) return;
            object key = Pool[i].Key;
            float now = StackNowMs;
            Chain ch;
            bool had = Chains.TryGetValue(key, out ch);
            if (!had || now - ch.LastMs > ChainGapMs)
            {
                if (Chains.Count > 64) PruneChains(now);
                if (had) FadeTotal(ch.Slot, key); // a new chain: the old total no longer matches what is on screen
                ch = new Chain { Slot = -1 };
            }
            ch.Sum += Pool[i].Value;
            ch.Count++;
            ch.Crit |= Pool[i].Kind == DamageNumberKind.Crit;
            ch.LastMs = now;
            if (Pool[i].Last && ch.Count >= 2) ch.Slot = ShowTotal(ch, i);
            Chains[key] = ch;
        }

        static bool IsTotalOf(int slot, object key)
        {
            return slot >= 0 && Pool[slot].Active && Pool[slot].Total && ReferenceEquals(Pool[slot].Key, key);
        }

        static void FadeTotal(int slot, object key)
        {
            if (!IsTotalOf(slot, key)) return;
            float life = MotionOf(ref Pool[slot]).LifeMs;
            if (Pool[slot].AgeMs < 0f) Pool[slot].Active = false; // not shown yet
            else if (Pool[slot].AgeMs < life - CullFadeMs) Pool[slot].AgeMs = life - CullFadeMs;
        }

        /// <summary>Updates the chain's live TOTAL (bumps it: back to the top of its pop) or adds a new one TotalDelayMs after part i.</summary>
        static int ShowTotal(Chain ch, int i)
        {
            object key = Pool[i].Key;
            DamageNumberKind kind = ch.Crit ? DamageNumberKind.Crit : DamageNumberKind.Total;
            string text = ch.Crit ? ch.Sum + "!" : ch.Sum.ToString();
            if (IsTotalOf(ch.Slot, key))
            {
                Pool[ch.Slot].Text = text;
                Pool[ch.Slot].Value = ch.Sum;
                if (Pool[ch.Slot].Kind != kind) { Pool[ch.Slot].Kind = kind; Pool[ch.Slot].BigBurst = true; Pool[ch.Slot].AgeMs = Mathf.Min(Pool[ch.Slot].AgeMs, 0f); }
                else if (Pool[ch.Slot].AgeMs > TotalBumpAgeMs) Pool[ch.Slot].AgeMs = TotalBumpAgeMs;
                return ch.Slot;
            }
            cursor = (cursor + 1) % Capacity;
            Pool[cursor] = new Entry
            {
                Active = true, Total = true, Counted = true, Kind = kind, Text = text, Value = ch.Sum, Color = Color.white,
                X = Pool[i].X, Y = Pool[i].Y, Key = key, OffX = Pool[i].OffX, // a last part has no jitter: the lane centre Seed = Random.value,
                AgeMs = -TotalDelayMs, LifeMs = TotalLifeMs, RisePx = 0.01f, BigBurst = ch.Crit,
            };
            return cursor;
        }

        /// <summary>Each frame after the ladder: a TOTAL eases to just above the top of its ladder's visible numbers (rise included).</summary>
        static void UpdateTotals(float ease)
        {
            float normalH = DamageNumberMotion.Of(DamageNumberKind.Normal).HeightPx;
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active || !Pool[i].Total) continue;
                float top;
                if (!ClusterTop.TryGetValue(Pool[i].Key, out top)) top = 0f;
                float h = DamageNumberMotion.Of(Pool[i].Kind).HeightPx;
                float target = top > 0f ? top - normalH * (StepMul - 0.5f) + h * 0.5f + TotalGapPx : 0f; // centre over the top number's upper edge
                Pool[i].Lift = Pool[i].AgeMs < 0f ? target : Pool[i].Lift + (target - Pool[i].Lift) * ease;
            }
            ClusterTop.Clear();
        }

        /// <summary>Called by the ladder for each visible number: `top` = its lift + step + rise (720p px).</summary>
        static void NoteClusterTop(object key, float top)
        {
            float have;
            if (!ClusterTop.TryGetValue(key, out have) || top > have) ClusterTop[key] = top;
        }

        static readonly List<object> StaleChains = new List<object>();

        static void PruneChains(float now)
        {
            StaleChains.Clear();
            foreach (KeyValuePair<object, Chain> kv in Chains) if (now - kv.Value.LastMs > 3000f) StaleChains.Add(kv.Key);
            for (int i = 0; i < StaleChains.Count; i++) Chains.Remove(StaleChains[i]);
        }
    }
}
