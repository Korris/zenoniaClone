using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shared part of the three floors of Rootburrow V4 (21/11/2026, user: "Rootburrow vẫn chưa đủ độ sâu"): each floor is a 72 x 48 dark violet 2.5D cave
    /// (View25D.Maps, View25DProfile.For: darker every floor) whose rows come from Tools/build-rootburrow-delve-rows.py (T solid, d floor), ground from two
    /// violet swatches in 2 x 2 chunks (Tools/build-map-25d-dungeon-ground.py), standing walls / crystals / rune orbs / stairs as props on solid tiles
    /// (Tools/place-map-25d-dungeon-props.py) that also light the dark (DungeonLights). Floors link by stairs: a stairs mouth prop on the solid tile north of
    /// a tunnel's end, its portal tiles the floor row under it. docs/reference/rootburrow-dark-25d/, plans/261121-1145-dark-dungeon-boss/plan.md.
    /// </summary>
    public abstract class RootburrowFloorMap : TileMapBase
    {
        readonly MapProp[] props;

        protected RootburrowFloorMap(string[] rows, MapProp[] props) : base(rows, "Tw")
        {
            this.props = props;
            foreach (MapProp p in props)
            {
                PropDef d;
                if (VillagePropDefs.TryGet(p.Type, out d)) BlockTiles(p.Tx, p.Ty - d.FootH + 1, d.FootW, d.FootH);
                else Debug.LogError("[ZenoniaProto] unknown village prop " + p.Type);
            }
        }

        public override int GroundChunkCols { get { return 2; } } // 4608 x 3072 px: 2 x 2 chunks of 2304 x 1536
        public override int GroundChunkRows { get { return 2; } }
        public override string Music { get { return "frost-hollow"; } } // no track of its own yet
        public override bool Hostile { get { return true; } }
        public override MapProp[] Props { get { return props; } }
        protected static Vector2 Feet(int tx, int ty) { return FrostHollowMap.TileFeet(tx, ty, 2f); }
    }
}
