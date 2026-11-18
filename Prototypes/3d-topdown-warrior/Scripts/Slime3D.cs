using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Field monster "Leafdrop Slime" (ChatGPT design, Tripo static model; docs/reference/3d-warrior-prototype/monster-3d-chatgpt/): wanders around
/// its spawn, chases the hero inside 6 m, winds up and hits at 1.1 m, gives up beyond 10 m from home, dies when its HP runs out and comes back
/// 6 s later. The model has no skeleton, so every motion is a volume-preserving squash of a ground-level pivot (ChatGPT spec):
/// idle = slow breathing, move = squash-and-hop, attack = pull back then butt forward, hurt = sideways wobble, death = melt into a puddle.
/// </summary>
public class Slime3D : MonoBehaviour
{
    public static readonly List<Slime3D> All = new List<Slime3D>();
    public static Slime3D Selected;   // v19: the hero's current target (set by HeroController3D) - drawn 7 % brighter
    public Transform Hero;
    public int MaxHp = 4200;
    public float Speed = 1.7f, Aggro = 6f, Leash = 10f, Reach = 1.1f;

    Transform pivot;                  // ground-level parent of the model: all squash / hop happens here
    Renderer[] renderers;
    Vector3 home, wanderGoal;
    int hp;
    float timer, deadUntil, hitAt = -10f, hopPhase, seed, flashUntil;
    MaterialPropertyBlock flashBlock;
    float shownLevel = 1f;
    bool moving;
    enum S { Idle, Wander, Chase, Windup, Recover, Dead }
    S state;

    public bool Alive { get { return state != S.Dead; } }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        // re-parent the model under a pivot at the root (feet level) so scaling squashes towards the ground
        var body = transform.GetChild(0);   // ProtoBuilder.Spawn puts the model first; the contact-shadow quad after it stays flat on the ground
        pivot = new GameObject("Squash").transform;
        pivot.SetParent(transform, false);
        body.SetParent(pivot, true);
        renderers = pivot.GetComponentsInChildren<Renderer>();
        seed = Random.Range(0f, 10f);
        home = transform.position;
        hp = MaxHp;
        Enter(S.Idle, Random.Range(0.5f, 2.5f));
    }

    void Enter(S s, float t) { state = s; timer = t; }

    /// <summary>0..1 remaining HP (target frame).</summary>
    public float Hp01 => Mathf.Clamp01((float)hp / MaxHp);

    public void TakeHit(int dmg, bool crit, bool byAlly = false)
    {
        if (!Alive) return;
        flashUntil = Time.time + 0.1f;   // v16: 0.1 s white hit flash (ChatGPT combat readability)
        hp -= dmg;
        Hud3D.Pop(transform.position + Vector3.up * 0.9f, dmg, crit, byAlly);   // ally hits pop dim (other players)
        hitAt = Time.time;
        // v19 (ChatGPT game feel): pushed 0.14 m away from the hero
        if (Hero != null) { Vector3 away = transform.position - Hero.position; away.y = 0f; if (away.sqrMagnitude > 0.001f) transform.position = TileWalk.Move(transform.position, away.normalized * 0.14f); }
        if (hp <= 0) { Enter(S.Dead, 0f); deadUntil = Time.time + 6f; Hud3D.Kills++; return; }
        if (state != S.Windup) Enter(S.Chase, 0.25f);
    }

    void Update()
    {
        Vector3 toHero = Hero.position - transform.position; toHero.y = 0f;
        float dHero = toHero.magnitude, dHome = Vector3.Distance(transform.position, home);
        timer -= Time.deltaTime;
        moving = false;
        switch (state)
        {
            case S.Dead:
                if (Time.time > deadUntil) { transform.position = home; hp = MaxHp; Show(true); Enter(S.Idle, 1f); }
                break;
            case S.Idle:
                if (dHero < Aggro) { Enter(S.Chase, 0f); break; }
                if (timer <= 0f) { wanderGoal = home + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f)); Enter(S.Wander, 4f); }
                break;
            case S.Wander:
                if (dHero < Aggro) { Enter(S.Chase, 0f); break; }
                if (Step(wanderGoal - transform.position, Speed * 0.6f) < 0.2f || timer <= 0f) Enter(S.Idle, Random.Range(1.5f, 3.5f));
                break;
            case S.Chase:
                if (dHome > Leash || dHero > Aggro * 1.6f) { wanderGoal = home; Enter(S.Wander, 6f); break; }
                if (dHero <= Reach) { Face(toHero); Enter(S.Windup, 0.55f); break; }
                if (timer <= 0f) Step(toHero, Speed);
                break;
            case S.Windup:
                if (timer <= 0f) { if (dHero <= Reach + 0.4f) Hud3D.Pop(Hero.position + Vector3.up * 1.2f, Random.Range(40, 90), false, false, true); Enter(S.Recover, 0.6f); }
                break;
            case S.Recover:
                if (timer <= 0f) Enter(S.Chase, 0f);
                break;
        }
        Animate();
        Flash();
    }

    /// <summary>Squash pose for the current state: y = height factor, xz keep the volume (1/sqrt y), plus hop height, lean and lunge.</summary>
    void Animate()
    {
        float t = Time.time + seed, y = 1f, hop = 0f, lunge = 0f, tilt = 0f;
        if (state == S.Dead)
        {
            float k = Mathf.Clamp01((Time.time - hitAt) / 0.4f);   // melt into a puddle in 0.4 s, then vanish
            y = Mathf.Lerp(1f, 0.12f, k);
            if (k >= 1f && renderers[0].enabled) Show(false);
        }
        else if (state == S.Windup)
        {
            float k = 1f - timer / 0.55f;                         // 0-0.35 pull back and squash, 0.35-0.55 butt forward and stretch
            if (k < 0.64f) { float a = k / 0.64f; y = 1f - 0.22f * a; lunge = -0.1f * a; }
            else { float a = (k - 0.64f) / 0.36f; y = 0.78f + 0.4f * Mathf.Sin(a * Mathf.PI); lunge = 0.3f * Mathf.Sin(a * Mathf.PI); }
        }
        else if (moving)
        {
            hopPhase += Time.deltaTime / 0.5f;                    // one hop every 0.5 s
            float p = Mathf.Repeat(hopPhase, 1f), air = Mathf.Sin(p * Mathf.PI);
            hop = 0.12f * air; y = p < 0.12f ? 0.82f : 0.9f + 0.18f * air;   // squashed on landing / take-off, stretched in the air
        }
        else { y = 1f + 0.04f * Mathf.Sin(t * Mathf.PI * 2f / 1.6f); hopPhase = 0f; }   // breathing, 1.6 s

        float since = Time.time - hitAt;
        if (state != S.Dead && since < 0.3f)                      // hurt: fast sideways wobble decaying over 0.3 s
        {
            float d = 1f - since / 0.3f;
            tilt = 14f * Mathf.Sin(since * 50f) * d; y *= 1f - 0.12f * d;
        }
        float xz = 1f / Mathf.Sqrt(Mathf.Max(0.05f, y));
        if (state == S.Dead) xz = Mathf.Lerp(1f, 1.5f, Mathf.Clamp01((Time.time - hitAt) / 0.4f));
        pivot.localScale = new Vector3(xz, y, xz);
        pivot.localPosition = new Vector3(0f, hop, lunge);
        pivot.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    void Show(bool on) { foreach (var r in renderers) r.enabled = on; }

    /// <summary>Hit flash: albedo tint x1.9 for 0.1 s through a property block (Standard multiplies by _Color).</summary>
    void Flash()
    {
        float level = Time.time < flashUntil ? 1.9f : Selected == this && Alive ? 1.07f : 1f;   // hit flash, else selected +7 %
        if (Mathf.Approximately(level, shownLevel)) return;
        shownLevel = level; if (flashBlock == null) flashBlock = new MaterialPropertyBlock();
        foreach (var r in renderers) { if (level > 1f) { flashBlock.SetColor("_Color", new Color(level, level, level, 1f)); r.SetPropertyBlock(flashBlock); } else r.SetPropertyBlock(null); }
    }

    float Step(Vector3 to, float speed)
    {
        to.y = 0f;
        float d = to.magnitude;
        if (d < 0.05f) return d;
        moving = true;
        Face(to);
        transform.position = TileWalk.Move(transform.position, to / d * Mathf.Min(d, speed * Time.deltaTime));
        return d;
    }

    void Face(Vector3 dir) { if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime); }
}
