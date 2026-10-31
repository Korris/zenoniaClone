using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>One running buff as the HUD sees it.</summary>
    public struct BuffView { public BuffKind Kind; public byte Source; public float LeftMs, TotalMs; }

    /// <summary>Which skill a buff came from, in one byte (class &lt;&lt; 3 | tree slot), so the HUD can show that skill's icon on any side.</summary>
    public static class BuffSource
    {
        public const byte None = 255;

        public static byte Pack(string classId, int skillIndex)
        {
            int c = Array.IndexOf(RpgClasses.Ids, classId);
            return c < 0 || skillIndex < 0 || skillIndex >= SkillDef.PerClass ? None : (byte)((c << 3) | skillIndex);
        }

        /// <summary>The skill behind a packed source; null for None or anything out of range.</summary>
        public static SkillDef Def(byte source)
        {
            int c = source >> 3;
            return source == None || c >= RpgClasses.Ids.Length ? null : SkillTable.Get(RpgClasses.Ids[c], source & 7);
        }
    }

    /// <summary>
    /// Temporary buffs of one hero (engine-free). Same kind refreshes: the stronger value wins and the timer restarts at the longer time.
    /// AtkPct / AtkSpeedPct / DefPct are percent, Shield is an absolute HP amount that soaks damage, NextHitsPct is a % bonus for the next `charges` hits.
    /// </summary>
    public sealed class BuffState
    {
        sealed class Entry { public BuffKind Kind; public float Value, LeftMs, TotalMs; public int Charges; public byte Source = BuffSource.None; }
        readonly List<Entry> list = new List<Entry>(4);

        public bool Any { get { return list.Count > 0; } }
        public float AtkMul { get { return 1f + Sum(BuffKind.AtkPct) / 100f; } }
        public float AtkSpeedMul { get { return 1f + Sum(BuffKind.AtkSpeedPct) / 100f; } }
        public float DefMul { get { return 1f + Sum(BuffKind.DefPct) / 100f; } }
        public float ShieldLeft { get { return Sum(BuffKind.Shield); } }

        float Sum(BuffKind k) { Entry e = Find(k); return e != null ? e.Value : 0f; }

        Entry Find(BuffKind k) { for (int i = 0; i < list.Count; i++) if (list[i].Kind == k) return list[i]; return null; }

        public bool Has(BuffKind k) { return Find(k) != null; }

        public void Add(BuffKind kind, float value, int ms, int charges = 0, byte source = BuffSource.None)
        {
            if (kind == BuffKind.None || ms <= 0 || value <= 0f) return;
            Entry e = Find(kind);
            if (e == null) { list.Add(new Entry { Kind = kind, Value = value, LeftMs = ms, TotalMs = ms, Charges = charges, Source = source }); return; }
            e.Value = Math.Max(e.Value, value); e.LeftMs = Math.Max(e.LeftMs, ms); e.TotalMs = e.LeftMs; e.Charges = Math.Max(e.Charges, charges);
            if (source != BuffSource.None) e.Source = source;
        }

        /// <summary>The running buffs for display, without touching them; `into` is cleared first.</summary>
        public void CopyViews(List<BuffView> into)
        {
            into.Clear();
            for (int i = 0; i < list.Count; i++)
                into.Add(new BuffView { Kind = list[i].Kind, Source = list[i].Source, LeftMs = list[i].LeftMs, TotalMs = list[i].TotalMs });
        }

        /// <summary>Remaining ms of a buff (0 when absent) and its value: what a predicting client mirrors from the server.</summary>
        public float LeftMs(BuffKind k) { Entry e = Find(k); return e != null ? e.LeftMs : 0f; }
        public float ValueOf(BuffKind k) { return Sum(k); }

        /// <summary>Forces a buff to exactly this value / time (server state restore); ms &lt;= 0 removes it.</summary>
        public void Set(BuffKind kind, float value, float ms)
        {
            Entry e = Find(kind);
            if (ms <= 0f || value <= 0f) { if (e != null) list.Remove(e); return; }
            if (e == null) list.Add(new Entry { Kind = kind, Value = value, LeftMs = ms, TotalMs = ms });
            else { e.Value = value; e.LeftMs = ms; }
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
