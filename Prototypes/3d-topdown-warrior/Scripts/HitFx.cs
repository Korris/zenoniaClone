using System.Collections;
using UnityEngine;

/// <summary>
/// Minimal hit feedback for the basic attack (ChatGPT preset): a 0.10 s additive spark at the contact point, a 30 ms hit stop (client only:
/// Time.timeScale), and the swing / hit / crit sounds from the main game. No camera shake for normal attacks.
/// </summary>
public class HitFx : MonoBehaviour
{
    public static HitFx I;
    public Material SparkMat;
    public AudioClip Swing, Hit, Crit;
    AudioSource src;
    float stopUntil = -1f;

    void Awake()
    {
        I = this;
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f; src.volume = 0.8f;
    }

    public static void PlaySwing() { if (I != null && I.Swing != null) I.src.PlayOneShot(I.Swing, 0.6f); }

    /// <summary>Spark + sound + hit stop at `at` (world). Crit: bigger spark, crit sound, longer stop.</summary>
    public static void Impact(Vector3 at, bool crit)
    {
        if (I == null) return;
        I.StartCoroutine(I.SparkRoutine(at, crit ? 0.55f : 0.38f, crit ? 0.15f : 0.10f));
        var clip = crit ? I.Crit : I.Hit;
        if (clip != null) I.src.PlayOneShot(clip, crit ? 1f : 0.8f);
        I.StartCoroutine(I.HitStopRoutine(crit ? 0.05f : 0.03f));
    }

    IEnumerator SparkRoutine(Vector3 at, float size, float life)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        var r = q.GetComponent<Renderer>(); r.sharedMaterial = SparkMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        float roll = Random.Range(0f, 360f), t0 = Time.unscaledTime;
        var cam = Camera.main.transform;
        while (Time.unscaledTime - t0 < life)
        {
            float k = (Time.unscaledTime - t0) / life;
            q.transform.position = at;
            q.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up) * Quaternion.Euler(0f, 0f, roll);
            q.transform.localScale = Vector3.one * size * (0.6f + 0.7f * k);
            yield return null;
        }
        Destroy(q);
    }

    IEnumerator HitStopRoutine(float seconds)
    {
        float until = Time.unscaledTime + seconds;
        if (until <= stopUntil) yield break;
        stopUntil = until;
        Time.timeScale = 0f;
        while (Time.unscaledTime < stopUntil) yield return null;
        Time.timeScale = 1f;
    }
}
