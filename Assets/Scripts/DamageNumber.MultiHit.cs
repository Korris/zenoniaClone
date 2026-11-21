using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// RO-style multi-hit numbers (user 21/11: "dam khi xài skill sẽ nhảy lên nhiều lần ... cảm giác rất phê"): one real hit of your own on a
    /// monster is drawn as N numbers whose sum is the real damage exactly (SkillHitSplit.Split, +-10 % per part), IntervalMs apart, each with a
    /// harder pop, on the monster's ladder (DamageNumber.Stream.cs). Parts before the last fade fast (PartLifeMs); the last one is 30 % bigger and
    /// keeps the full life, so a burst ends on one big number. A crit makes every part gold. Presentation only: the server, the damage and the kill do not wait.
    /// Each part is at least MinPartDmg (no "1 1 1 1" split of a small hit). Tiny hits (damage over time, or 1/8 of the monster's biggest live hit
    /// and less, like burn ticks of 1 after a 110 finisher) do not get their own number: they add into one small chip beside the ladder.
    /// </summary>
    public partial class DamageNumber
    {
        const float PartPop = 1.6f, LastPartSize = 1.3f, LethalIntervalMul = 0.6f, PartLifeMs = 500f, LastRisePx = 30f;
        const int MinPartDmg = 4, TinyRatio = 8;
        const float ChipSize = 0.68f, ChipOffY = -34f, ChipLifeMs = 700f; // just under the ladder's base number (a gap, so "30" + "2" never reads "302")
        static readonly Color ChipTint = new Color(1f, 0.66f, 0.36f); // warm burn orange: a chip never reads as a digit of the white numbers

        /// <summary>Your own hit of `dmg` on `target` at (x, y). `lethal`: the hit killed it, the remaining parts follow faster.
        /// Returns the parts shown (null when one number) and when they appear (ms after now: StartDelayMs + i x interval) so the HP bar can follow.</summary>
        public static int[] SpawnOwnHit(float x, float y, int dmg, bool crit, bool finisher, bool quiet, bool lethal, object target, out float startDelayMs, out float intervalMs)
        {
            startDelayMs = intervalMs = 0f;
            if (!NetMode.RendersFx || instance == null) return null;
            bool fx = FeelConfig.FxOn;
            if (!crit && !finisher && dmg > 0 && (quiet || dmg * TinyRatio <= PeakFor(target))) { SpawnChip(x, y, dmg, target); return null; }
            HitSplit split = SkillHitContext.ForLocalHit(quiet);
            int parts = fx ? SkillHitSplit.CrowdParts(split.PartsFor(finisher), CountOwnLive()) : 1;
            parts = Mathf.Min(parts, Mathf.Max(1, dmg / MinPartDmg));
            Color c = crit ? new Color(1f, 0.85f, 0.2f) : finisher ? new Color(1f, 0.69f, 0.23f) : Color.white;
            if (parts <= 1)
            {
                Spawn(x, y, crit && fx ? dmg + "!" : dmg.ToString(), c, crit || finisher, crit, false, target);
                NotePeak(target, dmg);
                return null;
            }
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
                    LifeMs = last ? 0f : PartLifeMs, RisePx = last ? LastRisePx : 0f,
                };
                StreamPlace(ref e, target);
                cursor = (cursor + 1) % Capacity;
                Pool[cursor] = e;
            }
            NotePeak(target, dmg);
            startDelayMs = split.StartDelayMs;
            intervalMs = interval;
            return values;
        }

        /// <summary>A tiny hit: added into the monster's live chip (which pops again and restarts its life), else a new small chip just under
        /// the monster's ladder. The chip shows the running total, so every point of damage is still on screen.</summary>
        static void SpawnChip(float x, float y, int dmg, object target)
        {
            var e = new Entry
            {
                Active = true, Text = dmg.ToString(), Color = ChipTint, X = x, Y = y, Kind = DamageNumberKind.Normal, Seed = Random.value,
                Chip = true, ChipValue = dmg, SizeMul = ChipSize, LifeMs = ChipLifeMs, RisePx = 0.01f, // a chip stays put: its age restarts on every add
            };
            StreamPlace(ref e, target); // the monster's ladder (shared when merged): one chip per ladder
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active || !Pool[i].Chip || !ReferenceEquals(Pool[i].Key, e.Key) || Pool[i].AgeMs > ChipLifeMs - 150f) continue;
                Pool[i].ChipValue += dmg;
                Pool[i].Text = Pool[i].ChipValue.ToString();
                Pool[i].AgeMs = 0f;
                return;
            }
            e.OffY = ChipOffY;
            cursor = (cursor + 1) % Capacity;
            Pool[cursor] = e;
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
