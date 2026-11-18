using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Look of the Frost Hollow prototype (v4 "Soft Winter" preset from ChatGPT): linear colour space, one soft sun at 0.8, cool trilight ambient,
/// snow albedo pulled down to #D8E5ED so it stops burning out, and Post Processing v2 on the camera (ACES tone mapping, slight negative
/// exposure, a little contrast, ambient occlusion, FXAA). Bloom stays off while the lighting is being judged.
/// </summary>
public static partial class ProtoBuilder
{
    static readonly Color SnowTint = Hex("D8E5ED"), SunColor = Hex("FFF0DE");
    const int PostFxLayer = 8;

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

    static void Ground(float w, float h)
    {
        var chunks = new[] { ("r0c0", 0, 1), ("r0c1", 1, 1), ("r1c0", 0, 0), ("r1c1", 1, 0) };
        foreach (var (name, cx, cy) in chunks)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Ground_" + name;
            q.transform.position = new Vector3((cx + 0.5f) * w / 2f, 0f, (cy + 0.5f) * h / 2f);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f); q.transform.localScale = new Vector3(w / 2f, h / 2f, 1f);
            var m = Mat("Assets/Env/Ground_" + name + ".mat", "Assets/Env/frost-hollow-large-ground-" + name + ".png", null, SnowTint);
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

    /// <summary>ACES + exposure + AO + FXAA through Post Processing v2 (Built-in RP). `exposure` is the --exposure build arg (default -0.4 EV).</summary>
    static void PostFx(GameObject camGo, float exposure)
    {
        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        var cg = profile.AddSettings<ColorGrading>();
        cg.gradingMode.Override(GradingMode.HighDefinitionRange); cg.tonemapper.Override(Tonemapper.ACES);
        cg.postExposure.Override(exposure); cg.contrast.Override(10f); cg.saturation.Override(-5f);
        var ao = profile.AddSettings<AmbientOcclusion>();
        ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance); ao.intensity.Override(0.25f); ao.radius.Override(0.3f);
        AssetDatabase.DeleteAsset("Assets/Env/PostFx.asset");
        AssetDatabase.CreateAsset(profile, "Assets/Env/PostFx.asset");

        var volGo = new GameObject("PostFxVolume") { layer = PostFxLayer };
        var vol = volGo.AddComponent<PostProcessVolume>(); vol.isGlobal = true; vol.sharedProfile = profile;
        var layer = camGo.AddComponent<PostProcessLayer>();
        layer.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        layer.volumeLayer = 1 << PostFxLayer; layer.volumeTrigger = camGo.transform;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
    }

    static void ConfigureTexture(string p)
    {
        var ti = AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti == null) return;
        bool ground = p.Contains("frost-hollow-large-ground");
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
