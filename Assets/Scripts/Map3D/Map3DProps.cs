using UnityEngine;

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
            if (p.Type == "tauGuildHallHouse" && Map3DGuildHallBuilder.HasArt && !Map3D.Test("noguild")) { PlaceGuildHall(p, d); return true; }
            if (Map3DTownWallBuilder.HasArt && !Map3D.Test("nowalls") && PlaceTownWall(p, d)) return true;
            Map3DTreeBuilder.Kind kind;
            Solid s = Classify(p.Type, out kind);
            if (s == Solid.None) return false;
            Map3DHeightField f = Map3DHeightField.For(MapRegistry.Current);
            float tx = p.Tx + d.FootW * 0.5f, ty = p.Ty + 1f - Mathf.Max(1, d.FootH) * 0.5f; // footprint centre (tile coords, y down)
            int hash = Hash(p.Tx, p.Ty, p.Type);
            if (s == Solid.Bridge) { PlaceBridge(p, d, tx, hash); return true; }
            if (s == Solid.Tree && ExposeRidge(f, tx, ty, hash)) return true; // left out on purpose: the knoll under it shows

            Place(parent, p.Type, s, kind, d, tx, ty, f, hash);
            return true;
        }

        /// <summary>
        /// "Exposed Low Blocker Ridges" (ChatGPT RO3 loop round 2, Outdoor Depth V4.0 B): on Clover Meadows' inner knolls (solid ground raised 0.6-2 m,
        /// away from the map border) 35 % of the trees are not drawn in 3D so the knoll's silhouette reads instead of a canopy. Visual only: the rows,
        /// the server and the 2.5D map keep every tree.
        /// </summary>
        static bool ExposeRidge(Map3DHeightField f, float tx, float ty, int hash)
        {
            if (MapRegistry.Current == null || MapRegistry.Current.Id != TauCloverMeadowsMap.MapId || Map3D.Test("nodepth")) return false;
            float edge = Mathf.Min(Mathf.Min(tx, f.MapW - tx), Mathf.Min(ty, f.MapH - ty));
            if (edge < 7f || f.HeightAt(tx, ty) < 0.4f) return false;
            return (hash >> 5) % 100 < 35;
        }

        /// <summary>Guild Hall hybrid 3D (Map3DGuildHallBuilder): centred on the rows' 10 x 5 m solid block (footprint 10 wide, the block runs 5 rows up
        /// from the prop's 2-row base), door to the south; real shadows, hero occlusion fade from Map3DPainted.</summary>
        static void PlaceGuildHall(MapProp p, PropDef d)
        {
            Map3DHeightField f = Map3DHeightField.For(MapRegistry.Current);
            float tx = p.Tx + d.FootW * 0.5f, ty = p.Ty + 1f - 2.5f;
            Vector3 pos = Map3D.World(tx, ty, 0f);
            pos.z += Map3DTerrain.Sink;
            Map3DBatcher.Add(Map3DGuildHallBuilder.Structure, Map3DAssets.GuildA, Map3DAssets.GuildA, pos, Quaternion.identity, 1f);
            Map3DBatcher.Add(Map3DGuildHallBuilder.Detail, Map3DAssets.GuildB, Map3DAssets.GuildB, pos, Quaternion.identity, 1f);
        }

        /// <summary>Village town wall, gate towers, gate arch and the Great Oak curb ring as fixed 3D (Map3DTownWallBuilder / Map3DGateBuilder; user 24/11:
        /// the billboard walls turned with the camera). All stand on the line between the wall's two solid rows (rows 27-28: y = 28), ground height 0
        /// (the footing runs 0.9 m into the bank). False = not one of these types.</summary>
        static bool PlaceTownWall(MapProp p, PropDef d)
        {
            Vector3 pos;
            switch (p.Type)
            {
                case "tauWallLeft": case "tauWallRight":
                    pos = Ground(p.Tx + d.FootW * 0.5f, p.Ty + 1f - d.FootH * 0.5f);
                    Map3DBatcher.Add(Map3DTownWallBuilder.Wall(d.FootW, (p.Tx / 4) % 3), Map3DAssets.TownStone, Map3DAssets.Leaves("oak"), pos, Quaternion.identity, 1f); // ivy: the trees' own leaf material, merged into their batches
                    return true;
                case "tauGateTowerL": case "tauGateTowerR": // 4 m block from the prop's west tile (L: 16-20 up to the road, R: 24-28 from the road)
                {
                    Map3DGateBuilder.Parts t = Map3DGateBuilder.Tower(p.Type == "tauGateTowerL");
                    pos = Ground(p.Tx + Map3DGateBuilder.BlockWidth * 0.5f, p.Ty);
                    Map3DBatcher.Add(t.Stone, Map3DAssets.TownStone, Map3DAssets.TownStone, pos, Quaternion.identity, 1f);
                    Map3DBatcher.Add(t.Roof, Map3DAssets.TownRoof, Map3DAssets.TownRoof, pos, Quaternion.identity, 1f);
                    Map3DBatcher.Add(t.Detail, Map3DAssets.GuildB, Map3DAssets.GuildB, pos, Quaternion.identity, 1f, false); // flat on the stone: no shadow pass
                    return true;
                }
                case "tauGateArchBeam": // the sprite sits 2 rows north of the wall line (y-sort over heroes in the gate); the 3D arch spans the road on the line
                {
                    Mesh detail;
                    Mesh a = Map3DGateBuilder.Arch(d.FootW, out detail);
                    pos = Ground(p.Tx + d.FootW * 0.5f, p.Ty + 2f);
                    Map3DBatcher.Add(a, Map3DAssets.TownStone, Map3DAssets.TownStone, pos, Quaternion.identity, 1f);
                    Map3DBatcher.Add(detail, Map3DAssets.GuildB, Map3DAssets.GuildB, pos, Quaternion.identity, 1f, false);
                    return true;
                }
                case "tauShrineEntrance": case "tauGeminiGateMarker": // Clover's stone gates (Map3DShrineGateBuilder)
                {
                    Material cards = Map3DAssets.SpriteCard(p.Type);
                    if (cards == null) return false;
                    bool shrine = p.Type == "tauShrineEntrance";
                    Map3DShrineGateBuilder.Parts g = shrine ? Map3DShrineGateBuilder.Shrine() : Map3DShrineGateBuilder.Gemini();
                    // shrine: over the portal road (4 tiles), 0.3 m into the portal row so the plinths stay off the walkable row south of it; gemini: centre of its 4 x 2 solid block
                    pos = shrine ? Ground(p.Tx + d.FootW * 0.5f, p.Ty + 0.3f) : Ground(p.Tx + d.FootW * 0.5f, p.Ty + 1f - d.FootH * 0.5f);
                    if (!shrine) // the gemini block is an inner blocker: Clover's relief raises it as a knoll (0.6-1.2 m), stand the gate on it
                    {
                        Map3DHeightField f = Map3DHeightField.For(MapRegistry.Current);
                        float gx = p.Tx + d.FootW * 0.5f, gy = p.Ty + 1f - d.FootH * 0.5f;
                        float h = Mathf.Min(f.HeightAt(gx - 1.6f, gy), f.HeightAt(gx + 1.6f, gy), f.HeightAt(gx, gy + 0.4f));
                        pos.z -= Mathf.Max(0f, h - 0.1f) * Map3D.M;
                    }
                    Map3DBatcher.Add(g.Stone, Map3DAssets.TownStone, Map3DAssets.TownStone, pos, Quaternion.identity, 1f);
                    Map3DBatcher.Add(g.Roots, Map3DAssets.Bark, Map3DAssets.Leaves("oak"), pos, Quaternion.identity, 1f);
                    Map3DBatcher.Add(g.Cards, cards, cards, pos, Quaternion.identity, 1f, false);
                    return true;
                }
                case "tauBrambleWallMedium": // bramble hedge: crossed cards of its sprite, standing on the terrain of its solid tile
                {
                    Material cards = Map3DAssets.SpriteCard(p.Type);
                    if (cards == null) return false;
                    Map3DHeightField f = Map3DHeightField.For(MapRegistry.Current);
                    float tx = p.Tx + d.FootW * 0.5f, ty = p.Ty + 0.5f;
                    pos = Map3D.World(tx, ty, f.HeightAt(tx, ty));
                    pos.z += Map3DTerrain.Sink;
                    Map3DBatcher.Add(Map3DShrineGateBuilder.CrossedCards(d.W, d.H), cards, cards, pos, Quaternion.AngleAxis(Hash(p.Tx, p.Ty, p.Type) % 40 - 20, Vector3.forward), 1f);
                    return true;
                }
                case "tauGardenMonument": // east garden pillar in its round flower bed, centred on its 3 x 2 solid block (Map3DMonumentBuilder, user 25/11)
                {
                    Material cards = Map3DAssets.SpriteCard(p.Type);
                    if (cards == null) return false;
                    Map3DMonumentBuilder.Parts m = Map3DMonumentBuilder.Get();
                    pos = Ground(p.Tx + d.FootW * 0.5f, p.Ty + 1f - d.FootH * 0.5f);
                    Map3DBatcher.Add(m.Stone, Map3DAssets.TownStone, Map3DAssets.TownStone, pos, Quaternion.identity, 1f);
                    Map3DBatcher.Add(m.Cards, cards, cards, pos, Quaternion.identity, 1f, false);
                    Material flowers = Map3DAssets.SpriteCard(Map3DMonumentBuilder.FlowerSprite);
                    if (flowers != null) Map3DBatcher.Add(m.Flowers, flowers, flowers, pos, Quaternion.identity, 1f, false);
                    return true;
                }
                case "tauParkBench": // TEST 25/11: ChatGPT's bench model built on the approved bench sprite (front = the sprite on an alpha-cut plane, 3D frame, faces +Z);
                {                    // --map3d-test benchsprite3d = this one, benchmodel = its first procedural box bench (user: "sao cái ghế xấu vậy")
                    bool boxy = Map3D.Test("benchmodel");
                    if (!boxy && !Map3D.Test("benchsprite3d")) return false; // both tries looked worse than the sprite (25/11): flat sprite by default
                    string dir = boxy ? "willowbrook-bench/Willowbrook_Bench" : "willowbrook-bench-sprite/Willowbrook_Bench_SpriteBased";
                    Mesh bench = Map3DAssets.Model(dir);
                    Material bm = Map3DAssets.ModelMaterial(boxy ? "willowbrook-bench/Bench_Handpainted_Atlas_1024" : "willowbrook-bench-sprite/Willowbrook_Bench_SpriteBased_Atlas_1024");
                    if (bench == null || bm == null) return false;
                    pos = Ground(p.Tx + d.FootW * 0.5f, p.Ty + 0.5f);
                    // model y up -> world -z (up), model +z -> world +y (north); turned 180 deg so the seat faces south like the sprite
                    Quaternion q = Quaternion.AngleAxis(boxy ? 0f : 180f, Vector3.forward) * Quaternion.Euler(-90f, 0f, 0f);
                    Map3DBatcher.Add(bench, bm, bm, pos, q, Map3D.M);
                    return true;
                }
                case "tauOakPlanterRing": // round the Great Oak's 3 x 3 solid block (rows 12-14), its rim ~0.9 m onto the plaza (walk-through curb, as the 2.5D sprite)
                    pos = Ground(p.Tx + d.FootW * 0.5f, p.Ty - 0.8f); // near the Great Oak trunk (ty 14.5)
                    Map3DBatcher.Add(Map3DTownWallBuilder.Ring(1.7f, 16), Map3DAssets.TownStone, Map3DAssets.TownStone, pos, Quaternion.identity, 1f);
                    return true;
            }
            return false;
        }

        static Vector3 Ground(float tx, float ty)
        {
            Vector3 pos = Map3D.World(tx, ty, 0f);
            pos.z += Map3DTerrain.Sink;
            return pos;
        }

        /// <summary>Generated object for a prop kind at tile coords (tx, ty) (also the border forest: Map3DBorderForest).</summary>
        public static void PlaceKind(Transform parent, string name, Map3DTreeBuilder.Kind kind, float tx, float ty, Map3DHeightField f, int hash, bool cast = true)
        {
            int variant = (hash >> 3) % Map3DTreeBuilder.Variants;
            Build(parent, name, Map3DTreeBuilder.Get(kind, variant), Map3DAssets.Bark, Map3DAssets.Leaves(Map3DTreeBuilder.Look(kind)),
                tx, ty, f, hash, 0.92f + (hash % 17) / 100f, cast, Map3DTreeBuilder.Key(kind, variant)); // distant ones swap to impostors (Map3DImpostors)
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
            int x0 = cx, x1 = cx, y0 = row, y1 = row;
            while (tm != null && tm.CharAt(x0 - 1, row) == 'B') x0--;
            while (tm != null && tm.CharAt(x1 + 1, row) == 'B') x1++;
            while (tm != null && tm.CharAt(cx, y0 - 1) == 'B') y0--;   // Taurus: the deck may run north-south over an east-west stream
            while (tm != null && tm.CharAt(cx, y1 + 1) == 'B') y1++;
            bool northSouth = y1 - y0 > x1 - x0;
            float spanX = x1 - x0 + 1, spanY = y1 - y0 + 1;
            Vector3 pos = Map3D.World(x0 + spanX * 0.5f, y0 + spanY * 0.5f, 0f);
            pos.z += Map3DTerrain.Sink;
            Mesh m = northSouth ? Map3DWoodBuilder.Bridge(spanY + 0.8f, Mathf.Max(1.8f, spanX - 0.2f), spanY - 1f, hash % 3)
                                : Map3DWoodBuilder.Bridge(spanX + 0.8f, 1.8f, spanX - 1f, hash % 3);
            Map3DBatcher.Add(m, Map3DAssets.Wood, Map3DAssets.Wood, pos, northSouth ? Quaternion.AngleAxis(90f, Vector3.forward) : Quaternion.identity, 1f);
        }

        static void Build(Transform parent, string name, Mesh mesh, Material m0, Material m1, float tx, float ty, Map3DHeightField f, int hash, float scale, bool cast = true, int impostor = -1)
        {
            Vector3 pos = Map3D.World(tx, ty, f.HeightAt(tx, ty));
            pos.z += Map3DTerrain.Sink;
            Map3DBatcher.Add(mesh, m0, m1, pos, Quaternion.AngleAxis(hash % 360, Vector3.forward), scale, cast, 0, impostor); // merged per cell after the map is built
        }

        static Solid Classify(string type, out Map3DTreeBuilder.Kind kind)
        {
            string t = type.ToLowerInvariant();
            kind = Map3DTreeBuilder.Kind.Oak;
            if (t.Contains("planter")) return Solid.None; // tauOakPlanterRing: a flat stone ring, not a tree
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
            if (t.Contains("stones")) return Solid.RockCluster; // tauAncientRootsStones
            if (t.Contains("elderwood") || t.Contains("great")) { kind = Map3DTreeBuilder.Kind.OakAncient; return Solid.Tree; } // Taurus elderwood / the village Great Oak
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
            sr.receiveShadows = true;
            Map3DBatcher.Register(sr, true); // casts at High (realtime) and into the Medium bake (GraphicsQuality)
        }

        /// <summary>Flat decor kept as a sprite: lifted 2 cm above the walkable plane (the terrain sits 1.5 cm below z = 0) so it never z-fights.</summary>
        public static void LiftFlat(SpriteRenderer sr)
        {
            Vector3 p = sr.transform.position;
            float z = -0.35f;
            if (sr.sprite != null && sr.sprite.name.Contains("Lily") && MapRegistry.Current != null) // lily pads float on the village lake's lowered water (Map3DLakeWall)
            {
                Map3DHeightField f = Map3DHeightField.For(MapRegistry.Current);
                if (f.DeepLake) z = -(f.WaterLevel + 0.03f) * Map3D.M;
            }
            sr.transform.position = new Vector3(p.x, p.y, z);
        }
    }
}
