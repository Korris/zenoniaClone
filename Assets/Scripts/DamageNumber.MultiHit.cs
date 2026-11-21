using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// RO-style multi-hit numbers (user 21/11: "dam khi xài skill sẽ nhảy lên nhiều lần ... cảm giác rất phê"): one real hit of your own on a
    /// monster is drawn as N numbers whose sum is the real damage exactly (SkillHitSplit.Split, +-10 % per part), IntervalMs apart, each with a
    /// harder pop; the last part is 20 % bigger. A crit makes every part gold. Presentation only: the server, the damage and the kill do not wait.
    /// Parts are pool entries with a negative age (not drawn until their turn), placed by the same-spot cascade (DamageNumber.Stream.cs).
    /// </summary>
    public partial class DamageNumber
    {
        const float PartPop = 1.6f, LastPartSize = 1.2f, LethalIntervalMul = 0.6f;

        /// <summary>Your own hit of `dmg` on `target` at (x, y). `lethal`: the hit killed it, the remaining parts follow faster.
        /// Returns the parts shown (null when one number) and when they appear (ms after now: StartDelayMs + i x interval) so the HP bar can follow.</summary>
        public static int[] SpawnOwnHit(float x, float y, int dmg, bool crit, bool finisher, bool quiet, bool lethal, object target, out float startDelayMs, out float intervalMs)
        {
            startDelayMs = intervalMs = 0f;
            if (!NetMode.RendersFx || instance == null) return null;
            bool fx = FeelConfig.FxOn;
            HitSplit split = SkillHitContext.ForLocalHit(quiet);
            int parts = fx ? SkillHitSplit.CrowdParts(split.PartsFor(finisher), CountOwnLive()) : 1;
            Color c = crit ? new Color(1f, 0.85f, 0.2f) : finisher ? new Color(1f, 0.69f, 0.23f) : Color.white;
            if (parts <= 1) { Spawn(x, y, crit && fx ? dmg + "!" : dmg.ToString(), c, crit || finisher, crit, false, target); return null; }
            int[] values = SkillHitSplit.Split(dmg, parts, Random.Range(int.MinValue, int.MaxValue));
            float interval = split.IntervalMs * (lethal ? LethalIntervalMul : 1f);
            for (int i = 0; i < values.Length; i++)
            {
                bool last = i == values.Length - 1;
                string text = crit ? values[i] + "!" : values[i].ToString();
                // Only the closing part of a finisher keeps the finisher look (ring + slash); the others are plain white (or gold crits).
                DamageNumberKind kind = crit ? DamageNumberKind.Crit : finisher && last ? DamageNumberKind.Finisher : DamageNumberKind.Normal;
                var e = new Entry
                {
                    Active = true, Text = text, Color = c, X = x, Y = y, Kind = kind, Seed = Random.value,
                    AgeMs = -(split.StartDelayMs + i * interval), PopMul = PartPop, SizeMul = last && kind != DamageNumberKind.Finisher ? LastPartSize : 1f,
                };
                StreamPlace(ref e, target);
                cursor = (cursor + 1) % Capacity;
                Pool[cursor] = e;
            }
            startDelayMs = split.StartDelayMs;
            intervalMs = interval;
            return values;
        }

        /// <summary>Live numbers of your own (crowd LOD of the split).</summary>
        static int CountOwnLive()
        {
            int n = 0;
            for (int i = 0; i < Capacity; i++)
                if (Pool[i].Active && Pool[i].Key != null) n++;
            return n;
        }
    }
}
