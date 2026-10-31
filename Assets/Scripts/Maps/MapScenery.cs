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
        const string PortalLightSheet = "Sprites/fx/map-portal-light"; // one row of 128 x 224 px cells, 16 x 28 units on screen; the pivot is the ring at the base of the light
        const int PortalCellW = 128, PortalCellH = 224;
        const float PortalPpu = 8f, PortalPivotY = 0.13f;
        const float PortalFrameSeconds = 0.1f;
        const float PortalHaloUnits = 24f, PortalCoreUnits = 11f, PortalHotUnits = 5f, PortalPulsePerSecond = 3f;
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
            if (ShowsPortals) foreach (MapPortal portal in map.Portals) BuildPortal(parent, map, portal);
            if (ShowsVillagers) TownNpc.CreateAll(parent, map);
        }

        static void BuildProp(Transform parent, MapProp p)
        {
            PropDef d;
            if (!VillagePropDefs.TryGet(p.Type, out d)) return;
            // Horizontal centre of the footprint, bottom edge of its lowest row (web y-down row ty -> y-up MapH - (ty+1)*16).
            float x = (p.Tx + d.FootW * 0.5f) * GameConfig.TileUnits, y = GameConfig.MapH - (p.Ty + 1) * GameConfig.TileUnits;
            Sprite s = PropSprite(p.Type);
            SpriteRenderer sr = ProceduralSprites.Child(parent, p.Type, s != null ? s : ProceduralSprites.White(false));
            sr.sortingOrder = GameConfig.OrderBase(y);
            if (s != null) { sr.transform.position = new Vector3(x, y, 0f); return; }
            sr.color = MissingPropTint;
            sr.transform.localScale = new Vector3(d.FootW * GameConfig.TileUnits, d.FootH * GameConfig.TileUnits, 1f);
            sr.transform.position = new Vector3(x, y + d.FootH * GameConfig.TileUnits * 0.5f, 0f);
        }

        static Sprite PropSprite(string type)
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

        /// <summary>
        /// A column of light rising from the portal tile, under every entity. A wide road needs a pair of trigger tiles, but only the upper one is lit so each entry shows
        /// one point. Without the art it falls back to a soft glow on a dark amber halo, because a pale light alone vanishes on snow and a gold one on the dirt roads.
        /// </summary>
        static void BuildPortal(Transform parent, IGameMap map, MapPortal portal)
        {
            foreach (MapPortal other in map.Portals)
                if (other.TargetMapId == portal.TargetMapId && other.Tx == portal.Tx && other.Ty == portal.Ty - 1) return;
            var go = new GameObject("Portal_" + portal.TargetMapId);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((portal.Tx + 0.5f) * GameConfig.TileUnits, GameConfig.MapH - (portal.Ty + 0.5f) * GameConfig.TileUnits, 0f);
            PortalLight light = go.AddComponent<PortalLight>();
            // Checked here because SpriteSheet.Grid logs an error when the art is missing, and the glow below is a supported state.
            Sprite[][] frames = Resources.Load<Texture2D>(PortalLightSheet) != null ? SpriteSheet.Grid(PortalLightSheet, PortalCellW, PortalCellH, PortalPpu, new Vector2(0.5f, PortalPivotY)) : null;
            if (frames != null && frames.Length > 0 && frames[0].Length > 0)
            {
                light.Frames = frames[0];
                light.Body = ProceduralSprites.Child(go.transform, "Light", frames[0][0]);
                light.Body.sortingOrder = GameConfig.GroundOrder + 2;
                return;
            }
            SpriteRenderer halo = ProceduralSprites.Child(go.transform, "Halo", FxSprites.Dot());
            halo.color = new Color(0.27f, 0.11f, 0f, 0.85f);
            halo.transform.localScale = new Vector3(PortalHaloUnits, PortalHaloUnits, 1f);
            halo.sortingOrder = GameConfig.GroundOrder + 1;
            light.Body = ProceduralSprites.Child(go.transform, "Light", FxSprites.Dot());
            light.Body.color = new Color(1f, 0.75f, 0.24f);
            light.Body.sortingOrder = GameConfig.GroundOrder + 2;
            SpriteRenderer hot = ProceduralSprites.Child(light.Body.transform, "Hot", FxSprites.Dot());
            hot.color = new Color(1f, 0.98f, 0.88f);
            hot.transform.localScale = new Vector3(PortalHotUnits / PortalCoreUnits, PortalHotUnits / PortalCoreUnits, 1f);
            hot.sortingOrder = GameConfig.GroundOrder + 3;
        }

        /// <summary>Cycles the light strip, or breathes the procedural glow when there is no strip.</summary>
        sealed class PortalLight : MonoBehaviour
        {
            public Sprite[] Frames;
            public SpriteRenderer Body;
            void Update()
            {
                if (Frames != null) { Body.sprite = Frames[(int)(Time.time / PortalFrameSeconds) % Frames.Length]; return; }
                float k = 0.5f + 0.5f * Mathf.Sin(Time.time * PortalPulsePerSecond);
                Body.transform.localScale = Vector3.one * PortalCoreUnits * (0.9f + 0.2f * k);
                Color c = Body.color; c.a = 0.8f + 0.2f * k; Body.color = c;
            }
        }
    }
}
