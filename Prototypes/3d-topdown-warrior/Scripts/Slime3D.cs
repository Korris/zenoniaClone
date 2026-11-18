using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Field monster (Quaternius GreenBlob, CC0, tinted ice blue): wanders around its spawn, chases the hero inside 6 m, winds up and hits at 1.1 m,
/// gives up beyond 10 m from home, dies after its HP runs out and comes back 6 s later. Clip names are matched by keyword at build time.
/// </summary>
public class Slime3D : MonoBehaviour
{
    public static readonly List<Slime3D> All = new List<Slime3D>();
    public string IdleState, MoveState, AttackState, HitState, DeathState;
    public Transform Hero;
    public int MaxHp = 4200;
    public float Speed = 1.7f, Aggro = 6f, Leash = 10f, Reach = 1.1f;

    Animator anim;
    Vector3 home, wanderGoal;
    string current;
    int hp;
    float timer, deadUntil;
    enum S { Idle, Wander, Chase, Windup, Recover, Dead }
    S state;

    public bool Alive { get { return state != S.Dead; } }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
        home = transform.position;
        hp = MaxHp;
        Enter(S.Idle, Random.Range(0.5f, 2.5f));
    }

    void Play(string s, bool restart = false)
    {
        if (string.IsNullOrEmpty(s) || (s == current && !restart)) return;
        if (restart) anim.CrossFadeInFixedTime(s, 0.08f, 0, 0f); else anim.CrossFadeInFixedTime(s, 0.15f);
        current = s;
    }

    void Enter(S s, float t) { state = s; timer = t; }

    public void TakeHit(int dmg, bool crit)
    {
        if (!Alive) return;
        hp -= dmg;
        Hud3D.Pop(transform.position + Vector3.up * 0.9f, dmg, crit);
        if (hp <= 0) { Play(DeathState, true); Enter(S.Dead, 0f); deadUntil = Time.time + 6f; Hud3D.Kills++; return; }
        Play(HitState, true);
        if (state != S.Windup) Enter(S.Chase, 0.25f);
    }

    void Update()
    {
        Vector3 toHero = Hero.position - transform.position; toHero.y = 0f;
        float dHero = toHero.magnitude, dHome = Vector3.Distance(transform.position, home);
        timer -= Time.deltaTime;
        switch (state)
        {
            case S.Dead:
                if (Time.time > deadUntil) { transform.position = home; hp = MaxHp; Play(IdleState, true); Enter(S.Idle, 1f); }
                return;
            case S.Idle:
                Play(IdleState);
                if (dHero < Aggro) { Enter(S.Chase, 0f); break; }
                if (timer <= 0f) { wanderGoal = home + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f)); Enter(S.Wander, 4f); }
                break;
            case S.Wander:
                if (dHero < Aggro) { Enter(S.Chase, 0f); break; }
                if (Step(wanderGoal - transform.position, Speed * 0.6f) < 0.2f || timer <= 0f) Enter(S.Idle, Random.Range(1.5f, 3.5f));
                break;
            case S.Chase:
                if (dHome > Leash || dHero > Aggro * 1.6f) { wanderGoal = home; Enter(S.Wander, 6f); break; }
                if (dHero <= Reach) { Face(toHero); Play(AttackState, true); Enter(S.Windup, 0.55f); break; }
                if (timer <= 0f) Step(toHero, Speed);
                break;
            case S.Windup:
                if (timer <= 0f) { if (dHero <= Reach + 0.4f) Hud3D.Pop(Hero.position + Vector3.up * 1.2f, Random.Range(40, 90), false, false, true); Enter(S.Recover, 0.6f); }
                break;
            case S.Recover:
                if (timer <= 0f) Enter(S.Chase, 0f);
                break;
        }
    }

    float Step(Vector3 to, float speed)
    {
        to.y = 0f;
        float d = to.magnitude;
        if (d < 0.05f) return d;
        Play(MoveState);
        Face(to);
        transform.position = TileWalk.Move(transform.position, to / d * Mathf.Min(d, speed * Time.deltaTime));
        return d;
    }

    void Face(Vector3 dir) { if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime); }
}
