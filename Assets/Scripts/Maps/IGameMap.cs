using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>A walk-through tile that moves the hero to another map (web map definition `portals`).</summary>
    public struct MapPortal
    {
        public int Tx, Ty;               // trigger tile (web rows are top-down: row 0 = top of the map)
        public string TargetMapId;
        public int TargetTx, TargetTy;   // arrival tile in the target map
        public MapPortal(int tx, int ty, string targetMapId, int targetTx, int targetTy)
        { Tx = tx; Ty = ty; TargetMapId = targetMapId; TargetTx = targetTx; TargetTy = targetTy; }
    }

    /// <summary>A painted prop (house, tree, well...) standing on tile (Tx, Ty) = bottom-left footprint tile; see VillagePropDefs.</summary>
    public struct MapProp
    {
        public string Type; public int Tx, Ty;
        public MapProp(string type, int tx, int ty) { Type = type; Tx = tx; Ty = ty; }
    }

    /// <summary>A villager standing on tile (Tx, Ty); Id is the key of NpcDefs.</summary>
    public struct MapNpc
    {
        public string Id; public int Tx, Ty;
        public MapNpc(string id, int tx, int ty) { Id = id; Tx = tx; Ty = ty; }
    }

    /// <summary>
    /// One playable map: tile collision, start tile, portals and the scenery to draw. Every map is 36x24 tiles of 16 units (GameConfig.MapW / MapH),
    /// so the camera clamp and the interest grid do not depend on the map. MapRegistry.Current is the map the local sim runs.
    /// </summary>
    public interface IGameMap
    {
        string Id { get; }
        string Name { get; }
        /// <summary>Resources path (no extension) of the painted backdrop, 64 source px per tile.</summary>
        string GroundTexture { get; }
        /// <summary>Track name in Resources/Audio/music.</summary>
        string Music { get; }
        /// <summary>False = a safe town: no monster spawner.</summary>
        bool Hostile { get; }
        Vector2 PlayerStart { get; }
        MapPortal[] Portals { get; }
        /// <summary>The portals for a hero whose boss gate on this map is open or shut (Portals asks the local save; the server asks per hero). Without a gate: Portals.</summary>
        MapPortal[] PortalsFor(bool gateOpen);
        MapProp[] Props { get; }
        MapNpc[] Npcs { get; }

        bool IsSolidTile(int tx, int ty);
        bool RectBlocked(float x, float y, float w, float h);
        Vector2 Move(Vector2 feet, float dx, float dy, float w, float h, out bool blockedX, out bool blockedY);
    }
}
