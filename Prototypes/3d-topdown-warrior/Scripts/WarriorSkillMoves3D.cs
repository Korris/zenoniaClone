using System.Collections;
using UnityEngine;
using K = SkillFx3D.Kind;
using F = SkillFx3D.Face;

/// <summary>
/// Timelines of the four Warrior skills (times in seconds from the key press, as ChatGPT specified: chatgpt-hud3d-spec.txt).
/// Effects use SkillFx3D pieces and ArcRibbon3D meshes; times run on Time.time so the basic attack's hit stop also freezes them.
/// </summary>
public partial class WarriorSkills3D
{
    ArcRibbon3D slashRibbon;
    readonly ArcRibbon3D[] windRibbons = new ArcRibbon3D[3];
    static readonly float[] WindRadius = { 1.25f, 1.65f, 2.0f }, WindHeight = { 0.25f, 0.65f, 1.05f };

    void BuildRibbons()
    {
        var ps = WarriorSkillDefs3D.All[WarriorSkillDefs3D.PowerSlash];
        slashRibbon = new ArcRibbon3D("PowerSlashArc", 120f, 0.23f, SlashBodyMat, SlashCoreMat, T(ps.Main, 1.9f, 0.62f), T(ps.Core, 2.2f, 0.5f));
        var ww = WarriorSkillDefs3D.All[WarriorSkillDefs3D.Whirlwind];
        for (int i = 0; i < 3; i++) windRibbons[i] = new ArcRibbon3D("WhirlBand" + i, 230f, 0.09f + 0.02f * i, SlashBodyMat, SlashCoreMat, T(ww.Main, 1.8f, 0.5f), T(ww.Core, 2.4f, 0.55f));
    }

    // ---------- 1 Power Slash: crescent 0.12-0.34, hit 0.22 (120° fan, R 2.5 m, 180 %), ice shards ----------
    IEnumerator PowerSlashRun(Vector3 fwd)
    {
        var d = WarriorSkillDefs3D.All[WarriorSkillDefs3D.PowerSlash];
        float t0 = Time.time; bool hit = false;
        SlashAnim();
        yield return Until(t0, 0.12f);
        slashRibbon.Show(true);
        while (Time.time - t0 < 0.36f)
        {
            float t = Time.time - t0;
            // nearly level plane (tilted 12°, starting on the right): ChatGPT wants the arc 0.3-0.8 m above the ground. A 35° tilt (like the
            // basic slash) put the 1.5 m arc's far end 1.4 m up, drawn above the slimes instead of through them.
            Vector3 side = (Vector3.Cross(Vector3.up, fwd) * -0.98f + Vector3.up * 0.21f).normalized;
            slashRibbon.Place(transform.position + Vector3.up * 0.5f - Camera.main.transform.forward * 0.6f, fwd, side, 1.5f);
            slashRibbon.Paint(Mathf.Clamp01((t - 0.12f) / 0.08f) * 1.15f, Mathf.Clamp01((t - 0.22f) / 0.12f) * 1.15f - 0.15f, 1f);
            if (!hit && t >= 0.22f)
            {
                hit = true;
                foreach (var s in Slime3D.All) if (s.Alive && InFan(s, transform.position, fwd, 2.5f, 60f)) { Damage(s, 180f); HitFlash(s.transform.position + Vector3.up * 0.3f, d.Main, d.Core, 0.7f); }
                for (int i = 0; i < 7; i++)   // ice shards 0.08-0.15 m thrown out along the arc
                {
                    Vector3 dir = Quaternion.AngleAxis(Random.Range(-60f, 60f), Vector3.up) * fwd;
                    SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Star, From = transform.position + Vector3.up * 0.6f + dir * 1.3f, Velocity = dir * Random.Range(1.5f, 2.6f) + Vector3.up * 1.2f, Gravity = 6f,
                        Size0 = Random.Range(0.12f, 0.2f), Size1 = 0.05f, Roll0 = 0f, Roll1 = 360f, Color = T(i % 2 == 0 ? d.Core : d.Main, 2f, 0.7f), Life = 0.4f, FadeOutAt = 0.5f, Pull = 0.4f });
                }
            }
            yield return null;
        }
        slashRibbon.Show(false);
    }

    // ---------- 2 Whirlwind: gather 0-0.15, spin, hits 0.25 / 0.50 / 0.75 (R 2.0 m, 65 %), fade 0.88-1.05 ----------
    IEnumerator WhirlwindRun(Vector3 fwd)
    {
        var d = WarriorSkillDefs3D.All[WarriorSkillDefs3D.Whirlwind];
        float t0 = Time.time; int hits = 0; float baseYaw = transform.eulerAngles.y;
        for (int i = 0; i < 12; i++)   // leaves and wind motes riding the bands
            SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = i % 3 == 0 ? K.Glow : K.Leaf, Orbit = true, From = transform.position + Vector3.up * Random.Range(0.15f, 0.9f), To = transform.position + Vector3.up * Random.Range(0.6f, 1.3f),
                OrbitRadius = Random.Range(1.0f, 2.0f), OrbitSpeed = -Random.Range(420f, 600f), OrbitAngle = i * 30f, Size0 = 0.16f, Size1 = 0.1f, Roll0 = 0f, Roll1 = 720f,
                Color = T(i % 3 == 0 ? d.Core : d.Main, 1.8f, 0.7f), FadeIn = 0.12f, FadeOutAt = 0.8f, Life = 1.0f, Delay = 0.05f });
        foreach (var r in windRibbons) r.Show(true);
        while (Time.time - t0 < 1.05f)
        {
            float t = Time.time - t0;
            float spin = Mathf.Clamp01((t - 0.15f) / 0.73f);
            transform.rotation = Quaternion.Euler(0f, baseYaw - spin * 1080f, 0f);   // three turns, counter-clockwise from above
            if (hits < 3 && t >= 0.25f * (hits + 1))
            {
                hits++; SlashAnim();
                foreach (var s in Slime3D.All) if (s.Alive && InCircle(s, transform.position, 2.0f)) { Damage(s, 65f); HitFlash(s.transform.position + Vector3.up * 0.3f, d.Main, d.Core, 0.55f); }
            }
            float alpha = Mathf.Clamp01(t / 0.15f) * (1f - Mathf.Clamp01((t - 0.88f) / 0.17f));
            for (int i = 0; i < 3; i++)
            {
                float yaw = -(t * 760f + i * 120f);
                Vector3 mid = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, side = Quaternion.Euler(0f, yaw + 90f, 0f) * Vector3.forward;
                side = (side + Vector3.up * (0.12f * (i - 1))).normalized;   // bands slightly tilted against each other
                windRibbons[i].Place(transform.position + Vector3.up * WindHeight[i], mid, side, WindRadius[i]);
                windRibbons[i].Paint(1.15f, -0.15f, alpha * (0.8f + 0.2f * i));
            }
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0f, baseYaw, 0f);
        foreach (var r in windRibbons) r.Show(false);
    }

    // ---------- 3 Charging Strike: crouch, dash 4 m in 0.10-0.33 (pierce, 220 % each), stop 0.38, after-images gone 0.55 ----------
    IEnumerator ChargeRun(Vector3 fwd)
    {
        var d = WarriorSkillDefs3D.All[WarriorSkillDefs3D.ChargingStrike];
        float t0 = Time.time;
        var struck = new System.Collections.Generic.HashSet<Slime3D>();   // once per cast, not per frame
        yield return Until(t0, 0.1f);
        hero.SkillPose(hero.RunState); SlashAnim();
        float roll = SkillFx3D.ScreenRoll(transform.position, fwd);
        // wedge streak 1.5 x 0.8 m behind the hero and the 1.2 m spear point ahead, both riding along
        SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Wedge, Follow = transform, From = Vector3.up * 0.5f - fwd * 0.75f, To = Vector3.up * 0.5f - fwd * 0.75f, Size0 = 1.5f, Size1 = 1.5f, Aspect = 0.53f, Roll0 = roll, Roll1 = roll, Color = T(d.Main, 2f, 0.65f), FadeIn = 0.15f, FadeOutAt = 0.75f, Life = 0.3f, Pull = 0.5f });
        SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Glow, Follow = transform, From = Vector3.up * 0.55f + fwd * 0.9f, To = Vector3.up * 0.55f + fwd * 0.9f, Size0 = 1.2f, Size1 = 1.2f, Aspect = 0.14f, Roll0 = roll, Roll1 = roll, Color = T(d.Core, 2.4f, 0.7f), FadeIn = 0.1f, FadeOutAt = 0.75f, Life = 0.28f, Pull = 0.6f });
        float dashed = 0f; int ghosts = 0;
        while (Time.time - t0 < 0.33f && dashed < 4f)
        {
            float step = Mathf.Min(4f - dashed, 4f / 0.23f * Time.deltaTime);
            Vector3 before = transform.position;
            transform.position = TileWalk.Move(before, fwd * step);
            float moved = Vector3.Distance(before, transform.position); dashed += moved;
            if (moved < step * 0.3f) break;   // wall or cliff: stop the dash
            float t = Time.time - t0;
            if (ghosts < 2 && t >= 0.17f + ghosts * 0.08f)   // two after-images left along the path, gone by 0.55 s
            {
                ghosts++;
                Vector3 at = transform.position + Vector3.up * 0.5f - fwd * 0.6f;
                SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Wedge, From = at, To = at, Size0 = 1.3f, Size1 = 1.1f, Aspect = 0.45f, Roll0 = roll, Roll1 = roll, Color = T(d.Rim, 2f, 0.45f), FadeOutAt = 0.1f, Life = 0.55f - t, Pull = 0.5f });
            }
            foreach (var s in Slime3D.All)
            {
                if (!s.Alive || struck.Contains(s)) continue;
                Vector3 to = s.transform.position - transform.position; to.y = 0f;
                if (to.magnitude < 0.9f && Mathf.Abs(Vector3.Dot(to, Vector3.Cross(Vector3.up, fwd))) < 0.7f)
                { struck.Add(s); Damage(s, 220f); HitFlash(s.transform.position + Vector3.up * 0.3f, d.Main, d.Core, 0.8f); }
            }
            yield return null;
        }
        hero.SkillPose(hero.IdleState);
    }

    // ---------- 4 Inferno Crash: crouch, leap 0.15-0.65 (peak 1.3 m), slam 0.65: R 2.8 m 300 % + R 0.8 m 50 %, fire ring, embers 1.35 ----------
    IEnumerator InfernoRun(Vector3 fwd)
    {
        var d = WarriorSkillDefs3D.All[WarriorSkillDefs3D.InfernoCrash];
        float t0 = Time.time;
        Vector3 start = transform.position, land = start + fwd * 3f;
        if (hero.Target != null && hero.Target.Alive) { Vector3 to = hero.Target.transform.position - start; to.y = 0f; land = start + Vector3.ClampMagnitude(to, 3.2f) * 0.85f; }
        if (!TileWalk.Walkable(land)) land = start;
        // landing mark while in the air
        SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Ring, Face = F.Ground, From = land + Vector3.up * 0.04f, To = land + Vector3.up * 0.04f, Size0 = 1.2f, Size1 = 2.2f, Color = T(d.Main, 1.6f, 0.45f), FadeIn = 0.3f, FadeOutAt = 0.8f, Life = 0.5f, Delay = 0.15f });
        yield return Until(t0, 0.15f);
        while (Time.time - t0 < 0.65f)
        {
            float k = Mathf.Clamp01((Time.time - t0 - 0.15f) / 0.5f);
            transform.position = Vector3.Lerp(start, land, k) + Vector3.up * (1.3f * 4f * k * (1f - k));
            if (k > 0.6f && k - Time.deltaTime / 0.5f <= 0.6f) SlashAnim();   // the downward swing starts just after the peak
            yield return null;
        }
        transform.position = land;
        foreach (var s in Slime3D.All)
            if (s.Alive && InCircle(s, land, 2.8f)) { Damage(s, InCircle(s, land, 0.8f) ? 350f : 300f); HitFlash(s.transform.position + Vector3.up * 0.3f, d.Main, d.Core, 0.8f); }
        HitFx.Impact(land + Vector3.up * 0.5f, true, hero.Anim);   // crit star, sound and hit stop on the slam
        Vector3 g = land + Vector3.up * 0.05f;
        // fire ring along the ground, Ø 5.6 m by 0.85 s, gone by 1.10 s; cream core pillar Ø 1.1 m x 1.5 m
        SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Ring, Face = F.Ground, From = g, To = g, Size0 = 1.2f, Size1 = 5.6f, Color = T(d.Main, 2.2f, 0.75f), FadeOutAt = 0.45f, Life = 0.45f, Delay = 0.03f });
        SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Glow, Face = F.Ground, From = g, To = g, Size0 = 2.5f, Size1 = 4.5f, Color = T(d.Rim, 1.6f, 0.5f), FadeOutAt = 0.3f, Life = 0.6f });
        SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Flame, Face = F.Upright, From = land + Vector3.up * 0.75f, To = land + Vector3.up * 0.85f, Size0 = 0.8f, Size1 = 1.1f, Aspect = 1.36f, Color = T(d.Core, 1.8f, 0.6f), FadeOutAt = 0.45f, Life = 0.45f, Pull = 0.6f });
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f + Random.Range(-12f, 12f), r = Random.Range(1.0f, 2.1f);
            Vector3 p = land + Quaternion.Euler(0f, a, 0f) * Vector3.forward * r;
            float h = Mathf.Lerp(1.2f, 0.5f, (r - 1f) / 1.1f);   // taller flames on the inner edge
            SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Flame, Face = F.Upright, From = p + Vector3.up * h * 0.45f, To = p + Vector3.up * h * 0.6f, Size0 = h * 0.4f, Size1 = h * 0.7f, Aspect = 1.4f, Color = T(i % 2 == 0 ? d.Main : d.Rim, 2.1f, 0.75f), FadeIn = 0.1f, FadeOutAt = 0.55f, Life = 0.45f, Delay = 0.03f + r * 0.05f, Pull = 0.3f });
        }
        for (int i = 0; i < 8; i++)   // rock chips
        {
            Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Rock, From = land + Vector3.up * 0.2f, Velocity = dir * Random.Range(1.5f, 3f) + Vector3.up * Random.Range(2.5f, 4f), Gravity = 12f, Size0 = Random.Range(0.12f, 0.2f), Size1 = 0.12f, Roll0 = 0f, Roll1 = 540f, Color = new Color(0.5f, 0.45f, 0.42f, 0.5f), FadeOutAt = 0.8f, Life = 0.6f, Pull = 0.3f });
        }
        for (int i = 0; i < 12; i++)   // embers drifting up until 1.35 s
        {
            Vector3 p = land + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(0.3f, 2.4f);
            SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = K.Glow, From = p + Vector3.up * 0.2f, Velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.6f, 1.2f), Random.Range(-0.2f, 0.2f)), Size0 = 0.1f, Size1 = 0.04f, Color = T(i % 3 == 0 ? d.Core : d.Main, 2.2f, 0.75f), FadeOutAt = 0.6f, Life = 0.7f, Delay = 0.05f });
        }
        hero.SkillPose(hero.IdleState);
    }
}
