using System.Collections;
using UnityEngine;

/// <summary>
/// Crescent Slash v6 (ChatGPT review of v5): a 120° arc mesh (16 segments, outer radius 0.62 m, 0.22 m thick at mid-length, tapered to nothing at
/// both tips) drawn three times: an alpha-blended body, an additive white core at 0.4, and a fainter trailing copy (alpha ~0.22, radius x0.88,
/// 0.03 s late). The arc sweeps in from the right-shoulder tip, peaks, then is eaten from the tail, so it never sits still as a solid banana.
/// Its plane is tilted from the ground by the hero's facing relative to the camera: towards the camera 30° (opens sideways on screen),
/// away 45°, sideways 55°. Times are relative to SlashArc(): 0-0.05 sweep in, main gone by 0.16, trailing copy gone by 0.22.
/// One reused set of objects and vertex-colour arrays: nothing is allocated per frame.
/// </summary>
public class SlashArcFx : MonoBehaviour
{
    public static SlashArcFx I;
    public Material BodyMat, CoreMat;
    const int Seg = 16;
    // ChatGPT asked R 0.75 / 0.14 thick, but in game 0.14 m is 3-4 px (a grey hair) and at R 0.72 the arc sits inside the slime body
    // (slime centre 1.0 m away, radius ~0.3) so the depth test hides it: R 0.62 keeps it between hero and target.
    const float Arc = 120f, Outer = 0.62f, Thick = 0.22f;

    class Layer { public GameObject Go; public Mesh Mesh; public Color[] Colors; public MeshRenderer Core; }
    Layer main, trail;
    Coroutine running;
    static readonly int TintId = Shader.PropertyToID("_TintColor");

    void Awake()
    {
        I = this;
        main = MakeLayer("SlashMain", true);
        trail = MakeLayer("SlashTrail", false);
    }

    /// <summary>Arc mesh in a local frame where +X = the arc's middle direction and +Y = the side the sweep starts from. v: 0 inner, 1 outer.</summary>
    Layer MakeLayer(string name, bool withCore)
    {
        var mesh = new Mesh { name = name };
        var v = new Vector3[(Seg + 1) * 2]; var uv = new Vector2[v.Length]; var t = new int[Seg * 6];
        for (int i = 0; i <= Seg; i++)
        {
            float u = (float)i / Seg, a = Mathf.Lerp(Arc / 2f, -Arc / 2f, u) * Mathf.Deg2Rad;
            float w = Thick * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * u)), 0.8f);   // Sin(PI) is -8.7e-8: Pow of a negative = NaN, which broke the whole mesh
            Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            v[i * 2] = d * (Outer - w); v[i * 2 + 1] = d * Outer;
            uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
        }
        for (int i = 0; i < Seg; i++) { int q = i * 2, k = i * 6; t[k] = q; t[k + 1] = q + 1; t[k + 2] = q + 2; t[k + 3] = q + 1; t[k + 4] = q + 3; t[k + 5] = q + 2; }
        mesh.vertices = v; mesh.uv = uv; mesh.triangles = t; mesh.colors = new Color[v.Length]; mesh.RecalculateBounds();
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); Setup(r, BodyMat);
        // HDR tint (legacy particle shaders double it): ~1.5x warm colour, 1.2x alpha. At 1x the scene exposure (-0.6 EV) + ACES turned the
        // cream body into a 50 % grey ghost (v6 test); above 1 it stays bright after tonemapping and catches a little bloom.
        var body = new MaterialPropertyBlock(); body.SetColor(TintId, new Color(0.8f, 0.72f, 0.6f, 0.6f)); r.SetPropertyBlock(body);
        MeshRenderer core = null;
        if (withCore)
        {
            var c = new GameObject(name + "Core"); c.transform.SetParent(go.transform, false);
            c.AddComponent<MeshFilter>().sharedMesh = mesh;
            core = c.AddComponent<MeshRenderer>(); Setup(core, CoreMat);
            var mpb = new MaterialPropertyBlock(); mpb.SetColor(TintId, new Color(0.9f, 0.85f, 0.75f, 0.4f)); core.SetPropertyBlock(mpb);   // 0.4 additive (top of the ChatGPT 0.25-0.4 range), HDR white
        }
        go.SetActive(false);
        return new Layer { Go = go, Mesh = mesh, Colors = new Color[v.Length], Core = core };
    }

    static void Setup(MeshRenderer r, Material m)
    {
        r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
    }

    public static void Play(Transform hero) { if (I != null && I.BodyMat != null) { if (I.running != null) I.StopCoroutine(I.running); I.running = I.StartCoroutine(I.Run(hero)); } }

    IEnumerator Run(Transform hero)
    {
        main.Go.SetActive(true); trail.Go.SetActive(true);
        float t0 = Time.time;   // scaled time: the hit stop freezes the arc with everything else
        bool freeze = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--arcfreeze") >= 0;   // --arcfreeze: capture aid, holds the arc at its peak for 3 s
        while (Time.time - t0 < (freeze ? 3f : 0.22f) && hero != null)
        {
            float t = Time.time - t0; if (freeze) t = Mathf.Min(t, 0.07f);
            Place(main.Go.transform, hero, 1f);
            Place(trail.Go.transform, hero, 0.88f);
            Paint(main, t, 0.9f);
            Paint(trail, t - 0.03f, 0.25f);
            yield return null;
        }
        main.Go.SetActive(false); trail.Go.SetActive(false);
        running = null;
    }

    /// <summary>Tilt of the slash plane from the ground: 30° when the hero faces the camera, 45° facing away, 55° side-on.</summary>
    static float Tilt(Transform hero)
    {
        Vector3 camFwd = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        float towards = -Vector3.Dot(hero.forward, camFwd);   // 1 = faces the camera (down the screen), -1 = faces away
        return towards >= 0f ? Mathf.Lerp(55f, 30f, towards) : Mathf.Lerp(55f, 45f, -towards);
    }

    static void Place(Transform arc, Transform hero, float scale)
    {
        float tilt = Tilt(hero) * Mathf.Deg2Rad;
        Vector3 fwd = hero.forward, side = (hero.right * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt)).normalized;   // sweep starts high on the right
        // pulled 0.6 m towards the camera along its view ray: same place on screen, but in front of a slime that walked into the arc
        arc.position = hero.position + Vector3.up * 0.72f + fwd * 0.05f - Camera.main.transform.forward * 0.6f;
        arc.rotation = Quaternion.LookRotation(Vector3.Cross(fwd, side), side);   // local +X = forward, +Y = side
        arc.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// Vertex alpha along the arc at time t: the head sweeps from u = 0 to 1 in 0.05 s, the tail eats it from u = 0 between 0.06 and 0.16 s,
    /// and the middle stays brighter than the tips (head soft, middle clear, tail fading). Colour stays white; the textures carry the hue.
    /// </summary>
    void Paint(Layer l, float t, float peak)
    {
        float head = Mathf.Clamp01(t / 0.05f) * 1.15f, tail = Mathf.Clamp01((t - 0.06f) / 0.10f) * 1.15f;
        for (int i = 0; i <= Seg; i++)
        {
            float u = (float)i / Seg;
            float a = t < 0f ? 0f : peak * Mathf.Clamp01((head - u) / 0.15f) * Mathf.Clamp01((u - tail + 0.15f) / 0.15f) * (0.45f + 0.55f * Mathf.Sin(Mathf.PI * u));
            l.Colors[i * 2] = l.Colors[i * 2 + 1] = new Color(1f, 1f, 1f, a);
        }
        l.Mesh.colors = l.Colors;
    }
}
