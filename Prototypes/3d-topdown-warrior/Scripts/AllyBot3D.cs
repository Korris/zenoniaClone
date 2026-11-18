using UnityEngine;

/// <summary>
/// A fake party member (ChatGPT v18: "thử 1 NPC / 1 đồng đội giả" to test the MMORPG feel): another Warrior, tinted, that stays within ~4 m
/// of the hero, walks to the nearest live slime within 7 m of the hero and slashes it every 1.1-1.5 s; its damage numbers pop dim like
/// other players' in RO. Uses the hero's animator controller (base idle / walk, "Slash" on the Upper layer).
/// </summary>
public class AllyBot3D : MonoBehaviour
{
    public Transform Hero;
    public string IdleState, WalkState;
    public float Speed = 2.3f, Reach = 1.0f;
    public bool Guard;                       // v19: a standing NPC (faces the clearing, never fights)
    public Color Tint = new Color(0.78f, 0.9f, 1.08f);
    Animator anim;
    string current;
    float nextSwing;
    Slime3D target;
    HeroController3D heroCtl;

    void Start()
    {
        anim = GetComponentInChildren<Animator>(); anim.applyRootMotion = false;
        var block = new MaterialPropertyBlock(); block.SetColor("_Color", Tint);   // tinted: not the player
        foreach (var r in GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(block);
        Play(IdleState);
    }

    void Play(string s) { if (!string.IsNullOrEmpty(s) && s != current) { anim.CrossFadeInFixedTime(s, 0.15f); current = s; } }

    void Update()
    {
        if (Hero == null) return;
        if (Guard) { Vector3 f = Hero.position - transform.position; f.y = 0f; if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(f), 90f * Time.deltaTime); return; }
        if (target == null || !target.Alive || Vector3.Distance(target.transform.position, Hero.position) > 7f) target = Pick();
        // stand on the far side of the target from the hero, so the two Warriors never stack on one spot (v18 test)
        Vector3 away = target != null ? target.transform.position - Hero.position : Vector3.zero; away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = Hero.right;
        Vector3 goal = target != null ? target.transform.position + away.normalized * Reach : Hero.position + Hero.right * 1.8f - Hero.forward * 0.6f;
        Vector3 to = goal - transform.position; to.y = 0f;
        float stop = 0.15f;
        if (to.magnitude > stop)
        {
            transform.position = TileWalk.Move(transform.position, to.normalized * Mathf.Min(to.magnitude - stop + 0.01f, Speed * Time.deltaTime));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 540f * Time.deltaTime);
            Play(WalkState);
            return;
        }
        Play(IdleState);
        if (target == null) return;
        Vector3 face = target.transform.position - transform.position; face.y = 0f;
        if (face.sqrMagnitude > 0.001f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 540f * Time.deltaTime);
        if (Time.time < nextSwing) return;
        nextSwing = Time.time + Random.Range(1.1f, 1.5f);
        anim.CrossFadeInFixedTime(HeroController3D.SlashState, 0.06f, 1, 0f);
        target.TakeHit(Random.Range(250, 600), false, true);
    }

    Slime3D Pick()
    {
        Slime3D best = null; float bd = 7f;
        if (heroCtl == null) heroCtl = Hero.GetComponent<HeroController3D>();
        foreach (var s in Slime3D.All) { if (!s.Alive || (heroCtl != null && s == heroCtl.Target)) continue; float d = Vector3.Distance(s.transform.position, Hero.position); if (d < bd) { bd = d; best = s; } }   // prefer a different slime than the hero
        return best;
    }
}
