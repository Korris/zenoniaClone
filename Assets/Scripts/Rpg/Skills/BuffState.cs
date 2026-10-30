using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Temporary buffs of one hero (engine-free). Same kind refreshes: the stronger value wins and the timer restarts at the longer time.
    /// AtkPct / AtkSpeedPct are percent, Shield is an absolute HP amount that soaks damage, NextHitsPct is a % bonus for the next `charges` hits.
    /// </summary>
    public sealed class BuffState
    {
        sealed class Entry { public BuffKind Kind; public float Value, LeftMs; public int Charges; }
        readonly List<Entry> list = new List<Entry>(4);

        public bool Any { get { return list.Count > 0; } }
        public float AtkMul { get { return 1f + Sum(BuffKind.AtkPct) / 100f; } }
        public float AtkSpeedMul { get { return 1f + Sum(BuffKind.AtkSpeedPct) / 100f; } }
        public float ShieldLeft { get { return Sum(BuffKind.Shield); } }

        float Sum(BuffKind k) { Entry e = Find(k); return e != null ? e.Value : 0f; }

        Entry Find(BuffKind k) { for (int i = 0; i < list.Count; i++) if (list[i].Kind == k) return list[i]; return null; }

        public bool Has(BuffKind k) { return Find(k) != null; }

        public void Add(BuffKind kind, float value, int ms, int charges = 0)
        {
            if (kind == BuffKind.None || ms <= 0 || value <= 0f) return;
            Entry e = Find(kind);
            if (e == null) { list.Add(new Entry { Kind = kind, Value = value, LeftMs = ms, Charges = charges }); return; }
            e.Value = Math.Max(e.Value, value); e.LeftMs = Math.Max(e.LeftMs, ms); e.Charges = Math.Max(e.Charges, charges);
        }

        public void Tick(float dtMs)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                if ((list[i].LeftMs -= dtMs) <= 0f || list[i].Value <= 0f) list.RemoveAt(i);
        }

        /// <summary>Damage left after the shield soaked what it could (the shield shrinks; it ends at 0).</summary>
        public float Absorb(float dmg)
        {
            Entry e = Find(BuffKind.Shield);
            if (e == null || dmg <= 0f) return dmg;
            float soaked = Math.Min(e.Value, dmg);
            e.Value -= soaked;
            return dmg - soaked;
        }

        /// <summary>Multiplier of the caster's next hit (1 = none); spends one charge of NextHitsPct.</summary>
        public float ConsumeNextHit()
        {
            Entry e = Find(BuffKind.NextHitsPct);
            if (e == null) return 1f;
            float mul = 1f + e.Value / 100f;
            if (--e.Charges <= 0) list.Remove(e);
            return mul;
        }

        public void Clear() { list.Clear(); }
    }
}
