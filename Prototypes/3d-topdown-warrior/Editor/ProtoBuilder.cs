using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batch entry (-executeMethod ProtoBuilder.Build): Frost Hollow 3D prototype, v4. Ground = the 2D map's 4 painted chunks (72x48 tiles, 1 tile = 0.8 m),
/// walkability from its tile rows, Tripo 3D props on the cliff edges, the Tripo Warrior with sword (right hand, trail at the tip) and shield (left hand),
/// 10 Quaternius GreenBlob slimes tinted ice blue. The basic attack is a code-authored 0.48 s slash on an upper-body layer (ProtoSlashClip.cs);
/// the look is the ChatGPT "Soft Winter" preset (ProtoLook.cs). Builds D:/temp/proto3d-build/Warrior3D.exe. Optional arg: --exposure -0.4
/// </summary>
public static partial class ProtoBuilder
{
    const float Tile = 0.8f;
    static string[] rows;

    public static void Build()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        rows = File.ReadAllLines("Assets/Env/frost-hollow-rows.txt").Where(r => r.Length > 0).ToArray();
        float mapW = rows[0].Length * Tile, mapH = rows.Length * Tile;
        foreach (var p in Directory.GetFiles("Assets", "*.png", SearchOption.AllDirectories)) ConfigureTexture(p.Replace('\\', '/'));

        var warrior = Rigged("Assets/Warrior/warrior.fbx", "Assets/Warrior/Warrior.controller", "idle", out var wc);
        AddSlashLayer(wc, warrior, "Assets/Warrior");
        var slime = Rigged("Assets/Monster/GreenBlob.fbx", "Assets/Monster/Slime.controller", "idle", out var sc);
        var wMat = Mat("Assets/Warrior/Warrior.mat", "Assets/Warrior/knight_character_3d_model_basecolor.png", "Assets/Warrior/knight_character_3d_model_normal.png", Color.white);
        var sMat = Mat("Assets/Monster/Slime.mat", null, null, new Color(0.62f, 0.86f, 1f)); sMat.SetFloat("_Glossiness", 0.75f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Ground(mapW, mapH);
        Lighting();
        var rowsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Env/frost-hollow-rows.txt");

        Vector3 start = FindWalkable(new Vector2(mapW * 0.42f, mapH * 0.45f));
        var hero = Spawn(warrior, wMat, wc, "Hero", start, 1.0f);
        var hc = hero.AddComponent<HeroController3D>();
        hc.IdleState = Clip(wc, "idle"); hc.WalkState = Clip(wc, "walk"); hc.RunState = Clip(wc, "run"); hc.HitState = Clip(wc, "hit"); hc.DefeatState = Clip(wc, "defeat");
        Hold(hero, "RightHand", "Assets/Props/sword/sword.fbx", 0.55f, true);
        Hold(hero, "LeftHand", "Assets/Props/shield/shield.fbx", 0.42f, false);
        hero.AddComponent<TileRowsLoader>().Rows = rowsAsset;
        TargetRing();

        var rnd = new System.Random(3);
        for (int i = 0; i < 10; i++)
        {
            Vector3 p = FindWalkable(new Vector2(start.x + (float)(rnd.NextDouble() * 16 - 8), start.z + (float)(rnd.NextDouble() * 12 - 4)));
            if (Vector3.Distance(p, start) < 2.5f) continue;
            var s = Spawn(slime, sMat, sc, "FrostSlime" + i, p, 0.45f);
            var sl = s.AddComponent<Slime3D>();
            sl.IdleState = Clip(sc, "idle"); sl.MoveState = Clip(sc, "walk", "jump", "run"); sl.AttackState = Clip(sc, "attack", "bite", "punch"); sl.HitState = Clip(sc, "hit", "damage"); sl.DeathState = Clip(sc, "death", "die");
            sl.Hero = hero.transform;
            var col = s.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.22f, 0f); col.radius = 0.3f; col.height = 0.45f;   // mouse targeting
            s.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        }
        Props(rnd, start);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.75f, 0.85f); cam.farClipPlane = 150f;
        cam.allowHDR = true;
        camGo.AddComponent<AudioListener>();
        var follow = camGo.AddComponent<CameraFollow3D>(); follow.Target = hero.transform; follow.Distance = 7f;
        PostFx(camGo, Arg("exposure", -0.4f));
        camGo.AddComponent<OcclusionFade>().Hero = hero.transform;
        var hud = new GameObject("HUD"); hud.AddComponent<Hud3D>(); hud.AddComponent<ProtoCapture>();
        HitFxSetup(hud);

        EditorSceneManager.SaveScene(scene, "Assets/Proto.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Proto.unity", true) };
        PlayerSettings.productName = "Warrior3D"; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.resizableWindow = true;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Proto.unity" }, locationPathName = "D:/temp/proto3d-build/Warrior3D.exe", target = BuildTarget.StandaloneWindows64 });
        Debug.Log("[proto] BUILD " + report.summary.result + " errors " + report.summary.totalErrors);
    }

    /// <summary>Spark material + the three warrior sounds copied from the main game (swing, hit, crit).</summary>
    static void HitFxSetup(GameObject host)
    {
        var fx = host.AddComponent<HitFx>();
        var spark = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { color = new Color(1f, 0.95f, 0.7f, 1f) };
        AssetDatabase.DeleteAsset("Assets/Env/Spark.mat"); AssetDatabase.CreateAsset(spark, "Assets/Env/Spark.mat");
        fx.SparkMat = spark;
        fx.Swing = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/swing.mp3");
        fx.Hit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/hit.mp3");
        fx.Crit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/crit.mp3");
    }

    /// <summary>Flat translucent yellow disc that HeroController3D parks under the current target.</summary>
    static void TargetRing()
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); ring.name = "TargetRing";
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.transform.localScale = new Vector3(0.7f, 0.01f, 0.7f);
        var m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { color = new Color(1f, 0.85f, 0.3f, 0.55f) };
        AssetDatabase.DeleteAsset("Assets/Env/Ring.mat"); AssetDatabase.CreateAsset(m, "Assets/Env/Ring.mat");
        var r = ring.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        ring.SetActive(false);
    }

    static GameObject Rigged(string fbx, string ctrlPath, string defaultKey, out AnimatorController ctrl)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        mi.animationType = ModelImporterAnimationType.Generic; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        var clips = mi.defaultClipAnimations;
        foreach (var c in clips) c.loopTime = Has(c.name, "idle", "walk", "run", "relax", "fly", "jump");
        mi.clipAnimations = clips; mi.SaveAndReimport();
        var anims = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
        Debug.Log("[proto] " + fbx + " clips: " + string.Join(", ", anims.Select(c => c.name + " " + c.length.ToString("0.00"))));
        AssetDatabase.DeleteAsset(ctrlPath);
        ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var c in anims) { var st = sm.AddState(c.name); st.motion = c; if (Has(c.name, defaultKey) && !Has(c.name, "relax")) sm.defaultState = st; }
        return AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
    }

    static string Clip(AnimatorController ctrl, params string[] keys)
    {
        var names = ctrl.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
        foreach (var k in keys) { var hit = names.Where(n => Has(n, k) && !Has(n, "relax")).OrderBy(n => n.Length).FirstOrDefault(); if (hit != null) return hit; }
        Debug.LogWarning("[proto] no clip for " + string.Join("/", keys) + " in " + string.Join(",", names));
        return names.FirstOrDefault() ?? "";
    }

    static Material Mat(string path, string tex, string normal, Color tint)
    {
        AssetDatabase.DeleteAsset(path);
        var m = new Material(Shader.Find("Standard")) { color = tint, mainTexture = tex == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(tex) };
        if (normal != null) { m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal)); m.EnableKeyword("_NORMALMAP"); }
        m.SetFloat("_Metallic", 0.1f); m.SetFloat("_Glossiness", 0.35f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static GameObject Spawn(GameObject model, Material mat, AnimatorController ctrl, string name, Vector3 at, float height)
    {
        var root = new GameObject(name); root.transform.position = at;
        var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
        body.transform.SetParent(root.transform, false);
        foreach (var r in body.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
        var anim = body.GetComponent<Animator>() ?? body.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        FitHeight(body, at, height);
        return root;
    }

    /// <summary>Scales a model to `height` metres and stands it on `at`.</summary>
    static void FitHeight(GameObject go, Vector3 at, float height)
    {
        Bounds b = Bounds(go);
        go.transform.localScale *= height / Mathf.Max(0.001f, b.size.y);
        b = Bounds(go);
        go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
    }

    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>Tripo props on cliff tiles that border snow: pines most, then rocks, crystals, a few runes.</summary>
    static void Props(System.Random rnd, Vector3 heroStart)
    {
        var kinds = new[] { ("pine", 2.1f, 0.10), ("rock", 0.9f, 0.04), ("crystal", 1.0f, 0.035), ("rune", 1.5f, 0.008) };
        var models = kinds.ToDictionary(k => k.Item1, k => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/" + k.Item1 + "/" + k.Item1 + ".fbx"));
        var mats = kinds.ToDictionary(k => k.Item1, k => FolderMat("Assets/Props/" + k.Item1 + "/" + k.Item1 + ".fbx"));
        var fades = kinds.ToDictionary(k => k.Item1, k => FadeTwin(mats[k.Item1], "Assets/Props/" + k.Item1 + "/MatFade.mat"));
        var parent = new GameObject("Props").transform;
        int placed = 0;
        for (int ty = 1; ty < rows.Length - 1; ty++)
            for (int tx = 1; tx < rows[ty].Length - 1; tx++)
            {
                if (rows[ty][tx] != 'C') continue;
                bool edge = rows[ty - 1][tx] == 's' || rows[ty + 1][tx] == 's' || rows[ty][tx - 1] == 's' || rows[ty][tx + 1] == 's';
                if (!edge) continue;
                double roll = rnd.NextDouble(), acc = 0;
                foreach (var (name, height, chance) in kinds)
                {
                    acc += chance; if (roll >= acc) continue;
                    var at = new Vector3((tx + 0.5f) * Tile, 0f, (rows.Length - 1 - ty + 0.5f) * Tile);
                    if (Vector3.Distance(at, heroStart) < 3f) break;   // keep the spawn clear so the hero is not born behind a pine
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(models[name]);
                    go.transform.SetParent(parent, true);
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mats[name], r.sharedMaterials.Length).ToArray();
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    FitHeight(go, at, height * (0.85f + (float)rnd.NextDouble() * 0.3f));
                    var fm = go.AddComponent<FadeMaterials>(); fm.Opaque = mats[name]; fm.Fade = fades[name];
                    placed++;
                    break;
                }
            }
        Debug.Log("[proto] props placed " + placed);
    }

    static Vector3 FindWalkable(Vector2 near)
    {
        for (int r = 0; r < 40; r++)
            for (int a = 0; a < 16; a++)
            {
                Vector3 p = new Vector3(near.x + Mathf.Cos(a * 0.39f) * r * 0.4f, 0f, near.y + Mathf.Sin(a * 0.39f) * r * 0.4f);
                int tx = Mathf.FloorToInt(p.x / Tile), ty = rows.Length - 1 - Mathf.FloorToInt(p.z / Tile);
                if (ty >= 0 && ty < rows.Length && tx >= 0 && tx < rows[ty].Length && rows[ty][tx] == 's') return p;
            }
        return new Vector3(near.x, 0f, near.y);
    }

    /// <summary>A Standard-shader "Fade" copy of `opaque` for props that must turn translucent when they block the camera.</summary>
    static Material FadeTwin(Material opaque, string path)
    {
        var m = new Material(opaque);
        m.SetFloat("_Mode", 2f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON"); m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static bool Has(string name, params string[] keys) { string n = name.ToLowerInvariant(); return keys.Any(k => n.Contains(k)); }
}
