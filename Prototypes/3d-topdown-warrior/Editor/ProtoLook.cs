using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Look of the prototype maps. Frost Hollow = ChatGPT "Soft Winter" (sun 0.8, cool trilight, snow albedo #D8E5ED); Willowbrook = ChatGPT "Prontera"
/// (sun 1.1 #FFF1D7 from the camera's upper left, ambient #C5DBF0/#A8BCA1/#73846F, fog #BDD8C0). Both: linear colour space, Post Processing v2
/// on the camera (ACES, slight negative exposure, AO, FXAA, a whisper of bloom on the forest). Also the code-drawn crescent slash texture.
/// </summary>
public static partial class ProtoBuilder
{
    static readonly Color SnowTint = Hex("D8E5ED"), SunColor = Hex("FFF0DE");
    const int PostFxLayer = 8;

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

    /// <summary>The 2D map's 4 painted ground chunks as quads (r0 = north), tinted so the painting does not burn out.</summary>
    static void Ground(float w, float h, string prefix, Color tint)
    {
        var chunks = new[] { ("r0c0", 0, 1), ("r0c1", 1, 1), ("r1c0", 0, 0), ("r1c1", 1, 0) };
        foreach (var (name, cx, cy) in chunks)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Ground_" + name;
            q.transform.position = new Vector3((cx + 0.5f) * w / 2f, 0f, (cy + 0.5f) * h / 2f);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f); q.transform.localScale = new Vector3(w / 2f, h / 2f, 1f);
            var m = Mat(prefix + name + ".mat", prefix + name + ".png", null, tint);
            m.SetFloat("_Glossiness", 0.05f);
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
        sun.type = LightType.Directional; sun.intensity = 1.1f; sun.color = Hex("FFF1D7");
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.7f; sun.transform.rotation = Quaternion.Euler(50f, -40f, 0f);   // from the camera's upper left
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("C5DBF0"); RenderSettings.ambientEquatorColor = Hex("A8BCA1"); RenderSettings.ambientGroundColor = Hex("73846F");
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Hex("BDD8C0"); RenderSettings.fogStartDistance = 18f; RenderSettings.fogEndDistance = 45f;
        QualitySettings.shadowDistance = 40f;
    }

    /// <summary>ACES + exposure + AO + FXAA through Post Processing v2 (Built-in RP). `exposure` is the --exposure build arg (default -0.4 EV).</summary>
    static void PostFx(GameObject camGo, float exposure, string scene)
    {
        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        var cg = profile.AddSettings<ColorGrading>();
        cg.gradingMode.Override(GradingMode.HighDefinitionRange); cg.tonemapper.Override(Tonemapper.ACES);
        cg.postExposure.Override(exposure); cg.contrast.Override(10f); cg.saturation.Override(scene == "ProtoForest" ? 8f : -5f);
        var ao = profile.AddSettings<AmbientOcclusion>();
        ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance); ao.intensity.Override(scene == "ProtoForest" ? 0.4f : 0.25f); ao.radius.Override(0.3f);
        if (scene == "ProtoForest") { var bloom = profile.AddSettings<Bloom>(); bloom.intensity.Override(0.2f); bloom.threshold.Override(1.3f); bloom.softKnee.Override(0.5f); }
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

    /// <summary>
    /// Crescent slash sprite drawn in code (Assets/Env/slash-arc.png): a 110° arc, thick in the middle and tapered to the tips, ivory core (#FFFDF2)
    /// over warm gold (#FFE4A3) with an amber rim (#F0AE55); alpha-blended particle material so it still reads on bright grass or snow.
    /// </summary>
    static Material SlashArcMaterial()
    {
        const string png = "Assets/Env/slash-arc.png";
        int S = 256; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color[S * S];
        Color core = Hex("FFFDF2"), body = Hex("FFE4A3"), rim = Hex("F0AE55");
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float u = (x + 0.5f) / S * 2f - 1f, v = (y + 0.5f) / S * 2f - 1f;
            float r = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u) * Mathf.Rad2Deg;              // arc centred on +X, spanning -55°..+55°
            float span = Mathf.Clamp01(1f - Mathf.Abs(ang) / 55f);                                        // 1 at the middle of the arc, 0 at the tips
            float width = 0.17f * Mathf.Sqrt(span), radial = Mathf.Abs(r - 0.78f) / Mathf.Max(width, 1e-3f);
            float a = span <= 0f ? 0f : Mathf.Clamp01(1.15f - radial);
            Color c = radial < 0.35f ? core : Color.Lerp(body, rim, Mathf.Clamp01((radial - 0.35f) / 0.65f));
            px[y * S + x] = new Color(c.r, c.g, c.b, a * a);
        }
        tex.SetPixels(px); File.WriteAllBytes(png, tex.EncodeToPNG()); AssetDatabase.ImportAsset(png);
        var ti = (TextureImporter)AssetImporter.GetAtPath(png); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = false; ti.SaveAndReimport();
        var m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(png) };
        AssetDatabase.DeleteAsset("Assets/Env/SlashArc.mat"); AssetDatabase.CreateAsset(m, "Assets/Env/SlashArc.mat");
        return m;
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
