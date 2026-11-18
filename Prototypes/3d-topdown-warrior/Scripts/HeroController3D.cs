using UnityEngine;

/// <summary>
/// 3D top-down hero with a Ragnarok-style basic attack: pick a slime (left click, or J / Space = nearest), the hero walks into reach, turns to it
/// and repeats one short slash every AttackInterval (0.75 s; the clip is 0.48 s, the hit lands at 0.22 s) until the target dies. Tab toggles Auto
/// battle (keeps picking the nearest slime within AutoRange). WASD / arrows move relative to the camera (Shift runs) and drop a manual target.
/// The slash lives on the animator's "Upper" layer, so the legs keep walking or idling. H plays the hit reaction, K toggles the defeat pose.
/// Feel pass (19/11): velocity eases in / out (Accel, Decel) instead of switching on and off, walk / run is one "Locomotion" blend tree whose
/// clip rates are matched to the ground speed at build time (no foot sliding), the hero may walk while the upper body finishes a swing, starts
/// the swing while still arriving, and slides round tile corners (TileWalk.Slide).
/// </summary>
public class HeroController3D : MonoBehaviour
{
    public string IdleState, WalkState, RunState, HitState, DefeatState;
    public const string SlashState = "Slash";
    public const string LocoState = "Locomotion", SpeedParam = "Speed", RateParam = "MoveRate";   // built by ProtoBuilder.AddLocomotion
    public float WalkSpeed = 1.2f, RunSpeed = 3.4f, TurnSpeed = 720f;   // 19/11: was 2.4 / 4.6 - the Tripo clips only cover 0.35 / 0.95 m/s, so the feet slid 5-7x
    public float Accel = 12f, Decel = 16f, Radius = 0.22f;   // 0 -> run in 0.28 s, run -> 0 in 0.21 s; body radius against blocked tiles
    /// <summary>Scripted stick input for unattended feel tests (ProtoCapture --path); null = keyboard.</summary>
    public static Vector3? ScriptMove; public static bool ScriptRun;
    public float Reach = 1.0f, AttackInterval = 0.75f, SwingTime = 0.48f, HitAt = 0.22f, AutoRange = 10f;
    public static bool AutoRequested;
    public bool Auto;
    public Slime3D Target;

    Animator anim;
    Transform ring;
    WarriorSkills3D skills;   // keys 1-4 (WarriorSkills3D): while a skill runs it owns movement and animation
    string current;
    public float ArcAt = 0.13f;   // crescent slash appears at 0.13 s of the swing and is gone by 0.35 s (SlashArcFx, ChatGPT timeline)
    float lockLeft, nextAttack, swingStart = -1f;
    bool defeated, hitDone, arcDone, hasLoco;
    Vector3 vel;            // smoothed ground velocity
    Slime3D swingTarget;    // the slime this swing was aimed at (walking away mid-swing drops Target, not the hit)
    public float Speed => vel.magnitude;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
        skills = GetComponent<WarriorSkills3D>();
        foreach (var tr in GetComponentsInChildren<TrailRenderer>()) tr.emitting = false;   // the thin tip trail is replaced by HitFx.SlashArc
        var ringGo = GameObject.Find("TargetRing"); if (ringGo != null) { ring = ringGo.transform; ringGo.SetActive(false); }
        var arrowGo = GameObject.Find("TargetArrow"); if (arrowGo != null) { arrow = arrowGo.transform; arrowGo.SetActive(false); }
        Auto = AutoRequested;
        hasLoco = anim.HasState(0, Animator.StringToHash(LocoState));
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
        if (Input.GetKeyDown(KeyCode.K)) { defeated = !defeated; Target = null; vel = Vector3.zero; if (defeated) Restart(DefeatState); else Play(IdleState); }
        if (Input.GetKeyDown(KeyCode.Tab)) Auto = !Auto;
        UpdateRing();
        if (defeated) return;
        if (skills != null && skills.Casting) { vel = Vector3.zero; return; }   // the skill owns position and pose
        if (lockLeft > 0f) { lockLeft -= Time.deltaTime; Locomote(Vector3.zero, null, false); return; }   // keep the hit clip playing
        if (Input.GetKeyDown(KeyCode.H)) { Restart(HitState); lockLeft = 0.5f; vel = Vector3.zero; return; }

        if (Input.GetMouseButtonDown(0)) { var s = SlimeUnderMouse(); if (s != null) Target = s; }
        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space)) { var s = Nearest(AutoRange); if (s != null) Target = s; }

        if (swingStart >= 0f) UpdateSwing();   // upper-body layer: the legs keep doing whatever Locomote decides

        Vector3 input = ScriptMove ?? new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 0.01f)
        {
            if (!Auto) Target = null;
            Transform cam = Camera.main.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized, right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            bool run = ScriptMove.HasValue ? ScriptRun : Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Locomote((fwd * input.z + right * input.x).normalized * (run ? RunSpeed : WalkSpeed), null);
            return;
        }

        if (Target == null || !Target.Alive) Target = Auto ? Nearest(AutoRange) : null;
        if (Target == null) { Locomote(Vector3.zero, null); return; }

        Vector3 to = Target.transform.position - transform.position; to.y = 0f;
        float d = to.magnitude;
        if (d > Reach)
        {
            // arrive: cap the speed so the hero brakes into reach instead of stopping dead on the spot
            float speed = Mathf.Min(d > 3f ? RunSpeed : WalkSpeed, Mathf.Sqrt(2f * Decel * (d - Reach)) + 0.3f);
            Locomote(to / d * speed, null);
        }
        else Locomote(Vector3.zero, to);
        // swing as soon as the target is in reach and roughly in front; the body keeps turning through the 0.22 s wind-up
        if (swingStart < 0f && d <= Reach + 0.15f && Time.time >= nextAttack && Vector3.Angle(transform.forward, to) < 50f) StartSwing();
    }

    /// <summary>Eases the velocity towards `desired`, moves (sliding along blocked tiles), turns to the motion or to `face`, and drives the
    /// legs: idle when (nearly) still, else the Locomotion blend tree with Speed = ground speed and the clip rate slowed while accelerating.</summary>
    void Locomote(Vector3 desired, Vector3? face, bool animate = true)
    {
        float rate = desired.sqrMagnitude > vel.sqrMagnitude ? Accel : Decel;
        vel = Vector3.MoveTowards(vel, desired, rate * Time.deltaTime);
        if (vel.sqrMagnitude > 1e-4f && Time.deltaTime > 0f)
        {
            Vector3 before = transform.position;
            transform.position = TileWalk.Slide(before, vel * Time.deltaTime, Radius);
            vel = Vector3.ClampMagnitude((transform.position - before) / Time.deltaTime, vel.magnitude);   // a wall eats the speed
        }
        Vector3 look = face ?? (desired.sqrMagnitude > 0.01f ? desired : vel);
        if (look.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), TurnSpeed * Time.deltaTime);

        if (!animate) return;
        float s = vel.magnitude;
        bool wasMoving = current == LocoState || current == WalkState || current == RunState;
        if (!(wasMoving ? s > 0.08f : s > 0.2f)) { Play(IdleState, 0.18f); return; }   // hysteresis: no idle / walk flicker
        if (!hasLoco) { Play(s > (WalkSpeed + RunSpeed) * 0.5f ? RunState : WalkState); return; }
        anim.SetFloat(SpeedParam, Mathf.Clamp(s, WalkSpeed, RunSpeed));
        anim.SetFloat(RateParam, s < WalkSpeed ? Mathf.Max(0.45f, s / WalkSpeed) : 1f);
        Play(LocoState, 0.15f);
    }

    // ---- hooks for WarriorSkills3D ----
    public Animator Anim { get { return anim; } }
    public bool Defeated { get { return defeated; } }
    /// <summary>A skill starts: drop the half-done basic swing and wait a moment before the next one.</summary>
    public void SkillStarted() { swingStart = -1f; nextAttack = Time.time + 0.25f; vel = Vector3.zero; }
    /// <summary>Base-layer state chosen by a skill (run during a dash, idle after); keeps `current` in sync so Play() works afterwards.</summary>
    public void SkillPose(string state) { Restart(state); }

    void StartSwing()
    {
        anim.CrossFadeInFixedTime(SlashState, 0.06f, 1, 0f);
        swingStart = Time.time; hitDone = false; arcDone = false; swingTarget = Target;
        nextAttack = Time.time + AttackInterval;
        HitFx.PlaySwing();
    }

    /// <summary>Runs the 0.48 s swing: crescent arc at ArcAt, damage at HitAt, then free again (the layer returns to Empty by itself).</summary>
    void UpdateSwing()
    {
        float t = Time.time - swingStart;
        if (!arcDone && t >= ArcAt) { arcDone = true; HitFx.SlashArc(transform); }
        if (!hitDone && t >= HitAt) { hitDone = true; Strike(); }
        if (t >= SwingTime) swingStart = -1f;
    }

    /// <summary>The sword lands on the target if it is still in front and within reach (+ a little slack for a slime that moved).</summary>
    void Strike()
    {
        var t = swingTarget;
        if (t == null || !t.Alive) return;
        Vector3 to = t.transform.position - transform.position; to.y = 0f;
        if (to.magnitude > Reach + 0.5f || Vector3.Dot(transform.forward, to.normalized) < 0.3f) return;
        bool crit = Random.value < 0.2f;
        t.TakeHit(Random.Range(700, 1300) * (crit ? 2 : 1), crit);
        HitFx.Impact(t.transform.position + Vector3.up * 0.3f - to.normalized * 0.15f, crit, anim, t);
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

    Transform arrow;

    /// <summary>v18: 1.2 m gold selection ring turning slowly under the target + a bobbing arrow 1.0-1.1 m above it, facing the camera.</summary>
    MaterialPropertyBlock ringBlock;

    void UpdateRing()
    {
        Slime3D.Selected = Target != null && Target.Alive ? Target : null;
        if (ring == null) return;
        bool show = Target != null && Target.Alive;
        if (ring.gameObject.activeSelf != show) { ring.gameObject.SetActive(show); if (arrow != null) arrow.gameObject.SetActive(show); }
        if (!show) return;
        ring.position = Target.transform.position + Vector3.up * 0.1f;
        // v19: two states - gold while just selected, warm red while fighting it (swing within the last 1.2 s)
        bool fighting = Time.time < nextAttack + 0.45f;
        if (ringBlock == null) ringBlock = new MaterialPropertyBlock();
        ringBlock.SetColor("_TintColor", fighting ? new Color(0.9f, 0.45f, 0.3f, 0.9f) : new Color(0.75f, 0.68f, 0.5f, 0.85f));
        ring.GetComponent<Renderer>().SetPropertyBlock(ringBlock);   // above the terrain bumps
        ring.rotation = Quaternion.Euler(90f, Time.time * 40f, 0f);
        if (arrow != null)
        {
            var cam = Camera.main.transform;
            arrow.position = Target.transform.position + Vector3.up * (1.0f + 0.08f * Mathf.Sin(Time.time * 5f));
            arrow.rotation = Quaternion.LookRotation(cam.forward, Vector3.up);
        }
    }
}
