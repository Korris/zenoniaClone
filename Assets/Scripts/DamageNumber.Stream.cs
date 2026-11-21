using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Same-spot stack for your own hits on one monster (ChatGPT, RO-style spec 2026-11-20, chat "So sánh số sát thương RPG",
    /// https://chatgpt.com/c/6b0015c8-84c8-83ec-9236-67e79b0751b7): a hit pops at the same point above the monster, with no sideways dodge;
    /// older numbers keep rising and fading, so basic hits ~400 ms apart read as a vertical stack on their own.
    /// 21/11 multi-hit (SkillHitSplit, DamageNumber.MultiHit.cs): numbers that appear within BurstGapMs of the previous one on the same monster
    /// cascade upward, CascadeStepPx per number with a 4 px left / right nudge, over CascadeSlots slots (RO style), so a 6-part skill hit
    /// 80 ms apart does not print on one point. The first number of a burst still takes the base point. At most MaxPlainPerTarget plain numbers
    /// (delayed parts included) live per monster: one more fades the oldest out in 80 ms. Crits take cascade slots too but are never culled.
    /// </summary>
    public partial class DamageNumber
    {
        const int MaxPlainPerTarget = 8; // was 3 (20/11): a skill hit now shows up to 8 parts
        const float CullFadeMs = 80f, BurstGapMs = 300f, CascadeStepPx = 17f;
        const int CascadeSlots = 6;

        struct Stack { public float LastAppearMs; public int Slot; }
        static readonly Dictionary<object, Stack> Stacks = new Dictionary<object, Stack>();

        static float StackNowMs { get { return Time.unscaledTime * 1000f; } }

        static void StreamPlace(ref Entry e, object key)
        {
            e.Key = key;
            float appear = StackNowMs + Mathf.Max(0f, -e.AgeMs);
            Stack st;
            int slot = Stacks.TryGetValue(key, out st) && appear - st.LastAppearMs <= BurstGapMs ? st.Slot + 1 : 0;
            if (Stacks.Count > 64 && slot == 0) PruneStacks(appear);
            Stacks[key] = new Stack { LastAppearMs = Mathf.Max(appear, st.LastAppearMs), Slot = slot };
            int s = slot % CascadeSlots;
            e.OffX = s == 0 ? 0f : (s & 1) == 1 ? 4f : -4f;
            e.OffY = s * CascadeStepPx * DamageNumberMotion.Of(e.Kind).HeightPx / 30f; // a 52 px crit climbs further than a 30 px hit
            if (e.Kind == DamageNumberKind.Crit) return; // crits are never culled and never cull
            int live = 0, oldest = -1;
            float oldestAge = float.NegativeInfinity;
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active || Pool[i].Kind == DamageNumberKind.Crit || !ReferenceEquals(Pool[i].Key, key)) continue;
                live++;
                if (Pool[i].AgeMs > oldestAge) { oldestAge = Pool[i].AgeMs; oldest = i; }
            }
            if (live < MaxPlainPerTarget || oldest < 0) return;
            float life = DamageNumberMotion.Of(Pool[oldest].Kind).LifeMs;
            if (Pool[oldest].AgeMs < life - CullFadeMs) Pool[oldest].AgeMs = life - CullFadeMs; // fade out fast instead of popping away
        }

        static readonly List<object> StaleKeys = new List<object>();

        static void PruneStacks(float now)
        {
            StaleKeys.Clear();
            foreach (KeyValuePair<object, Stack> kv in Stacks) if (now - kv.Value.LastAppearMs > 2000f) StaleKeys.Add(kv.Key);
            for (int i = 0; i < StaleKeys.Count; i++) Stacks.Remove(StaleKeys[i]);
        }
    }
}
