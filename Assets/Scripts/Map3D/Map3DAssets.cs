using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Textures and shared materials of the 3D map prototype (Map3D). Textures: Resources/Map3D/*.png, made by Tools/build-map-3d-textures.py from
    /// ChatGPT paintings (Assets/ArtLibrary/maps/&lt;map&gt;/map3d/: leaf / needle atlases, bark strips, rock) and the 2.5D ground swatches (grass, dirt).
    /// One material per look, shared by every instance (GPU instancing on), so the forest draws in few batches. Colours: ChatGPT spec sections 1.3, 2.
    /// </summary>
    public static class Map3DAssets
    {
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        static Texture2D noise;

        /// <summary>Asset folder key of a map (Resources/Map3D/&lt;key&gt;-splat.bytes, ArtLibrary/maps/&lt;key&gt;).</summary>
        public static string Key(IGameMap map)
        {
            return map.Id == WillowbrookMeadowsMap.MapId ? "willowbrook-meadows" : map.Id;
        }

        public static Texture2D Tex(string name)
        {
            var t = Resources.Load<Texture2D>("Map3D/" + name);
            if (t == null) { Debug.LogWarning("[ZenoniaProto] Map3D texture missing: Resources/Map3D/" + name + ".png"); return Texture2D.whiteTexture; }
            t.wrapMode = TextureWrapMode.Repeat;
            t.anisoLevel = 4;
            return t;
        }

        /// <summary>Copy of a foliage material without its ShadowCaster pass (22/11 perf pass): leaf cards / grass that never cast drop out of the shadow
        /// maps AND of the camera depth pre-pass the screen-space sun shadow needs (built-in forward draws it with the ShadowCaster pass), where the
        /// shader clipped them anyway. Other materials come back unchanged.</summary>
        public static Material NoShadowPass(Material m)
        {
            if (m == null || !m.shader.name.Contains("Foliage")) return m;
            return Get(m.name + "_nocast", () =>
            {
                var c = new Material(m);
                c.SetShaderPassEnabled("ShadowCaster", false);
                return c;
            });
        }

        /// <summary>`--map3d-test noprepass`: foliage without the LEAFDEPTH depth pre-pass (the lit pass clips and tests LEqual again), for A/B FPS runs.</summary>
        static void NoLeafPrepass(Material m)
        {
            m.SetShaderPassEnabled("Always", false);
            m.SetFloat("_LeafZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            m.EnableKeyword("M3D_LEAF_NOPREPASS");
        }

        static Shader Find(string name)
        {
            Shader s = Resources.Load<Shader>("Shaders/" + name);
            if (s == null || !s.isSupported) { Debug.LogWarning("[ZenoniaProto] Map3D shader missing / unsupported: " + name); return Shader.Find("Standard"); }
            return s;
        }

        static Material Get(string key, System.Func<Material> make)
        {
            Material m;
            if (Mats.TryGetValue(key, out m) && m != null) return m;
            m = make();
            m.name = "Map3D_" + key;
            m.hideFlags = HideFlags.DontSave;
            m.enableInstancing = true;
            Mats[key] = m;
            return m;
        }

        static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f); }

        /// <summary>Tileable value noise (r: 4 cells, g: 9 cells, b: 17 cells across), for macro variation, edge breakup and water ripples.</summary>
        public static Texture2D Noise
        {
            get
            {
                if (noise != null) return noise;
                const int N = 256;
                var px = new Color32[N * N];
                int[] cells = { 4, 9, 17 };
                var lat = new float[3][];
                var rng = new System.Random(1234);
                for (int c = 0; c < 3; c++) { lat[c] = new float[cells[c] * cells[c]]; for (int k = 0; k < lat[c].Length; k++) lat[c][k] = (float)rng.NextDouble(); }
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        var v = new byte[3];
                        for (int c = 0; c < 3; c++)
                        {
                            int n = cells[c];
                            float u = x * n / (float)N, w = y * n / (float)N;
                            int x0 = (int)u, y0 = (int)w;
                            float fx = u - x0, fy = w - y0;
                            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
                            float[] L = lat[c];
                            float a = L[(y0 % n) * n + x0 % n], b = L[(y0 % n) * n + (x0 + 1) % n], d = L[((y0 + 1) % n) * n + x0 % n], e = L[((y0 + 1) % n) * n + (x0 + 1) % n];
                            v[c] = (byte)(Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(d, e, fx), fy) * 255f);
                        }
                        px[y * N + x] = new Color32(v[0], v[1], v[2], 255);
                    }
                noise = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { name = "Map3DNoise", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                noise.SetPixels32(px);
                noise.Apply(true, true);
                return noise;
            }
        }

        public static Material Terrain(Map3DHeightField f)
        {
            Material m = Get("terrain", () =>
            {
                var t = new Material(Find("Map3DTerrain"));
                t.SetTexture("_Grass", Tex("ground-grass"));
                t.SetTexture("_Dirt", Tex("ground-dirt"));
                t.SetTexture("_Rock", Tex("rock"));
                Texture2D pave = Resources.Load<Texture2D>("Map3D/ground-paving");
                t.SetTexture("_Paving", pave != null ? Tex("ground-paving") : Tex("rock"));
                t.SetTexture("_Noise", Noise);
                t.SetColor("_GrassTint", new Color(0.92f, 0.86f, 1.10f));   // swatch mean (135,183,68) -> the 2.5D olive #83A65C (less lime)
                t.SetColor("_ForestTint", new Color(0.73f, 0.75f, 0.84f));  // forest floor #607D4D over the grass
                t.SetColor("_DirtTint", new Color(0.89f, 1.00f, 1.53f));    // -> #CDA477 (round 2)    // orange swatch (230,164,78) -> warm beige road #D3B17B
                t.SetColor("_RockTint", new Color(1.02f, 1.10f, 1.20f));
                t.SetColor("_BedColor", Hex(0x4E6A5A));
                return t;
            });
            m.SetTexture("_Control", f.Control);
            float a = Map3DHeightField.Apron * Map3D.M;
            m.SetVector("_CtrlRect", new Vector4(-a, -a, 1f / (GameConfig.MapW + 2f * a), 1f / (GameConfig.MapH + 2f * a)));
            return m;
        }

        /// <summary>Leaf cards of a look: "oak", "oakYoung", "birch", "willow", "pine", "bush", "bushLilac", "bushWhite".</summary>
        public static Material Leaves(string look)
        {
            return Get("leaves_" + look, () =>
            {
                bool pine = look == "pine", grass = look == "grass";
                var m = new Material(Find("Map3DFoliage"));
                m.SetTexture("_MainTex", Tex(pine ? "leaf-pine" : grass ? "leaf-grass" : "leaf-oak"));
                Color t1 = Color.white, t2 = Color.white;
                switch (look)
                {
                    case "oak": t1 = new Color(0.82f, 0.88f, 0.78f); t2 = new Color(0.90f, 0.93f, 0.72f); break;           // highlights toward #A9C45F, not yellow (round 2)          // a few warmer yellow-green clusters
                    case "oakYoung": t1 = new Color(0.90f, 0.96f, 0.80f); t2 = new Color(0.95f, 0.95f, 0.70f); break;
                    case "birch": t1 = new Color(1.0f, 1.0f, 0.70f); t2 = new Color(1.0f, 0.93f, 0.55f); break;
                    case "willow": t1 = new Color(0.85f, 0.95f, 0.74f); t2 = new Color(0.95f, 1.0f, 0.78f); break;
                    case "pine": t1 = new Color(0.82f, 0.86f, 0.78f); t2 = new Color(0.92f, 0.95f, 0.80f); break;
                    case "bush": t1 = new Color(0.78f, 0.86f, 0.72f); t2 = new Color(0.92f, 0.95f, 0.72f); break;
                    case "bushLilac": t1 = new Color(0.78f, 0.86f, 0.72f); t2 = new Color(1.25f, 0.82f, 1.45f); break;   // lilac blossom clusters
                    case "bushWhite": t1 = new Color(0.78f, 0.86f, 0.72f); t2 = new Color(1.45f, 1.40f, 1.35f); break;
                    case "grass": t1 = new Color(1.05f, 1.05f, 0.98f); t2 = t1; break;
                }
                m.SetColor("_Tint", t1);
                m.SetColor("_Tint2", t2);
                m.SetFloat("_Sss", pine ? 0.08f : 0.12f);
                m.SetFloat("_WindMul", look.StartsWith("bush") ? 0.5f : look == "willow" ? 1.6f : 1f);
                if (Map3D.Test("noprepass")) NoLeafPrepass(m);
                return m;
            });
        }

        /// <summary>Bark of every tree (one atlas, the strip picked by the mesh UVs: Map3DTreeBuilder.BarkStrip).</summary>
        public static Material Bark
        {
            get
            {
                return Get("bark", () =>
                {
                    var m = new Material(Find("Map3DPainted"));
                    m.SetTexture("_MainTex", Tex("bark"));
                    m.SetTexture("_MossTex", Tex("ground-grass"));
                    m.SetFloat("_Mode", 0f);
                    m.SetColor("_Tint", new Color(0.92f, 0.90f, 0.88f));
                    return m;
                });
            }
        }

        /// <summary>Rocks and boulders: triplanar painted stone + moss on top.</summary>
        public static Material Rock
        {
            get
            {
                return Get("rock", () =>
                {
                    var m = new Material(Find("Map3DPainted"));
                    m.SetTexture("_MainTex", Tex("rock"));
                    m.SetTexture("_MossTex", Tex("ground-grass"));
                    m.SetFloat("_Mode", 1f);
                    m.SetFloat("_TriScale", 1f / 40f); // one texture repeat per 2.5 m
                    m.SetFloat("_WindMul", 0f);
                    m.SetColor("_Tint", new Color(1.12f, 1.16f, 1.18f)); // the painted stone is dark under the soft ramp: lift toward the 2.5D beige-grey rocks
                    m.SetColor("_MossTint", new Color(0.66f, 0.70f, 0.50f));
                    return m;
                });
            }
        }

        /// <summary>Ruin columns and walls: the painted paving slabs as triplanar masonry, ivory #B8AF9C, a little moss on top.</summary>
        public static Material Ruin
        {
            get
            {
                return Get("ruin", () =>
                {
                    var m = new Material(Find("Map3DPainted"));
                    Texture2D pave = Resources.Load<Texture2D>("Map3D/ground-paving");
                    m.SetTexture("_MainTex", pave != null ? Tex("ground-paving") : Tex("rock"));
                    m.SetTexture("_MossTex", Tex("ground-grass"));
                    m.SetFloat("_Mode", 1f);
                    m.SetFloat("_TriScale", 1f / 36f);
                    m.SetFloat("_WindMul", 0f);
                    m.SetColor("_Tint", new Color(0.98f, 0.98f, 0.97f));
                    m.SetColor("_MossTint", new Color(0.66f, 0.70f, 0.50f));
                    return m;
                });
            }
        }

        /// <summary>Bridge, shed, fences, signpost, barricade, haystack, barrels (Map3DWoodBuilder / Map3DShedBuilder): the ChatGPT wood atlas
        /// (2x2: planks, beam grain, shingles, thatch), cell per vertex, uv tiling inside the cell (Map3DPainted _Mode 2), moss only where the mesh asks.</summary>
        public static Material Wood
        {
            get
            {
                return Get("wood", () =>
                {
                    var m = new Material(Find("Map3DPainted"));
                    m.SetTexture("_MainTex", Tex("wood"));
                    m.SetTexture("_MossTex", Tex("ground-grass"));
                    m.SetFloat("_Mode", 2f);
                    m.SetFloat("_WindMul", 0f);
                    m.SetColor("_Tint", new Color(1.04f, 1.02f, 1.0f));
                    m.SetColor("_MossTint", new Color(0.66f, 0.70f, 0.50f));
                    return m;
                });
            }
        }

        /// <summary>Reed clumps: crossed cards of the painted 2.5D reed sprite (Resources/Sprites/village/props/wbxReeds), foliage shading + wind; null without the art.</summary>
        public static Material Reeds
        {
            get
            {
                var tex = Resources.Load<Texture2D>("Sprites/village/props/wbxReeds");
                if (tex == null) return null;
                return Get("reeds", () =>
                {
                    var m = new Material(Find("Map3DFoliage"));
                    m.SetTexture("_MainTex", tex);
                    m.SetColor("_Tint", new Color(0.95f, 0.97f, 0.92f));
                    m.SetColor("_Tint2", new Color(0.95f, 0.97f, 0.92f));
                    m.SetFloat("_Cutoff", 0.4f);
                    m.SetFloat("_Sss", 0.1f);
                    m.SetFloat("_WindMul", 0.8f);
                    if (Map3D.Test("noprepass")) NoLeafPrepass(m);
                    return m;
                });
            }
        }

        public static Material Water
        {
            get
            {
                return Get("water", () =>
                {
                    var m = new Material(Find("Map3DWater"));
                    m.SetTexture("_Noise", Noise);
                    return m;
                });
            }
        }

        /// <summary>Alpha-cutout lit material of the props that stay sprites (shared; the sprite comes per renderer).</summary>
        public static Material SpriteCutout { get { return Get("spriteCutout", () => new Material(Find("Map3DSpriteCutout"))); } }
    }
}
