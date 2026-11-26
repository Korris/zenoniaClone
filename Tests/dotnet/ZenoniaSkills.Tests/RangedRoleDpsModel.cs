using System;
using System.Collections.Generic;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>
/// Steady-state damage-per-second estimate of a class (Archer vs Mechanic role split, 2026-11-26). Reads the live tables (ComboConfigs, SkillTable,
/// RpgClasses / Leveling, LegacySkillNumbers, TurretConfig), so a retune shows up here. Not the game loop: an expected-value model.
///
/// Assumptions (same for every class):
/// - Hero at the origin facing +x; targets are circles of BodyTiles radius (a monster box). One target at (D, 0), or a pack of 5: that one plus
///   four PackGap tiles around it (front / back / both sides). Skill areas (rain, centred strikes) are aimed at the target / pack centre; AroundSelf
///   areas count from the hero; bolts and skill projectiles fly at the nearest target (auto-aim).
/// - Damage per hit = atk x mult x (1 + 0.6 crit) - def / 2 (CombatMath.Damage: crit 1.6x, base crit 8 % + class bonus).
/// - Skills at rank 1, no cooldowns (SkillTable.CooldownScale 0): MP is the limit. MP per second = regen 2.2 + the pool spread over FightSec.
///   Buffs (ATK %, attack speed %, Mark) are kept up or not, whichever is better; same-kind buffs do not stack (BuffState keeps the strongest).
///   The rest of the MP goes greedily to the damage skill with the best (damage - basic attack lost during its cast lock) per MP.
/// </summary>
public static class RangedRoleDpsModel
{
    public const float D = 3f, PackGap = 1.2f, BodyTiles = 0.5f, FightSec = 60f, MonsterDef = 20f, GearAtk = 40f, MpRegen = 2.2f, BaseCrit = 0.08f;
    const float U = 16f; // world units per tile

    public struct Result { public float Basic, Skills, Total; public string Plan; }

    public static float[][] Targets(int n)
    {
        var t = new List<float[]> { new[] { D, 0f } };
        if (n > 1) { t.Add(new[] { D - PackGap, 0f }); t.Add(new[] { D + PackGap, 0f }); t.Add(new[] { D, PackGap }); t.Add(new[] { D, -PackGap }); }
        return t.ToArray();
    }

    static float Dist(float ax, float ay, float bx, float by) { float dx = ax - bx, dy = ay - by; return (float)Math.Sqrt(dx * dx + dy * dy); }

    /// <summary>Nearest target to the hero (where a bolt / auto-aimed projectile lands first).</summary>
    static float[] Nearest(float[][] t) { float[] best = t[0]; foreach (float[] p in t) if (p[0] * p[0] + p[1] * p[1] < best[0] * best[0] + best[1] * best[1]) best = p; return best; }

    static int Within(float[][] t, float cx, float cy, float r) { int n = 0; foreach (float[] p in t) if (Dist(p[0], p[1], cx, cy) <= r) n++; return n; }

    /// <summary>Basic attack: (sum of mult x targets) per second of one full chain, finisher scaled by level. Returns hits per second through `hitsPerSec`.</summary>
    static float BasicMultPerSec(ComboHit[] hits, int level, float[][] t, out float hitsPerSec)
    {
        float multSum = 0f, ms = 0f, n = 0f;
        float[] first = Nearest(t);
        for (int i = 0; i < hits.Length; i++)
        {
            ComboHit h = hits[i];
            float mult = i == hits.Length - 1 ? h.Mult + ComboSystem.FinisherMultPerLevel * (level - 1) : h.Mult;
            int targets = h.Bolt != null && h.Bolt.ExplodeRadius > 0f ? Within(t, first[0], first[1], h.Bolt.ExplodeRadius / U) : 1; // ComboProjectile.Explode: centre distance
            multSum += mult * targets; n += targets; ms += h.TotalMs;
        }
        hitsPerSec = n / (ms / 1000f);
        return multSum / (ms / 1000f);
    }

    /// <summary>One cast of a skill: sum of pct (as a multiplier) over every target hit, and the number of hits (each loses def / 2).</summary>
    static void SkillHits(SkillDef s, float[][] t, out float multSum, out float hitCount)
    {
        multSum = 0f; hitCount = 0f;
        if (s.Legacy == LegacySkill.RocketBarrage)
        {
            for (int i = 0; i < LegacySkillNumbers.RocketBarrageRockets; i++) // RocketBarrage.cs: fixed fan in the facing, radius on the monster box
            {
                float a = -(i / (LegacySkillNumbers.RocketBarrageRockets - 1f) - 0.5f) * LegacySkillNumbers.RocketBarrageFanDeg * (float)Math.PI / 180f;
                float dist = LegacySkillNumbers.RocketBarrageRange * (LegacySkillNumbers.RocketBarrageNear + (i % 3) * LegacySkillNumbers.RocketBarrageStep) / U;
                int n = Within(t, dist * (float)Math.Cos(a), dist * (float)Math.Sin(a), LegacySkillNumbers.RocketBarrageRadius / U + BodyTiles);
                multSum += n * LegacySkillNumbers.RocketBarrageMult; hitCount += n;
            }
            return;
        }
        float[] first = Nearest(t);
        float aim = (float)Math.Atan2(first[1], first[0]);
        foreach (SkillEffect e in s.Effects)
        {
            float pct = e.Pct / 100f;
            if (e.Kind == EffectKind.Projectile && pct > 0f)
            {
                float hw = Math.Max(0.25f, e.Width * 0.5f) + BodyTiles;
                for (int k = 0; k < e.Count; k++)
                {
                    float a = aim + (e.Count == 1 ? 0f : (-e.SpreadDeg / 2f + k * e.SpreadDeg / (e.Count - 1))) * (float)Math.PI / 180f;
                    float cx = (float)Math.Cos(a), cy = (float)Math.Sin(a);
                    var on = new List<float[]>();
                    foreach (float[] p in t)
                    {
                        float along = p[0] * cx + p[1] * cy, side = Math.Abs(-p[0] * cy + p[1] * cx);
                        if (along > 0f && along <= e.Range + BodyTiles && side <= hw) on.Add(new[] { along, p[0], p[1] });
                    }
                    if (on.Count == 0) continue;
                    on.Sort((x, y) => x[0].CompareTo(y[0]));
                    int n = e.ImpactRadius > 0f ? Within(t, on[0][1], on[0][2], e.ImpactRadius + BodyTiles) : e.Pierce ? on.Count : 1;
                    multSum += n * pct; hitCount += n;
                }
            }
            else if (e.Kind == EffectKind.Strike && pct > 0f)
            {
                bool onHero = s.Targeting == SkillTargeting.AroundSelf || e.Range <= 0f; // else an aimed area: sits on the target / pack centre
                float cx = onHero ? 0f : t[0][0], cy = onHero ? 0f : t[0][1];
                int n = Within(t, cx, cy, e.Radius + BodyTiles) * Math.Max(1, e.Hits);
                multSum += n * pct; hitCount += n;
            }
            else if (e.Kind == EffectKind.Rain)
            {
                float cx = t[0][0], cy = t[0][1], hits = 0f; int samples = 0; // drops uniform in the scatter disc around the pack centre
                for (int gx = -20; gx <= 20; gx++)
                    for (int gy = -20; gy <= 20; gy++)
                    {
                        float px = gx / 20f * e.Radius, py = gy / 20f * e.Radius;
                        if (px * px + py * py > e.Radius * e.Radius) continue;
                        samples++; hits += Within(t, cx + px, cy + py, e.ImpactRadius + BodyTiles);
                    }
                float perDrop = hits / samples;
                multSum += perDrop * e.Count * pct; hitCount += perDrop * e.Count;
            }
            else if (e.Kind == EffectKind.Turret)
            {
                float shots = (TurretConfig.DurationMs - TurretConfig.FirstShotMs) / (float)TurretConfig.FireIntervalMs + 1f; // one target per shot
                multSum += shots * pct; hitCount += shots;
            }
        }
    }

    static float Buff(SkillDef s, BuffKind k) { float v = 0f; foreach (SkillEffect e in s.Effects) if (e.Kind == EffectKind.Buff && e.Buff.Kind == k) v = Math.Max(v, e.Buff.Value); return v; }
    static float MarkPct(SkillDef s) { foreach (SkillEffect e in s.Effects) if (e.Status.Kind == StatusKind.Mark) return e.Status.Value; return 0f; }
    static int BuffMs(SkillDef s) { foreach (SkillEffect e in s.Effects) { if (e.Kind == EffectKind.Buff) return e.Buff.Ms; if (e.Status.Kind == StatusKind.Mark) return e.Status.Ms; } return 0; }

    public static Result Estimate(string classId, ComboHit[] hits, int level, int targets)
    {
        ClassStatDef c = RpgClasses.Get(classId);
        BaseStats st = Leveling.StatsForLevel(level, classId);
        float[][] t = Targets(targets);
        float crit = 1f + 0.6f * (BaseCrit + c.CritBonus), mpRate = MpRegen + st.MaxMp / FightSec;
        var buffs = new List<SkillDef>(); var dmg = new List<SkillDef>();
        foreach (SkillDef s in SkillTable.ForClass(classId))
        {
            if (s.UnlockLevel > level) continue;
            if (Buff(s, BuffKind.AtkPct) > 0f || Buff(s, BuffKind.AtkSpeedPct) > 0f || MarkPct(s) > 0f) buffs.Add(s); else dmg.Add(s);
        }
        Result best = new Result { Total = -1f };
        for (int mask = 0; mask < 1 << buffs.Count; mask++)
        {
            float atkPct = 0f, spdPct = 0f, mark = 0f, mpLeft = mpRate, timeLeft = 1f; string plan = "";
            for (int b = 0; b < buffs.Count; b++)
            {
                if ((mask & 1 << b) == 0) continue;
                SkillDef s = buffs[b]; float per = 1000f / BuffMs(s);
                atkPct = Math.Max(atkPct, Buff(s, BuffKind.AtkPct)); spdPct = Math.Max(spdPct, Buff(s, BuffKind.AtkSpeedPct)); mark = Math.Max(mark, MarkPct(s));
                mpLeft -= s.Mp * per; timeLeft -= s.CastLockMs / 1000f * per; plan += s.Name + " up; ";
            }
            if (mpLeft < 0f) continue;
            float atk = (st.Atk + GearAtk) * (1f + atkPct / 100f), taken = 1f + mark / 100f, hps;
            float basicMult = BasicMultPerSec(hits, level, t, out hps) * (1f + spdPct / 100f);
            hps *= 1f + spdPct / 100f;
            float basicDps = (atk * crit * basicMult - MonsterDef / 2f * hps) * taken;
            // damage skills: value per MP of a cast = its damage minus the basic attack lost while the cast holds the hero
            var cand = new List<float[]>(); // value, mp, lockSec, maxPerSec, damage, index
            for (int i = 0; i < dmg.Count; i++)
            {
                SkillDef s = dmg[i]; float m, n; SkillHits(s, t, out m, out n);
                if (m <= 0f) continue;
                float d = (atk * crit * m - MonsterDef / 2f * n) * taken, lockSec = s.CastLockMs / 1000f;
                bool turret = Array.Exists(s.Effects, e => e.Kind == EffectKind.Turret);
                cand.Add(new[] { d - basicDps * lockSec, s.Mp, lockSec, turret ? 1000f / TurretConfig.DurationMs : 99f, d, i });
            }
            cand.Sort((x, y) => (y[0] / y[1]).CompareTo(x[0] / x[1]));
            float skillDps = 0f;
            foreach (float[] k in cand)
            {
                if (k[0] <= 0f || mpLeft <= 0f || timeLeft <= 0f) break;
                float rate = Math.Min(k[3], Math.Min(mpLeft / k[1], timeLeft / k[2]));
                mpLeft -= rate * k[1]; timeLeft -= rate * k[2]; skillDps += rate * k[4];
                plan += dmg[(int)k[5]].Name + " x" + (rate * 60f).ToString("0.#") + "/min; ";
            }
            float total = basicDps * timeLeft + skillDps;
            if (total > best.Total) best = new Result { Basic = basicDps * timeLeft, Skills = skillDps, Total = total, Plan = plan };
        }
        return best;
    }

    /// <summary>Per skill: % ATK landed per cast (summed over the targets) and per MP, for the log.</summary>
    public static string Breakdown(string classId, int level, int targets)
    {
        float[][] t = Targets(targets); string o = "";
        foreach (SkillDef s in SkillTable.ForClass(classId))
        {
            if (s.UnlockLevel > level) continue;
            float m, n; SkillHits(s, t, out m, out n);
            if (m > 0f) o += s.Name + " " + (m * 100f).ToString("0") + "% (" + (m * 100f / s.Mp).ToString("0.0") + "/MP, " + n.ToString("0.#") + " hits); ";
        }
        return o;
    }
}
