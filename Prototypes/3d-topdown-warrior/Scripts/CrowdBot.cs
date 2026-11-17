using UnityEngine;

/// <summary>Another "player" in the raid: faces the boss and swings at random intervals, now and then steps back and returns; numbers pop dimmer than the hero's.</summary>
public class CrowdBot : MonoBehaviour
{
    public string IdleState, WalkState, SlashState, ChopState, HitState;
    public Transform Boss;
    Animator anim;
    float nextSwing;
    string current;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
        anim.speed = Random.Range(0.9f, 1.1f);
        anim.Play(IdleState, 0, Random.value);
        current = IdleState;
        var block = new MaterialPropertyBlock();   // light tint so the crowd is not 20 identical copies
        block.SetColor("_Color", Color.HSVToRGB(Random.value, 0.18f, 1f));
        foreach (var r in GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(block);
        nextSwing = Time.time + Random.Range(0.2f, 2.5f);
        Face();
    }

    void Face()
    {
        Vector3 to = Boss.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
    }

    void Update()
    {
        if (Time.time < nextSwing) return;
        float r = Random.value;
        string s = r < 0.45f ? SlashState : r < 0.9f ? ChopState : HitState;
        anim.CrossFadeInFixedTime(s, 0.1f, 0, 0f);
        current = s;
        nextSwing = Time.time + Random.Range(2.4f, 3.4f);
        if (s != HitState) Hud3D.Pop(Boss.position + Vector3.up * Random.Range(1.4f, 3.2f) + Random.insideUnitSphere * 0.8f, Random.Range(300, 900), false, true);
    }
}
