using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Height and layer data of a 3D map (Map3D), on a 0.5 m vertex grid that runs <see cref="Apron"/> metres past every map edge (ChatGPT spec section 1):
    /// every walkable tile stays exactly at 0 (heroes and monsters walk on the 2D plane), solid border tiles rise as a forest bank (0.3-1.2 m, starting
    /// 0.25 m inside them), the ground past the map edge becomes hills (up to ~3 m) so no end of the world shows at any yaw, the stream / pond bed sinks
    /// 0.55 m (from the water channel of the splat, Tools/build-map-3d-splat.py), open grass dips a few centimetres at most (never above 0: no buried feet).
    /// Edge portals get a flat dirt corridor outward. Also builds the layer control texture of the whole terrain (map splat inside, forest floor + corridor
    /// dirt outside). Coordinates: tile x right, tile y DOWN like the rows (Map3D.World converts).
    /// </summary>
    public sealed class Map3DHeightField
    {
        public const float Apron = 30f;    // metres of terrain past each map edge (camera at any yaw / the widest zoom)
        public const float Step = 0.5f;    // vertex spacing, metres
        public const int SplatPx = 8;      // control texels per metre

        public int MapW, MapH;             // tiles
        public int Nx, Ny;                 // vertices
        public float[] H;                  // metres, up positive, Nx * Ny, row 0 = north (ty = -Apron)
        public Texture2D Control;          // RGBA: dirt, forest, paving, water over the whole terrain (linear)
        public Texture2D HeightTex;        // RFloat heights (metres) on the vertex grid: the water shader reads the depth per pixel (smooth shore)
        byte[] mapSplat;                   // the map's own splat (RGBA, SplatPx per tile, rows top-down)
        int splatW, splatH;
        bool[] open;                       // extended tile grid: walkable (or portal corridor)
        int tw, th;                        // extended tile grid size
        float[] dOpen;                     // metres from the nearest open cell, on the vertex grid

        static Map3DHeightField cached;
        static string cachedId;

        public static Map3DHeightField For(IGameMap map)
        {
            if (cached != null && cachedId == map.Id) return cached;
            cached = new Map3DHeightField(map);
            cachedId = map.Id;
            return cached;
        }

        public float MinTx { get { return -Apron; } }
        public float MinTy { get { return -Apron; } }

        Map3DHeightField(IGameMap map)
        {
            MapW = map.Width; MapH = map.Height;
            LoadSplat(map);
            BuildOpen(map);
            Nx = Mathf.RoundToInt((MapW + 2f * Apron) / Step) + 1;
            Ny = Mathf.RoundToInt((MapH + 2f * Apron) / Step) + 1;
            DistanceToOpen();
            BuildHeights();
            BuildControl();
            HeightTex = new Texture2D(Nx, Ny, TextureFormat.RFloat, false, true) { name = "Map3DHeight", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            HeightTex.SetPixelData(H, 0);
            HeightTex.Apply(false, false);
        }

        /// <summary>Shader params to sample HeightTex from a world position: (MinTx, MinTy, 1 / Step, MapH units), (1 / Nx, 1 / Ny).</summary>
        public Vector4 HeightRect { get { return new Vector4(MinTx, MinTy, 1f / Step, GameConfig.MapH); } }
        public Vector4 HeightSize { get { return new Vector4(1f / Nx, 1f / Ny, 0f, 0f); } }

        void LoadSplat(IGameMap map)
        {
            var ta = Resources.Load<TextAsset>("Map3D/" + Map3DAssets.Key(map) + "-splat");
            if (ta == null || ta.bytes.Length < 8) { Debug.LogWarning("[ZenoniaProto] Map3D: no splat for " + map.Id + " (Tools/build-map-3d-splat.py)"); return; }
            byte[] b = ta.bytes;
            splatW = b[4] | (b[5] << 8); splatH = b[6] | (b[7] << 8);
            if (b.Length < 8 + splatW * splatH * 4) { splatW = splatH = 0; return; }
            mapSplat = new byte[splatW * splatH * 4];
            System.Buffer.BlockCopy(b, 8, mapSplat, 0, mapSplat.Length);
        }

        /// <summary>Map splat channel c (0 dirt, 1 forest, 2 paving, 3 water) at tile coords, bilinear; 0 outside the map or without a splat.</summary>
        public float Splat(float tx, float ty, int c)
        {
            if (mapSplat == null) return 0f;
            float u = tx * SplatPx - 0.5f, v = ty * SplatPx - 0.5f;
            int x0 = Mathf.FloorToInt(u), y0 = Mathf.FloorToInt(v);
            float fx = u - x0, fy = v - y0;
            float a = Px(x0, y0, c), b = Px(x0 + 1, y0, c), d = Px(x0, y0 + 1, c), e = Px(x0 + 1, y0 + 1, c);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(d, e, fx), fy);
        }

        float Px(int x, int y, int c)
        {
            if (x < 0 || y < 0 || x >= splatW || y >= splatH) return 0f;
            return mapSplat[(y * splatW + x) * 4 + c] / 255f;
        }

        // ---- open cells: walkable tiles of the map + a corridor out of every edge portal ----
        void BuildOpen(IGameMap map)
        {
            int a = Mathf.RoundToInt(Apron);
            tw = MapW + 2 * a; th = MapH + 2 * a;
            open = new bool[tw * th];
            var tm = map as TileMapBase;
            for (int ty = 0; ty < MapH; ty++)
                for (int tx = 0; tx < MapW; tx++)
                {
                    char c = tm != null ? tm.CharAt(tx, ty) : (map.IsSolidTile(tx, ty) ? 'T' : 'g');
                    if (c != 'T') open[(ty + a) * tw + tx + a] = true; // water tiles are not a bank: the bed sinks from the painted water channel instead
                }
            foreach (MapPortal p in map.Portals) Corridor(p.Tx, p.Ty, a);
        }

        /// <summary>Flat corridor from an edge portal tile straight out of the map (3 tiles wide), across the solid border rows too.</summary>
        void Corridor(int px, int py, int a)
        {
            int dl = px, dr = MapW - 1 - px, dt = py, db = MapH - 1 - py;
            int m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(dt, db));
            if (m > 4) return; // an inner portal (cave mouth, stairs): nothing to open
            int dx = m == dl ? -1 : m == dr ? 1 : 0, dy = dx != 0 ? 0 : (m == dt ? -1 : 1);
            for (int s = 0; s < a + 8; s++)
                for (int w = -1; w <= 1; w++)
                {
                    int x = px + dx * s + (dy != 0 ? w : 0), y = py + dy * s + (dx != 0 ? w : 0);
                    int ex = x + a, ey = y + a;
                    if (ex < 0 || ey < 0 || ex >= tw || ey >= th) continue;
                    open[ey * tw + ex] = true;
                    corridor.Add(new Vector2Int(x, y));
                }
        }

        readonly HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();

        bool OpenAt(float tx, float ty)
        {
            int a = Mathf.RoundToInt(Apron);
            int x = Mathf.FloorToInt(tx) + a, y = Mathf.FloorToInt(ty) + a;
            if (x < 0 || y < 0 || x >= tw || y >= th) return false;
            return open[y * tw + x];
        }

        /// <summary>The tile (x, y) is part of an edge-portal corridor (dirt track out of the map).</summary>
        public bool IsCorridor(int tx, int ty) { return corridor.Contains(new Vector2Int(tx, ty)); }

        /// <summary>The tile is walkable or a portal corridor (props and border trees keep off it).</summary>
        public bool IsOpenTile(int tx, int ty) { return OpenAt(tx + 0.5f, ty + 0.5f); }

        /// <summary>Two-pass chamfer distance (metres) from every vertex to the nearest vertex inside an open cell.</summary>
        void DistanceToOpen()
        {
            int n = Nx * Ny;
            dOpen = new float[n];
            const float Big = 1e6f, D1 = Step, D2 = Step * 1.41421f;
            for (int j = 0; j < Ny; j++)
                for (int i = 0; i < Nx; i++)
                {
                    float tx = MinTx + i * Step, ty = MinTy + j * Step;
                    // a vertex counts as open only when every cell touching it is open: the rise starts on the solid side of the border line
                    bool o = OpenAt(tx - 0.01f, ty - 0.01f) && OpenAt(tx + 0.01f, ty - 0.01f) && OpenAt(tx - 0.01f, ty + 0.01f) && OpenAt(tx + 0.01f, ty + 0.01f);
                    bool any = OpenAt(tx - 0.01f, ty - 0.01f) || OpenAt(tx + 0.01f, ty - 0.01f) || OpenAt(tx - 0.01f, ty + 0.01f) || OpenAt(tx + 0.01f, ty + 0.01f);
                    dOpen[j * Nx + i] = o || any ? 0f : Big; // the border line itself stays at 0 (walkable tiles stay flat up to their edge)
                }
            for (int j = 0; j < Ny; j++)
                for (int i = 0; i < Nx; i++)
                {
                    int k = j * Nx + i;
                    float v = dOpen[k];
                    if (i > 0) v = Mathf.Min(v, dOpen[k - 1] + D1);
                    if (j > 0) v = Mathf.Min(v, dOpen[k - Nx] + D1);
                    if (i > 0 && j > 0) v = Mathf.Min(v, dOpen[k - Nx - 1] + D2);
                    if (i < Nx - 1 && j > 0) v = Mathf.Min(v, dOpen[k - Nx + 1] + D2);
                    dOpen[k] = v;
                }
            for (int j = Ny - 1; j >= 0; j--)
                for (int i = Nx - 1; i >= 0; i--)
                {
                    int k = j * Nx + i;
                    float v = dOpen[k];
                    if (i < Nx - 1) v = Mathf.Min(v, dOpen[k + 1] + D1);
                    if (j < Ny - 1) v = Mathf.Min(v, dOpen[k + Nx] + D1);
                    if (i < Nx - 1 && j < Ny - 1) v = Mathf.Min(v, dOpen[k + Nx + 1] + D2);
                    if (i > 0 && j < Ny - 1) v = Mathf.Min(v, dOpen[k + Nx - 1] + D2);
                    dOpen[k] = v;
                }
        }

        /// <summary>Metres from the tile point to the nearest walkable cell (vertex grid, nearest vertex).</summary>
        public float DistanceToOpenAt(float tx, float ty)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt((tx - MinTx) / Step), 0, Nx - 1), j = Mathf.Clamp(Mathf.RoundToInt((ty - MinTy) / Step), 0, Ny - 1);
            return dOpen[j * Nx + i];
        }

        void BuildHeights()
        {
            H = new float[Nx * Ny];
            for (int j = 0; j < Ny; j++)
                for (int i = 0; i < Nx; i++)
                {
                    float tx = MinTx + i * Step, ty = MinTy + j * Step;
                    int k = j * Nx + i;
                    float d = dOpen[k];
                    float outX = Mathf.Max(-tx, tx - MapW), outY = Mathf.Max(-ty, ty - MapH), outside = Mathf.Max(0f, Mathf.Max(outX, outY));
                    float n1 = Mathf.PerlinNoise(tx * 0.13f + 11.3f, ty * 0.13f + 5.1f), n2 = Mathf.PerlinNoise(tx * 0.06f + 41.7f, ty * 0.06f + 23.9f);
                    float n3 = Mathf.PerlinNoise(tx * 0.35f + 3.3f, ty * 0.35f + 71.2f);
                    float h = 0f;
                    if (d > 0f)
                    {
                        h += Smooth(0.25f, 3.0f, d) * (0.35f + 0.85f * n1);                 // forest bank on the solid border
                        h += Smooth(2f, 16f, Mathf.Min(d, outside + 2f)) * (1.1f + 1.9f * n2); // hills past the edge (never next to a corridor)
                        h += (n3 - 0.5f) * 0.25f * Smooth(0.5f, 2f, d);                       // small lumps on the bank
                    }
                    float w = Splat(tx, ty, 3);
                    if (w > 0.02f) h -= 0.55f * Smooth(0.1f, 0.8f, w) + Mathf.Max(0f, h) * Smooth(0.1f, 0.6f, w); // stream / pond bed follows the painted water, not the tile rows
                    if (d <= 0f && w <= 0.02f)
                    {
                        float dirt = Mathf.Max(Splat(tx, ty, 0), Splat(tx, ty, 2));
                        h = -0.06f * n3 * (1f - dirt);                                      // open grass: shallow dips only
                    }
                    H[k] = h;
                }
        }

        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        /// <summary>Terrain height (metres) at tile coords, bilinear on the vertex grid.</summary>
        public float HeightAt(float tx, float ty)
        {
            float u = (tx - MinTx) / Step, v = (ty - MinTy) / Step;
            int i = Mathf.Clamp(Mathf.FloorToInt(u), 0, Nx - 2), j = Mathf.Clamp(Mathf.FloorToInt(v), 0, Ny - 2);
            float fx = Mathf.Clamp01(u - i), fy = Mathf.Clamp01(v - j);
            int k = j * Nx + i;
            return Mathf.Lerp(Mathf.Lerp(H[k], H[k + 1], fx), Mathf.Lerp(H[k + Nx], H[k + Nx + 1], fx), fy);
        }

        /// <summary>Layer control of the whole terrain: inside the map the map splat, outside forest floor with patches, corridors dirt.</summary>
        void BuildControl()
        {
            int w = Mathf.RoundToInt((MapW + 2f * Apron) * SplatPx), h = Mathf.RoundToInt((MapH + 2f * Apron) * SplatPx);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float tx = MinTx + (x + 0.5f) / SplatPx, ty = MinTy + (y + 0.5f) / SplatPx;
                    byte r, g, b, a;
                    bool inside = tx >= 0f && ty >= 0f && tx < MapW && ty < MapH;
                    float forestOut = Mathf.Clamp01(0.55f + 0.6f * (Mathf.PerlinNoise(tx * 0.09f + 7f, ty * 0.09f + 3f) - 0.5f));
                    if (inside && mapSplat != null)
                    {
                        r = (byte)(Splat(tx, ty, 0) * 255f); g = (byte)(Splat(tx, ty, 1) * 255f); b = (byte)(Splat(tx, ty, 2) * 255f); a = (byte)(Splat(tx, ty, 3) * 255f);
                        float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(tx, MapW - tx), Mathf.Min(ty, MapH - ty)) / 2f); // blend into the outside forest floor
                        g = (byte)Mathf.Lerp(forestOut * 255f, g, edge);
                    }
                    else { r = 0; g = (byte)(forestOut * 255f); b = 0; a = 0; }
                    if (!inside && IsCorridor(Mathf.FloorToInt(tx), Mathf.FloorToInt(ty))) r = 230;
                    // walkable tiles inside the border rows that the corridor crosses (south gate) keep the map's own road
                    px[(h - 1 - y) * w + x] = new Color32(r, g, b, a); // texture row 0 = south (world y up)
                }
            Control = new Texture2D(w, h, TextureFormat.RGBA32, true, true) { name = "Map3DControl", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            Control.SetPixels32(px);
            Control.Apply(true, true);
        }
    }
}
