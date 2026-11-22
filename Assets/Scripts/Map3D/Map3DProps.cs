using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Map props on a 3D map (Map3D), called by MapScenery for every prop of the table: trees, bushes, rocks, logs and stumps become generated meshes
    /// (Map3DTreeBuilder / Map3DRockBuilder) standing on the terrain at their footprint, turned and scaled a little per tile (seeded: same map every
    /// visit); ruins (Map3DRuinBuilder); bridge, fences, signpost, barricade (Map3DWoodBuilder), shed, haystack, reeds (Map3DShedBuilder) since 22/11
    /// (the flat sprites skewed under the turning camera). Anything unnamed stays a sprite: standing ones go alpha-cutout (MakeCutout) so they take
    /// and cast the real shadow. Rows, solid tiles and positions do not change: only the drawing.
    /// </summary>
    public static class Map3DProps
    {
        enum Solid { None, Tree, Rock, RockCluster, Log, Stump, PillarTall, PillarShort, Wall, WallCorner, Bridge, Shed, Fence, FenceBroken, Haystack, Signpost, Barricade, Reeds }

        /// <summary>Builds the 3D version of the prop when its type has one; false = draw the sprite as usual.</summary>
        public static bool TryBuild(Transform parent, MapProp p, PropDef d)
        {
            Map3DTreeBuilder.Kind kind;
            Solid s = Classify(p.Type, out kind);
            if (s == Solid.None) return false;
            Map3DHeightField f = Map3DHeightField.For(MapRegistry.Current);
            float tx = p.Tx + d.FootW * 0.5f, ty = p.Ty + 1f - Mathf.Max(1, d.FootH) * 0.5f; // footprint centre (tile coords, y down)
            int hash = Hash(p.Tx, p.Ty, p.Type);
            if (s == Solid.Bridge) { PlaceBridge(p, d, tx, hash); return true; }
            Place(parent, p.Type, s, kind, d, tx, ty, f, hash);
            return true;
        }

        /// <summary>Generated object for a prop kind at tile coords (tx, ty) (also the border forest: Map3DBorderForest).</summary>
        public static void PlaceKind(Transform parent, string name, Map3DTreeBuilder.Kind kind, float tx, float ty, Map3DHeightField f, int hash, bool cast = true)
        {
            Build(parent, name, Map3DTreeBuilder.Get(kind, (hash >> 3) % Map3DTreeBuilder.Variants), Map3DAssets.Bark, Map3DAssets.Leaves(Map3DTreeBuilder.Look(kind)),
                tx, ty, f, hash, 0.92f + (hash % 17) / 100f, cast);
        }

        static void Place(Transform parent, string type, Solid s, Map3DTreeBuilder.Kind kind, PropDef d, float tx, float ty, Map3DHeightField f, int hash)
        {
            int seed = hash & 0xffff;
            switch (s)
            {
                case Solid.Tree: PlaceKind(parent, type, kind, tx, ty, f, hash); break;
                case Solid.Rock:
                {
                    Vector3 size = new Vector3(d.W * 0.60f, d.H * 0.48f, d.W * 0.46f); // lower: the 2.5D sprites show the top face, the mesh height is less
                    Build(parent, type, Map3DRockBuilder.Rock(size, seed % 6), Map3DAssets.Rock, Map3DAssets.Rock, tx, ty, f, hash, 1f);
                    break;
                }
                case Solid.RockCluster:
                {
                    Vector3 size = new Vector3(d.W * 0.7f, d.H * 0.75f, Mathf.Max(1.2f, d.W * 0.35f));
                    int n = Mathf.Clamp(Mathf.RoundToInt(d.W * 0.9f), 2, 5);
                    Build(parent, type, Map3DRockBuilder.Cluster(size, n, seed % 5), Map3DAssets.Rock, Map3DAssets.Rock, tx, ty, f, hash, 1f);
                    break;
                }
                case Solid.Log:
                    Build(parent, type, Map3DRockBuilder.Wood(d.W * 0.62f, 0.26f, true, seed % 3), Map3DAssets.Bark, Map3DAssets.Bark, tx, ty, f, hash, 1f);
                    break;
                case Solid.PillarTall: case Solid.PillarShort:
                    Build(parent, type, Map3DRuinBuilder.Pillar(s == Solid.PillarTall, seed % 3), Map3DAssets.Ruin, Map3DAssets.Ruin, tx, ty, f, hash - hash % 360 + (hash % 4) * 90, 1f);
                    break;
                case Solid.Wall: case Solid.WallCorner:
                    Build(parent, type, Map3DRuinBuilder.Wall(Mathf.Max(1.6f, d.W * 0.62f), s == Solid.WallCorner, seed % 3), Map3DAssets.Ruin, Map3DAssets.Ruin, tx, ty, f, hash - hash % 360, 1f);
                    break;
                case Solid.Shed:
                    Build(parent, type, Map3DShedBuilder.Shed(d.FootW + 0.4f, Mathf.Max(1, d.FootH) + 0.6f, seed % 3), Map3DAssets.Wood, Map3DAssets.Wood, tx, ty, f, 0, 1f);
                    break;
                case Solid.Fence: case Solid.FenceBroken:
                    Build(parent, type, Map3DWoodBuilder.Fence(d.FootW + 0.3f, s == Solid.FenceBroken, seed % 3), Map3DAssets.Wood, Map3DAssets.Wood, tx, ty, f, (hash % 13 + 354) % 360, 1f);
                    break;
                case Solid.Haystack:
                    Build(parent, type, Map3DShedBuilder.Haystack(d.FootW * 0.5f, 2.0f, seed % 3), Map3DAssets.Wood, Map3DAssets.Wood, tx, ty, f, hash, 1f);
                    break;
                case Solid.Signpost:
                    Build(parent, type, Map3DWoodBuilder.Signpost(seed % 3), Map3DAssets.Wood, Map3DAssets.Wood, tx, ty, f, hash, 1f);
                    break;
                case Solid.Barricade: // the road runs east-west: the barricade spans it north-south
                    Build(parent, type, Map3DWoodBuilder.Barricade(Mathf.Max(1, d.FootH) + 0.4f, seed % 3), Map3DAssets.Wood, Map3DAssets.Wood, tx, ty, f, 90, 1f);
                    break;
                case Solid.Reeds:
                    Build(parent, type, Map3DShedBuilder.Reeds(1.8f, 2.2f, seed % 4), Map3DAssets.Reeds, Map3DAssets.Reeds, tx, ty, f, hash, 0.9f + (hash % 21) / 100f, true);
                    break;
                case Solid.Stump:
                    Build(parent, type, Map3DRockBuilder.Wood(0.55f, 0.36f, false, seed % 3), Map3DAssets.Bark, Map3DAssets.Bark, tx, ty, f, hash, 1f);
                    break;
            }
        }

        /// <summary>Plank bridge over the stream on the row of the map's bridge tiles ('B'; the 2D sprite was anchored a row lower), spanning all of them,
        /// deck top exactly at the walkable plane (height 0), not at the terrain height (the stream bed sinks under it).</summary>
        static void PlaceBridge(MapProp p, PropDef d, float tx, int hash)
        {
            var tm = MapRegistry.Current as TileMapBase;
            int cx = p.Tx + d.FootW / 2, row = p.Ty;
            for (int y = p.Ty; y >= p.Ty - 2 && tm != null; y--) if (tm.CharAt(cx, y) == 'B') { row = y; break; }
            int x0 = cx, x1 = cx;
            while (tm != null && tm.CharAt(x0 - 1, row) == 'B') x0--;
            while (tm != null && tm.CharAt(x1 + 1, row) == 'B') x1++;
            float span = x1 - x0 + 1;
            Vector3 pos = Map3D.World(x0 + span * 0.5f, row + 0.5f, 0f);
            pos.z += Map3DTerrain.Sink;
            Mesh m = Map3DWoodBuilder.Bridge(span + 0.8f, 1.8f, span - 1f, hash % 3);
            Map3DBatcher.Add(m, Map3DAssets.Wood, Map3DAssets.Wood, pos, Quaternion.identity, 1f);
        }

        static void Build(Transform parent, string name, Mesh mesh, Material m0, Material m1, float tx, float ty, Map3DHeightField f, int hash, float scale, bool cast = true)
        {
            Vector3 pos = Map3D.World(tx, ty, f.HeightAt(tx, ty));
            pos.z += Map3DTerrain.Sink;
            Map3DBatcher.Add(mesh, m0, m1, pos, Quaternion.AngleAxis(hash % 360, Vector3.forward), scale, cast); // merged per cell after the map is built
        }

        static Solid Classify(string type, out Map3DTreeBuilder.Kind kind)
        {
            string t = type.ToLowerInvariant();
            kind = Map3DTreeBuilder.Kind.Oak;
            if (t.Contains("pillartall")) return Solid.PillarTall;
            if (t.Contains("pillarshort")) return Solid.PillarShort;
            if (t.Contains("wallcorner")) return Solid.WallCorner;
            if (t.Contains("walllow")) return Solid.Wall;
            if (t.Contains("bridge")) return Solid.Bridge;
            if (t.Contains("shed")) return Solid.Shed;
            if (t.Contains("fencehay")) return Solid.FenceBroken;
            if (t.Contains("fence")) return Solid.Fence;
            if (t.Contains("haystack")) return Solid.Haystack;
            if (t.Contains("signpost")) return Solid.Signpost;
            if (t.Contains("barricade")) return Solid.Barricade;
            if (t.Contains("reed")) return Map3DAssets.Reeds != null ? Solid.Reeds : Solid.None;
            if (t.Contains("palisade") || t.Contains("pillar") || t.Contains("wall") || t.Contains("hay") || t.Contains("sign")) return Solid.None;
            if (t.Contains("pine")) { kind = Map3DTreeBuilder.Kind.Pine; return Solid.Tree; }
            if (t.Contains("birch")) { kind = Map3DTreeBuilder.Kind.Birch; return Solid.Tree; }
            if (t.Contains("willow")) { kind = Map3DTreeBuilder.Kind.Willow; return Solid.Tree; }
            if (t.Contains("ancient")) { kind = Map3DTreeBuilder.Kind.OakAncient; return Solid.Tree; }
            if (t.Contains("young") || t.Contains("oakslim") || t.Contains("oaksmall")) { kind = Map3DTreeBuilder.Kind.OakYoung; return Solid.Tree; }
            if (t.Contains("oak") || t.Contains("tree")) return Solid.Tree;
            if (t.Contains("bushlilac")) { kind = Map3DTreeBuilder.Kind.BushLilac; return Solid.Tree; }
            if (t.Contains("bushwhite")) { kind = Map3DTreeBuilder.Kind.BushWhite; return Solid.Tree; }
            if (t.Contains("bush")) { kind = Map3DTreeBuilder.Kind.Bush; return Solid.Tree; }
            if (t.Contains("stump")) return Solid.Stump;
            if (t.Contains("log")) return t.Contains("rocklog") ? Solid.Rock : Solid.Log;
            if (t.Contains("rocks") || t.Contains("cliff")) return Solid.RockCluster;
            if (t.Contains("rock") || t.Contains("boulder")) return Solid.Rock;
            return Solid.None;
        }

        static int Hash(int x, int y, string type)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ type.GetHashCode();
                return h & 0x7fffffff;
            }
        }

        /// <summary>A prop kept as a standing sprite: alpha-cutout lit material (real shadows in and out), standing on the terrain height.</summary>
        public static void MakeCutout(SpriteRenderer sr)
        {
            sr.sharedMaterial = Map3DAssets.SpriteCutout;
            sr.shadowCastingMode = ShadowCastingMode.On;
            sr.receiveShadows = true;
        }

        /// <summary>Flat decor kept as a sprite: lifted 2 cm above the walkable plane (the terrain sits 1.5 cm below z = 0) so it never z-fights.</summary>
        public static void LiftFlat(SpriteRenderer sr)
        {
            Vector3 p = sr.transform.position;
            sr.transform.position = new Vector3(p.x, p.y, -0.35f);
        }
    }
}
