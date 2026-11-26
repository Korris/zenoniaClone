using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>How one real hit of a skill is SHOWN: N damage numbers popping IntervalMs apart (RO-style multi-hit, user 21/11 "dam nhảy lên nhiều lần
    /// cho phê"). FinisherHits is used instead of Hits for a hit flagged Finisher (the closing blow of a chain).</summary>
    public struct HitSplit
    {
        public int Hits, IntervalMs, StartDelayMs, FinisherHits;
        public HitSplit(int hits, int intervalMs, int startDelayMs = 0, int finisherHits = 0)
        {
            Hits = hits; IntervalMs = intervalMs; StartDelayMs = startDelayMs; FinisherHits = finisherHits > 0 ? finisherHits : hits;
        }

        /// <summary>Parts for this hit: FinisherHits for a chain's finisher, else Hits.</summary>
        public int PartsFor(bool finisher) { return finisher ? FinisherHits : Hits; }
    }

    /// <summary>
    /// Client presentation table of the multi-hit damage numbers. PURELY cosmetic: the server, the damage rolls and the totals never change; a hit of
    /// D damage is drawn as N numbers whose sum is exactly D (Split). Skill FX can read For(skillId) to sync impact pulses with the numbers.
    /// Defaults by kind: one strong hit 3-5, a skill that already lands several real hits close together 1-3 (its real hits already make the
    /// count), ultimates 6-8, basic attack 1 (combo finisher 2). Engine-free (dotnet tests: SkillHitSplitTests).
    /// </summary>
    public static class SkillHitSplit
    {
        public const int MinHits = 1, MaxHits = 8, MinIntervalMs = 60, MaxIntervalMs = 120;

        /// <summary>Basic attacks: 1 number, the combo finisher 2.</summary>
        public static readonly HitSplit Basic = new HitSplit(1, 70, 0, 2);

        /// <summary>A damaging skill missing from the table.</summary>
        public static readonly HitSplit DefaultSkill = new HitSplit(3, 80);

        /// <summary>No split (damage over time ticks, quiet hits).</summary>
        public static readonly HitSplit None = new HitSplit(1, 70);

        static readonly Dictionary<string, HitSplit> Table = new Dictionary<string, HitSplit>
        {
            // Warrior. Power Slash / Earth Break / Blade Cyclone / Inferno Crash already land 8-18 real hits 35-100 ms apart: 1 number each, the finisher splits.
            { "warrior.power-slash", new HitSplit(1, 60, 0, 3) },
            { "warrior.whirlwind", new HitSplit(3, 70) },          // 3 real hits 250 ms apart -> 9 numbers
            { "warrior.earth-break", new HitSplit(1, 60, 0, 4) },
            { "warrior.flame-cleave", new HitSplit(4, 80) },
            { "warrior.charging-strike", new HitSplit(4, 75) },
            { "warrior.blade-cyclone", new HitSplit(1, 60, 0, 5) },
            { "warrior.inferno-crash", new HitSplit(1, 60, 0, 6) },
            // Wizard
            { "wizard.arcane-bolt", new HitSplit(3, 80) },
            { "wizard.fireball", new HitSplit(4, 80) },
            { "wizard.ice-storm", new HitSplit(3, 90) },           // 4 waves 400 ms apart
            { "wizard.arcane-lance", new HitSplit(4, 70) },
            { "wizard.frost-nova", new HitSplit(4, 80) },
            { "wizard.elemental-burst", new HitSplit(5, 80) },
            { "wizard.astral-cataclysm", new HitSplit(3, 60) },    // 8 drops 200 ms apart -> 24 numbers
            // Mechanic
            { "mechanic.power-shot", new HitSplit(3, 80) },
            { "mechanic.plasma-barrage", new HitSplit(2, 70) },    // 6 bolts at once
            { "mechanic.rocket-barrage", new HitSplit(3, 70) },    // 10 rockets
            { "mechanic.rocket-punch", new HitSplit(4, 80) },
            { "mechanic.deploy-turret", new HitSplit(2, 60) },     // a shot every interval for the turret's life
            { "mechanic.emp-burst", new HitSplit(5, 80) },
            { "mechanic.orbital-cannon", new HitSplit(4, 60) },    // 6 shells 250 ms apart
            // Cleric
            { "cleric.divine-light", new HitSplit(4, 80) },
            { "cleric.purification", new HitSplit(5, 80) },
            { "cleric.divine-ascension", new HitSplit(7, 75) },
            // Assassin
            { "assassin.shadow-strike", new HitSplit(2, 60) },     // 2 real hits 120 ms apart
            { "assassin.shadow-step", new HitSplit(4, 70) },
            { "assassin.blade-dance", new HitSplit(2, 65) },       // 4 real hits 150 ms apart
            { "assassin.phantom-rush", new HitSplit(3, 60) },      // 3 dashes 180 ms apart
            { "assassin.venom-daggers", new HitSplit(3, 80) },
            { "assassin.shadow-clone", new HitSplit(2, 70) },      // the clone strikes every 600 ms
            { "assassin.nightfall-execution", new HitSplit(2, 60) },// 8 real hits 120 ms apart -> 16 numbers
            // Fighter
            { "fighter.dash-punch", new HitSplit(3, 75) },
            { "fighter.ground-slam", new HitSplit(4, 80) },
            { "fighter.rapid-jabs", new HitSplit(1, 60) },         // 5 real jabs 90 ms apart already
            { "fighter.shockwave", new HitSplit(4, 75) },
            { "fighter.tiger-rush", new HitSplit(3, 60) },         // 3 hooks 160 ms apart
            { "fighter.dragon-fist", new HitSplit(7, 75) },
            // Archer
            { "archer.piercing-arrow", new HitSplit(3, 70) },
            { "archer.multi-shot", new HitSplit(2, 70) },          // 5 arrows at once
            { "archer.arrow-rain", new HitSplit(2, 60) },          // 8 arrows 120 ms apart
            { "archer.binding-shot", new HitSplit(4, 80) },
            { "archer.starfall-volley", new HitSplit(3, 60) },     // 10 arrows 150 ms apart
        };

        /// <summary>The split of a skill (DefaultSkill when the id is unknown, Basic for null).</summary>
        public static HitSplit For(string skillId)
        {
            if (skillId == null) return Basic;
            HitSplit s;
            return Table.TryGetValue(skillId, out s) ? s : DefaultSkill;
        }

        /// <summary>True when the table has an explicit row for the id.</summary>
        public static bool Has(string skillId) { return skillId != null && Table.ContainsKey(skillId); }

        /// <summary>True when the skill deals damage (a Strike / Projectile / Rain / Summon / Turret with Pct, or a legacy timeline).</summary>
        public static bool Damages(SkillDef def)
        {
            if (def == null) return false;
            if (def.Legacy != LegacySkill.None) return true;
            foreach (SkillEffect e in def.Effects)
            {
                if (e.Pct <= 0f || e.Ally) continue;
                if (e.Kind == EffectKind.Strike || e.Kind == EffectKind.Projectile || e.Kind == EffectKind.Rain || e.Kind == EffectKind.Summon || e.Kind == EffectKind.Turret || e.Kind == EffectKind.Zone) return true;
            }
            return false;
        }

        /// <summary>How long after the cast the skill's hits can still arrive (ms, before network / travel slack): last timeline step, zone / summon /
        /// turret lifetime, or the hand-made legacy timelines.</summary>
        public static int ActiveMs(SkillDef def)
        {
            if (def == null) return 0;
            switch (def.Legacy)
            {
                case LegacySkill.EarthBreak: return (int)LegacySkillNumbers.EarthBreakTotalMs;
                case LegacySkill.BladeCyclone: return 2750;
                case LegacySkill.IceStorm: return (int)(LegacySkillNumbers.IceStormCastMs + LegacySkillNumbers.IceStormWaves * LegacySkillNumbers.IceStormWaveMs + LegacySkillNumbers.IceStormTailMs);
                case LegacySkill.RocketBarrage: return 1800;
            }
            int end = SkillSchedule.LastStepMs(SkillSchedule.Build(def.Effects));
            foreach (SkillEffect e in def.Effects)
                if (e.Kind == EffectKind.Zone || e.Kind == EffectKind.Summon || e.Kind == EffectKind.Turret) end = Math.Max(end, e.AtMs + e.DurationMs);
            return end;
        }

        /// <summary>Crowd LOD: fewer parts when many of your numbers are already on screen (an AoE on a crowd), 1 when the pool is crowded.</summary>
        public static int CrowdParts(int parts, int liveOwnNumbers)
        {
            if (liveOwnNumbers > 150) return 1;
            if (liveOwnNumbers > 90) return Math.Min(parts, 2);
            if (liveOwnNumbers > 50) return Math.Min(parts, 4);
            return parts;
        }

        /// <summary>
        /// Splits `total` into `parts` numbers that add up to exactly `total`: each part varies by up to +-10 % (deterministic from `seed`), every
        /// part is at least 1, and the rounding remainder goes to the last part. Fewer parts when total is smaller than parts.
        /// </summary>
        public static int[] Split(int total, int parts, int seed)
        {
            parts = Math.Max(MinHits, Math.Min(MaxHits, parts));
            if (total <= 0) return new[] { total };
            if (total < parts) parts = total;
            if (parts == 1) return new[] { total };
            var w = new double[parts];
            double sum = 0;
            uint h = (uint)seed * 2654435761u + 0x9E3779B9u;
            for (int i = 0; i < parts; i++)
            {
                h ^= h << 13; h ^= h >> 17; h ^= h << 5; // xorshift32
                w[i] = 0.9 + 0.2 * (h % 10001u) / 10000.0;
                sum += w[i];
            }
            var r = new int[parts];
            int given = 0;
            for (int i = 0; i < parts - 1; i++)
            {
                r[i] = Math.Max(1, (int)Math.Floor(total * w[i] / sum));
                given += r[i];
            }
            r[parts - 1] = total - given;
            for (int i = parts - 2; r[parts - 1] < 1 && i >= 0; i--) // tiny totals: borrow back so the last part stays >= 1
                while (r[i] > 1 && r[parts - 1] < 1) { r[i]--; r[parts - 1]++; }
            return r;
        }
    }
}
