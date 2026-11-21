using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Where an ArcRibbonFx sorts: Over = FX band over the crowd; Ground = flat on the ground band; Split = round a hero (the part
    /// above `splitY` behind them, the rest in front); Behind = just behind the hero.</summary>
    public enum RibbonSort { Over, Ground, Split, Behind }

    /// <summary>
    /// Mesh Trail of the RO3 Soft Fantasy skills (rule 9; 2026-11-21, first for the Warrior's sword slashes): a soft ribbon that follows a path (an
    /// arc round the hero, a straight thrust, a leap) instead of a TrailRenderer, so a 60-90 ms sweep still bends smoothly (a TrailRenderer only gets
    /// one point per frame). The head runs along the path over `sweepMs` (ease-out), the tail follows `tailMs` later and catches up after the sweep
    /// (rule 9: tail gone in 0.12-0.25 s); width tapers from the head (widest, brightest: core -> light -> main colour) to a point at the tail.
    /// Shader LightTrail (alpha body + low additive core) with ClericLightLook.Trail(palette). It lives under a ClericLightFx host and runs on its
    /// clock (AgeMs, anchor, crowd LOD), so it ends / clears with it (ClericLightFx.ClearAll on a map change). Paths are offsets from the host's
    /// anchor (a hero's feet when the host follows one). Generic: any class may use it.
    /// </summary>
    public sealed class ArcRibbonFx : MonoBehaviour
    {
        /// <summary>Point of the path at u (0 = start .. 1 = end), as an offset from the host anchor in world units.</summary>
        public delegate Vector2 PathFn(float u);

        static readonly List<Vector3> V = new List<Vector3>(256), V2 = new List<Vector3>(256);
        static readonly List<Color> C = new List<Color>(256), C2 = new List<Color>(256);
        static readonly List<Vector2> U = new List<Vector2>(256), U2 = new List<Vector2>(256);
        static readonly List<int> T = new List<int>(384), T2 = new List<int>(384);

        ClericLightFx host;
        PathFn path;
        ClericLightLook.Pal pal;
        RibbonSort sort;
        HeroController around;
        float t0, sweepMs, tailMs, width, alpha, splitY;
        int order;
        bool linear;
        Mesh front, back;
        MeshRenderer rFront, rBack;

        /// <summary>A ribbon on `host` starting at `t0` ms. width in world units (rule 9: 0.08-0.22 H), alpha 0..1, `mat` = a LightTrail material
        /// (default ClericLightLook.Trail(p)).</summary>
        public static ArcRibbonFx Add(ClericLightFx host, PathFn path, float t0, float sweepMs, float tailMs, float width, ClericLightLook.Pal p, float alpha,
            RibbonSort sort = RibbonSort.Over, int order = 0, HeroController around = null, float splitY = 6f, Material mat = null)
        {
            if (host == null || path == null) return null;
            var go = new GameObject("Ribbon");
            go.transform.SetParent(host.transform, false);
            var r = go.AddComponent<ArcRibbonFx>();
            r.host = host; r.path = path; r.pal = p; r.sort = sort; r.around = around; r.splitY = splitY;
            r.t0 = t0; r.sweepMs = Mathf.Max(1f, sweepMs); r.tailMs = Mathf.Max(1f, tailMs); r.order = order;
            FxTier tier = host.Lod;
            r.width = width * (tier == FxTier.Own ? 1f : tier == FxTier.Near ? 0.9f : 0.75f);
            r.alpha = alpha * (tier == FxTier.Own ? 1f : tier == FxTier.Near ? 0.75f : 0.5f); // the slash is the skill's main shape: kept for strangers, dimmer
            Material m = mat != null ? mat : ClericLightLook.Trail(p);
            r.rFront = r.Part("Front", m, out r.front);
            if (sort == RibbonSort.Split) r.rBack = r.Part("Back", m, out r.back);
            return r;
        }

        /// <summary>An arc of the ellipse centred at `centre` (half width rx, height rx x squash), from parameter angle a0 to a1 (deg, anticlockwise
        /// when a1 &gt; a0; several turns allowed).</summary>
        public static PathFn Arc(Vector2 centre, float rx, float squash, float a0Deg, float a1Deg)
        {
            return u =>
            {
                float a = Mathf.Lerp(a0Deg, a1Deg, u) * Mathf.Deg2Rad;
                return centre + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * rx * squash);
            };
        }

        /// <summary>The head runs at a constant speed instead of easing out (a spin round the hero, a leap).</summary>
        public ArcRibbonFx Linear() { linear = true; return this; }

        /// <summary>A straight ribbon from a to b.</summary>
        public static PathFn Line(Vector2 a, Vector2 b) { return u => Vector2.Lerp(a, b, u); }

        MeshRenderer Part(string name, Material m, out Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            mesh = new Mesh { name = "ArcRibbon" };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = false;
            return r;
        }

        float Ease(float x) { x = Mathf.Clamp01(x); return linear ? x : 1f - (1f - x) * (1f - x); }

        void LateUpdate()
        {
            if (host == null) return;
            float t = host.AgeMs - t0;
            float head = Ease(t / sweepMs), tail = Ease((t - tailMs) / sweepMs);
            bool on = t >= 0f && head - tail > 0.004f && FeelConfig.FxOn && alpha > 0.01f;
            rFront.enabled = on; if (rBack != null) rBack.enabled = on;
            if (!on) return;
            Vector2 anchor = host.Anchor;
            transform.position = new Vector3(anchor.x, anchor.y, 0f);
            transform.rotation = Quaternion.identity;
            float fade = t <= sweepMs ? 1f : Mathf.Clamp01(1f - (t - sweepMs) / tailMs) * 0.5f + 0.5f;
            int n = Mathf.Clamp(Mathf.CeilToInt((head - tail) * 56f), 4, 56);
            V.Clear(); C.Clear(); U.Clear(); T.Clear(); V2.Clear(); C2.Clear(); U2.Clear(); T2.Clear();
            Vector2 prevL = Vector2.zero, prevR = Vector2.zero; Color prevC = Color.clear; float prevS = 0f;
            for (int i = 0; i <= n; i++)
            {
                float s = i / (float)n, u = Mathf.Lerp(tail, head, s);           // s: 0 tail .. 1 head
                Vector2 p = path(u), q = path(Mathf.Min(1f, u + 0.01f)), q0 = path(Mathf.Max(0f, u - 0.01f));
                Vector2 d = q - q0;
                Vector2 nrm = d.sqrMagnitude > 1e-6f ? new Vector2(-d.y, d.x).normalized : Vector2.up;
                float w = width * Mathf.Pow(s, 0.7f) * (s > 0.9f ? Mathf.Lerp(1f, 0.6f, (s - 0.9f) / 0.1f) : 1f) * 0.5f;
                Vector2 l = p + nrm * w, r = p - nrm * w;
                Color c = s > 0.85f ? Color.Lerp(pal.Light, pal.Core, (s - 0.85f) / 0.15f) : Color.Lerp(pal.Main, pal.Light, Mathf.Pow(s / 0.85f, 1.6f)); // body colour over most of the length, hot only at the head
                c.a = alpha * fade * Mathf.SmoothStep(0f, 1f, s / 0.35f);
                bool isBack = sort == RibbonSort.Split && p.y > splitY;
                if (i > 0) Quad(isBack, prevL, prevR, l, r, prevC, c, prevS, s);
                prevL = l; prevR = r; prevC = c; prevS = s;
            }
            Flush(front, V, C, U, T);
            if (back != null) Flush(back, V2, C2, U2, T2);
            int baseOrder;
            int heroOrder = around != null ? GameConfig.OrderBase(around.Position.y) : GameConfig.OrderBase(anchor.y);
            switch (sort)
            {
                case RibbonSort.Ground: baseOrder = UltimateFx.GroundFxOrder + order; break;
                case RibbonSort.Behind: baseOrder = heroOrder - 2 + order; break;
                case RibbonSort.Split: baseOrder = heroOrder + 8 + order; rBack.sortingOrder = heroOrder - 2 + order; break;
                default: baseOrder = FeelConfig.FxOrder - 20 + order; break;
            }
            rFront.sortingOrder = baseOrder;
        }

        /// <summary>One segment: into the back list (the far half of a Split ribbon) or the front list (Behind uses the front renderer, sorted behind).</summary>
        void Quad(bool b, Vector2 l0, Vector2 r0, Vector2 l1, Vector2 r1, Color c0, Color c1, float s0, float s1)
        {
            List<Vector3> v = b ? V2 : V; List<Color> c = b ? C2 : C; List<Vector2> uv = b ? U2 : U; List<int> t = b ? T2 : T;
            // Path points are offsets from the host anchor and the mesh object sits ON the anchor, so they are already local vertices.
            // (Until 21/11 the anchor was subtracted a second time: every ribbon drew near the world origin, off screen, and the Warrior's
            // slashes were simply missing in game; Sổ lỗi 2026-11-21 Warrior grand pass.)
            int k = v.Count;
            v.Add(l0); v.Add(r0); v.Add(l1); v.Add(r1);
            c.Add(c0); c.Add(c0); c.Add(c1); c.Add(c1);
            uv.Add(new Vector2(s0, 1f)); uv.Add(new Vector2(s0, 0f)); uv.Add(new Vector2(s1, 1f)); uv.Add(new Vector2(s1, 0f));
            t.Add(k); t.Add(k + 2); t.Add(k + 1); t.Add(k + 1); t.Add(k + 2); t.Add(k + 3);
        }

        static void Flush(Mesh m, List<Vector3> v, List<Color> c, List<Vector2> uv, List<int> t)
        {
            m.Clear();
            if (v.Count == 0) return;
            m.SetVertices(v); m.SetColors(c); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
        }

        void OnDestroy()
        {
            if (front != null) Destroy(front);
            if (back != null) Destroy(back);
        }
    }
}
