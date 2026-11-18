using UnityEngine;

/// <summary>
/// Pool of textured quads for the Warrior skill effects (WarriorSkills3D). Each piece is spawned once with a start/end state and then animated
/// here every frame: position (lerp, or velocity + gravity, or orbit around a centre), size, roll, HDR colour and fade. Nothing is allocated
/// after Awake: 64 quads are made up front, the oldest one is reused when all are busy, and one MaterialPropertyBlock is shared.
/// Facing modes: Billboard (faces the camera), Upright (turns to the camera around world Y: fire pillars), Ground (lies flat: rings).
/// Pull: metres moved towards the camera along the view ray with the size compensated, so the piece keeps its place on screen but is not
/// hidden by a slime body or cut by the ground (the ZTest problem logged for the basic slash).
/// </summary>
public class SkillFx3D : MonoBehaviour
{
    public enum Face { Billboard, Upright, Ground }
    public enum Kind { Glow, Ring, Wedge, Star, Flame, Rock, Leaf }

    /// <summary>One spawn request. Fill the fields you need; the defaults give a camera-facing piece that stays put and fades out.</summary>
    public struct Spec
    {
        public Kind Kind; public Face Face;
        public Vector3 From, To, Velocity; public float Gravity;
        public Transform Follow;                 // when set, From/To are offsets from this transform
        public bool Orbit; public float OrbitRadius, OrbitSpeed, OrbitAngle;   // degrees per second around From (world Y)
        public float Size0, Size1, Aspect, Roll0, Roll1, Pull;
        public Color Color; public float FadeIn, FadeOutAt, Life, Delay;
    }

    class Piece { public GameObject Go; public Transform Tr; public MeshRenderer R; public Spec S; public float Born; public bool On; }

    public static SkillFx3D I;
    public Material GlowMat, RingMat, WedgeMat, StarMat, FlameMat, RockMat, LeafMat;   // set by ProtoBuilder (ProtoSkillLook.cs)
    const int PoolSize = 64;
    readonly Piece[] pool = new Piece[PoolSize];
    MaterialPropertyBlock mpb;
    static readonly int TintId = Shader.PropertyToID("_TintColor");
    int next;

    void Awake()
    {
        I = this;
        mpb = new MaterialPropertyBlock();
        var mesh = MakeQuad();
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject("SkillFx" + i);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.SetActive(false);
            pool[i] = new Piece { Go = go, Tr = go.transform, R = r };
        }
    }

    /// <summary>Unit quad in XY facing -Z (like the built-in Quad), with vertex colours so it batches with the particle shaders.</summary>
    static Mesh MakeQuad()
    {
        var m = new Mesh { name = "SkillFxQuad" };
        m.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        m.RecalculateBounds();
        return m;
    }

    Material Mat(Kind k)
    {
        switch (k)
        {
            case Kind.Ring: return RingMat; case Kind.Wedge: return WedgeMat; case Kind.Star: return StarMat;
            case Kind.Flame: return FlameMat; case Kind.Rock: return RockMat; case Kind.Leaf: return LeafMat;
            default: return GlowMat;
        }
    }

    public static void Spawn(Spec s)
    {
        if (I == null) return;
        if (s.Aspect <= 0f) s.Aspect = 1f;
        if (s.Life <= 0f) s.Life = 0.3f;
        if (s.FadeOutAt <= 0f) s.FadeOutAt = 0.6f;
        Piece p = I.pool[I.next]; I.next = (I.next + 1) % PoolSize;   // ring buffer: when all are busy the oldest is reused
        p.S = s; p.Born = Time.time; p.On = true;
        p.R.sharedMaterial = I.Mat(s.Kind);
        p.Go.SetActive(false);   // shown on its first Update, after its delay
    }

    void LateUpdate()
    {
        var cam = Camera.main; if (cam == null) return;
        Transform ct = cam.transform;
        for (int i = 0; i < PoolSize; i++)
        {
            Piece p = pool[i];
            if (!p.On) continue;
            float age = Time.time - p.Born - p.S.Delay;
            if (age < 0f) continue;
            if (age >= p.S.Life) { p.On = false; p.Go.SetActive(false); continue; }
            if (!p.Go.activeSelf) p.Go.SetActive(true);
            Animate(p, age / p.S.Life, age, ct);
        }
    }

    void Animate(Piece p, float k, float age, Transform ct)
    {
        ref Spec s = ref p.S;
        float ease = 1f - (1f - k) * (1f - k);
        Vector3 pos;
        if (s.Orbit)
        {
            float a = (s.OrbitAngle + s.OrbitSpeed * age) * Mathf.Deg2Rad;
            pos = s.From + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * s.OrbitRadius + (s.To - s.From) * k;
        }
        else if (s.Velocity != Vector3.zero || s.Gravity != 0f) pos = s.From + s.Velocity * age + Vector3.down * (0.5f * s.Gravity * age * age);
        else pos = Vector3.Lerp(s.From, s.To, ease);
        if (s.Follow != null) pos = s.Follow.position + pos;

        float size = Mathf.Lerp(s.Size0, s.Size1, ease), roll = Mathf.Lerp(s.Roll0, s.Roll1, k);
        if (s.Pull > 0f)
        {
            float d = Vector3.Distance(ct.position, pos), pull = Mathf.Min(s.Pull, d * 0.6f);
            pos -= ct.forward * pull;   // along the view direction: same screen spot, size scaled by the new distance
            size *= Mathf.Max(0.1f, (d - pull) / d);
        }
        p.Tr.position = pos;
        switch (s.Face)
        {
            case Face.Ground: p.Tr.rotation = Quaternion.Euler(90f, roll, 0f); break;
            case Face.Upright:
                Vector3 f = Vector3.ProjectOnPlane(ct.forward, Vector3.up);
                p.Tr.rotation = Quaternion.LookRotation(f.sqrMagnitude > 1e-4f ? f : Vector3.forward, Vector3.up) * Quaternion.Euler(0f, 0f, roll);
                break;
            default: p.Tr.rotation = Quaternion.LookRotation(ct.forward, ct.up) * Quaternion.Euler(0f, 0f, roll); break;
        }
        p.Tr.localScale = new Vector3(size, size * s.Aspect, 1f);

        float a01 = (s.FadeIn > 0f ? Mathf.Clamp01(k / s.FadeIn) : 1f) * (1f - Mathf.Clamp01((k - s.FadeOutAt) / Mathf.Max(0.01f, 1f - s.FadeOutAt)));
        Color c = s.Color; c.a *= a01;
        mpb.SetColor(TintId, c);
        p.R.SetPropertyBlock(mpb);
    }

    /// <summary>Screen roll (degrees) that points a piece's +X along world direction `dir` as seen from the camera at `at`.</summary>
    public static float ScreenRoll(Vector3 at, Vector3 dir)
    {
        var cam = Camera.main; if (cam == null) return 0f;
        Vector3 a = cam.WorldToScreenPoint(at), b = cam.WorldToScreenPoint(at + dir);
        return Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
    }

    /// <summary>HDR tint for the legacy particle shaders (they double the tint): `hex` at `boost` brightness, alpha `a` (0.5 = as painted).</summary>
    public static Color Tint(string hex, float boost, float a)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return new Color(c.r * 0.5f * boost, c.g * 0.5f * boost, c.b * 0.5f * boost, a);
    }
}
