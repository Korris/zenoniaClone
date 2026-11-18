using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batch entry (-executeMethod ProtoBuilder.Build): the 3D top-down Warrior prototype, v5. Two maps built from the 2D game's painted ground chunks
/// (72x48 tiles, 1 tile = 0.8 m) and tile rows: Frost Hollow (Tripo snow props) and Willowbrook forest (code-built trees, bushes, grass, flowers:
/// ProtoForest.cs). Same Tripo Warrior with sword + shield, Quaternius slimes, Ragnarok-style single-slash auto attack (ProtoSlashClip.cs),
/// Soft Winter / Prontera lighting (ProtoLook.cs). A Boot scene picks the map from `--map frost|forest` (default forest).
/// Builds D:/temp/proto3d-build/Warrior3D.exe. Optional build arg: --exposure -0.4
/// </summary>
public static partial class ProtoBuilder
{
    const float Tile = 0.8f;
    static string[] rows;

    class MapDef { public string Scene, Rows, Ground; public bool Forest; public Vector2 Start; }

    public static void Build()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        foreach (var p in Directory.GetFiles("Assets", "*.png", SearchOption.AllDirectories)) ConfigureTexture(p.Replace('\\', '/'));
        var maps = new[]
        {
            new MapDef { Scene = "Proto", Rows = "Assets/Env/frost-hollow-rows.txt", Ground = "Assets/Env/frost-hollow-large-ground-", Forest = false, Start = new Vector2(0.42f, 0.45f) },
            new MapDef { Scene = "ProtoForest", Rows = "Assets/Env/willowbrook-rows.txt", Ground = "Assets/Env/willowbrook-large-ground-", Forest = true, Start = new Vector2(0.12f, 0.5f) },
        };
        foreach (var m in maps) BuildScene(m);

        var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Boot").AddComponent<MapBoot>();
        EditorSceneManager.SaveScene(boot, "Assets/Boot.unity");

        var scenes = new[] { "Assets/Boot.unity", "Assets/Proto.unity", "Assets/ProtoForest.unity" };
        EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
        PlayerSettings.productName = "Warrior3D"; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.resizableWindow = true;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = "D:/temp/proto3d-build/Warrior3D.exe", target = BuildTarget.StandaloneWindows64 });
        Debug.Log("[proto] BUILD " + report.summary.result + " errors " + report.summary.totalErrors);
    }

    static void BuildScene(MapDef map)
    {
        rows = File.ReadAllLines(map.Rows).Where(r => r.Length > 0).ToArray();
        float mapW = rows[0].Length * Tile, mapH = rows.Length * Tile;

        var warrior = Rigged("Assets/Warrior/warrior.fbx", "Assets/Warrior/Warrior.controller", "idle", out var wc);
        AddSlashLayer(wc, warrior, "Assets/Warrior");
        var slime = Rigged("Assets/Monster/GreenBlob.fbx", "Assets/Monster/Slime.controller", "idle", out var sc);
        var wMat = Mat("Assets/Warrior/Warrior.mat", "Assets/Warrior/knight_character_3d_model_basecolor.png", "Assets/Warrior/knight_character_3d_model_normal.png", Color.white);
        var sMat = Mat("Assets/Monster/Slime.mat", null, null, new Color(0.62f, 0.86f, 1f)); sMat.SetFloat("_Glossiness", 0.75f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Ground(mapW, mapH, map.Ground, map.Forest ? Hex("EAF2DC") : SnowTint);
        if (map.Forest) LightingForest(); else Lighting();
        var rowsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(map.Rows);

        Vector3 start = FindWalkable(new Vector2(mapW * map.Start.x, mapH * map.Start.y));
        var hero = Spawn(warrior, wMat, wc, "Hero", start, 1.0f);
        var hc = hero.AddComponent<HeroController3D>();
        hc.IdleState = Clip(wc, "idle"); hc.WalkState = Clip(wc, "walk"); hc.RunState = Clip(wc, "run"); hc.HitState = Clip(wc, "hit"); hc.DefeatState = Clip(wc, "defeat");
        Hold(hero, "RightHand", "Assets/Props/sword/sword.fbx", 0.55f, true);
        Hold(hero, "LeftHand", "Assets/Props/shield/shield.fbx", 0.42f, false);
        hero.AddComponent<TileRowsLoader>().Rows = rowsAsset;
        TargetRing();

        var rnd = new System.Random(3);
        for (int i = 0; i < 12; i++)
        {
            Vector3 p = FindWalkable(new Vector2(start.x + (float)(rnd.NextDouble() * 16 - 6), start.z + (float)(rnd.NextDouble() * 12 - 6)));
            if (Vector3.Distance(p, start) < 2.5f) continue;
            var s = Spawn(slime, sMat, sc, "FrostSlime" + i, p, 0.45f);
            var sl = s.AddComponent<Slime3D>();
            sl.IdleState = Clip(sc, "idle"); sl.MoveState = Clip(sc, "walk", "jump", "run"); sl.AttackState = Clip(sc, "attack", "bite", "punch"); sl.HitState = Clip(sc, "hit", "damage"); sl.DeathState = Clip(sc, "death", "die");
            sl.Hero = hero.transform;
            var col = s.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.22f, 0f); col.radius = 0.3f; col.height = 0.45f;   // mouse targeting
            s.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        }
        if (map.Forest) ForestProps(rnd, start); else Props(rnd, start);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor; cam.farClipPlane = 150f;
        cam.allowHDR = true;
        camGo.AddComponent<AudioListener>();
        var follow = camGo.AddComponent<CameraFollow3D>(); follow.Target = hero.transform; follow.Distance = 7f;
        PostFx(camGo, Arg("exposure", -0.4f), map.Scene);
        camGo.AddComponent<OcclusionFade>().Hero = hero.transform;
        var hud = new GameObject("HUD"); hud.AddComponent<Hud3D>().MapName = map.Forest ? "Willowbrook Meadows" : "Frost Hollow"; hud.AddComponent<ProtoCapture>();
        HitFxSetup(hud);

        EditorSceneManager.SaveScene(scene, "Assets/" + map.Scene + ".unity");
    }

    /// <summary>Spark / slash-arc materials + the three warrior sounds copied from the main game (swing, hit, crit).</summary>
    static void HitFxSetup(GameObject host)
    {
        var fx = host.AddComponent<HitFx>();
        var spark = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { color = new Color(1f, 0.95f, 0.7f, 1f) };
        AssetDatabase.DeleteAsset("Assets/Env/Spark.mat"); AssetDatabase.CreateAsset(spark, "Assets/Env/Spark.mat");
        fx.SparkMat = spark;
        fx.ArcMat = SlashArcMaterial();
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
                if (rows[ty][tx] != 'C' || !Neighbour(tx, ty, 's')) continue;
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

    static bool WalkableChar(char c) { return c == 's' || c == 'g' || c == 'B'; }

    static Vector3 FindWalkable(Vector2 near)
    {
        for (int r = 0; r < 40; r++)
            for (int a = 0; a < 16; a++)
            {
                Vector3 p = new Vector3(near.x + Mathf.Cos(a * 0.39f) * r * 0.4f, 0f, near.y + Mathf.Sin(a * 0.39f) * r * 0.4f);
                int tx = Mathf.FloorToInt(p.x / Tile), ty = rows.Length - 1 - Mathf.FloorToInt(p.z / Tile);
                if (ty >= 0 && ty < rows.Length && tx >= 0 && tx < rows[ty].Length && WalkableChar(rows[ty][tx]) && rows[ty][tx] != 'B') return p;
            }
        return new Vector3(near.x, 0f, near.y);
    }
}
