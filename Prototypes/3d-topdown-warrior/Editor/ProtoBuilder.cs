using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batch entry (-executeMethod ProtoBuilder.Build): imports the Tripo Warrior FBX (Generic rig, looping idle / walk / run), makes its material and
/// Animator, lays the Sunken Reliquary painting as a 3D ground (1 tile = 0.8 m, hero ~1 m tall), puts the Mire Hydra Queen billboard on its
/// Wanted tile (30,14) with 20 Warrior "players" around it, saves the scene and builds D:/temp/proto3d-build/Warrior3D.exe.
/// </summary>
public static class ProtoBuilder
{
    const string Fbx = "Assets/Warrior/warrior.fbx";
    const float Tile = 0.8f, MapW = 36 * Tile, MapH = 24 * Tile;

    public static void Build()
    {
        ConfigureTextures();
        var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.importAnimation = true;
        var clips = mi.defaultClipAnimations;
        foreach (var c in clips) c.loopTime = Has(c.name, "idle", "walk", "run", "relax");
        mi.clipAnimations = clips;
        mi.SaveAndReimport();

        var anims = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
        Debug.Log("[proto] clips: " + string.Join(", ", anims.Select(c => c.name + " " + c.length.ToString("0.00") + "s")));
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath("Assets/Warrior/Warrior.controller");
        var sm = ctrl.layers[0].stateMachine;
        foreach (var c in anims) { var st = sm.AddState(c.name); st.motion = c; if (Has(c.name, "slash", "chop")) st.speed = 2.5f; if (Has(c.name, "idle") && !Has(c.name, "relax")) sm.defaultState = st; }
        string idle = Pick(anims, "idle"), walk = Pick(anims, "walk"), run = Pick(anims, "run"), slash = Pick(anims, "slash"), chop = Pick(anims, "chop"), hit = Pick(anims, "hit"), defeat = Pick(anims, "defeat");

        var mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Warrior/knight_character_3d_model_basecolor.png");
        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Warrior/knight_character_3d_model_normal.png"));
        mat.EnableKeyword("_NORMALMAP");
        mat.SetFloat("_Metallic", 0.15f); mat.SetFloat("_Glossiness", 0.35f);
        AssetDatabase.CreateAsset(mat, "Assets/Warrior/Warrior.mat");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildEnvironment();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        var bossPos = new Vector3((30 + 0.5f) * Tile, 0f, (24 - 14 - 0.5f) * Tile);
        var boss = new GameObject("MireHydraQueen");
        boss.transform.position = bossPos;
        var bb = boss.AddComponent<BossBillboard>();
        bb.Sheet = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Env/mireHydraQueen.png");

        var hero = MakeWarrior(model, mat, ctrl, "Hero", bossPos + new Vector3(0f, 0f, -3.2f));
        var hc = hero.AddComponent<HeroController3D>();
        hc.IdleState = idle; hc.WalkState = walk; hc.RunState = run; hc.SlashState = slash; hc.ChopState = chop; hc.HitState = hit; hc.DefeatState = defeat;
        hc.BoundsMin = new Vector2(0.4f, 0.4f); hc.BoundsMax = new Vector2(MapW - 0.4f, MapH - 0.4f); hc.Target = boss.transform;
        hero.transform.rotation = Quaternion.LookRotation(Vector3.forward);

        var rnd = new System.Random(7);
        for (int i = 0; i < 20; i++)
        {
            float a = i / 20f * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.2f, r = 2.1f + (float)rnd.NextDouble() * 1.4f;
            Vector3 p = bossPos + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r * 0.8f);
            if (Vector3.Distance(p, hero.transform.position) < 0.9f) continue;
            p.x = Mathf.Min(p.x, MapW - 0.5f);
            var bot = MakeWarrior(model, mat, ctrl, "Player" + i, p);
            var cb = bot.AddComponent<CrowdBot>();
            cb.IdleState = idle; cb.WalkState = walk; cb.SlashState = slash; cb.ChopState = chop; cb.HitState = hit; cb.Boss = boss.transform;
        }

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 32f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f, 0.28f, 0.3f); cam.farClipPlane = 120f;
        camGo.AddComponent<CameraFollow3D>().Target = hero.transform;
        new GameObject("HUD").AddComponent<Hud3D>();

        EditorSceneManager.SaveScene(scene, "Assets/Proto.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Proto.unity", true) };
        PlayerSettings.productName = "Warrior3D";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.resizableWindow = true;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Proto.unity" }, locationPathName = "D:/temp/proto3d-build/Warrior3D.exe", target = BuildTarget.StandaloneWindows64 });
        Debug.Log("[proto] BUILD " + report.summary.result + " errors " + report.summary.totalErrors);
    }

    static GameObject MakeWarrior(GameObject model, Material mat, AnimatorController ctrl, string name, Vector3 at)
    {
        var root = new GameObject(name);
        root.transform.position = at;
        var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
        body.transform.SetParent(root.transform, false);
        foreach (var r in body.GetComponentsInChildren<Renderer>()) { r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
        var anim = body.GetComponent<Animator>() ?? body.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // normalise to 1 m tall, feet on the ground
        var smr = body.GetComponentsInChildren<SkinnedMeshRenderer>();
        Bounds b = smr[0].bounds; foreach (var s in smr) b.Encapsulate(s.bounds);
        float k = 1.0f / Mathf.Max(0.001f, b.size.y);
        body.transform.localScale *= k;
        b = smr[0].bounds; foreach (var s in smr) b.Encapsulate(s.bounds);
        body.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        return root;
    }

    static void BuildEnvironment()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ground.name = "Ground_SunkenReliquary";
        ground.transform.position = new Vector3(MapW / 2f, 0f, MapH / 2f);
        ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ground.transform.localScale = new Vector3(MapW, MapH, 1f);
        var gm = new Material(Shader.Find("Standard")) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Env/sunken-reliquary-ground.png") };
        gm.SetFloat("_Glossiness", 0.05f);
        AssetDatabase.CreateAsset(gm, "Assets/Env/Ground.mat");
        ground.GetComponent<Renderer>().sharedMaterial = gm;

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.15f; sun.color = new Color(1f, 0.96f, 0.88f);
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.55f;
        sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.75f, 0.8f, 0.9f); RenderSettings.ambientEquatorColor = new Color(0.6f, 0.62f, 0.6f); RenderSettings.ambientGroundColor = new Color(0.35f, 0.33f, 0.3f);
        QualitySettings.shadowDistance = 45f;
    }

    static void ConfigureTextures()
    {
        foreach (string p in new[] { "Assets/Warrior/knight_character_3d_model_basecolor.png", "Assets/Warrior/knight_character_3d_model_normal.png", "Assets/Env/sunken-reliquary-ground.png", "Assets/Env/mireHydraQueen.png" })
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            if (ti == null) { Debug.LogError("[proto] missing " + p); continue; }
            ti.maxTextureSize = p.Contains("ground") ? 4096 : 2048;
            if (p.Contains("normal")) ti.textureType = TextureImporterType.NormalMap;
            if (p.Contains("Env/")) { ti.wrapMode = TextureWrapMode.Clamp; ti.anisoLevel = 8; ti.alphaIsTransparency = true; }
            ti.SaveAndReimport();
        }
    }

    static bool Has(string name, params string[] keys) { string n = name.ToLowerInvariant(); return keys.Any(k => n.Contains(k)); }

    /// <summary>The clip whose name contains `key` (idle prefers the plain idle over standing_relax).</summary>
    static string Pick(List<AnimationClip> clips, string key)
    {
        var hits = clips.Where(c => Has(c.name, key)).OrderBy(c => c.name.Length).ToList();
        if (hits.Count == 0) { Debug.LogError("[proto] no clip for " + key); return clips.Count > 0 ? clips[0].name : ""; }
        return hits[0].name;
    }
}
