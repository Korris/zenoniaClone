using UnityEngine;

/// <summary>
/// 3D top-down hero with a Ragnarok-style basic attack: pick a slime (left click, or J / Space = nearest), the hero walks into reach, turns to it
/// and repeats one short slash every AttackInterval (0.75 s; the clip is 0.48 s, the hit lands at 0.22 s) until the target dies. Tab toggles Auto
/// battle (keeps picking the nearest slime within AutoRange). WASD / arrows move relative to the camera (Shift runs) and drop a manual target.
/// The slash lives on the animator's "Upper" layer, so the legs keep the idle loop. H plays the hit reaction, K toggles the defeat pose.
/// </summary>
public class HeroController3D : MonoBehaviour
{
    public string IdleState, WalkState, RunState, HitState, DefeatState;
    public const string SlashState = "Slash";
    public float WalkSpeed = 2.4f, RunSpeed = 4.6f, TurnSpeed = 720f;
    public float Reach = 1.0f, AttackInterval = 0.75f, SwingTime = 0.48f, HitAt = 0.22f, TrailFrom = 0.08f, TrailTo = 0.30f, AutoRange = 10f;
    public static bool AutoRequested;
    public bool Auto;
    public Slime3D Target;

    Animator anim;
    TrailRenderer trail;
    Transform ring;
    string current;
    float lockLeft, nextAttack, swingStart = -1f;
    bool defeated, hitDone;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
        trail = GetComponentInChildren<TrailRenderer>();
        var ringGo = GameObject.Find("TargetRing"); if (ringGo != null) ring = ringGo.transform;
        Auto = AutoRequested;
        Play(IdleState, 0f);
    }

    void Play(string state, float fade = 0.12f)
    {
        if (string.IsNullOrEmpty(state) || state == current) return;
        anim.CrossFadeInFixedTime(state, fade);
        current = state;
    }

    /// <summary>Replays a one-shot base-layer clip from the start even when it is the current state.</summary>
    void Restart(string state)
    {
        if (string.IsNullOrEmpty(state)) return;
        anim.CrossFadeInFixedTime(state, 0.08f, 0, 0f);
        current = state;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) { defeated = !defeated; Target = null; if (defeated) Restart(DefeatState); else Play(IdleState); }
        if (Input.GetKeyDown(KeyCode.Tab)) Auto = !Auto;
        UpdateRing();
        if (defeated) return;
        if (lockLeft > 0f) { lockLeft -= Time.deltaTime; return; }
        if (Input.GetKeyDown(KeyCode.H)) { Restart(HitState); lockLeft = 0.5f; return; }

        if (Input.GetMouseButtonDown(0)) { var s = SlimeUnderMouse(); if (s != null) Target = s; }
        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space)) { var s = Nearest(AutoRange); if (s != null) Target = s; }

        if (swingStart >= 0f) { UpdateSwing(); return; }

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 0.01f)
        {
            if (!Auto) Target = null;
            Transform cam = Camera.main.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized, right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            Move((fwd * input.z + right * input.x).normalized, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            return;
        }

        if (Target == null || !Target.Alive) Target = Auto ? Nearest(AutoRange) : null;
        if (Target == null) { Play(IdleState); return; }

        Vector3 to = Target.transform.position - transform.position; to.y = 0f;
        float d = to.magnitude;
        if (d > Reach) { Move(to / d, d > 3f); return; }
        Play(IdleState);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), TurnSpeed * Time.deltaTime);
        if (Time.time >= nextAttack && Vector3.Angle(transform.forward, to) < 12f) StartSwing();
    }

    void Move(Vector3 dir, bool run)
    {
        Vector3 p = transform.position + dir * (run ? RunSpeed : WalkSpeed) * Time.deltaTime;
        transform.position = TileWalk.Move(transform.position, p - transform.position);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), TurnSpeed * Time.deltaTime);
        Play(run ? RunState : WalkState);
    }

    void StartSwing()
    {
        anim.CrossFadeInFixedTime(SlashState, 0.06f, 1, 0f);
        swingStart = Time.time; hitDone = false;
        nextAttack = Time.time + AttackInterval;
        HitFx.PlaySwing();
    }

    /// <summary>Runs the 0.48 s swing: trail while the blade moves fast, damage at HitAt, then free again (the layer returns to Empty by itself).</summary>
    void UpdateSwing()
    {
        float t = Time.time - swingStart;
        if (trail != null) trail.emitting = t >= TrailFrom && t <= TrailTo;
        if (!hitDone && t >= HitAt) { hitDone = true; Strike(); }
        if (t >= SwingTime) { swingStart = -1f; if (trail != null) trail.emitting = false; }
    }

    /// <summary>The sword lands on the target if it is still in front and within reach (+ a little slack for a slime that moved).</summary>
    void Strike()
    {
        if (Target == null || !Target.Alive) return;
        Vector3 to = Target.transform.position - transform.position; to.y = 0f;
        if (to.magnitude > Reach + 0.5f || Vector3.Dot(transform.forward, to.normalized) < 0.3f) return;
        bool crit = Random.value < 0.2f;
        Target.TakeHit(Random.Range(700, 1300) * (crit ? 2 : 1), crit);
        HitFx.Impact(Target.transform.position + Vector3.up * 0.3f - to.normalized * 0.15f, crit);
    }

    Slime3D Nearest(float within)
    {
        Slime3D best = null; float bd = within;
        foreach (var s in Slime3D.All) { if (!s.Alive) continue; float d = Vector3.Distance(s.transform.position, transform.position); if (d < bd) { bd = d; best = s; } }
        return best;
    }

    Slime3D SlimeUnderMouse()
    {
        Slime3D best = null; float bd = float.MaxValue;
        foreach (var h in Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), 200f))
        {
            var s = h.collider.GetComponentInParent<Slime3D>();
            if (s != null && s.Alive && h.distance < bd) { bd = h.distance; best = s; }
        }
        return best;
    }

    void UpdateRing()
    {
        if (ring == null) return;
        bool show = Target != null && Target.Alive;
        if (ring.gameObject.activeSelf != show) ring.gameObject.SetActive(show);
        if (show) ring.position = Target.transform.position + Vector3.up * 0.02f;
    }
}
