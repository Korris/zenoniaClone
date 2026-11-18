using UnityEngine;

/// <summary>
/// 3D top-down prototype hero: WASD / arrows move relative to the camera (Shift runs), J / Space / left mouse alternates the two sword
/// clips, H plays the hit reaction, K toggles the defeat pose. Clips are exported "in place" from Tripo, so the script moves the transform.
/// </summary>
public class HeroController3D : MonoBehaviour
{
    public string IdleState, WalkState, RunState, SlashState, ChopState, HitState, DefeatState;
    public float WalkSpeed = 2.4f, RunSpeed = 4.6f, TurnSpeed = 720f, AttackLock = 1.1f;
    public Vector2 BoundsMin, BoundsMax;
    public Transform Target;      // the boss: attacks in range pop damage numbers

    Animator anim;
    string current;
    float lockLeft;
    bool nextChop, defeated;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
        Play(IdleState, 0f);
    }

    void Play(string state, float fade = 0.12f)
    {
        if (string.IsNullOrEmpty(state) || state == current) return;
        anim.CrossFadeInFixedTime(state, fade);
        current = state;
    }

    /// <summary>Replays a one-shot clip from the start even when it is the current state.</summary>
    void Restart(string state)
    {
        if (string.IsNullOrEmpty(state)) return;
        anim.CrossFadeInFixedTime(state, 0.08f, 0, 0f);
        current = state;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) { defeated = !defeated; if (defeated) Restart(DefeatState); else Play(IdleState); }
        if (defeated) return;
        if (lockLeft > 0f) { lockLeft -= Time.deltaTime; return; }

        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            Restart(nextChop ? ChopState : SlashState);
            nextChop = !nextChop;
            lockLeft = AttackLock;
            FaceTargetIfClose();
            Invoke(nameof(Strike), 0.25f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.H)) { Restart(HitState); lockLeft = 0.5f; return; }

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude < 0.01f) { Play(IdleState); return; }
        Transform cam = Camera.main.transform;
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized, right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 dir = (fwd * input.z + right * input.x).normalized;
        bool run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Vector3 p = transform.position + dir * (run ? RunSpeed : WalkSpeed) * Time.deltaTime;
        transform.position = TileWalk.Move(transform.position, p - transform.position);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), TurnSpeed * Time.deltaTime);
        Play(run ? RunState : WalkState);
    }

    /// <summary>The sword lands: every live slime within 1.7 m in front takes a hit.</summary>
    void Strike()
    {
        foreach (var s in Slime3D.All.ToArray())
        {
            if (!s.Alive) continue;
            Vector3 to = s.transform.position - transform.position; to.y = 0f;
            if (to.magnitude < 1.7f && Vector3.Dot(transform.forward, to.normalized) > 0.2f) { bool crit = Random.value < 0.2f; s.TakeHit(Random.Range(700, 1300) * (crit ? 2 : 1), crit); }
        }
    }

    void FaceTargetIfClose()
    {
        Slime3D best = null; float bd = 2.5f;
        foreach (var s in Slime3D.All) { if (!s.Alive) continue; float d = Vector3.Distance(s.transform.position, transform.position); if (d < bd) { bd = d; best = s; } }
        if (best == null) return;
        Vector3 to = best.transform.position - transform.position; to.y = 0f;
        if (to.magnitude < 4f && to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
    }
}
