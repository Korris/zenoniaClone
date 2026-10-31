using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Inventory requests of one player (equip / unequip / use / discard / sort / enhance): validated and applied by the same PlayerRpg rules the
    /// offline bag uses, then answered with the player's full state so the client mirror never diverges. Equipment changes update the shared look.
    /// </summary>
    public static class ServerInventoryActions
    {
        public static void Apply(ServerReplication repl, NetPlayer p, InvActionMsg m)
        {
            if (p.Hero == null) return;
            Zone.Enter(p.Hero.HomeZone); // a network callback: the events it raises belong to this hero's zone
            string before = LookKey(p);
            string refusal = m.Op == InvOp.PotionBuy ? ShopRefusal(p, m) : null;
            ActionResult r = refusal != null ? ActionResult.Fail(refusal) : p.Rpg.ApplyInvAction(m.Op, m.ItemUid, m.Slot);
            if (LookKey(p) != before)
            {
                p.Hero.SyncGearView();
                NetEventBus.Appearance(p.Hero);
            }
            repl.SendRpgState(p, m.ReqId, r.Ok, r.Text);
            p.RpgDirty = false;
        }

        /// <summary>A purchase counts only with the seller on the map this hero is on, in reach of where the server has the hero. Never the client's word.</summary>
        static string ShopRefusal(NetPlayer p, InvActionMsg m)
        {
            IGameMap map = p.Hero.HomeZone != null && p.Hero.HomeZone.Map != null ? p.Hero.HomeZone.Map : MapRegistry.Current;
            float distance = float.MaxValue;
            foreach (MapNpc n in map.Npcs)
                if (n.Id == m.ItemUid) distance = UnityEngine.Vector2.Distance(p.Hero.Position, FrostHollowMap.TileFeet(n.Tx, n.Ty, 2f));
            return PotionShops.Refusal(m.ItemUid, map.Id, PotionCatalog.SlotSize(m.Slot), distance);
        }

        /// <summary>Weapon / offhand identity + enhance level: changes when the visible gear changes.</summary>
        static string LookKey(NetPlayer p)
        {
            Item w = p.Hero.Rpg.GetEquipped(Slots.Weapon), o = p.Hero.Rpg.GetEquipped(Slots.Offhand);
            return (w != null ? w.uid + w.enhance : "-") + "|" + (o != null ? o.uid + o.enhance : "-");
        }
    }
}
