using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Keys 1-4 cast the four Warrior skills (WarriorSkillDefs3D). Checks cooldown and MP (HeroStats3D), faces the current or nearest slime,
/// then runs the skill's timeline (WarriorSkillMoves3D.cs) while HeroController3D stands aside (Casting). Damage is dealt at the timeline's
/// hit marks, never waiting for an effect to finish. Capture aid: `--skills 3.5:1,6:2` casts skill 2 at 6 s (unscaled, like --shots).
/// </summary>
public partial class WarriorSkills3D : MonoBehaviour
{
    public static WarriorSkills3D I;
    public Material SlashBodyMat, SlashCoreMat;   // ribbon materials (ProtoSkillLook.cs)
    public AudioClip Swing, Hit, Crit;
    public float Atk = 1000f;

    public bool Casting { get { return Time.time < lockUntil; } }
    public readonly float[] ReadyAt = new float[4], PressedAt = { -9f, -9f, -9f, -9f }, DeniedAt = { -9f, -9f, -9f, -9f };

    HeroController3D hero;
    AudioSource src;
    float lockUntil, lastHitSound = -1f;
    float[] autoTimes; int[] autoKeys; int autoNext;

    void Awake()
    {
        I = this;
        hero = GetComponent<HeroController3D>();
        src = gameObject.AddComponent<AudioSource>(); src.spatialBlend = 0f; src.volume = 0.8f;
        WarriorSkillDefs3D.LoadIcons();
        ParseAuto();
        BuildRibbons();
    }

    void ParseAuto()
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, "--skills");
        if (i < 0 || i + 1 >= a.Length) return;
        var parts = a[i + 1].Split(',');
        autoTimes = new float[parts.Length]; autoKeys = new int[parts.Length];
        for (int k = 0; k < parts.Length; k++)
        {
            var tk = parts[k].Split(':');
            autoTimes[k] = float.Parse(tk[0], CultureInfo.InvariantCulture); autoKeys[k] = int.Parse(tk[1]) - 1;
        }
    }

    void Update()
    {
        for (int k = 0; k < 4; k++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + k) || Input.GetKeyDown(KeyCode.Keypad1 + k)) TryCast(k);
        if (autoTimes != null && autoNext < autoTimes.Length && Time.unscaledTime >= autoTimes[autoNext]) TryCast(autoKeys[autoNext++]);
    }

    public float CooldownLeft(int k) { return Mathf.Max(0f, ReadyAt[k] - Time.time); }

    public bool TryCast(int k)
    {
        if (k < 0 || k >= 4 || hero == null || hero.Defeated || Casting) return false;
        var d = WarriorSkillDefs3D.All[k];
        if (CooldownLeft(k) > 0f || !HeroStats3D.Spend(d.Mp)) { DeniedAt[k] = Time.time; return false; }
        ReadyAt[k] = Time.time + d.Cooldown; PressedAt[k] = Time.time;
        lockUntil = Time.time + d.Lock;
        hero.SkillStarted();
        Vector3 fwd = FaceFoe(6f);
        if (Swing != null) src.PlayOneShot(Swing, 0.7f);
        switch (k)
        {
            case WarriorSkillDefs3D.PowerSlash: StartCoroutine(PowerSlashRun(fwd)); break;
            case WarriorSkillDefs3D.Whirlwind: StartCoroutine(WhirlwindRun(fwd)); break;
            case WarriorSkillDefs3D.ChargingStrike: StartCoroutine(ChargeRun(fwd)); break;
            default: StartCoroutine(InfernoRun(fwd)); break;
        }
        return true;
    }

    /// <summary>Turns to the current target (or the nearest live slime within `range`) and returns the flat facing direction.</summary>
    Vector3 FaceFoe(float range)
    {
        Slime3D foe = hero.Target != null && hero.Target.Alive ? hero.Target : null;
        if (foe == null)
        {
            float best = range;
            foreach (var s in Slime3D.All) { if (!s.Alive) continue; float d = Vector3.Distance(s.transform.position, transform.position); if (d < best) { best = d; foe = s; } }
        }
        if (foe != null)
        {
            Vector3 to = foe.transform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
        }
        Vector3 f = transform.forward; f.y = 0f;
        return f.normalized;
    }

    /// <summary>Upper-body slash clip (the legs keep whatever the base layer plays).</summary>
    void SlashAnim() { if (hero.Anim != null) hero.Anim.CrossFadeInFixedTime(HeroController3D.SlashState, 0.05f, 1, 0f); }

    /// <summary>Damage roll for `pct` % ATK: ±10 %, 20 % crit x1.6. Plays a hit sound at most every 70 ms.</summary>
    void Damage(Slime3D s, float pct)
    {
        bool crit = UnityEngine.Random.value < 0.2f;
        int dmg = Mathf.RoundToInt(Atk * pct / 100f * UnityEngine.Random.Range(0.9f, 1.1f) * (crit ? 1.6f : 1f));
        s.TakeHit(dmg, crit);
        if (Time.time - lastHitSound >= 0.07f) { lastHitSound = Time.time; var c = crit ? Crit : Hit; if (c != null) src.PlayOneShot(c, crit ? 0.9f : 0.7f); }
    }

    /// <summary>True when slime `s` stands inside the fan: within `radius` of `origin` and `halfDeg` of `fwd` (its body radius counts).</summary>
    static bool InFan(Slime3D s, Vector3 origin, Vector3 fwd, float radius, float halfDeg)
    {
        Vector3 to = s.transform.position - origin; to.y = 0f;
        float d = to.magnitude;
        if (d > radius + 0.3f) return false;
        return d < 0.4f || Vector3.Angle(fwd, to) <= halfDeg;
    }

    static bool InCircle(Slime3D s, Vector3 c, float radius)
    {
        Vector3 to = s.transform.position - c; to.y = 0f;
        return to.magnitude <= radius + 0.3f;
    }

    static Color T(string hex, float boost, float a) { return SkillFx3D.Tint(hex, boost, a); }

    /// <summary>Coloured impact on a slime: a star flash pulled to the camera, plus two little chips.</summary>
    static void HitFlash(Vector3 at, string main, string core, float size)
    {
        SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = SkillFx3D.Kind.Star, From = at, To = at, Size0 = size * 0.6f, Size1 = size, Roll0 = UnityEngine.Random.Range(0f, 90f), Roll1 = 20f, Color = T(core, 2.2f, 0.7f), Life = 0.14f, FadeOutAt = 0.4f, Pull = 0.6f });
        SkillFx3D.Spawn(new SkillFx3D.Spec { Kind = SkillFx3D.Kind.Glow, From = at, To = at, Size0 = size * 0.9f, Size1 = size * 1.3f, Color = T(main, 2f, 0.55f), Life = 0.18f, FadeOutAt = 0.2f, Pull = 0.55f });
    }

    static IEnumerator Until(float t0, float s) { while (Time.time - t0 < s) yield return null; }
}
