using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Look of the prototype maps. Frost Hollow = ChatGPT "Soft Winter" (sun 0.8, cool trilight, snow albedo #D8E5ED); Willowbrook = ChatGPT "Prontera"
/// (sun 1.1 #FFF1D7 from the camera's upper left, ambient #C5DBF0/#A8BCA1/#73846F, fog #BDD8C0). Both: linear colour space, Post Processing v2
/// on the camera (ACES, slight negative exposure, AO, FXAA, a whisper of bloom on the forest). Also the code-drawn slash, impact-star and contact-shadow textures.
/// </summary>
public static partial class ProtoBuilder
{
    static readonly Color SnowTint = Hex("D8E5ED"), SunColor = Hex("FFF0DE");
    const int PostFxLayer = 8;

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

    /// <summary>The 2D map's 4 painted ground chunks as quads (r0 = north). `dapple`: sun flecks under the crowns as a Standard detail map
    /// (×2 multiply, 0.5 = neutral), one 14 m tile, offset per chunk so it continues across chunk borders.</summary>
    static void Ground(float w, float h, string prefix, Color tint, bool dapple = false)
    {
        var dap = dapple ? DappleTexture() : null;
        var chunks = new[] { ("r0c0", 0, 1), ("r0c1", 1, 1), ("r1c0", 0, 0), ("r1c1", 1, 0) };
        foreach (var (name, cx, cy) in chunks)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Ground_" + name;
            q.transform.position = new Vector3((cx + 0.5f) * w / 2f, 0f, (cy + 0.5f) * h / 2f);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f); q.transform.localScale = new Vector3(w / 2f, h / 2f, 1f);
            var m = Mat(prefix + name + ".mat", prefix + name + ".png", null, tint);
            m.SetFloat("_Glossiness", 0.05f);
            if (dap != null)
            {
                m.SetTexture("_DetailAlbedoMap", dap); m.EnableKeyword("_DETAIL_MULX2");
                m.SetTextureScale("_DetailAlbedoMap", new Vector2(w / 2f / 14f, h / 2f / 14f));
                m.SetTextureOffset("_DetailAlbedoMap", new Vector2(cx * w / 2f / 14f, cy * h / 2f / 14f));
            }
            q.GetComponent<Renderer>().sharedMaterial = m;
        }
    }

    static void Lighting()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 0.8f; sun.color = SunColor;
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.7f; sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("B7D3EC"); RenderSettings.ambientEquatorColor = Hex("A4B8CC"); RenderSettings.ambientGroundColor = Hex("7B91A7");
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(0.62f, 0.75f, 0.85f); RenderSettings.fogStartDistance = 22f; RenderSettings.fogEndDistance = 55f;
        QualitySettings.shadowDistance = 40f;
    }

    static void LightingForest()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        // v9 (ChatGPT): sun #FFF0D8 1.0, shadows 0.7, ambient olive / cool shade; dappled light = ground detail map (Ground(dapple), a sun cookie never showed)
        sun.type = LightType.Directional; sun.intensity = 1.0f; sun.color = Hex("FFF0D8");
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.7f; sun.transform.rotation = Quaternion.Euler(50f, -40f, 0f);   // from the camera's upper left
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("BDD2C0"); RenderSettings.ambientEquatorColor = Hex("93A48F"); RenderSettings.ambientGroundColor = Hex("64765D");
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Hex("BDD8C0"); RenderSettings.fogStartDistance = 18f; RenderSettings.fogEndDistance = 45f;
        QualitySettings.shadowDistance = 40f;
    }

    /// <summary>ACES + exposure + AO + FXAA through Post Processing v2 (Built-in RP). `exposure` is the --exposure build arg (default -0.4 EV).</summary>
    static void PostFx(GameObject camGo, float exposure, string scene)
    {
        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        var cg = profile.AddSettings<ColorGrading>();
        cg.gradingMode.Override(GradingMode.HighDefinitionRange); cg.tonemapper.Override(Tonemapper.ACES);
        cg.postExposure.Override(exposure); cg.contrast.Override(10f); cg.saturation.Override(scene == "ProtoForest" ? -8f : -5f);   // forest v9: grass ~10 % less saturated (ChatGPT olive target)
        var ao = profile.AddSettings<AmbientOcclusion>();
        ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance); ao.intensity.Override(scene == "ProtoForest" ? 0.4f : 0.25f); ao.radius.Override(0.3f);
        if (scene == "ProtoGlade")   // ChatGPT forest spec: ACES 0 EV, contrast +8, saturation +5, temperature +5, AO 0.45 r 0.35, bloom 0.15 / 1.1, vignette 0.08
        {
            cg.contrast.Override(8f); cg.saturation.Override(5f); cg.temperature.Override(5f);
            ao.intensity.Override(0.65f); ao.radius.Override(0.35f);   // v11
            var gb = profile.AddSettings<Bloom>(); gb.intensity.Override(0.11f);   // v13 gb.threshold.Override(1.1f); gb.softKnee.Override(0.5f);
            var vg = profile.AddSettings<Vignette>(); vg.intensity.Override(0.08f);
        }
        if (scene == "ProtoForest") { var bloom = profile.AddSettings<Bloom>(); bloom.intensity.Override(0.15f); bloom.threshold.Override(1.3f); bloom.softKnee.Override(0.5f); }
        string path = "Assets/Env/PostFx-" + scene + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(profile, path);

        var volGo = new GameObject("PostFxVolume") { layer = PostFxLayer };
        var vol = volGo.AddComponent<PostProcessVolume>(); vol.isGlobal = true; vol.sharedProfile = profile;
        var layer = camGo.AddComponent<PostProcessLayer>();
        layer.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        layer.volumeLayer = 1 << PostFxLayer; layer.volumeTrigger = camGo.transform;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
    }

    /// <summary>Writes a code-drawn RGBA texture (pixel function of u, v in 0..1), imports it clamped without mips, returns the asset.</summary>
    static Texture2D CodeTexture(string png, int w, int h, System.Func<float, float, Color> pixel, TextureWrapMode wrap = TextureWrapMode.Clamp)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); var px = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = pixel((x + 0.5f) / w, (y + 0.5f) / h);
        tex.SetPixels(px); File.WriteAllBytes(png, tex.EncodeToPNG()); AssetDatabase.ImportAsset(png);
        var ti = (TextureImporter)AssetImporter.GetAtPath(png); ti.alphaIsTransparency = true; ti.wrapMode = wrap; ti.mipmapEnabled = wrap == TextureWrapMode.Repeat; ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(png);
    }

    static Material ParticleMat(string shader, Texture2D tex, string path)
    {
        var m = new Material(Shader.Find(shader)) { mainTexture = tex };
        AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path);
        return m;
    }

    /// <summary>
    /// Crescent Slash v6 (ChatGPT): the arc shape is a mesh built at runtime (SlashArcFx), these are its cross-section textures, v = 0 inner edge
    /// to 1 outer edge. Body (alpha blend): amber rim #F5BC65 fading out inwards, warm #FFEAC0 body, white edge #FFFFFF at the blade path.
    /// Core (additive, drawn on top at ~0.3): a thin white line near the outer edge.
    /// </summary>
    static Material SlashBodyMaterial(out Material core)
    {
        Color white = Hex("FFFFFF"), body = Hex("FFEAC0"), rim = Hex("F5BC65");
        var bodyTex = CodeTexture("Assets/Env/slash-body.png", 4, 64, (u, v) =>
        {
            // v6 test: a white band from 0.72 up read as a pale grey hair on grass; keep white to the outer 15 % so the warm body carries it
            Color c = v > 0.85f ? white : v > 0.45f ? Color.Lerp(rim, body, (v - 0.45f) / 0.4f) : rim;
            c.a = Mathf.Clamp01(v / 0.25f) * Mathf.Clamp01((1f - v) / 0.06f);
            return c;
        });
        var coreTex = CodeTexture("Assets/Env/slash-core.png", 4, 64, (u, v) => new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.Abs(v - 0.8f) / 0.15f)));
        core = ParticleMat("Legacy Shaders/Particles/Additive", coreTex, "Assets/Env/SlashCore.mat");
        return ParticleMat("Legacy Shaders/Particles/Alpha Blended", bodyTex, "Assets/Env/SlashBody.mat");
    }

    /// <summary>Soft round contact shadow (alpha 0.35 at the centre, fading to 0 at the rim), dark moss colour so it does not look grey on grass.</summary>
    /// <summary>v17: faint warm ring under the hero (ChatGPT: ground ring 10-15 % alpha) - soft edge band at 80-95 % of the radius.</summary>
    static Material HeroRingMaterial()
    {
        var tex = CodeTexture("Assets/Env/hero-ring.png", 64, 64, (u, v) =>
        {
            float r = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
            float band = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.1f);
            return new Color(1f, 0.93f, 0.7f, 0.13f * band + 0.04f * Mathf.Clamp01(1f - r));
        });
        return ParticleMat("Legacy Shaders/Particles/Alpha Blended", tex, "Assets/Env/HeroRing.mat");
    }

    /// <summary>Soft ring texture: band centred at `radius` (0-1 of the half size), `width` wide, in `color`.</summary>
    static Texture2D RingTexture(string png, Color color, float radius, float width)
    {
        return CodeTexture(png, 128, 128, (u, v) =>
        {
            float r = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
            float band = Mathf.Clamp01(1f - Mathf.Abs(r - radius) / width);
            return new Color(color.r, color.g, color.b, band * band * (3f - 2f * band));
        });
    }

    static Material BlobShadowMaterial()
    {
        Color moss = Hex("1F2A18");
        var tex = CodeTexture("Assets/Env/blob-shadow.png", 64, 64, (u, v) =>
        {
            float r = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
            return new Color(moss.r, moss.g, moss.b, 0.35f * Mathf.Pow(Mathf.Clamp01(1f - r), 0.8f));   // pow 1.5 / 0.3 was invisible under the feet (v6 test)
        });
        return ParticleMat("Legacy Shaders/Particles/Alpha Blended", tex, "Assets/Env/BlobShadow.mat");
    }

    /// <summary>
    /// Impact star drawn in code (Assets/Env/impact-star.png): 5 slim points, ivory centre (#FFF4C5) going to an amber rim (#E99B42), soft glow
    /// around it; alpha-blended so it keeps its warm edge on bright ground. Also used, small, for the sparks that fly out.
    /// </summary>
    static Material ImpactStarMaterial()
    {
        const string png = "Assets/Env/impact-star.png";
        int S = 128; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color[S * S];
        Color core = Hex("FFF4C5"), rim = Hex("E99B42");
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = (x + 0.5f) / S * 2f - 1f, v = (y + 0.5f) / S * 2f - 1f;
            float r = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u);
            float point = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 2.5f)), 3f);            // 5 lobes (power 6 was too slim to read in game)
            float edge = 0.36f + 0.62f * point;                                       // star radius at this angle
            float body = Mathf.Clamp01((edge - r) / 0.08f), glow = Mathf.Clamp01(1f - r / 0.55f) * 0.45f;
            float a = Mathf.Max(body, glow * glow);
            Color c = Color.Lerp(core, rim, Mathf.Clamp01(r / Mathf.Max(edge, 0.01f)));
            px[y * S + x] = new Color(c.r, c.g, c.b, a);
        }
        tex.SetPixels(px); File.WriteAllBytes(png, tex.EncodeToPNG()); AssetDatabase.ImportAsset(png);
        var ti = (TextureImporter)AssetImporter.GetAtPath(png); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = false; ti.SaveAndReimport();
        var m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(png) };
        m.SetColor("_TintColor", new Color(0.8f, 0.75f, 0.65f, 0.5f));   // HDR: at 1x the -0.6 EV exposure + ACES turned the ivory star rust-brown
        AssetDatabase.DeleteAsset("Assets/Env/Spark.mat"); AssetDatabase.CreateAsset(m, "Assets/Env/Spark.mat");
        return m;
    }

    /// <summary>
    /// Dappled sun under the crowns (ChatGPT v9: patches 0.25-1.2 m across covering ~35 %, never black): 1024² tiling grey detail map for the
    /// ground, 0.5 = untouched, shade 0.38 sRGB (= 55 % after the linear-space ×2 detail multiply), soft edges, seamless. (A directional-light cookie with the
    /// same pattern did nothing in the Unity 6 player, so the flecks are painted into the ground material instead.)
    /// </summary>
    static Texture2D DappleTexture()
    {
        const int S = 1024; const float metres = 14f;
        var rnd = new System.Random(11); var px = new Color[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0.5f, 0.5f, 0.5f, 1f);
        for (int n = 0; n < 230; n++)
        {
            float bx = (float)rnd.NextDouble() * S, by = (float)rnd.NextDouble() * S, br = (0.125f + (float)rnd.NextDouble() * 0.475f) * S / metres;
            int r = Mathf.CeilToInt(br * 1.4f);
            for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
            {
                float shade = Mathf.Clamp01((1.25f - Mathf.Sqrt(dx * dx + dy * dy) / br) / 0.4f);
                if (shade <= 0f) continue;
                int i = (((int)by + dy + S) % S) * S + ((int)bx + dx + S) % S;
                float v = Mathf.Min(px[i].r, Mathf.Lerp(0.5f, 0.38f, shade));   // sRGB 0.38 -> linear x unity_ColorSpaceDouble = 0.55
                px[i] = new Color(v, v, v, 1f);
            }
        }
        return CodeTexture("Assets/Env/dapple-detail.png", S, S, (u, v) => px[Mathf.Min(S - 1, (int)(v * S)) * S + Mathf.Min(S - 1, (int)(u * S))], TextureWrapMode.Repeat);
    }

    static void ConfigureTexture(string p)
    {
        var ti = AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti == null) return;
        bool ground = p.Contains("large-ground");
        ti.maxTextureSize = ground ? 4096 : 1024;
        if (p.Contains("ormal")) ti.textureType = TextureImporterType.NormalMap;
        if (ground) { ti.wrapMode = TextureWrapMode.Clamp; ti.anisoLevel = 8; }
        ti.SaveAndReimport();
    }

    /// <summary>A build argument "--name value" (e.g. --exposure -0.4), or `fallback`.</summary>
    static float Arg(string name, float fallback)
    {
        var a = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(a, "--" + name);
        return i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }
}
