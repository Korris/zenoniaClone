using System.Collections;
using UnityEngine;

/// <summary>
/// Basic-attack feedback (ChatGPT v6 numbers): the crescent arc (SlashArcFx), and on contact an ivory star flash Ø 0.30 m for 0.07 s
/// (crit 0.40 m / 0.10 s), 6 short rays shooting 0.12-0.25 m out in 0.15 s, 4 small glints flying 0.15-0.35 m in 0.2 s, a 35 ms hit stop
/// and the swing / hit / crit sounds from the main game. No camera shake for normal attacks. The hit stop freezes only the attacker's
/// animator and the struck slime (19/11; it used to be Time.timeScale = 0, which froze every slime, ally and the hero's walk at once).
/// </summary>
public class HitFx : MonoBehaviour
{
    public static HitFx I;
    public Material SparkMat, GroundRingMat;   // v18: ground ring = impact burst + ground mark
    public AudioClip Swing, Hit, Crit;
    AudioSource src;

    void Awake()
    {
        I = this;
        // Soft particles fade every Legacy particle quad within ~1 m of what is behind it (the AO pass gives the camera a depth texture):
        // the slash arc vanished against slimes and the contact shadows vanished on the ground. These effects need hard edges.
        QualitySettings.softParticles = false;
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f; src.volume = 0.8f;
    }

    public static void PlaySwing() { if (I != null && I.Swing != null) I.src.PlayOneShot(I.Swing, 0.6f); }

    /// <summary>Crescent arc of the hero's diagonal slash (see SlashArcFx).</summary>
    public static void SlashArc(Transform hero) { SlashArcFx.Play(hero); }

    /// <summary>Flash + rays + glints + sound + hit stop at `at` (world). Crit: bigger, longer flash, crit sound, longer stop.</summary>
    public static void Impact(Vector3 at, bool crit, Animator attacker = null, Slime3D victim = null)
    {
        if (I == null) return;
        at -= Camera.main.transform.forward * 0.4f;   // pull towards the camera so the slime body does not hide the star
        I.StartCoroutine(I.Flash(at, crit ? 0.4f : 0.3f, crit ? 0.1f : 0.07f));
        if (I.GroundRingMat != null) I.StartCoroutine(I.GroundRing(new Vector3(at.x, 0.1f, at.z), crit ? 1.4f : 1.0f, crit));   // v19: crit ring 1.4 m, brighter
        for (int i = 0; i < 8; i++) I.StartCoroutine(I.Fly(at, i * 45f + Random.Range(-20f, 20f), Random.Range(0.12f, 0.25f), 0.15f, true));
        for (int i = 0; i < 4; i++) I.StartCoroutine(I.Fly(at, Random.Range(0f, 360f), Random.Range(0.15f, 0.35f), 0.2f, false));
        var clip = crit ? I.Crit : I.Hit;
        if (clip != null) I.src.PlayOneShot(clip, crit ? 1f : 0.8f);
        float stop = crit ? 0.05f : 0.035f;
        if (attacker != null) I.StartCoroutine(I.FreezeAnimator(attacker, stop));
        if (victim != null) victim.Freeze(stop);
    }

    static GameObject FxQuad(Material m)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        return q;
    }

    IEnumerator Flash(Vector3 at, float size, float life)
    {
        var q = FxQuad(SparkMat);
        float roll = Random.Range(0f, 360f), t0 = Time.unscaledTime;
        var cam = Camera.main.transform;
        while (Time.unscaledTime - t0 < life)
        {
            float k = (Time.unscaledTime - t0) / life;
            q.transform.position = at;
            q.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up) * Quaternion.Euler(0f, 0f, roll);
            q.transform.localScale = Vector3.one * size * (0.7f + 0.5f * k);
            yield return null;
        }
        Destroy(q);
    }

    /// <summary>A particle flying `dist` out from the impact in the camera plane over `life` s (ease-out). Rays are thin streaks pointing
    /// along their flight; glints are small round chips. Both shrink to nothing.</summary>
    IEnumerator Fly(Vector3 at, float angle, float dist, float life, bool ray)
    {
        var q = FxQuad(SparkMat);
        var cam = Camera.main.transform;
        float t0 = Time.unscaledTime;
        Vector3 dir = Quaternion.AngleAxis(angle, cam.forward) * cam.up;
        while (Time.unscaledTime - t0 < life)
        {
            float k = (Time.unscaledTime - t0) / life;
            q.transform.position = at + dir * dist * (1f - (1f - k) * (1f - k));
            q.transform.rotation = Quaternion.LookRotation(cam.forward, ray ? dir : cam.up);
            q.transform.localScale = ray ? new Vector3(0.035f, 0.13f, 1f) * (1f - k) : Vector3.one * 0.07f * (1f - k);
            yield return null;
        }
        Destroy(q);
    }

    /// <summary>v18 (ChatGPT combat VFX): a warm ring bursting on the ground 0.3 -> diameter in 0.18 s, fading; doubles as the ground mark.</summary>
    IEnumerator GroundRing(Vector3 at, float diameter, bool crit = false)
    {
        var q = FxQuad(GroundRingMat);
        q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var r = q.GetComponent<Renderer>(); var mpb = new MaterialPropertyBlock();
        float t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < 0.22f)
        {
            float k = (Time.unscaledTime - t0) / 0.22f;
            q.transform.position = at;
            q.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, diameter, 1f - (1f - k) * (1f - k));
            mpb.SetColor("_TintColor", crit ? new Color(1f, 0.85f, 0.5f, 0.8f * (1f - k)) : new Color(0.75f, 0.68f, 0.55f, 0.51f * (1f - k)));   // v19: dust -15 %, crit bright gold r.SetPropertyBlock(mpb);
            yield return null;
        }
        Destroy(q);
    }

    /// <summary>Hit stop on one animator: speed 0 for `seconds` (real time), then back to 1. Overlapping stops just extend it.</summary>
    readonly System.Collections.Generic.Dictionary<Animator, float> frozen = new System.Collections.Generic.Dictionary<Animator, float>();
    void OnDisable() { foreach (var a in frozen.Keys) if (a != null) a.speed = 1f; frozen.Clear(); }   // never leave an animator stopped

    IEnumerator FreezeAnimator(Animator a, float seconds)
    {
        float until = Time.unscaledTime + seconds;
        if (frozen.TryGetValue(a, out var u) && u >= until) yield break;
        bool running = frozen.ContainsKey(a);
        frozen[a] = until;
        if (running) yield break;   // the first coroutine keeps waiting for the new end
        a.speed = 0f;
        while (a != null && Time.unscaledTime < frozen[a]) yield return null;
        frozen.Remove(a);
        if (a != null) a.speed = 1f;
    }
}
