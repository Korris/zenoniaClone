using UnityEditor;
using UnityEngine;

/// <summary>
/// Build-time setup for the Warrior skills and the skill HUD: code-drawn white effect textures (the colour comes from each skill's HDR tint,
/// see WarriorSkillDefs3D), their particle materials, the components on the hero / HUD objects, and the import settings of the ChatGPT HUD
/// pieces copied by setup-proto3d.ps1 into Assets/Resources/ProtoHud (uncompressed, no mipmaps, clamped: they are drawn 1:1 by OnGUI).
/// </summary>
public static partial class ProtoBuilder
{
    /// <summary>Adds HeroStats3D + WarriorSkills3D to the hero and SkillFx3D + SkillBar3D to the HUD object.</summary>
    static void SkillsSetup(GameObject hero, GameObject hud)
    {
        hero.AddComponent<HeroStats3D>();
        var ws = hero.AddComponent<WarriorSkills3D>();
        ws.SlashBodyMat = ParticleMat("Legacy Shaders/Particles/Alpha Blended", RibbonBodyTexture(), "Assets/Env/SkillRibbonBody.mat");
        ws.SlashCoreMat = ParticleMat("Legacy Shaders/Particles/Additive", CodeTexture("Assets/Env/skill-ribbon-core.png", 4, 64, (u, v) => new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.Abs(v - 0.82f) / 0.12f))), "Assets/Env/SkillRibbonCore.mat");
        ws.Swing = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/swing.mp3");
        ws.Hit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/hit.mp3");
        ws.Crit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/crit.mp3");

        var fx = hud.AddComponent<SkillFx3D>();
        const string Alpha = "Legacy Shaders/Particles/Alpha Blended", Add = "Legacy Shaders/Particles/Additive";
        fx.GlowMat = ParticleMat(Add, CodeTexture("Assets/Env/skill-glow.png", 64, 64, (u, v) => White(Mathf.Pow(Mathf.Clamp01(1f - R(u, v)), 2f))), "Assets/Env/SkillGlow.mat");
        fx.RingMat = ParticleMat(Alpha, CodeTexture("Assets/Env/skill-ring.png", 256, 256, RingPixel), "Assets/Env/SkillRing.mat");
        fx.WedgeMat = ParticleMat(Alpha, CodeTexture("Assets/Env/skill-wedge.png", 128, 64, WedgePixel), "Assets/Env/SkillWedge.mat");
        fx.StarMat = ParticleMat(Alpha, CodeTexture("Assets/Env/skill-star4.png", 128, 128, Star4Pixel), "Assets/Env/SkillStar.mat");
        fx.FlameMat = ParticleMat(Alpha, CodeTexture("Assets/Env/skill-flame.png", 64, 96, FlamePixel), "Assets/Env/SkillFlame.mat");
        fx.RockMat = ParticleMat(Alpha, CodeTexture("Assets/Env/skill-rock.png", 32, 32, RockPixel), "Assets/Env/SkillRock.mat");
        fx.LeafMat = ParticleMat(Alpha, CodeTexture("Assets/Env/skill-leaf.png", 32, 32, LeafPixel), "Assets/Env/SkillLeaf.mat");
        hud.AddComponent<SkillBar3D>();
    }

    static float R(float u, float v) { return Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f; }
    static Color White(float a) { return new Color(1f, 1f, 1f, Mathf.Clamp01(a)); }

    /// <summary>Ribbon cross-section, v = 0 inner edge to 1 outer edge: soft inside, solid body, crisp bright outer edge.</summary>
    static Texture2D RibbonBodyTexture()
    {
        return CodeTexture("Assets/Env/skill-ribbon-body.png", 4, 64, (u, v) =>
        {
            float a = Mathf.Clamp01(v / 0.3f) * Mathf.Clamp01((1f - v) / 0.05f);
            float lift = v > 0.8f ? 1f : 0.75f + 0.25f * v;   // brighter near the blade's outer path
            return new Color(lift, lift, lift, a);
        });
    }

    /// <summary>Ground fire ring: a solid band at r 0.78-0.95 with a ragged flame edge outside and a faint glow inside.</summary>
    static Color RingPixel(float u, float v)
    {
        float r = R(u, v), ang = Mathf.Atan2(v - 0.5f, u - 0.5f);
        float ragged = 0.94f + 0.04f * Mathf.Sin(ang * 23f) + 0.02f * Mathf.Sin(ang * 57f + 1.3f);
        float band = Mathf.Clamp01((r - 0.72f) / 0.08f) * Mathf.Clamp01((ragged - r) / 0.03f);
        float inner = Mathf.Clamp01(r / 0.8f) * 0.28f * (r < ragged ? 1f : 0f);
        float lift = 0.8f + 0.2f * Mathf.Clamp01((r - 0.75f) / 0.15f);
        return new Color(lift, lift, lift, Mathf.Max(band, inner));
    }

    /// <summary>Motion wedge, head at +X: thin tail widening to the head, faint at the tail, solid near the head with a rounded front.</summary>
    static Color WedgePixel(float u, float v)
    {
        float half = 0.08f + 0.42f * u, d = Mathf.Abs(v - 0.5f);
        float a = Mathf.Clamp01((half - d) / 0.06f) * Mathf.Pow(u, 1.4f) * Mathf.Clamp01((1f - u) / 0.08f);
        float lift = d < half * 0.35f ? 1f : 0.8f;
        return new Color(lift, lift, lift, a);
    }

    /// <summary>4-point star (hit flash): slim cross rays plus a soft round core.</summary>
    static Color Star4Pixel(float u, float v)
    {
        float x = Mathf.Abs(u - 0.5f) * 2f, y = Mathf.Abs(v - 0.5f) * 2f, r = R(u, v);
        float ray = Mathf.Max(Mathf.Clamp01(1f - y / Mathf.Max(0.02f, 0.16f * (1f - x))) * (1f - x), Mathf.Clamp01(1f - x / Mathf.Max(0.02f, 0.16f * (1f - y))) * (1f - y));
        float core = Mathf.Clamp01(1f - r / 0.35f);
        return White(Mathf.Max(ray, core * core) * 1.2f);
    }

    /// <summary>Flame tongue: round base at the bottom, wavy body thinning to a tip that fades out (v2: the solid v1 read as orange cones).</summary>
    static Color FlamePixel(float u, float v)
    {
        float w = 0.4f * Mathf.Pow(Mathf.Clamp01(1f - v), 0.55f) * Mathf.Clamp01(v / 0.12f + 0.45f);
        float x = Mathf.Abs(u - 0.5f + 0.07f * Mathf.Sin(v * 11f) * v);
        float body = Mathf.Clamp01((w - x) / 0.14f);
        float a = body * Mathf.Pow(Mathf.Clamp01(1f - v), 0.6f) * Mathf.Clamp01(v / 0.08f + 0.3f);
        float lift = 0.65f + 0.35f * Mathf.Clamp01(1f - x / Mathf.Max(0.01f, w)) * Mathf.Clamp01(1.2f - v);   // hot centre, darker rim
        return new Color(lift, lift, lift, a);
    }

    static Color RockPixel(float u, float v)
    {
        float ang = Mathf.Atan2(v - 0.5f, u - 0.5f), edge = 0.8f + 0.15f * Mathf.Sin(ang * 3f + 0.7f) + 0.05f * Mathf.Sin(ang * 7f);
        float shade = 0.7f + 0.3f * (v - u + 1f) * 0.5f;
        return new Color(shade, shade * 0.95f, shade * 0.9f, Mathf.Clamp01((edge - R(u, v)) / 0.08f));
    }

    static Color LeafPixel(float u, float v)
    {
        float x = (u - 0.5f) * 2f, y = (v - 0.5f) * 2f, e = x * x / 0.95f + y * y / 0.25f;
        float vein = Mathf.Abs(y) < 0.06f ? 0.85f : 1f;
        return new Color(vein, vein, vein, Mathf.Clamp01((1f - e) / 0.15f));
    }

    /// <summary>ChatGPT HUD pieces in Assets/Resources/ProtoHud: drawn 1:1 by OnGUI, so no compression (keeps the thin silver rims) and no mips.</summary>
    static void ConfigureHudTextures()
    {
        foreach (var p in System.IO.Directory.GetFiles("Assets/Resources/ProtoHud", "*.png"))
        {
            var ti = AssetImporter.GetAtPath(p.Replace('\\', '/')) as TextureImporter;
            if (ti == null) continue;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true; ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
        }
    }
}
