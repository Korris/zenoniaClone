using UnityEditor;
using UnityEngine;

/// <summary>
/// v9 foliage (ChatGPT review of v8: "real leaves"): every crown, bush and grass tuft is built from alpha-cut cards textured with the ChatGPT
/// leaf atlas (docs/reference/3d-warrior-prototype/foliage-3d-chatgpt/leaf-atlas-4x4.png: rows 0-1 tree clusters, row 2 bush clusters,
/// row 3 = tall grass, tall grass, short grass, white flowers). Card normals point out of the crown so the wrap light shades it like a volume;
/// a dark olive core of lumps fills the gaps between cards. Each kind has a few variants saved as mesh assets and shared by all instances.
/// </summary>
public static partial class ProtoBuilder
{
    const string LeafAtlas = "Assets/Env/leaf-atlas-4x4.png";

    /// <summary>One card (two triangles) showing atlas cell `cell` (0 = top-left, row-major), centred at `c`, facing `n`, rolled `roll` rad.</summary>
    static void LeafCard(MeshBuf b, Vector3 c, Vector3 n, Vector3 shadeNormal, float size, int cell, float roll, Color tint, float sway)
    {
        Vector3 t = Vector3.Cross(Mathf.Abs(n.y) > 0.95f ? Vector3.forward : Vector3.up, n).normalized, bt = Vector3.Cross(n, t);
        Vector3 x = (t * Mathf.Cos(roll) + bt * Mathf.Sin(roll)) * size * 0.5f, y = (bt * Mathf.Cos(roll) - t * Mathf.Sin(roll)) * size * 0.5f;
        float u0 = (cell % 4) * 0.25f, v0 = 1f - (cell / 4 + 1) * 0.25f;
        int i = b.V.Count;
        b.V.Add(c - x - y); b.V.Add(c + x - y); b.V.Add(c + x + y); b.V.Add(c - x + y);
        b.UV.Add(new Vector2(u0, v0)); b.UV.Add(new Vector2(u0 + 0.25f, v0)); b.UV.Add(new Vector2(u0 + 0.25f, v0 + 0.25f)); b.UV.Add(new Vector2(u0, v0 + 0.25f));
        for (int k = 0; k < 4; k++) { b.N.Add(shadeNormal); Color col = tint; col.a = k < 2 ? sway * 0.4f : sway; b.C.Add(col); }
        b.T.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
    }

    /// <summary>Per-cluster tint: shade from bottom (0) to top (1) of the crown, ±6° hue, ±10 % brightness, ~15 % less saturated than the atlas.</summary>
    static Color LeafTint(System.Random rnd, float height01)
    {
        Color.RGBToHSV(Color.Lerp(new Color(0.58f, 0.66f, 0.56f), new Color(0.98f, 1f, 0.94f), height01), out float h, out float s, out float v);
        h = Mathf.Repeat(h + ((float)rnd.NextDouble() - 0.5f) * (12f / 360f), 1f); v *= 0.9f + (float)rnd.NextDouble() * 0.2f; s *= 0.85f;
        return Color.HSVToRGB(h, s, v);
    }

    /// <summary>Leaf crown of a tree variant: kind 0 round, 1 wide, 2 tall (ChatGPT A/B/C). 24-34 clusters x 4 cards (~100-140 cards), cards 0.38-0.62 m.</summary>
    static Mesh TreeLeavesMesh(int variant, int kind)
    {
        var rnd = new System.Random(500 + variant); var b = new MeshBuf();
        Vector3 crown = new Vector3(0f, kind == 2 ? 2.6f : 2.3f, 0f);
        Vector3 radii = kind == 1 ? new Vector3(1.6f, 0.85f, 1.45f) : kind == 2 ? new Vector3(1.05f, 1.3f, 1.05f) : new Vector3(1.3f, 1.0f, 1.3f);
        int clusters = 24 + rnd.Next(11);
        for (int c = 0; c < clusters; c++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, up = -0.45f + (float)rnd.NextDouble() * 1.45f, depth = 0.78f + (float)rnd.NextDouble() * 0.27f;
            Vector3 dir = new Vector3(Mathf.Cos(a), up, Mathf.Sin(a)).normalized, center = crown + Vector3.Scale(dir, radii) * depth;
            Color tint = LeafTint(rnd, Mathf.InverseLerp(crown.y - radii.y, crown.y + radii.y, center.y));
            for (int k = 0; k < 4; k++)
            {
                Vector3 p = center + Random3(rnd) * 0.18f, n = (dir + Random3(rnd) * 0.6f + Vector3.up * 0.35f).normalized;
                LeafCard(b, p, n, (dir + Vector3.up * 0.3f).normalized, 0.38f + (float)rnd.NextDouble() * 0.24f, rnd.Next(8), (float)rnd.NextDouble() * 6.28f, tint, 0.25f);
            }
        }
        return b.Save("Assets/Env/Meshes/tree-leaves" + variant + ".asset");
    }

    /// <summary>Trunk with 3-5 roots + dark olive core lumps (shrunk inside the cards) for the same tree variant.</summary>
    static Mesh TreeCoreMesh(int variant, int kind)
    {
        var rnd = new System.Random(600 + variant); var b = new MeshBuf();
        Color bark = Hex("866345").linear, dark = Hex("385D3A").linear, mid = Hex("4E7444").linear;
        float trunkTop = kind == 2 ? 1.9f : 1.6f;
        Trunk(b, 0f, trunkTop, 0.21f, 0.14f, bark);
        int roots = 3 + rnd.Next(3);
        for (int r = 0; r < roots; r++)   // a root = a squashed lump leaning out from the trunk foot, 0.3-0.8 m long
        {
            float a = (r + (float)rnd.NextDouble() * 0.5f) / roots * Mathf.PI * 2f, len = 0.3f + (float)rnd.NextDouble() * 0.5f;
            Lump(b, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (0.12f + len * 0.5f) + Vector3.up * 0.03f, new Vector3(len * 0.5f, 0.07f, 0.1f), 1, rnd, 0.1f, bark * 0.6f, bark * 0.8f, bark, 0f, 0.2f, 0.05f);
        }
        Vector3 crown = new Vector3(0f, kind == 2 ? 2.6f : 2.3f, 0f);
        Vector3 radii = kind == 1 ? new Vector3(1.6f, 0.85f, 1.45f) : kind == 2 ? new Vector3(1.05f, 1.3f, 1.05f) : new Vector3(1.3f, 1.0f, 1.3f);
        for (int i = 0; i < 5; i++)
        {
            Vector3 off = new Vector3(((float)rnd.NextDouble() - 0.5f) * radii.x * 0.6f, ((float)rnd.NextDouble() - 0.4f) * radii.y * 0.5f, ((float)rnd.NextDouble() - 0.5f) * radii.z * 0.6f);
            Lump(b, crown + off, radii * (0.45f + (float)rnd.NextDouble() * 0.1f), 2, rnd, 0.08f, dark * 0.8f, dark, mid, crown.y - radii.y, crown.y + radii.y);
        }
        return b.Save("Assets/Env/Meshes/tree-core" + variant + ".asset");
    }

    /// <summary>Bush cards: 12-20 clusters x 3 cards (36-60), 0.20-0.34 m, row-2 atlas cells; kind 0 round low, 1 wide, 2 lopsided.</summary>
    static Mesh BushLeavesMesh(int variant, int kind, out Mesh core)
    {
        var rnd = new System.Random(700 + variant); var b = new MeshBuf(); var cb = new MeshBuf();
        Vector3 radii = kind == 1 ? new Vector3(0.75f, 0.32f, 0.5f) : new Vector3(0.5f, 0.34f, 0.5f), centre = new Vector3(kind == 2 ? 0.12f : 0f, 0.3f, 0f);
        int clusters = 16 + rnd.Next(9);   // v9 test: 12-20 left the dark core showing
        for (int c = 0; c < clusters; c++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, up = -0.2f + (float)rnd.NextDouble() * 1.2f;
            Vector3 dir = new Vector3(Mathf.Cos(a), up, Mathf.Sin(a)).normalized, p0 = centre + Vector3.Scale(dir, radii) * (0.85f + (float)rnd.NextDouble() * 0.2f);
            if (kind == 2 && dir.x > 0f) p0.x += 0.15f;   // lopsided tail on one side
            Color tint = LeafTint(rnd, Mathf.InverseLerp(0f, 0.7f, p0.y));
            for (int k = 0; k < 3; k++)
                LeafCard(b, p0 + Random3(rnd) * 0.1f, (dir + Random3(rnd) * 0.5f + Vector3.up * 0.4f).normalized, (dir + Vector3.up * 0.4f).normalized,
                         0.2f + (float)rnd.NextDouble() * 0.14f, 8 + rnd.Next(4), (float)rnd.NextDouble() * 6.28f, tint, 0.35f);
        }
        Color dark = Hex("466D42").linear, mid = Hex("587F4A").linear;
        for (int i = 0; i < 4; i++)
            Lump(cb, centre + new Vector3(((float)rnd.NextDouble() - 0.5f) * radii.x * 0.6f, -0.05f, ((float)rnd.NextDouble() - 0.5f) * radii.z * 0.6f), radii * 0.42f, 2, rnd, 0.1f, dark * 0.8f, dark, mid, 0f, 0.6f);
        core = cb.Save("Assets/Env/Meshes/bush-core" + variant + ".asset");
        return b.Save("Assets/Env/Meshes/bush-leaves" + variant + ".asset");
    }

    /// <summary>Grass / flower tuft: two crossed cards of one row-3 cell. kind 0 tall (0.18-0.30 m), 1 short (0.08-0.16 m), 2 flowers (0.16-0.24 m).</summary>
    static Mesh TuftMesh(int variant, int kind)
    {
        var rnd = new System.Random(800 + variant); var b = new MeshBuf();
        float h = kind == 0 ? 0.18f + (float)rnd.NextDouble() * 0.12f : kind == 1 ? 0.1f + (float)rnd.NextDouble() * 0.06f : 0.16f + (float)rnd.NextDouble() * 0.08f;
        int cell = kind == 0 ? 12 + rnd.Next(2) : kind == 1 ? 14 : 15;
        Color tint = kind == 2 ? Color.white : Color.HSVToRGB(0.25f, 0.42f, 0.86f + (float)rnd.NextDouble() * 0.1f);
        float yaw = (float)rnd.NextDouble() * Mathf.PI;
        for (int k = 0; k < 2; k++)
        {
            float a = yaw + k * Mathf.PI * 0.5f;
            LeafCard(b, Vector3.up * h * 0.5f, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), Vector3.up, h * 1.05f, cell, 0f, tint, 1f);
        }
        return b.Save("Assets/Env/Meshes/tuft" + kind + "-" + variant + ".asset");
    }

    /// <summary>Warm-grey rock ~0.5 m tall (ChatGPT v9: #939184, lit face #B3B09E): 2-3 lumpy, flattened lumps sunk a little into the ground.</summary>
    static Mesh RockMesh(int variant)
    {
        var rnd = new System.Random(900 + variant); var b = new MeshBuf();
        Color dark = Hex("6E6C62").linear, mid = Hex("939184").linear, light = Hex("B3B09E").linear;
        int lumps = 2 + rnd.Next(2);
        for (int i = 0; i < lumps; i++)
        {
            float w = 0.32f + (float)rnd.NextDouble() * 0.14f;
            Vector3 c = new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.3f, 0.16f + (float)rnd.NextDouble() * 0.06f, ((float)rnd.NextDouble() - 0.5f) * 0.3f);
            Lump(b, c, new Vector3(w, 0.26f + (float)rnd.NextDouble() * 0.06f, w * 0.85f) * (i == 0 ? 1f : 0.7f), 2, rnd, 0.26f, dark, mid, light, -0.05f, 0.5f, 0.05f);
        }
        return b.Save("Assets/Env/Meshes/rock" + variant + ".asset");
    }

    /// <summary>Proto/Leaf material over the atlas (+ the Proto/LeafFade twin when `fadePath` is set). Atlas mips preserve alpha coverage.</summary>
    static Material LeafMat(string name, float sway, out Material fade, string fadePath = null)
    {
        var ti = AssetImporter.GetAtPath(LeafAtlas) as TextureImporter;
        if (ti != null) { ti.alphaIsTransparency = true; ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.45f; ti.wrapMode = TextureWrapMode.Clamp; ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(LeafAtlas);
        var m = new Material(Shader.Find("Proto/Leaf")) { mainTexture = tex, enableInstancing = true };
        m.SetFloat("_Sway", sway);
        AssetDatabase.DeleteAsset("Assets/Env/" + name + ".mat"); AssetDatabase.CreateAsset(m, "Assets/Env/" + name + ".mat");
        fade = null;
        if (fadePath != null)
        {
            fade = new Material(Shader.Find("Proto/LeafFade")) { mainTexture = tex };
            AssetDatabase.DeleteAsset(fadePath); AssetDatabase.CreateAsset(fade, fadePath);
        }
        return m;
    }
}
