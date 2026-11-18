using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batch entry (-executeMethod ProtoBuilder.Build): the 3D top-down Warrior prototype, v5. Two maps built from the 2D game's painted ground chunks
/// (72x48 tiles, 1 tile = 0.8 m) and tile rows: Frost Hollow (Tripo snow props) and Willowbrook forest (code-built trees, bushes, grass, flowers:
/// ProtoForest.cs). Same Tripo Warrior with sword + shield, Leafdrop slimes (ChatGPT + Tripo), Ragnarok-style single-slash auto attack (ProtoSlashClip.cs),
/// Soft Winter / Prontera lighting (ProtoLook.cs). A Boot scene picks the map from `--map frost|forest` (default forest).
/// Builds <project>/../proto3d-build/Warrior3D.exe (D:/temp/proto3d-build with the default project path). Optional build arg: --exposure -0.4
/// </summary>
public static partial class ProtoBuilder
{
    const float Tile = 0.8f;
    static string[] rows;

    class MapDef { public string Scene, Rows, Ground; public bool Forest, Glade; public Vector2 Start; }

    public static void Build()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        builtMats.Clear();
        // png + jpg: Tripo exports its maps as .jpg, and an un-flagged normal map lit those models wrong (black rocks, v11 test)
        foreach (var p in Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories).Where(f => f.EndsWith(".png") || f.EndsWith(".jpg"))) ConfigureTexture(p.Replace('\\', '/'));
        if (Directory.Exists("Assets/Resources/ProtoHud")) ConfigureHudTextures();   // ChatGPT HUD pieces: uncompressed, no mips (ProtoSkillLook.cs)
        var maps = new[]
        {
            new MapDef { Scene = "Proto", Rows = "Assets/Env/frost-hollow-rows.txt", Ground = "Assets/Env/frost-hollow-large-ground-", Forest = false, Start = new Vector2(0.42f, 0.45f) },
            new MapDef { Scene = "ProtoForest", Rows = "Assets/Env/willowbrook-rows.txt", Ground = "Assets/Env/willowbrook-large-ground-", Forest = true, Start = new Vector2(0.12f, 0.5f) },
        };
        foreach (var m in maps) BuildScene(m);
        // Willowbrook Glade: the real-3D forest (direction A), tile grid generated from its layout (ProtoGlade.cs)
        BuildScene(new MapDef { Scene = "ProtoGlade", Rows = GladeRows("Assets/Env/glade-rows.txt"), Glade = true, Start = new Vector2(GladeCentre.x / GladeSize, GladeCentre.y / GladeSize) });

        var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Boot").AddComponent<MapBoot>();
        EditorSceneManager.SaveScene(boot, "Assets/Boot.unity");

        var scenes = new[] { "Assets/Boot.unity", "Assets/Proto.unity", "Assets/ProtoForest.unity", "Assets/ProtoGlade.unity" };
        EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
        PlayerSettings.productName = "Warrior3D"; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900; PlayerSettings.resizableWindow = true;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = System.IO.Path.GetFullPath("../proto3d-build/Warrior3D.exe"), target = BuildTarget.StandaloneWindows64 });
        Debug.Log("[proto] BUILD " + report.summary.result + " errors " + report.summary.totalErrors);
    }

    static void BuildScene(MapDef map)
    {
        rows = File.ReadAllLines(map.Rows).Where(r => r.Length > 0).ToArray();
        float mapW = rows[0].Length * Tile, mapH = rows.Length * Tile;

        var warrior = Rigged("Assets/Warrior/warrior.fbx", "Assets/Warrior/Warrior.controller", "idle", out var wc);
        AddSlashLayer(wc, warrior, "Assets/Warrior");
        // Leafdrop Slime: ChatGPT design -> Tripo static model (retopo ~9.5k tris), animated by squash in Slime3D
        var slime = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Monster/leafdrop/leafdrop.fbx");
        var wMat = Mat("Assets/Warrior/Warrior.mat", "Assets/Warrior/knight_character_3d_model_basecolor.png", "Assets/Warrior/knight_character_3d_model_normal.png", Color.white);
        wMat.shader = Shader.Find("Proto/HeroRim"); wMat.SetFloat("_RimAmount", 0.12f);   // v18: rim 8-12 % (ChatGPT)
        var sMat = Mat("Assets/Monster/Slime.mat", "Assets/Monster/leafdrop/leafdrop_basecolor.jpg", "Assets/Monster/leafdrop/leafdrop_normal.jpg", Color.white);
        sMat.SetFloat("_Metallic", 0f); sMat.SetFloat("_Glossiness", 0.6f);   // soft gel shine, not glass (ChatGPT spec)

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (map.Glade) GladeTerrain();
        else Ground(mapW, mapH, map.Ground, map.Forest ? Hex("E2E8D6") : SnowTint, map.Forest);   // forest: a touch darker so the dirt road does not burn out (v6)
        var shadowMat = BlobShadowMaterial();
        if (map.Glade) LightingGlade(); else if (map.Forest) LightingForest(); else Lighting();
        var rowsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(map.Rows);

        Vector3 start = FindWalkable(new Vector2(mapW * map.Start.x, mapH * map.Start.y));
        // Warrior V2 (character plan v1) for the hero when present; party members keep V1 so both show in one shot (A/B, ChatGPT task 10)
        bool hasV2 = File.Exists("Assets/WarriorV2/warrior-v2.fbx");
        var heroModel = warrior; var heroMat = wMat; var hcCtrl = wc;
        if (hasV2)
        {
            heroModel = Rigged("Assets/WarriorV2/warrior-v2.fbx", "Assets/WarriorV2/WarriorV2.controller", "idle", out hcCtrl);
            AddSlashLayer(hcCtrl, heroModel, "Assets/WarriorV2");
            heroMat = Mat("Assets/WarriorV2/WarriorV2.mat", "Assets/WarriorV2/basecolor.png", "Assets/WarriorV2/normal.png", Color.white);
            heroMat.shader = Shader.Find("Proto/SoftToon");
            heroMat.SetTexture("_MetalMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/WarriorV2/metallic.png"));
        }
        var hero = Spawn(heroModel, heroMat, hcCtrl, "Hero", start, 1.0f);
        Decal(hero.transform, Vector3.zero, 0.7f, shadowMat);   // soft contact shadow, local to the moving root
        Decal(hero.transform, Vector3.zero, 0.42f, shadowMat);   // v19: darker core (+15-20 %)
        if (map.Glade) Decal(hero.transform, Vector3.zero, 1.15f, HeroRingMaterial());   // v17: faint warm ring so the Warrior reads at 22 m (ChatGPT)
        var hc = hero.AddComponent<HeroController3D>();
        AddLocomotion(hcCtrl, heroModel, 1.0f, hc.WalkSpeed, hc.RunSpeed);   // spawned at 1.0 m tall (Spawn above)
        hc.IdleState = Clip(hcCtrl, "idle"); hc.WalkState = Clip(hcCtrl, "walk"); hc.RunState = Clip(hcCtrl, "run"); hc.HitState = Clip(hcCtrl, "hit"); hc.DefeatState = Clip(hcCtrl, "defeat");
        Hold(hero, "RightHand", "Assets/Props/sword/sword.fbx", hasV2 ? 0.6f : 0.55f, true);      // V2: sword 60 % of hero height (ChatGPT 55-65 %)
        Hold(hero, "LeftHand", "Assets/Props/shield/shield.fbx", hasV2 ? 0.36f : 0.42f, false);  // V2: shield 36 % (ChatGPT 30-35 % tall)
        hero.AddComponent<TileRowsLoader>().Rows = rowsAsset;
        if (map.Glade)   // v18: one fake party member (ChatGPT: test the MMORPG feel)
        {
            // v19: 2 party members + 1 guard NPC by the bridge path (ChatGPT MMO density)
            var spots = new[] { (new Vector2(start.x + 1.8f, start.z - 0.6f), false, new Color(0.78f, 0.9f, 1.08f)), (new Vector2(start.x - 1.8f, start.z + 0.8f), false, new Color(1.08f, 0.92f, 0.82f)), (new Vector2(21.6f, 17.6f), true, new Color(0.95f, 1.05f, 0.85f)) };
            foreach (var (pos, guard, tint) in spots)
            {
                var allyGo = Spawn(warrior, wMat, wc, guard ? "GuardNPC" : "Ally", FindWalkable(pos), 1.0f);
                Decal(allyGo.transform, Vector3.zero, 0.7f, shadowMat); Decal(allyGo.transform, Vector3.zero, 0.42f, shadowMat);
                Hold(allyGo, "RightHand", "Assets/Props/sword/sword.fbx", 0.55f, true);
                Hold(allyGo, "LeftHand", "Assets/Props/shield/shield.fbx", 0.42f, false);
                var ally = allyGo.AddComponent<AllyBot3D>(); ally.Hero = hero.transform; ally.IdleState = Clip(wc, "idle"); ally.WalkState = Clip(wc, "walk"); ally.Guard = guard; ally.Tint = tint;
            }
        }
        TargetRing();
        TargetArrow();

        var rnd = new System.Random(3);
        for (int i = 0; i < (map.Glade ? 15 : 12); i++)   // v19: glade 15 slimes
        {
            Vector3 p = map.Glade ? FindWalkable(new Vector2(start.x + (float)(rnd.NextDouble() * 9 - 4.5), start.z + (float)(rnd.NextDouble() * 9 - 4.5)))
                                  : FindWalkable(new Vector2(start.x + (float)(rnd.NextDouble() * 16 - 6), start.z + (float)(rnd.NextDouble() * 12 - 6)));
            if (Vector3.Distance(p, start) < 2.5f) continue;
            var s = Spawn(slime, sMat, null, "LeafdropSlime" + i, p, 0.45f);
            Decal(s.transform, Vector3.zero, 0.75f, shadowMat);
            var sl = s.AddComponent<Slime3D>();
            sl.Hero = hero.transform;
            var col = s.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.22f, 0f); col.radius = 0.3f; col.height = 0.45f;   // mouse targeting
            s.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        }
        if (map.Glade) GladeProps(rnd, start); else if (map.Forest) ForestProps(rnd, start, map.Ground); else Props(rnd, start);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor; cam.farClipPlane = 150f;
        cam.allowHDR = true;
        camGo.AddComponent<AudioListener>();
        var follow = camGo.AddComponent<CameraFollow3D>(); follow.Target = hero.transform;
        follow.DefaultLevel = map.Glade ? 2 : 5;   // glade v11: 14.5 m / 52° / FOV 38 (ChatGPT); older maps keep 7 m / 50°
        PostFx(camGo, Arg("exposure", -0.4f), map.Scene);   // glade v11: -0.4 EV too (0 was washed out)   // forest v9: -0.4 EV (ChatGPT -0.35..-0.45; the dapple cookie already darkens ~35 % of the ground)
        camGo.AddComponent<OcclusionFade>().Hero = hero.transform;
        var hud = new GameObject("HUD"); hud.AddComponent<Hud3D>().MapName = map.Glade ? "Willowbrook Glade" : map.Forest ? "Willowbrook Meadows" : "Frost Hollow"; hud.AddComponent<ProtoCapture>();
        HitFxSetup(hud);
        SkillsSetup(hero, hud);   // HP/MP, skills 1-4, skill effects and the skill HUD (ProtoSkillLook.cs)

        EditorSceneManager.SaveScene(scene, "Assets/" + map.Scene + ".unity");
    }

    /// <summary>v18: small downward gold arrow above the selected target (positioned by HeroController3D).</summary>
    static void TargetArrow()
    {
        var a = GameObject.CreatePrimitive(PrimitiveType.Quad); a.name = "TargetArrow";
        Object.DestroyImmediate(a.GetComponent<Collider>()); a.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        var tex = CodeTexture("Assets/Env/target-arrow.png", 64, 64, (u, v) =>
        {
            float half = 0.42f * v;                                 // triangle pointing down: wide at the top
            float inside = Mathf.Clamp01((half - Mathf.Abs(u - 0.5f)) * 40f) * Mathf.Clamp01((v - 0.12f) * 40f) * Mathf.Clamp01((0.92f - v) * 40f);
            float edge = Mathf.Clamp01((half - Mathf.Abs(u - 0.5f) - 0.06f) * 40f);
            return new Color(Mathf.Lerp(0.55f, 1f, edge), Mathf.Lerp(0.33f, 0.88f, edge), Mathf.Lerp(0.1f, 0.35f, edge), inside);
        });
        var m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = tex };
        m.SetColor("_TintColor", new Color(0.62f, 0.6f, 0.55f, 0.55f));
        AssetDatabase.DeleteAsset("Assets/Env/TargetArrow.mat"); AssetDatabase.CreateAsset(m, "Assets/Env/TargetArrow.mat");
        var r = a.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // left active for GameObject.Find (hidden by HeroController3D.Start)
    }

    /// <summary>Spark / slash-arc materials + the three warrior sounds copied from the main game (swing, hit, crit).</summary>
    static void HitFxSetup(GameObject host)
    {
        var fx = host.AddComponent<HitFx>();
        fx.SparkMat = ImpactStarMaterial();
        fx.GroundRingMat = ParticleMat("Legacy Shaders/Particles/Alpha Blended", RingTexture("Assets/Env/impact-ring.png", new Color(1f, 0.93f, 0.75f), 0.78f, 0.16f), "Assets/Env/ImpactRing.mat");
        var arc = host.AddComponent<SlashArcFx>(); arc.BodyMat = SlashBodyMaterial(out arc.CoreMat);
        fx.Swing = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/swing.mp3");
        fx.Hit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/hit.mp3");
        fx.Crit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/crit.mp3");
    }

    /// <summary>Flat translucent yellow disc that HeroController3D parks under the current target.</summary>
    static void TargetRing()
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Quad); ring.name = "TargetRing";   // v18: 1.2 m soft gold ring (ChatGPT 1.1-1.3 m)
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        var m = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { mainTexture = RingTexture("Assets/Env/target-ring.png", new Color(1f, 0.86f, 0.35f), 0.82f, 0.11f) };
        m.SetColor("_TintColor", new Color(0.75f, 0.68f, 0.5f, 0.85f));   // v18: brighter so the 1.2 m ring reads at 12.5 m
        AssetDatabase.DeleteAsset("Assets/Env/Ring.mat"); AssetDatabase.CreateAsset(m, "Assets/Env/Ring.mat");
        var r = ring.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        // left active: GameObject.Find in HeroController3D.Start cannot see inactive objects (the ring never showed before v18); it hides it there
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
                    go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f) * models[name].transform.rotation;   // keep the FBX import rotation (Z-up files)
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
