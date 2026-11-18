using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Willowbrook Meadows as a Ragnarok-Origin-style green map, built from code only (no Blender): round layered-canopy trees on the 'T' tiles,
/// bushes, cross-quad grass clumps, flower patches and tinted Tripo rocks scattered over the 'g' tiles, a glossy sheet over the 'w' water.
/// Palette follows the ChatGPT "Prontera" table (grass #8FC65B/#6FA849/#4F803F, canopy #93C963/#579C4B/#367548, trunk #866345, rock #8C9A8D).
/// </summary>
public static partial class ProtoBuilder
{
    static void ForestProps(System.Random rnd, Vector3 heroStart)
    {
        var parent = new GameObject("Props").transform;
        Material trunk = Flat("Trunk", Hex("866345")), canopyL = Flat("CanopyLight", Hex("93C963")), canopyM = Flat("CanopyMid", Hex("579C4B")), canopyD = Flat("CanopyDark", Hex("367548"));
        Material trunkF = FadeTwin(trunk, "Assets/Env/TrunkFade.mat"), canopyLF = FadeTwin(canopyL, "Assets/Env/CanopyLightFade.mat"), canopyMF = FadeTwin(canopyM, "Assets/Env/CanopyMidFade.mat"), canopyDF = FadeTwin(canopyD, "Assets/Env/CanopyDarkFade.mat");
        Material bush = Flat("Bush", Hex("5FA34C")), bushF = FadeTwin(bush, "Assets/Env/BushFade.mat");
        Material water = Flat("Water", new Color(0.4f, 0.78f, 0.77f, 0.55f), 0.9f); water.SetFloat("_Mode", 2f); SetFadeBlend(water);
        var flowerMats = new[] { Flat("FlowerWhite", Hex("FFF6E0")), Flat("FlowerYellow", Hex("FFD95A")), Flat("FlowerPink", Hex("FF9FC2")) };
        var grassMats = new[] { GrassMat("GrassA", Hex("8FC65B")), GrassMat("GrassB", Hex("6FA849")), GrassMat("GrassC", Hex("7DBA52")) };
        var rock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/rock/rock.fbx");
        var rockMat = FolderMat("Assets/Props/rock/rock.fbx"); rockMat.color = Hex("B9C4B6"); var rockFade = FadeTwin(rockMat, "Assets/Props/rock/MatFadeForest.mat");

        int trees = 0, grass = 0;
        for (int ty = 0; ty < rows.Length; ty++)
            for (int tx = 0; tx < rows[ty].Length; tx++)
            {
                char c = rows[ty][tx];
                var at = new Vector3((tx + 0.5f) * Tile, 0f, (rows.Length - 1 - ty + 0.5f) * Tile);
                if (Vector3.Distance(at, heroStart) < 2.5f) continue;
                Vector3 jitter = new Vector3((float)rnd.NextDouble() - 0.5f, 0f, (float)rnd.NextDouble() - 0.5f) * Tile * 0.8f;
                double roll = rnd.NextDouble();
                if (c == 'T')
                {
                    bool edge = Neighbour(tx, ty, 'g');
                    if (roll < (edge ? 0.5 : 0.18)) { Tree(parent, at + jitter, 0.85f + (float)rnd.NextDouble() * 0.45f, rnd, trunk, trunkF, canopyL, canopyLF, canopyM, canopyMF, canopyD, canopyDF); trees++; }
                    else if (edge && roll < 0.75) Bush(parent, at + jitter, 0.7f + (float)rnd.NextDouble() * 0.5f, rnd, bush, bushF);
                }
                else if (c == 'g')
                {
                    bool nearTree = Neighbour(tx, ty, 'T');
                    if (roll < 0.004) { Tree(parent, at + jitter, 0.9f + (float)rnd.NextDouble() * 0.3f, rnd, trunk, trunkF, canopyL, canopyLF, canopyM, canopyMF, canopyD, canopyDF); trees++; }
                    else if (roll < (nearTree ? 0.10 : 0.02)) Bush(parent, at + jitter, 0.5f + (float)rnd.NextDouble() * 0.5f, rnd, bush, bushF);
                    else if (roll < 0.045) Flowers(parent, at + jitter, rnd, flowerMats[rnd.Next(flowerMats.Length)]);
                    else if (roll < 0.06) { var r = (GameObject)PrefabUtility.InstantiatePrefab(rock); r.transform.SetParent(parent, true); foreach (var rr in r.GetComponentsInChildren<Renderer>()) rr.sharedMaterial = rockMat; r.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f); FitHeight(r, at + jitter, 0.35f + (float)rnd.NextDouble() * 0.45f); var fm = r.AddComponent<FadeMaterials>(); fm.Opaque = rockMat; fm.Fade = rockFade; }
                    else if (roll < 0.40) { GrassClump(parent, at + jitter, 0.3f + (float)rnd.NextDouble() * 0.25f, rnd, grassMats[rnd.Next(grassMats.Length)]); grass++; }
                }
                else if (c == 'w')
                {
                    var q = Part(PrimitiveType.Quad, parent, at + Vector3.up * 0.015f, new Vector3(Tile * 1.02f, Tile * 1.02f, 1f), water);
                    q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }
        Debug.Log("[proto] forest trees " + trees + " grass " + grass);
    }

    static bool Neighbour(int tx, int ty, char want)
    {
        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
        {
            int x = tx + dx, y = ty + dy;
            if (y >= 0 && y < rows.Length && x >= 0 && x < rows[y].Length && rows[y][x] == want) return true;
        }
        return false;
    }

    /// <summary>Round tree ~3 m tall at scale 1: tapered trunk + dark under-layer, mid main canopy with two side lobes, light top cap.</summary>
    static void Tree(Transform parent, Vector3 at, float s, System.Random rnd, Material trunk, Material trunkF, Material cL, Material cLF, Material cM, Material cMF, Material cD, Material cDF)
    {
        var root = new GameObject("Tree").transform; root.SetParent(parent, false); root.position = at; root.localScale = Vector3.one * s;
        root.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.75f, 0f), new Vector3(0.28f, 0.75f, 0.28f), trunk, trunkF);
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 1.55f, 0f), new Vector3(1.9f, 1.1f, 1.9f), cD, cDF);
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 2.05f, 0f), new Vector3(2.0f, 1.7f, 2.0f), cM, cMF);
        Part(PrimitiveType.Sphere, root, new Vector3(0.6f, 1.85f, 0.35f), new Vector3(1.2f, 1.0f, 1.2f), cM, cMF);
        Part(PrimitiveType.Sphere, root, new Vector3(-0.55f, 1.95f, -0.3f), new Vector3(1.1f, 0.95f, 1.1f), cM, cMF);
        Part(PrimitiveType.Sphere, root, new Vector3(0.1f, 2.65f, -0.05f), new Vector3(1.25f, 0.9f, 1.25f), cL, cLF);
    }

    static void Bush(Transform parent, Vector3 at, float s, System.Random rnd, Material m, Material mF)
    {
        var root = new GameObject("Bush").transform; root.SetParent(parent, false); root.position = at; root.localScale = Vector3.one * s;
        root.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.6f, 0.9f), m, mF);
        Part(PrimitiveType.Sphere, root, new Vector3(0.35f, 0.22f, 0.15f), new Vector3(0.6f, 0.45f, 0.6f), m, mF);
        Part(PrimitiveType.Sphere, root, new Vector3(-0.3f, 0.2f, -0.2f), new Vector3(0.55f, 0.4f, 0.55f), m, mF);
    }

    /// <summary>Two crossed, double-sided quads with the generated grass-blade texture.</summary>
    static void GrassClump(Transform parent, Vector3 at, float h, System.Random rnd, Material m)
    {
        var root = new GameObject("Grass").transform; root.SetParent(parent, false); root.position = at;
        root.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 180f, 0f);
        for (int i = 0; i < 4; i++)
        {
            var q = Part(PrimitiveType.Quad, root, new Vector3(0f, h * 0.5f, 0f), new Vector3(h * 1.4f, h, 1f), m);
            q.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
        }
    }

    static void Flowers(Transform parent, Vector3 at, System.Random rnd, Material m)
    {
        var root = new GameObject("Flowers").transform; root.SetParent(parent, false); root.position = at;
        int n = 4 + rnd.Next(4);
        for (int i = 0; i < n; i++)
        {
            var p = new Vector3((float)rnd.NextDouble() - 0.5f, 0f, (float)rnd.NextDouble() - 0.5f) * 0.7f;
            Part(PrimitiveType.Sphere, root, p + Vector3.up * 0.1f, Vector3.one * (0.08f + (float)rnd.NextDouble() * 0.05f), m);
        }
    }

    /// <summary>Cutout grass material over a code-drawn blade texture (Assets/Env/grass-blades.png, written once).</summary>
    static Material GrassMat(string name, Color tint)
    {
        const string png = "Assets/Env/grass-blades.png";
        if (!File.Exists(png))
        {
            int S = 128; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color32[S * S];
            var r = new System.Random(7);
            for (int b = 0; b < 14; b++)
            {
                float x0 = (float)r.NextDouble() * S, top = S * (0.55f + 0.45f * (float)r.NextDouble()), lean = ((float)r.NextDouble() - 0.5f) * 0.5f;
                for (int y = 0; y < top; y++) { float w = 3.5f * (1f - y / top) + 0.6f; float cx = x0 + lean * y; for (int x = (int)(cx - w); x <= cx + w; x++) if (x >= 0 && x < S) { byte g = (byte)(150 + 90 * y / top); px[y * S + x] = new Color32(g, g, g, 255); } }
            }
            tex.SetPixels32(px); File.WriteAllBytes(png, tex.EncodeToPNG()); AssetDatabase.ImportAsset(png);
            var ti = (TextureImporter)AssetImporter.GetAtPath(png); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
        }
        var m = Mat("Assets/Env/" + name + ".mat", png, null, tint);
        m.SetFloat("_Mode", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; m.SetFloat("_Glossiness", 0.1f);
        return m;
    }

    static void SetFadeBlend(Material m)
    {
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue = 3000;
    }
}
