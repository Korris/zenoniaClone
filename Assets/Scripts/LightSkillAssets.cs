using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shared materials, textures and quad meshes of the shader-driven light skill FX (IceStormLightFx). Built-in pipeline: no URP, so no Light2D and no
    /// post-processing stack; the glow comes from HDR colours in LightBeamScroll / LightTrail / LightBlob that pass the BloomLite threshold (BloomEffect on the camera).
    /// Textures are grey masks on black in Resources/Sprites/fx/light-skill (procedural stand-ins, see Tools/build-light-skill-textures.py).
    /// Every renderer is a quad MeshRenderer, not a SpriteRenderer, because a SpriteRenderer overwrites _MainTex with its sprite.
    /// </summary>
    public static class LightSkillAssets
    {
        const string TexFolder = "Sprites/fx/light-skill/";
        static Material beam, burst, trail, glow, core, ring;
        static Mesh quadCentre, quadBottom;
        static MaterialPropertyBlock block;
        static int fadeId, dissolveId, ringId;
        static bool failed;

        /// <summary>False when a shader is missing or unsupported: the caller falls back to the old (choreography) art.</summary>
        public static bool Ready { get { Load(); return !failed; } }

        public static Material Beam { get { Load(); return beam; } }      // vertical light pillar (scrolling rays + dissolve)
        public static Material Burst { get { Load(); return burst; } }    // radial ray burst (same shader, polar mode)
        public static Material Trail { get { Load(); return trail; } }    // meteor tail (TrailRenderer)
        public static Material Glow { get { Load(); return glow; } }      // fake ground light (Light2D stand-in)
        public static Material Core { get { Load(); return core; } }      // meteor head
        public static Material Ring { get { Load(); return ring; } }      // impact ring

        public static void Load()
        {
            if (beam != null || failed) return;
            Shader sBeam = Find("LightBeamScroll"), sTrail = Find("LightTrail"), sBlob = Find("LightBlob");
            if (sBeam == null || sTrail == null || sBlob == null) { failed = true; return; }
            Texture2D streaks = Tex("beam-streaks", true), noise = Tex("noise-clouds", true), head = Tex("meteor-core", false), band = Tex("burst-ring", false);

            beam = new Material(sBeam) { name = "Light_Beam" };
            beam.SetTexture("_MainTex", streaks);
            beam.SetTexture("_NoiseTex", noise);
            beam.SetVector("_MainTiling", new Vector4(1f, 0.6f, 0f, 0f));
            // Snow rule: the additive part stays mostly blue (R low, peak B ~1.4) so on white snow it tints instead of clipping to white;
            // only the thin core passes the bloom threshold. The alpha body (saturated blue) carries the shape on bright ground.
            beam.SetColor("_EmissionColor", new Color(0.1f, 0.5f, 1.35f, 1f));
            beam.SetFloat("_CoreWhite", 0.55f);
            beam.SetColor("_BodyColor", new Color(0.08f, 0.38f, 0.95f, 0.6f));
            beam.SetColor("_EdgeColor", new Color(0.3f, 0.9f, 1.8f, 1f));

            burst = new Material(beam) { name = "Light_Burst" };
            burst.SetFloat("_Polar", 1f);
            burst.SetVector("_MainTiling", new Vector4(3f, 1f, 0f, 0f)); // rays repeat 3x around the circle
            burst.SetFloat("_ScrollSpeed", 2.4f);
            burst.SetFloat("_StreakMin", 0.15f);
            burst.SetColor("_BodyColor", new Color(0.08f, 0.4f, 1f, 0.6f));

            trail = new Material(sTrail) { name = "Light_Trail" };
            trail.SetTexture("_NoiseTex", noise);
            trail.SetColor("_EmissionColor", new Color(0.8f, 1.05f, 1.5f, 1f));
            trail.SetFloat("_CoreWhite", 0.7f);

            glow = new Material(sBlob) { name = "Light_Ground" };
            glow.SetFloat("_Falloff", 3.2f);
            glow.SetColor("_EmissionColor", new Color(0.06f, 0.35f, 0.9f, 1f)); // dark ground: a blue pool of light; snow: the tint pass below does the work
            glow.SetColor("_BodyColor", new Color(0.15f, 0.5f, 1f, 0.4f));

            core = new Material(sBlob) { name = "Light_MeteorCore" };
            core.SetTexture("_MainTex", head);
            core.SetColor("_EmissionColor", new Color(0.5f, 1.1f, 2.0f, 1f));
            core.SetFloat("_CoreWhite", 1.4f);
            core.SetColor("_BodyColor", new Color(0.08f, 0.38f, 1f, 0.85f));

            ring = new Material(sBlob) { name = "Light_ImpactRing" };
            ring.SetTexture("_MainTex", band);
            ring.SetColor("_EmissionColor", new Color(0.15f, 0.6f, 1.5f, 1f));
            ring.SetFloat("_CoreWhite", 0.5f);
            ring.SetColor("_BodyColor", new Color(0.08f, 0.4f, 1f, 0.8f));
        }

        static Shader Find(string name)
        {
            Shader s = Resources.Load<Shader>("Shaders/" + name);
            if (s != null && s.isSupported) return s;
            Debug.LogWarning("ZenoniaProto: shader '" + name + "' missing or unsupported, Ice Storm uses the old art.");
            return null;
        }

        static Texture2D Tex(string name, bool repeat)
        {
            var t = Resources.Load<Texture2D>(TexFolder + name);
            if (t == null) { Debug.LogError("[ZenoniaProto] Missing texture Resources/" + TexFolder + name + ".png"); return Texture2D.whiteTexture; }
            t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            return t;
        }

        /// <summary>A quad renderer under `parent`. bottomPivot: x -0.5..0.5, y 0..1 (pillars stand on their foot), else centred. Scale it with localScale.</summary>
        public static MeshRenderer Quad(Transform parent, string name, Material mat, bool bottomPivot, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = bottomPivot ? QuadMesh(ref quadBottom, 0f) : QuadMesh(ref quadCentre, -0.5f);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = order;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        static Mesh QuadMesh(ref Mesh slot, float y0)
        {
            if (slot != null) return slot;
            slot = new Mesh { name = y0 < 0f ? "LightQuadCentre" : "LightQuadBottom" };
            slot.vertices = new[] { new Vector3(-0.5f, y0, 0f), new Vector3(0.5f, y0, 0f), new Vector3(-0.5f, y0 + 1f, 0f), new Vector3(0.5f, y0 + 1f, 0f) };
            slot.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            slot.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            slot.RecalculateBounds();
            return slot;
        }

        /// <summary>Per-renderer fade / dissolve / burst radius without new materials (negative = leave unchanged).</summary>
        public static void Set(Renderer r, float fade, float dissolve = -1f, float ringRadius = -1f)
        {
            if (block == null) { block = new MaterialPropertyBlock(); fadeId = Shader.PropertyToID("_Fade"); dissolveId = Shader.PropertyToID("_Dissolve"); ringId = Shader.PropertyToID("_Ring"); }
            r.GetPropertyBlock(block);
            block.SetFloat(fadeId, Mathf.Clamp01(fade));
            if (dissolve >= 0f) block.SetFloat(dissolveId, Mathf.Clamp01(dissolve));
            if (ringRadius >= 0f) block.SetFloat(ringId, ringRadius);
            r.SetPropertyBlock(block);
        }
    }
}
