using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Willowbrook Meadows, v9 (ChatGPT review of v8, docs/reference/3d-warrior-prototype/chatgpt-v8-vs-ro3-review.txt): trees and bushes of
/// alpha-cut leaf cards from the ChatGPT leaf atlas over a dark core (ProtoFoliageLeaves.cs; 3 kinds round / wide / tall spawned 40/35/25 %),
/// trunks with roots, grass and flower tufts (~150 per 100 m2, thicker along the road rim), pebbles on the road rim, warm-grey rock groups
/// near trees, a dark earth patch under every crown, nothing on the painted dirt road, a glossy sheet over the 'w' water.
/// </summary>
public static partial class ProtoBuilder
{
    static void ForestProps(System.Random rnd, Vector3 heroStart, string groundPrefix)
    {
        var parent = new GameObject("Props").transform;
        var coreMat = FoliageMat("FoliageCore", false, 0f, out var coreFade, "Assets/Env/FoliageCoreFade.mat");
        var leafMat = LeafMat("LeafTree", 0.02f, out var leafFade, "Assets/Env/LeafTreeFade.mat");
        var tuftMat = LeafMat("LeafTuft", 0.025f, out _);
        // 6 tree variants (2 per kind: 0 round, 1 wide, 2 tall), 6 bush variants, 4 tall / 2 short grass tufts, 2 flower tufts
        var treeLeaves = new Mesh[6]; var treeCore = new Mesh[6]; var bushLeaves = new Mesh[6]; var bushCore = new Mesh[6];
        for (int i = 0; i < 6; i++) { treeLeaves[i] = TreeLeavesMesh(i, i / 2); treeCore[i] = TreeCoreMesh(i, i / 2); bushLeaves[i] = BushLeavesMesh(i, i / 2, out bushCore[i]); }
        var tallGrass = new Mesh[4]; var shortGrass = new Mesh[2]; var flowers = new Mesh[2];
        for (int i = 0; i < 4; i++) tallGrass[i] = TuftMesh(i, 0);
        for (int i = 0; i < 2; i++) { shortGrass[i] = TuftMesh(i, 1); flowers[i] = TuftMesh(i, 2); }
        var shadowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Env/BlobShadow.mat");
        Material water = Flat("Water", new Color(0.4f, 0.78f, 0.77f, 0.55f), 0.9f); water.SetFloat("_Mode", 2f); SetFadeBlend(water);
        var rocks = new Mesh[4]; for (int i = 0; i < 4; i++) rocks[i] = RockMesh(i);   // code rocks in ChatGPT warm grey (the Tripo rock is a blue snow rock)
        var ground = new GroundPainting(groundPrefix, rows[0].Length * Tile, rows.Length * Tile);

        float Rand(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        float Yaw() => Rand(0f, 360f);
        int Kind() { double k = rnd.NextDouble(); return k < 0.40 ? 0 : k < 0.75 ? 1 : 2; }   // spawn ratio 40 / 35 / 25 %
        void PlaceTree(Vector3 p, float s)
        {
            int v = Kind() * 2 + rnd.Next(2); float yaw = Yaw();
            MeshProp(parent, "TreeCore", treeCore[v], coreMat, coreFade, p, s, yaw);
            MeshProp(parent, "TreeLeaves", treeLeaves[v], leafMat, leafFade, p, s, yaw);
            if (shadowMat != null) Decal(parent, p, 1.3f * s, shadowMat);   // dark earth under the crown, R ~0.55-0.8 m
        }
        void PlaceBush(Vector3 p, float s)
        {
            int v = Kind() * 2 + rnd.Next(2); float yaw = Yaw();
            MeshProp(parent, "BushCore", bushCore[v], coreMat, coreFade, p, s, yaw);
            MeshProp(parent, "BushLeaves", bushLeaves[v], leafMat, leafFade, p, s, yaw);
        }
        void PlaceRock(Vector3 p, float h, bool big) => MeshProp(parent, big ? "Rock" : "Pebble", rocks[rnd.Next(4)], coreMat, big ? coreFade : null, p, h / 0.5f, Yaw(), big);   // rock mesh is ~0.5 m tall
        void Tuft(Mesh m, Vector3 p) => MeshProp(parent, "Tuft", m, tuftMat, null, p, Rand(0.85f, 1.2f), Yaw(), false);
        bool NearPath(Vector3 p) => ground.IsPath(p + new Vector3(0.6f, 0f, 0f)) || ground.IsPath(p - new Vector3(0.6f, 0f, 0f)) || ground.IsPath(p + new Vector3(0f, 0f, 0.6f)) || ground.IsPath(p - new Vector3(0f, 0f, 0.6f));

        int treeCount = 0, grass = 0, pebbles = 0;
        for (int ty = 0; ty < rows.Length; ty++)
            for (int tx = 0; tx < rows[ty].Length; tx++)
            {
                char c = rows[ty][tx];
                var at = new Vector3((tx + 0.5f) * Tile, 0f, (rows.Length - 1 - ty + 0.5f) * Tile);
                if (Vector3.Distance(at, heroStart) < 2.5f) continue;
                Vector3 jitter = new Vector3(Rand(-0.5f, 0.5f), 0f, Rand(-0.5f, 0.5f)) * Tile * 0.8f;
                double roll = rnd.NextDouble();
                if (c == 'T')
                {
                    bool edge = Neighbour(tx, ty, 'g');
                    if (roll < (edge ? 0.5 : 0.18)) { PlaceTree(at + jitter, Rand(0.85f, 1.2f)); treeCount++; }
                    else if (edge && roll < 0.75) PlaceBush(at + jitter, Rand(0.85f, 1.25f));
                }
                else if (c == 'g')
                {
                    Vector3 p = at + jitter;
                    if (ground.IsPath(p))
                    {
                        // the painted road itself: only a few pebbles near its rim
                        if (NearPathRim(ground, p) && rnd.NextDouble() < 0.06) { PlaceRock(p, Rand(0.04f, 0.12f), false); pebbles++; }
                        continue;
                    }
                    bool nearTree = Neighbour(tx, ty, 'T'), edge = NearPath(p);
                    if (roll < 0.004) { PlaceTree(p, Rand(0.9f, 1.15f)); treeCount++; continue; }
                    if (roll < (nearTree ? 0.10 : 0.02)) { PlaceBush(p, Rand(0.85f, 1.25f)); continue; }
                    if (roll < 0.06 && (nearTree || rnd.NextDouble() < 0.4)) { PlaceRock(p, Rand(0.25f, 0.55f), true); for (int k = 0; k < 3; k++) PlaceRock(p + new Vector3(Rand(-0.6f, 0.6f), 0f, Rand(-0.6f, 0.6f)), Rand(0.08f, 0.2f), false); continue; }
                    if (roll < 0.11) { for (int k = 0; k < 3; k++) Tuft(flowers[rnd.Next(2)], p + new Vector3(Rand(-0.3f, 0.3f), 0f, Rand(-0.3f, 0.3f))); continue; }
                    // grass: ~1 tuft per open tile (≈150 / 100 m2), 3-4 along the road rim (8-14 per metre of road edge), pebbles on the rim
                    int n = edge ? 3 + rnd.Next(2) : (roll < 0.85 ? 1 : 2);
                    for (int k = 0; k < n; k++)
                    {
                        var g = at + new Vector3(Rand(-0.5f, 0.5f), 0f, Rand(-0.5f, 0.5f)) * Tile;
                        if (ground.IsPath(g)) continue;
                        Tuft(rnd.NextDouble() < 0.7 ? tallGrass[rnd.Next(4)] : shortGrass[rnd.Next(2)], g); grass++;
                    }
                    if (edge && rnd.NextDouble() < 0.15) { PlaceRock(p + new Vector3(Rand(-0.3f, 0.3f), 0f, Rand(-0.3f, 0.3f)), Rand(0.05f, 0.16f), false); pebbles++; }
                }
                else if (c == 'w')
                {
                    var q = Part(PrimitiveType.Quad, parent, at + Vector3.up * 0.015f, new Vector3(Tile * 1.02f, Tile * 1.02f, 1f), water);
                    q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }
        Debug.Log("[proto] forest trees " + treeCount + " grass " + grass + " pebbles " + pebbles);
    }

    /// <summary>True on the road within ~0.5 m of grass (where pebbles gather).</summary>
    static bool NearPathRim(GroundPainting g, Vector3 p) =>
        !g.IsPath(p + new Vector3(0.5f, 0f, 0f)) || !g.IsPath(p - new Vector3(0.5f, 0f, 0f)) || !g.IsPath(p + new Vector3(0f, 0f, 0.5f)) || !g.IsPath(p - new Vector3(0f, 0f, 0.5f));

    /// <summary>A flat soft-shadow quad lying on the ground; `at` is local to `parent` (Props root = world, hero / slime root = 0). `diameter` in metres.</summary>
    static void Decal(Transform parent, Vector3 at, float diameter, Material m)
    {
        var q = Part(PrimitiveType.Quad, parent, at + Vector3.up * 0.012f, new Vector3(diameter, diameter, 1f), m);
        q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var r = q.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
    }

    /// <summary>Reads the 2D map's ground painting (the same 2x2 chunks as Ground()) to tell the dirt road (#D6BD82, red over green) from grass.</summary>
    class GroundPainting
    {
        readonly Texture2D[,] chunk = new Texture2D[2, 2];
        readonly float w, h;
        public GroundPainting(string prefix, float mapW, float mapH)
        {
            w = mapW; h = mapH;
            for (int cy = 0; cy < 2; cy++) for (int cx = 0; cx < 2; cx++)
            {
                string png = prefix + "r" + (1 - cy) + "c" + cx + ".png";
                if (!File.Exists(png)) continue;
                var t = new Texture2D(2, 2); t.LoadImage(File.ReadAllBytes(png)); chunk[cx, cy] = t;
            }
        }
        public bool IsPath(Vector3 p)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt(p.x / (w / 2f)), 0, 1), cy = Mathf.Clamp(Mathf.FloorToInt(p.z / (h / 2f)), 0, 1);
            var t = chunk[cx, cy]; if (t == null) return false;
            float u = p.x / (w / 2f) - cx, v = p.z / (h / 2f) - cy;
            Color c = t.GetPixelBilinear(u, v);
            return c.r > c.g * 0.97f;
        }
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






    static void SetFadeBlend(Material m)
    {
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue = 3000;
    }
}
