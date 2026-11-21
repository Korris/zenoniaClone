using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Zone server side of the dungeon chests and warp crystals (Rootburrow V5, DungeonPoints). Never the client's word: the distance is from where the server has the
    /// hero, the chest roll and the refill time are the server's, a warp checks the crystals this character attuned and its last hit. Chest / attune go on through
    /// PlayerRpg.ApplyInvAction (the same rules as offline); a warp moves the hero through ZoneTransferService.Teleport.
    /// </summary>
    public static class ServerDungeonActions
    {
        /// <summary>Null when a ChestOpen / WaypointTouch may go on: the chest / crystal is on the hero's map and in reach.</summary>
        public static string Refusal(NetPlayer p, IGameMap map, InvActionMsg m)
        {
            if (p.Hero.IsDead) return "Not while down";
            if (m.Op == InvOp.ChestOpen)
            {
                ChestDef c = DungeonPoints.Chest(m.ItemUid);
                if (c == null || c.MapId != map.Id) return "No such chest here";
                return Distance(p, c.Tx, c.Ty) > DungeonPoints.ServerReachUnits ? "Too far from the chest" : null;
            }
            WaypointDef w = DungeonPoints.Waypoint(m.ItemUid);
            if (w == null || w.MapId != map.Id) return "No such crystal here";
            return Distance(p, w.Tx, w.Ty) > DungeonPoints.ServerReachUnits + 8f ? "Too far from the crystal" : null; // attuning happens on approach: a little more slack
        }

        /// <summary>InvOp.WaypointWarp, ItemUid "fromId>toId": validated, answered, then the hero is moved (a refusal is answered with the reason).</summary>
        public static void Warp(ServerReplication repl, NetPlayer p, IGameMap map, InvActionMsg m)
        {
            string text = m.ItemUid ?? "";
            int cut = text.IndexOf('>');
            string from = cut > 0 ? text.Substring(0, cut) : "", to = cut > 0 ? text.Substring(cut + 1) : "";
            WaypointDef here = DungeonPoints.Waypoint(from);
            string refusal = p.Hero.IsDead ? "Not while down"
                : here == null || here.MapId != map.Id || Distance(p, here.Tx, here.Ty) > DungeonPoints.ServerReachUnits ? "Stand at a warp crystal"
                : DungeonPoints.WarpRefusal(p.Rpg.State, from, to, p.Hero.MsSinceHurt);
            string mapId = null; int tx = 0, ty = 0;
            if (refusal == null && !DungeonPoints.Target(to, out mapId, out tx, out ty)) refusal = "No such crystal";
            else if (refusal == null && (ZoneTransferService.Active == null || !ZoneTransferService.Active.Teleport(p, mapId, tx, ty))) refusal = "The crystal is silent (map not hosted)";
            repl.SendRpgState(p, m.ReqId, refusal == null, refusal ?? "");
            p.RpgDirty = false;
        }

        static float Distance(NetPlayer p, int tx, int ty) { return Vector2.Distance(p.Hero.Position, FrostHollowMap.TileFeet(tx, ty, 2f)); }
    }
}
