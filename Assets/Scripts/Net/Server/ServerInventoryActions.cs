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
            if (m.Op == InvOp.HairStyle) // a look choice only, no state and no reply: everyone near sees the new head
            {
                if (p.Hero.NetHairStyle != m.Slot) { p.Hero.NetHairStyle = m.Slot; NetEventBus.Appearance(p.Hero); }
                return;
            }
            bool questOp = m.Op == InvOp.QuestAccept || m.Op == InvOp.QuestTurnIn;
            bool shopOp = m.Op == InvOp.PotionBuy || m.Op == InvOp.PetBuy || m.Op == InvOp.GearBuy;
            string refusal = TradeService.IsTrading(p) && TradeBook.BlocksInventoryOp(m.Op) ? "Not while trading" : shopOp ? ShopRefusal(p, m) : questOp ? QuestRefusal(p, m) : null;
            if (refusal == null && m.Op == InvOp.Enhance && ZonePersistence.Active != null) { ZonePersistence.Active.Enhance(repl, p, m); return; } // with real characters only the API rolls an enhance: the answer comes later
            string before = LookKey(p);
            ActionResult r = refusal != null ? ActionResult.Fail(refusal) : p.Rpg.ApplyInvAction(m.Op, m.ItemUid, m.Slot);
            if (LookKey(p) != before) LookChanged(p);
            if ((m.Op == InvOp.PetBuy || m.Op == InvOp.GearBuy || questOp) && r.Ok && ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p); // a large sum of gold, a quest reward: written now, not at the next interval
            repl.SendRpgState(p, m.ReqId, r.Ok, r.Text);
            p.RpgDirty = false;
        }

        /// <summary>A purchase counts only with the seller on the map this hero is on, in reach of where the server has the hero. Never the client's word.</summary>
        static string ShopRefusal(NetPlayer p, InvActionMsg m)
        {
            IGameMap map = HeroMap(p);
            float distance = DistanceToNpc(p, map, m.ItemUid);
            if (m.Op == InvOp.PetBuy) return PetConfig.Refusal(m.ItemUid, map.Id, m.Slot, distance);
            if (m.Op == InvOp.GearBuy) return GearShops.Refusal(m.ItemUid, map.Id, m.Slot, distance);
            return PotionShops.Refusal(m.ItemUid, map.Id, PotionCatalog.SlotSize(m.Slot), distance);
        }

        /// <summary>The same for a quest: ItemUid is the quest, and its giver has to be in reach. Whether this step of the quest is allowed is PlayerState's to say.</summary>
        static string QuestRefusal(NetPlayer p, InvActionMsg m)
        {
            QuestDef q = QuestCatalog.Get(m.ItemUid);
            return QuestCatalog.Refusal(m.ItemUid, q != null ? DistanceToNpc(p, HeroMap(p), q.GiverNpcId) : float.MaxValue);
        }

        static IGameMap HeroMap(NetPlayer p) { return p.Hero.HomeZone != null && p.Hero.HomeZone.Map != null ? p.Hero.HomeZone.Map : MapRegistry.Current; }

        /// <summary>From where the server has the hero to this villager; MaxValue when he is not on this map.</summary>
        static float DistanceToNpc(NetPlayer p, IGameMap map, string npcId)
        {
            float distance = float.MaxValue;
            foreach (MapNpc n in map.Npcs)
                if (n.Id == npcId) distance = UnityEngine.Vector2.Distance(p.Hero.Position, FrostHollowMap.TileFeet(n.Tx, n.Ty, 2f));
            return distance;
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
