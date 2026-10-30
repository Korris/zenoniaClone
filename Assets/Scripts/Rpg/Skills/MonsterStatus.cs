using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Status effects of one monster (engine-free, sim side only). Timers + refresh rules live here; the Monster applies the results (speed, AI pause,
    /// tint, damage). Sources are opaque objects (the attacking hero) so damage over time is credited to its caster and stacks per source.
    /// </summary>
    public sealed class MonsterStatus
    {
        public const int DotTickMs = 500, MaxDotSources = 3;

        sealed class Dot { public object Src; public StatusKind Kind; public float Pct, Atk, Ms; }

        float slowMs, slowPct, chillMs, chillPct, freezeMs, stunMs, markMs, markPct, dotAcc;
        readonly List<Dot> dots = new List<Dot>(4);

        public object MarkSource { get; private set; }
        public int MarkCharges { get; private set; }

        /// <summary>Frozen or stunned: no AI, no movement.</summary>
        public bool Disabled { get { return freezeMs > 0f || stunMs > 0f; } }
        /// <summary>Move speed multiplier: 0 while disabled, else slow / chill (the stronger one).</summary>
        public float SpeedFactor { get { return Disabled ? 0f : StatusRules.SpeedFactor(Math.Max(slowMs > 0f ? slowPct : 0f, chillMs > 0f ? chillPct : 0f)); } }
        /// <summary>Incoming damage multiplier (mark).</summary>
        public float IncomingMul { get { return markMs > 0f ? StatusRules.MarkMultiplier(markPct) : 1f; } }
        public int DotSources { get { return dots.Count; } }

        public bool Has(StatusKind k)
        {
            switch (k)
            {
                case StatusKind.Slow: return slowMs > 0f;
                case StatusKind.Chill: return chillMs > 0f;
                case StatusKind.Freeze: return freezeMs > 0f;
                case StatusKind.Stun: return stunMs > 0f;
                case StatusKind.Mark: return markMs > 0f;
                case StatusKind.Burn:
                case StatusKind.Poison:
                    for (int i = 0; i < dots.Count; i++) if (dots[i].Kind == k) return true;
                    return false;
                default: return false;
            }
        }

        /// <summary>Bit (1 &lt;&lt; StatusKind) set of the active statuses, the mask SkillEffect.BonusVsMask is tested against.</summary>
        public int Mask
        {
            get
            {
                int m = 0;
                for (StatusKind k = StatusKind.Slow; k <= StatusKind.Chill; k++) if (Has(k)) m |= SkillEffect.StatusBit(k);
                return m;
            }
        }

        /// <summary>Most visible status (tint / replication): freeze, stun, burn, poison, mark, then slow / chill; None when clean.</summary>
        public StatusKind Dominant
        {
            get
            {
                if (freezeMs > 0f) return StatusKind.Freeze;
                if (stunMs > 0f) return StatusKind.Stun;
                if (Has(StatusKind.Burn)) return StatusKind.Burn;
                if (Has(StatusKind.Poison)) return StatusKind.Poison;
                if (markMs > 0f) return StatusKind.Mark;
                if (chillMs > 0f) return StatusKind.Chill;
                return slowMs > 0f ? StatusKind.Slow : StatusKind.None;
            }
        }

        public void Clear()
        {
            slowMs = chillMs = freezeMs = stunMs = markMs = dotAcc = 0f;
            MarkCharges = 0; MarkSource = null;
            dots.Clear();
        }

        /// <summary>Client proxy: shows chill from the snapshot flag without any rules (no boss conversion, no timer).</summary>
        public void ForceChill(float ms) { chillMs = ms; chillPct = ms > 0f ? 50f : 0f; }

        /// <summary>Applies a status. Returns false when ignored (boss immunity, knockback / launch, empty spec).</summary>
        public bool Apply(StatusSpec s, object src, bool boss, float casterAtk)
        {
            if (boss) s = StatusRules.ForBoss(s);
            if (s.Ms <= 0) return false;
            switch (s.Kind)
            {
                case StatusKind.Slow: StatusRules.Refresh(ref slowMs, ref slowPct, s.Ms, s.Value); return true;
                case StatusKind.Chill: StatusRules.Refresh(ref chillMs, ref chillPct, s.Ms, s.Value); return true;
                case StatusKind.Freeze: freezeMs = Math.Max(freezeMs, s.Ms); return true;
                case StatusKind.Stun: stunMs = Math.Max(stunMs, s.Ms); return true;
                case StatusKind.Mark:
                    StatusRules.Refresh(ref markMs, ref markPct, s.Ms, s.Value);
                    MarkSource = src; MarkCharges = Math.Max(MarkCharges, s.Charges);
                    return true;
                case StatusKind.Burn:
                case StatusKind.Poison: return s.Value > 0f && ApplyDot(s, src, casterAtk);
                default: return false;
            }
        }

        /// <summary>Spends one mark charge (Death Mark); the mark ends when the charges run out.</summary>
        public void ConsumeMarkCharge()
        {
            if (MarkCharges <= 0) return;
            if (--MarkCharges == 0) markMs = 0f;
        }

        bool ApplyDot(StatusSpec s, object src, float atk)
        {
            for (int i = 0; i < dots.Count; i++)
            {
                Dot d = dots[i];
                if (d.Kind != s.Kind || !ReferenceEquals(d.Src, src)) continue;
                StatusRules.Refresh(ref d.Ms, ref d.Pct, s.Ms, s.Value);
                d.Atk = Math.Max(d.Atk, atk);
                return true;
            }
            int sources = 0, weakest = -1;
            for (int i = 0; i < dots.Count; i++)
            {
                if (dots[i].Kind != s.Kind) continue;
                sources++;
                if (weakest < 0 || Power(dots[i]) < Power(dots[weakest])) weakest = i;
            }
            var add = new Dot { Src = src, Kind = s.Kind, Pct = s.Value, Atk = atk, Ms = s.Ms };
            if (sources < MaxDotSources) { dots.Add(add); return true; }
            if (Power(add) <= Power(dots[weakest])) return false; // cap reached: only a stronger dot displaces the weakest
            dots[weakest] = add;
            return true;
        }

        static float Power(Dot d) { return d.Atk * d.Pct * d.Ms; }

        /// <summary>
        /// Advances every timer. All damage over time ticks together every DotTickMs; `onDot(source, damage)` fires once per source per tick
        /// (burn + poison of the same caster are summed into one hit).
        /// </summary>
        public void Tick(float dtMs, Action<object, float> onDot)
        {
            slowMs = Math.Max(0f, slowMs - dtMs); chillMs = Math.Max(0f, chillMs - dtMs);
            freezeMs = Math.Max(0f, freezeMs - dtMs); stunMs = Math.Max(0f, stunMs - dtMs);
            if (markMs > 0f && (markMs -= dtMs) <= 0f) { markMs = 0f; MarkCharges = 0; MarkSource = null; }
            if (dots.Count == 0) { dotAcc = 0f; return; }
            dotAcc += dtMs;
            while (dotAcc >= DotTickMs)
            {
                dotAcc -= DotTickMs;
                if (onDot != null) FireTick(onDot);
            }
            for (int i = dots.Count - 1; i >= 0; i--)
            {
                dots[i].Ms -= dtMs;
                if (dots[i].Ms <= 0f) dots.RemoveAt(i);
            }
        }

        void FireTick(Action<object, float> onDot)
        {
            for (int i = 0; i < dots.Count; i++)
            {
                bool seen = false;
                for (int j = 0; j < i && !seen; j++) seen = ReferenceEquals(dots[j].Src, dots[i].Src);
                if (seen) continue;
                float sum = 0f;
                for (int j = i; j < dots.Count; j++)
                    if (ReferenceEquals(dots[j].Src, dots[i].Src)) sum += StatusRules.DotTickDamage(dots[j].Atk, dots[j].Pct, DotTickMs);
                onDot(dots[i].Src, sum);
            }
        }
    }
}
