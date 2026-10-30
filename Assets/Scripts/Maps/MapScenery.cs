using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The things a map draws on top of its painted backdrop: HD props y-sorted by their base (web prop-renderer.js: bottom of the footprint = sort key),
    /// the animated portal tiles (3 frames, 140 ms) and the villagers. Props are static, so each is a single SpriteRenderer.
    /// </summary>
    public static class MapScenery
    {
        const float PpuProp = 4f;             // web HD props: 4 source px per world unit
        const float PortalFrameSeconds = 0.14f;
        static readonly Dictionary<string, Sprite> PropSprites = new Dictionary<string, Sprite>();

        public static void Build(Transform parent, IGameMap map)
        {
            foreach (MapProp p in map.Props) BuildProp(parent, p);
            // Portals only work offline (the zone server does not transfer heroes between maps yet), so online they are not drawn either.
            if (NetMode.IsOffline) foreach (MapPortal portal in map.Portals) BuildPortal(parent, portal);
            if (NetMode.IsOffline) TownNpc.CreateAll(parent, map);
        }

        static void BuildProp(Transform parent, MapProp p)
        {
            PropDef d;
            if (!VillagePropDefs.TryGet(p.Type, out d)) return;
            Sprite s = PropSprite(p.Type);
            if (s == null) return;
            // Horizontal centre of the footprint, bottom edge of its lowest row (web y-down row ty -> y-up MapH - (ty+1)*16).
            float x = (p.Tx + d.FootW * 0.5f) * GameConfig.TileUnits, y = GameConfig.MapH - (p.Ty + 1) * GameConfig.TileUnits;
            SpriteRenderer sr = ProceduralSprites.Child(parent, p.Type, s);
            sr.transform.position = new Vector3(x, y, 0f);
            sr.sortingOrder = GameConfig.OrderBase(y);
        }

        static Sprite PropSprite(string type)
        {
            Sprite s;
            if (PropSprites.TryGetValue(type, out s) && s != null) return s;
            Texture2D tex = SpriteSheet.Load("Sprites/village/props/" + type);
            if (tex == null) return null;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), PpuProp, 0, SpriteMeshType.FullRect);
            PropSprites[type] = s;
            return s;
        }

        static void BuildPortal(Transform parent, MapPortal portal)
        {
            Sprite[][] frames = SpriteSheet.Grid("Sprites/village/tiles/portal", 64, 64, PpuProp, Vector2.zero);
            if (frames == null || frames.Length == 0) return;
            SpriteRenderer sr = ProceduralSprites.Child(parent, "Portal_" + portal.TargetMapId, frames[0][0]);
            sr.transform.position = new Vector3(portal.Tx * GameConfig.TileUnits, GameConfig.MapH - (portal.Ty + 1) * GameConfig.TileUnits, 0f);
            sr.sortingOrder = GameConfig.GroundOrder + 1; // flat on the ground, under every entity
            sr.gameObject.AddComponent<PortalAnim>().Frames = frames[0];
        }

        /// <summary>Cycles the portal circle frames.</summary>
        sealed class PortalAnim : MonoBehaviour
        {
            public Sprite[] Frames;
            SpriteRenderer sr;
            void Awake() { sr = GetComponent<SpriteRenderer>(); }
            void Update() { sr.sprite = Frames[(int)(Time.time / PortalFrameSeconds) % Frames.Length]; }
        }
    }
}
