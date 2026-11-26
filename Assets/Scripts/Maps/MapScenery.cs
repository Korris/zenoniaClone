using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The things a map draws on top of its painted backdrop: HD props y-sorted by their base (web prop-renderer.js: bottom of the footprint = sort key),
    /// a light point on each portal and the villagers. Props are static, so each is a single SpriteRenderer. Art that is not delivered yet must not
    /// leave invisible walls: a prop without its PNG draws its solid footprint as a flat block and warns once per type.
    /// </summary>
    public static class MapScenery
    {
        const float PpuProp = 4f;             // web HD props: 4 source px per world unit
        static readonly Dictionary<string, Sprite> PropSprites = new Dictionary<string, Sprite>();
        static readonly HashSet<string> MissingProps = new HashSet<string>();
        static readonly Color MissingPropTint = new Color(0.42f, 0.33f, 0.24f, 0.85f);

        /// <summary>A portal is drawn where it works: offline, and on a client whose server moves heroes between maps (not on a dev host, which runs one zone).</summary>
        public static bool ShowsPortals { get { return NetMode.IsOffline || NetClient.PortalsLive; } }
        /// <summary>Villagers need no server: a client draws them from the map data, each player sees them standing in the same place.</summary>
        public static bool ShowsVillagers { get { return NetMode.IsOffline || NetMode.IsClient; } }

        public static void Build(Transform parent, IGameMap map)
        {
            foreach (MapProp p in map.Props) BuildProp(parent, p);
            if (ShowsPortals)
            {
                // shader + particle + bloom light (user, 19/11/2026); `--portal-fx off` or missing shaders: edge exits fade into bright haze, inner ones keep the light column (13/11/2026)
                // `--map3d`: real 3D warp gates (stone ring, vortex, light column, point light); the flat quads skewed under the turning camera (22/11/2026)
                if (Map3D.Active && Map3DPortals.Build(parent, map)) { }
                else if (MapPortalLightFx.Active) MapPortalLightFx.Build(parent, map);
                else MapPortalHaze.Build(parent, map);
            }
            if (ShowsVillagers) TownNpc.CreateAll(parent, map);
            if (ShowsVillagers) DungeonPointView.Build(parent, map); // Rootburrow V5 chests + warp crystals: per-character state, drawn like the villagers
        }

        public const float FlatDecorMaxTiles = 3f; // sprite height up to which a walk-through prop counts as ground decoration

        /// <summary>A walk-through prop drawn on the ground (under heroes): low decoration, and every walk-through bridge whatever its height
        /// (a 4-wide bridge over a 3-row stream is drawn deeper than 3 tiles, Taurus 24/11/2026).</summary>
        /// <summary>Segments that butt together into one long wall: drawn on fixed south-facing cards (View25DDirectionalProps with one view).</summary>
        static bool IsWallRun(string type) { return type.StartsWith("tauTerraceWall", System.StringComparison.Ordinal) || type == "tauWallLeft" || type == "tauWallRight" || IsClimbStairs(type); }

        /// <summary>Stairs up a raised terrace (26/11, user: "cầu thang nhìn không có độ dốc" when it lay flat): it stands on a fixed card leaning like the
        /// wall, from the plaza up to the wall top, but draws UNDER every unit (ground band) so a hero climbing it is drawn on the steps, and never fades.</summary>
        static bool IsClimbStairs(string type) { return type.IndexOf("TerraceStairs", System.StringComparison.Ordinal) >= 0; }

        public static bool LiesFlat(string type, PropDef d)
        {
            return d.FootH == 0 && (d.H <= FlatDecorMaxTiles || type.IndexOf("Bridge", System.StringComparison.Ordinal) >= 0);
        }

        static void BuildProp(Transform parent, MapProp p)
        {
            PropDef d;
            if (!VillagePropDefs.TryGet(p.Type, out d)) return;
            // Horizontal centre of the footprint, bottom edge of its lowest row (web y-down row ty -> y-up MapH - (ty+1)*16).
            float x = (p.Tx + d.FootW * 0.5f) * GameConfig.TileUnits, y = GameConfig.MapH - (p.Ty + 1) * GameConfig.TileUnits;
            if (Map3D.Active && Map3DProps.TryBuild(parent, p, d)) return; // `--map3d`: trees, bushes, rocks, logs as generated meshes
            Sprite s = PropSprite(p.Type);
            SpriteRenderer sr = ProceduralSprites.Child(parent, p.Type, s != null ? s : ProceduralSprites.White(false));
            // Low walk-through decoration (bushes, small rocks, stumps, bones...) lies on the ground: under every hero and monster, never y-sorted over them.
            // Tall walk-through art (the Willowbrook palisade, reeds) keeps the y-sort so the hero can pass behind it.
            Sprite[] views = Map3D.Active && View25D.Active && s != null ? View25DDirectionalProps.Views(p.Type) : null; // multi-view art: stands and turns like a hero
            // wall runs (the raised terrace's retaining wall, the town wall either side of the gate): one fixed card facing south instead of a billboard
            // per segment, so a turned camera sees a straight wall, not a saw of separately turned pieces (26/11)
            if (views == null && View25D.Active && s != null && IsWallRun(p.Type)) views = new[] { s };
            sr.sortingOrder = IsClimbStairs(p.Type) && View25D.Active ? GameConfig.GroundOrder + 5 : LiesFlat(p.Type, d) && views == null ? GameConfig.GroundOrder + 4 : GameConfig.OrderBase(y);
            if (s != null)
            {
                sr.transform.position = new Vector3(x, y, 0f);
                if (View25D.Active && sr.sortingOrder != GameConfig.GroundOrder + 4)
                {
                    View25D.StandProp(sr); // --view25d: tall props stand up at their foot line
                    if (Map3D.Active) Map3DProps.MakeCutout(sr); // 3D map: cutout, real shadows in and out
                    if (views != null) View25DDirectionalProps.Register(sr, views);
                    if (IsClimbStairs(p.Type)) View25DPropFade.Unregister(sr); // under the hero anyway: never thinned while he climbs
                }
                else
                {
                    View25DLighting.ApplyGround(sr); // flat decor on a RO3 2.5D map: lit like the ground
                    if (Map3D.Active) Map3DProps.LiftFlat(sr);
                }
                return;
            }
            sr.color = MissingPropTint;
            int footH = Mathf.Max(1, d.FootH); // walk-through props (FootH 0) still need a visible placeholder when their art is missing
            sr.transform.localScale = new Vector3(d.FootW * GameConfig.TileUnits, footH * GameConfig.TileUnits, 1f);
            sr.transform.position = new Vector3(x, y + footH * GameConfig.TileUnits * 0.5f, 0f);
        }

        /// <summary>Cached sprite of a prop type (pivot bottom-centre), or null when its PNG is missing.</summary>
        public static Sprite PropSprite(string type)
        {
            Sprite s;
            if (PropSprites.TryGetValue(type, out s) && s != null) return s;
            string path = "Sprites/village/props/" + type;
            // Checked here because SpriteSheet.Load logs an error per call, which is one per prop instance on every map load.
            if (Resources.Load<Texture2D>(path) == null)
            {
                if (MissingProps.Add(type)) Debug.LogWarning("[ZenoniaProto] Missing prop art Resources/" + path + ".png; drawing its footprint");
                return null;
            }
            Texture2D tex = SpriteSheet.Load(path);
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), PpuProp, 0, SpriteMeshType.FullRect);
            PropSprites[type] = s;
            return s;
        }
    }
}
