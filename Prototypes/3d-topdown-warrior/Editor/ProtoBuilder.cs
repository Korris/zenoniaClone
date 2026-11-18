using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batch entry (-executeMethod ProtoBuilder.Build): Frost Hollow 3D prototype. Ground = the 2D map's 4 painted chunks (72x48 tiles, 1 tile = 0.8 m),
/// walkability from its tile rows, Tripo 3D props (pine, crystal, rock, rune) on the cliff edges, the Tripo Warrior holding the Tripo sword
/// (right hand) and shield (left hand), 10 Quaternius GreenBlob slimes tinted ice blue. Builds D:/temp/proto3d-build/Warrior3D.exe.
/// </summary>
public static class ProtoBuilder
{
    const float Tile = 0.8f;
    static string[] rows;

    public static void Build()
    {
        rows = File.ReadAllLines("Assets/Env/frost-hollow-rows.txt").Where(r => r.Length > 0).ToArray();
        float mapW = rows[0].Length * Tile, mapH = rows.Length * Tile;
        foreach (var p in Directory.GetFiles("Assets", "*.png", SearchOption.AllDirectories)) ConfigureTexture(p.Replace('\\', '/'));

        var warrior = Rigged("Assets/Warrior/warrior.fbx", "Assets/Warrior/Warrior.controller", "idle", out var wc);
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
        hc.IdleState = Clip(wc, "idle"); hc.WalkState = Clip(wc, "walk"); hc.RunState = Clip(wc, "run"); hc.SlashState = Clip(wc, "slash"); hc.ChopState = Clip(wc, "chop"); hc.HitState = Clip(wc, "hit"); hc.DefeatState = Clip(wc, "defeat");
        Hold(hero, "RightHand", "Assets/Props/sword/sword.fbx", 0.55f, true);
        Hold(hero, "LeftHand", "Assets/Props/shield/shield.fbx", 0.42f, false);
        hero.AddComponent<TileRowsLoader>().Rows = rowsAsset;

        var rnd = new System.Random(3);
        for (int i = 0; i < 10; i++)
        {
            Vector3 p = FindWalkable(new Vector2(start.x + (float)(rnd.NextDouble() * 16 - 8), start.z + (float)(rnd.NextDouble() * 12 - 4)));
            if (Vector3.Distance(p, start) < 2.5f) continue;
            var s = Spawn(slime, sMat, sc, "FrostSlime" + i, p, 0.55f);
            var sl = s.AddComponent<Slime3D>();
            sl.IdleState = Clip(sc, "idle"); sl.MoveState = Clip(sc, "walk", "jump", "run"); sl.AttackState = Clip(sc, "attack", "bite", "punch"); sl.HitState = Clip(sc, "hit", "damage"); sl.DeathState = Clip(sc, "death", "die");
            sl.Hero = hero.transform;
            s.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        }
        Props(rnd);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.75f, 0.85f); cam.farClipPlane = 150f;
        var follow = camGo.AddComponent<CameraFollow3D>(); follow.Target = hero.transform; follow.Distance = 9f;
        new GameObject("HUD").AddComponent<Hud3D>();

        EditorSceneManager.SaveScene(scene, "Assets/Proto.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Proto.unity", true) };
        PlayerSettings.productName = "Warrior3D"; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.resizableWindow = true;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Proto.unity" }, locationPathName = "D:/temp/proto3d-build/Warrior3D.exe", target = BuildTarget.StandaloneWindows64 });
        Debug.Log("[proto] BUILD " + report.summary.result + " errors " + report.summary.totalErrors);
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
        foreach (var c in anims) { var st = sm.AddState(c.name); st.motion = c; if (Has(c.name, "slash", "chop")) st.speed = 2.5f; if (Has(c.name, defaultKey) && !Has(c.name, "relax")) sm.defaultState = st; }
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

    /// <summary>A material from the Tripo export folder of `fbx` (its *BaseColor.png and *Normal_Bake.png).</summary>
    static Material FolderMat(string fbx)
    {
        string dir = Path.GetDirectoryName(fbx).Replace('\\', '/');
        string bc = Directory.GetFiles(dir, "*BaseColor.png").FirstOrDefault(), nm = Directory.GetFiles(dir, "*Normal*.png").FirstOrDefault();
        return Mat(dir + "/Mat.mat", bc?.Replace('\\', '/'), nm?.Replace('\\', '/'), Color.white);
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

    /// <summary>
    /// Puts a static Tripo item in a hand bone. Sword: the long axis is found from the mesh, the crossguard is the widest slab, the grip sits between it and
    /// the nearer end, and the blade is turned to point forward-down from the hand in the bind pose. Shield: thinnest axis = face normal, turned out to the left-front.
    /// </summary>
    static void Hold(GameObject hero, string boneSuffix, string fbx, float size, bool sword)
    {
        var bone = hero.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.EndsWith(boneSuffix));
        if (bone == null) { Debug.LogError("[proto] no bone " + boneSuffix); return; }
        var item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        item.name = sword ? "Sword" : "Shield";
        var mat = FolderMat(fbx);
        foreach (var r in item.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
        var mf = item.GetComponentInChildren<MeshFilter>();
        var verts = mf.sharedMesh.vertices.Select(v => mf.transform.localToWorldMatrix.MultiplyPoint3x4(v) - item.transform.position).ToArray();
        Vector3 min = verts.Aggregate(Vector3.positiveInfinity, Vector3.Min), max = verts.Aggregate(Vector3.negativeInfinity, Vector3.Max), ext = max - min;
        int longAx = ext.x >= ext.y && ext.x >= ext.z ? 0 : ext.y >= ext.z ? 1 : 2, thinAx = ext.x <= ext.y && ext.x <= ext.z ? 0 : ext.y <= ext.z ? 1 : 2;
        Vector3 L = Vector3.zero; L[longAx] = 1f; Vector3 T = Vector3.zero; T[thinAx] = 1f;
        float scale = size / ext[longAx];
        item.transform.localScale *= scale;
        Vector3 grip, bladeDir;
        if (sword)
        {
            int n = 24; var width = new float[n];
            foreach (var v in verts) { int i = Mathf.Clamp((int)((v[longAx] - min[longAx]) / ext[longAx] * n), 0, n - 1); Vector3 q = v; q[longAx] = 0; width[i] = Mathf.Max(width[i], q.magnitude); }
            int guard = 0; for (int i = 1; i < n; i++) if (width[i] > width[guard]) guard = i;
            bool hiltLow = guard < n / 2;
            float guardPos = min[longAx] + (guard + 0.5f) / n * ext[longAx], endPos = hiltLow ? min[longAx] : max[longAx];
            grip = (min + max) * 0.5f; grip[longAx] = (guardPos + endPos) * 0.5f;
            bladeDir = hiltLow ? L : -L;
        }
        else { grip = (min + max) * 0.5f; bladeDir = T; }
        Vector3 fwd = hero.transform.forward;
        Quaternion rot = sword ? Quaternion.FromToRotation(bladeDir, (fwd * 0.85f + Vector3.down * 0.5f).normalized)
                               : Quaternion.FromToRotation(bladeDir, (fwd * 0.6f - hero.transform.right * 0.8f).normalized);
        item.transform.rotation = rot;
        item.transform.position = bone.position - rot * (grip * scale) + (sword ? Vector3.zero : -hero.transform.right * 0.05f);
        item.transform.SetParent(bone, true);
    }

    static void Ground(float w, float h)
    {
        var chunks = new[] { ("r0c0", 0, 1), ("r0c1", 1, 1), ("r1c0", 0, 0), ("r1c1", 1, 0) };
        foreach (var (name, cx, cy) in chunks)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Ground_" + name;
            q.transform.position = new Vector3((cx + 0.5f) * w / 2f, 0f, (cy + 0.5f) * h / 2f);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f); q.transform.localScale = new Vector3(w / 2f, h / 2f, 1f);
            var m = Mat("Assets/Env/Ground_" + name + ".mat", "Assets/Env/frost-hollow-large-ground-" + name + ".png", null, Color.white);
            m.SetFloat("_Glossiness", 0.05f);
            q.GetComponent<Renderer>().sharedMaterial = m;
        }
    }

    static void Lighting()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.1f; sun.color = new Color(1f, 0.94f, 0.84f);
        sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.6f; sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.85f, 0.95f); RenderSettings.ambientEquatorColor = new Color(0.62f, 0.66f, 0.72f); RenderSettings.ambientGroundColor = new Color(0.4f, 0.42f, 0.46f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(0.62f, 0.75f, 0.85f); RenderSettings.fogStartDistance = 22f; RenderSettings.fogEndDistance = 55f;
        QualitySettings.shadowDistance = 40f;
    }

    /// <summary>Tripo props on cliff tiles that border snow: pines most, then rocks, crystals, a few runes.</summary>
    static void Props(System.Random rnd)
    {
        var kinds = new[] { ("pine", 2.3f, 0.10), ("rock", 0.9f, 0.04), ("crystal", 1.0f, 0.035), ("rune", 1.5f, 0.008) };
        var models = kinds.ToDictionary(k => k.Item1, k => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/" + k.Item1 + "/" + k.Item1 + ".fbx"));
        var mats = kinds.ToDictionary(k => k.Item1, k => FolderMat("Assets/Props/" + k.Item1 + "/" + k.Item1 + ".fbx"));
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
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(models[name]);
                    go.transform.SetParent(parent, true);
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mats[name], r.sharedMaterials.Length).ToArray();
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    FitHeight(go, at, height * (0.85f + (float)rnd.NextDouble() * 0.3f));
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

    static bool Has(string name, params string[] keys) { string n = name.ToLowerInvariant(); return keys.Any(k => n.Contains(k)); }
}
