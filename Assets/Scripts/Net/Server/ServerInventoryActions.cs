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
            string refusal = TradeService.IsTrading(p) && TradeBook.BlocksInventoryOp(m.Op) ? "Not while trading" : m.Op == InvOp.PotionBuy || m.Op == InvOp.PetBuy ? ShopRefusal(p, m) : null;
            if (refusal == null && m.Op == InvOp.Enhance && ZonePersistence.Active != null) { ZonePersistence.Active.Enhance(repl, p, m); return; } // with real characters only the API rolls an enhance: the answer comes later
            string before = LookKey(p);
            ActionResult r = refusal != null ? ActionResult.Fail(refusal) : p.Rpg.ApplyInvAction(m.Op, m.ItemUid, m.Slot);
            if (LookKey(p) != before) LookChanged(p);
            if (m.Op == InvOp.PetBuy && r.Ok && ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p); // a large sum of gold: written now, not at the next interval
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
            return m.Op == InvOp.PetBuy ? PetConfig.Refusal(m.ItemUid, map.Id, m.Slot, distance) : PotionShops.Refusal(m.ItemUid, map.Id, PotionCatalog.SlotSize(m.Slot), distance);
        }

        public static void LookChanged(NetPlayer p)
        {
            p.Hero.SyncGearView();
            NetEventBus.Appearance(p.Hero);
        }

        /// <summary>Weapon / offhand identity + enhance level and the pet: changes when the visible gear changes.</summary>
        public static string LookKey(NetPlayer p)
        {
            Item w = p.Hero.Rpg.GetEquipped(Slots.Weapon), o = p.Hero.Rpg.GetEquipped(Slots.Offhand), pet = p.Hero.Rpg.GetEquipped(Slots.Pet);
            return (w != null ? w.uid + w.enhance : "-") + "|" + (o != null ? o.uid + o.enhance : "-") + "|" + (pet != null ? pet.uid : "-");
        }
    }
}
