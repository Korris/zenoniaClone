using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Plain-language text of the skill tree detail pane, generated from the SkillDef data so it cannot drift from the real values at any rank.
    /// Pattern: what happens, damage or heal, range or area; statuses and boss exceptions go on their own coloured lines (UGUI rich text).
    /// Rank 0 (unlearned) describes rank 1. Design: ChatGPT chat "Thiet ke pane ky nang", 2026-10-30.
    /// </summary>
    public static class SkillDescribe
    {
        const string StatusBad = "#FF9566", StatusCold = "#79D9FF", StatusPurple = "#C8A5FF", BossColor = "#FFCA70";

        /// <summary>One to three sentences: what the skill does at this rank.</summary>
        public static string Description(SkillDef d, int rank)
        {
            rank = Math.Max(1, rank);
            if (d.Legacy == LegacySkill.EarthBreak)
                return "Charge, then slam " + Tiles(LegacySkillNumbers.EarthBreakRange / LegacySkillNumbers.TileUnits) + " ahead: " + Atk(LegacySkillNumbers.EarthBreakMult * 100f * d.DamageMul(rank))
                    + " within " + Tiles(LegacySkillNumbers.EarthBreakRadius / LegacySkillNumbers.TileUnits) + ". The sweep before it deals " + Atk(LegacySkillNumbers.EarthBreakMult * LegacySkillNumbers.EarthBreakSweep * 100f * d.DamageMul(rank))
                    + ", then " + LegacySkillNumbers.EarthBreakBurns + " burns of " + Atk(LegacySkillNumbers.EarthBreakMult * LegacySkillNumbers.EarthBreakBurn * 100f * d.DamageMul(rank)) + ".";
            if (d.Legacy == LegacySkill.IceStorm)
                return "Ice spears fall " + Tiles(LegacySkillNumbers.IceStormRange / LegacySkillNumbers.TileUnits) + " ahead: " + LegacySkillNumbers.IceStormWaves + " waves of "
                    + Atk(LegacySkillNumbers.IceStormMult * 100f * d.DamageMul(rank)) + " within " + Tiles(LegacySkillNumbers.IceStormRadius / LegacySkillNumbers.TileUnits) + ".";
            if (d.Legacy == LegacySkill.RocketBarrage)
                return LegacySkillNumbers.RocketBarrageRockets + " rockets rain on a " + Num(LegacySkillNumbers.RocketBarrageFanDeg) + "-degree fan up to " + Tiles(LegacySkillNumbers.RocketBarrageReach / LegacySkillNumbers.TileUnits) + " ahead: "
                    + Atk(LegacySkillNumbers.RocketBarrageMult * 100f * d.DamageMul(rank)) + " each within " + Tiles(LegacySkillNumbers.RocketBarrageRadius / LegacySkillNumbers.TileUnits) + ".";

            var sb = new StringBuilder();
            SkillEffect[] fx = d.Effects;
            for (int i = 0; i < fx.Length; i++)
            {
                SkillEffect e = fx[i];
                if (e.Kind == EffectKind.Motion && i + 1 < fx.Length && fx[i + 1].Kind == EffectKind.Strike)
                {
                    int repeats = 1;
                    while (i + 2 * repeats + 1 < fx.Length && SamePair(e, fx[i + 1], fx[i + 2 * repeats], fx[i + 2 * repeats + 1])) repeats++;
                    Add(sb, Motion(d, e) + ", then " + StrikeClause(d, fx[i + 1], rank));
                    if (repeats > 1) Add(sb, "Repeats " + repeats + " times");
                    i += 2 * repeats - 1;
                }
                else if (e.Kind == EffectKind.Buff)
                {
                    int last = i;
                    while (last + 1 < fx.Length && fx[last + 1].Kind == EffectKind.Buff && fx[last + 1].Buff.Ms == e.Buff.Ms && fx[last + 1].Ally == e.Ally) last++;
                    Add(sb, Buffs(d, fx, i, last, rank));
                    i = last;
                }
                else Add(sb, Sentence(d, e, rank));
            }
            return sb.ToString();
        }

        /// <summary>Coloured status / bonus lines with their boss exceptions; empty when the skill has none.</summary>
        public static string StatusLines(SkillDef d)
        {
            var lines = new List<string>();
            if (d.Legacy == LegacySkill.EarthBreak) { lines.Add(Tag("LAUNCH", StatusPurple) + " 1.5s"); lines.Add(Boss("immune")); }
            if (d.Legacy == LegacySkill.IceStorm)
            {
                lines.Add(Tag("CHILL", StatusCold) + " 50% slow, " + Secs((int)LegacySkillNumbers.IceStormChillMs));
                lines.Add(Boss(Num(StatusRules.ForBoss(new StatusSpec(StatusKind.Chill, 50f, 0)).Value) + "% slow"));
            }
            foreach (SkillEffect e in d.Effects)
            {
                if (e.Status.Kind != StatusKind.None) AddOnce(lines, StatusText(e.Status));
                if (e.Status.Kind != StatusKind.None) { string b = BossText(e.Status); if (b != null) AddOnce(lines, b); }
                if (e.BehindBonusPct > 0f) AddOnce(lines, Tag("BACKSTAB", StatusPurple) + " +" + Num(e.BehindBonusPct) + "% damage");
                if (e.BonusVsMask != 0) AddOnce(lines, Tag("BONUS", StatusPurple) + " +" + Num(e.BonusPct) + "% damage vs " + MaskNames(e.BonusVsMask));
            }
            return string.Join("\n", lines.ToArray());
        }

        public static string CostLine(SkillDef d, int rank) { return "MP " + d.MpAt(Math.Max(1, rank)) + "     " + SkillText.Cooldown(d); }

        public static string RankHeader(int rank) { return rank >= SkillDef.MaxRank ? "MAX RANK" : rank <= 0 ? "AT RANK 1" : "NEXT RANK  " + (rank + 1) + " / " + SkillDef.MaxRank; }

        /// <summary>What the next rank adds ("Damage 280% -> 314%"): repeated effects count once, MP only when it changes here, and skills whose rank adds no power say so.</summary>
        public static List<string> RankLines(SkillDef d, int rank)
        {
            var lines = new List<string>();
            int shown = Math.Max(1, rank), next = rank + 1;
            if (d.Legacy == LegacySkill.EarthBreak) lines.Add(ScaledLine("Slam", LegacySkillNumbers.EarthBreakMult * 100f, d, rank));
            else if (d.Legacy == LegacySkill.IceStorm) lines.Add(ScaledLine("Per wave", LegacySkillNumbers.IceStormMult * 100f, d, rank));
            else if (d.Legacy == LegacySkill.RocketBarrage) lines.Add(ScaledLine("Per rocket", LegacySkillNumbers.RocketBarrageMult * 100f, d, rank));
            else
                foreach (SkillEffect e in d.Effects)
                {
                    string label = e.Kind == EffectKind.Summon && d.Class != "mechanic" ? "Damage per strike" : SkillText.Label(e);
                    if (label != null) AddOnce(lines, SkillText.Line(label, SkillText.Percent(d, e, shown), SkillText.Percent(d, e, next), rank, "%", SkillText.Percent(d, e, shown)));
                }
            if (rank < SkillDef.MaxRank && lines.Count == 0) lines.Add("Ranks only raise the MP cost");
            if (rank > 0 && rank < SkillDef.MaxRank) lines.Add(SkillText.Mp(d, rank));
            return lines;
        }

        static string ScaledLine(string label, float basePct, SkillDef d, int rank)
        {
            int now = (int)Math.Round(basePct * d.DamageMul(rank)), up = (int)Math.Round(basePct * d.DamageMul(rank + 1));
            return SkillText.Line(label, now, up, rank, "%", now);
        }

        // ---- sentences ----

        static string Sentence(SkillDef d, SkillEffect e, int rank)
        {
            switch (e.Kind)
            {
                case EffectKind.Strike: return Cap(StrikeClause(d, e, rank));
                case EffectKind.Projectile: return Projectile(d, e, rank);
                case EffectKind.Motion: return Motion(d, e);
                case EffectKind.Heal: return Heal(d, e, rank);
                case EffectKind.Summon: return Summon(d, e, rank);
                case EffectKind.Rain:
                    return e.Count + " strikes fall around a spot " + Tiles(e.Range) + " ahead, one every " + Secs(e.IntervalMs) + ": " + Atk(d.PctAt(e, rank)) + " each in a " + Num(e.ImpactRadius) + "-tile blast";
                case EffectKind.Zone:
                    return e.Ally
                        ? "Create a healing zone for " + Secs(e.DurationMs) + ": heals you and allies within " + Tiles(e.Radius) + " for " + Atk(d.PctAt(e, rank)) + " every " + Secs(e.IntervalMs)
                        : (e.Attached ? "A zone follows you for " : "Create a zone for ") + Secs(e.DurationMs) + ": hits enemies within " + Tiles(e.Radius) + " for " + Atk(d.PctAt(e, rank)) + " every " + Secs(e.IntervalMs);
                default: return "";
            }
        }

        /// <summary>Lower-case clause, so it can follow a motion: "hit enemies within 3 tiles for 280% ATK".</summary>
        static string StrikeClause(SkillDef d, SkillEffect e, int rank)
        {
            if (e.Pct <= 0f)
            {
                string verb = e.Status.Kind.ToString().ToLowerInvariant();
                if (d.Targeting == SkillTargeting.NearestEnemy) return verb + " the nearest enemy within " + Tiles(d.TargetRange);
                return verb + (e.Count > 1 ? " up to " + e.Count + " enemies " : " enemies ") + Area(e);
            }
            string damage = e.Hits > 1 ? e.Hits + " times for " + Atk(d.PctAt(e, rank)) + " each" : "for " + Atk(d.PctAt(e, rank));
            return e.Shape == AreaShape.Single ? "strike " + damage : "hit enemies " + Area(e) + " " + damage;
        }

        static string Area(SkillEffect e)
        {
            switch (e.Shape)
            {
                case AreaShape.Arc: return "in a " + Num(e.AngleDeg) + "-degree arc, " + Tiles(e.Radius) + " ahead";
                case AreaShape.Cone: return "in a " + Num(e.AngleDeg) + "-degree cone, " + Tiles(e.Radius) + " ahead";
                case AreaShape.Line: return "along a " + Num(e.Length) + "-tile line";
                case AreaShape.Circle: return e.Range > 0f ? "in a " + Num(e.Radius) + "-tile circle " + Tiles(e.Range) + " ahead" : "within " + Tiles(e.Radius);
                default: return "nearby";
            }
        }

        static string Motion(SkillDef d, SkillEffect e)
        {
            if (e.Motion == MotionKind.Blink) return d.Targeting == SkillTargeting.NearestEnemy ? "Blink behind the nearest enemy within " + Tiles(d.TargetRange) : "Blink " + Tiles(e.MoveTiles);
            return (e.Motion == MotionKind.Leap ? "Leap " : "Dash ") + Tiles(e.MoveTiles);
        }

        static string Projectile(SkillDef d, SkillEffect e, int rank)
        {
            string what = (e.Homing ? "homing " : "") + (e.Pierce ? "piercing " : "") + "shot" + (e.Count > 1 ? "s" : "");
            string s = "Fire " + (e.Count > 1 ? e.Count + " " : "a ") + what + (e.SpreadDeg > 0f ? " in a " + Num(e.SpreadDeg) + "-degree fan" : "") + " up to " + Tiles(e.Range) + " for " + Atk(d.PctAt(e, rank)) + (e.Count > 1 ? " each" : "");
            return e.ImpactRadius > 0f ? s + ", exploding within " + Tiles(e.ImpactRadius) : s;
        }

        static string Buffs(SkillDef d, SkillEffect[] fx, int from, int to, int rank)
        {
            SkillEffect first = fx[from];
            var parts = new List<string>();
            for (int i = from; i <= to; i++)
            {
                float v = d.BuffAt(fx[i], rank);
                switch (fx[i].Buff.Kind)
                {
                    case BuffKind.AtkPct: parts.Add("+" + Num(v) + "% ATK"); break;
                    case BuffKind.AtkSpeedPct: parts.Add("+" + Num(v) + "% attack speed"); break;
                    case BuffKind.Shield: parts.Add("a shield worth " + Atk(v)); break;
                    case BuffKind.NextHitsPct: parts.Add("+" + Num(v) + "% damage on the next hits"); break;
                }
            }
            string who = first.Ally && first.Radius > 0f ? "You and allies within " + Tiles(first.Radius) + " gain " : "Gain ";
            return who + string.Join(" and ", parts.ToArray()) + " for " + Secs(first.Buff.Ms);
        }

        static string Heal(SkillDef d, SkillEffect e, int rank)
        {
            string amount = " for " + Atk(d.PctAt(e, rank));
            if (d.Targeting == SkillTargeting.LowestAlly) return "Heal the lowest-HP ally within " + Tiles(d.TargetRange) + amount;
            if (e.Radius <= 0f) return "Heal yourself" + amount;
            return e.Range > 0f ? "Heal allies in a " + Num(e.Radius) + "-tile circle " + Tiles(e.Range) + " ahead" + amount : "Heal you and allies within " + Tiles(e.Radius) + amount;
        }

        /// <summary>The mechanic's drone hovers by the caster and pulses an area; the assassin's clone chases enemies (SkillMinion).</summary>
        static string Summon(SkillDef d, SkillEffect e, int rank)
        {
            string dmg = Atk(d.PctAt(e, rank));
            return d.Class == "mechanic"
                ? "Call a drone for " + Secs(e.DurationMs) + ": it hits enemies within " + Tiles(e.Radius) + " for " + dmg + " every " + Secs(e.IntervalMs)
                : "Summon a clone for " + Secs(e.DurationMs) + ": it chases enemies and strikes for " + dmg + " every " + Secs(e.IntervalMs);
        }

        // ---- statuses ----

        static string StatusText(StatusSpec s)
        {
            switch (s.Kind)
            {
                case StatusKind.Burn: return Tag("BURN", StatusBad) + " " + Num(s.Value) + "% ATK/s, " + Secs(s.Ms);
                case StatusKind.Poison: return Tag("POISON", StatusBad) + " " + Num(s.Value) + "% ATK/s, " + Secs(s.Ms);
                case StatusKind.Freeze: return Tag("FREEZE", StatusCold) + " " + Secs(s.Ms);
                case StatusKind.Stun: return Tag("STUN", StatusBad) + " " + Secs(s.Ms);
                case StatusKind.Slow: return Tag("SLOW", StatusCold) + " " + Num(s.Value) + "%, " + Secs(s.Ms);
                case StatusKind.Chill: return Tag("CHILL", StatusCold) + " " + Num(s.Value) + "% slow, " + Secs(s.Ms);
                case StatusKind.Mark: return Tag("MARK", StatusPurple) + " +" + Num(s.Value) + "% damage taken" + (s.Charges > 0 ? ", your next " + s.Charges + " hits" : "") + ", " + Secs(s.Ms);
                case StatusKind.Knockback: return Tag("KNOCKBACK", StatusPurple) + " " + Tiles(s.Value);
                case StatusKind.Launch: return Tag("LAUNCH", StatusPurple);
                default: return s.Kind.ToString();
            }
        }

        /// <summary>The exception StatusRules.ForBoss applies to this status, null when bosses take it as is.</summary>
        static string BossText(StatusSpec s)
        {
            StatusSpec b = StatusRules.ForBoss(s);
            if (b.Kind == StatusKind.None) return Boss("immune");
            if (b.Kind != s.Kind) return Boss(Num(b.Value) + "% " + b.Kind.ToString().ToLowerInvariant() + " instead");
            return b.Value != s.Value ? Boss(Num(b.Value) + "% slow") : null;
        }

        static string MaskNames(int mask)
        {
            var names = new List<string>();
            foreach (StatusKind k in Enum.GetValues(typeof(StatusKind)))
                if (k != StatusKind.None && (mask & SkillEffect.StatusBit(k)) != 0) names.Add(k.ToString());
            return string.Join(", ", names.ToArray());
        }

        // ---- formatting ----

        static bool SamePair(SkillEffect m, SkillEffect s, SkillEffect m2, SkillEffect s2)
        {
            return m2.Kind == EffectKind.Motion && s2.Kind == EffectKind.Strike && m.Motion == m2.Motion && m.MoveTiles == m2.MoveTiles && s.Pct == s2.Pct && s.Shape == s2.Shape && s.Length == s2.Length;
        }

        static void Add(StringBuilder sb, string sentence) { if (sentence.Length == 0) return; if (sb.Length > 0) sb.Append(' '); sb.Append(sentence).Append('.'); }
        static void AddOnce(List<string> lines, string line) { if (!lines.Contains(line)) lines.Add(line); }
        static string Cap(string s) { return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1); }
        static string Tag(string name, string color) { return "<color=" + color + ">" + name + "</color>"; }
        static string Boss(string text) { return "<color=" + BossColor + ">Boss: " + text + "</color>"; }
        static string Atk(float pct) { return (int)Math.Round(pct) + "% ATK"; }
        static string Tiles(float t) { return Num(t) + (t == 1f ? " tile" : " tiles"); }
        static string Secs(int ms) { return Num(ms / 1000f) + "s"; }
        static string Num(float v) { return v.ToString("0.#", CultureInfo.InvariantCulture); }
    }
}
