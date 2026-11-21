using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the dungeon chests and warp crystals (Rootburrow V5, DungeonPoints), offline after spawning (capture runs never save):
    ///   --attune-all            attune every warp crystal of the dungeon (the warp list shows them all open)
    ///   --open-chest ID         open chest ID through the real rule (PlayerRpg: loot into the bag, refill timer): stand next to it (--tp) to see the lid FX
    ///   --open-waypoints        open the warp list of this map's crystal (what "Warp" / E does there)
    /// </summary>
    public partial class DebugCapture
    {
        void DebugDungeonPoints(HeroController hero)
        {
            string[] args = Environment.GetCommandLineArgs();
            PlayerRpg rpg = hero != null ? hero.Owner : null;
            if (rpg == null || !NetMode.IsOffline) return;
            if (Array.IndexOf(args, "--attune-all") >= 0)
                foreach (WaypointDef w in DungeonPoints.Waypoints) rpg.ApplyInvAction(InvOp.WaypointTouch, w.Id, 0);
            string chest = Arg(args, "--open-chest");
            if (chest != null) Debug.Log("[ZenoniaProto] capture: " + rpg.ApplyInvAction(InvOp.ChestOpen, chest, 0).Text);
            if (Array.IndexOf(args, "--open-waypoints") >= 0)
                foreach (WaypointDef w in DungeonPoints.WaypointsOn(MapRegistry.Current.Id)) { WaypointWindow.Open(w); break; }
        }
    }
}
