using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>Tooltip lines for the skill tree: "current -> next rank" per scaling value. Rank 0 (unlearned) shows the rank 1 values alone.</summary>
    public static class SkillText
    {
        public static string Cooldown(SkillDef d) { return "Cooldown " + (d.CooldownMs / 1000f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "s"; }

        public static string Mp(SkillDef d, int rank) { return Line("MP", d.MpAt(rank), d.MpAt(rank + 1), rank, "", d.MpAt(Math.Max(1, rank))); }

        /// <summary>MP line + one line per scaling effect value (damage, heal, shield, damage / heal over time).</summary>
        public static List<string> RankLines(SkillDef d, int rank)
        {
            var lines = new List<string> { Mp(d, rank) };
            if (d.Legacy != LegacySkill.None) return lines; // hand-written skill: numbers come from its adapter, shown by the UI as flavor text
            foreach (SkillEffect e in d.Effects)
            {
                int now = Percent(d, e, Math.Max(1, rank)), next = Percent(d, e, rank + 1);
                string label = Label(e);
                if (label != null) lines.Add(Line(label, now, next, rank, "%", now));
            }
            return lines;
        }

        internal static int Percent(SkillDef d, SkillEffect e, int rank)
        {
            return (int)Math.Round(e.Kind == EffectKind.Buff ? d.BuffAt(e, rank) : d.PctAt(e, rank));
        }

        /// <summary>Label of the scaling number an effect carries, null when it has none (fixed buffs / pure status / motion).</summary>
        internal static string Label(SkillEffect e)
        {
            switch (e.Kind)
            {
                case EffectKind.Heal: return "Heal";
                case EffectKind.Buff: return e.Buff.Kind == BuffKind.Shield ? "Shield" : null;
                case EffectKind.Zone: return e.Ally ? "Heal per tick" : "Damage per tick";
                case EffectKind.Summon: return "Damage per pulse";
                case EffectKind.Motion: return null;
                default: return e.Pct > 0f ? (e.Hits > 1 || e.Count > 1 ? "Damage per hit" : "Damage") : null;
            }
        }

        internal static string Line(string label, int now, int next, int rank, string unit, int shown)
        {
            if (rank <= 0 || rank >= SkillDef.MaxRank) return label + " " + shown + unit;
            return label + " " + now + unit + " -> " + next + unit;
        }
    }
}
