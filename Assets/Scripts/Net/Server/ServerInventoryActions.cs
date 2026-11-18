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
            if (m.Op == InvOp.HairStyle) // a look choice only, no reply: everyone near sees the new head; kept with the character at the next save
            {
                CharacterAppearanceData look = p.Rpg.State.Appearance;
                if (look != null && (look.hairStyle != m.Slot || !look.stored)) { look.stored = true; look.hairStyle = m.Slot; p.RpgDirty = true; }
                if (p.Hero.NetHairStyle != m.Slot) { p.Hero.NetHairStyle = m.Slot; NetEventBus.Appearance(p.Hero); }
                return;
            }
            if (m.Op == InvOp.Appearance) { ApplyAppearance(p, m.ItemUid); return; }
            bool questOp = m.Op == InvOp.QuestAccept || m.Op == InvOp.QuestTurnIn;
            bool shopOp = m.Op == InvOp.PotionBuy || m.Op == InvOp.PetBuy || m.Op == InvOp.GearBuy || m.Op == InvOp.BallBuy;
            string refusal = TradeService.IsTrading(p) && TradeBook.BlocksInventoryOp(m.Op) ? "Not while trading" : shopOp ? ShopRefusal(p, m) : questOp ? QuestRefusal(p, m) : null;
            if (refusal == null && m.Op == InvOp.Enhance && ZonePersistence.Active != null) { ZonePersistence.Active.Enhance(repl, p, m); return; } // with real characters only the API rolls an enhance: the answer comes later
            if (refusal == null && InvOpInfo.IsAccountOp(m.Op)) { AccountAction(repl, p, m); return; } // pet unlock / growth / gacha / balls: answered once the account is stored
            if (refusal == null && m.Op == InvOp.PetCapture) { Capture(repl, p, m); return; }
            string before = LookKey(p);
            ActionResult r = refusal != null ? ActionResult.Fail(refusal) : p.Rpg.ApplyInvAction(m.Op, m.ItemUid, m.Slot);
            if (LookKey(p) != before) LookChanged(p);
            if ((m.Op == InvOp.PetBuy || m.Op == InvOp.GearBuy || questOp) && r.Ok && ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p); // a large sum of gold, a quest reward: written now, not at the next interval
            repl.SendRpgState(p, m.ReqId, r.Ok, r.Text);
            p.RpgDirty = false;
        }

        /// <summary>InvOp.Appearance: style + hair / outfit colour picked on the client (character creation, or the device's old choice moved to the server).
        /// Validated (CharacterAppearanceData.TryApplyOpText), the headgear is never taken from the client; stored at once, shown to everyone near. No reply.</summary>
        static void ApplyAppearance(NetPlayer p, string text)
        {
            PlayerState state = p.Rpg.State;
            CharacterAppearanceData next;
            if (!CharacterAppearanceData.TryApplyOpText(text, state.Appearance, out next) || next.SameLook(state.Appearance)) return;
            next.classId = state.ClassId;
            state.Appearance = next;
            p.Hero.NetHairStyle = (byte)next.hairStyle;
            NetEventBus.Appearance(p.Hero);
            p.RpgDirty = true;
            if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
        }

        /// <summary>Pet buy / star up / gacha pull / UR exchange: PlayerRpg runs it through the account queue; the answer (state + text) goes out when it is stored or refused.</summary>
        static void AccountAction(ServerReplication repl, NetPlayer p, InvActionMsg m)
        {
            p.Rpg.ApplyAccountAction(m.Op, m.Slot, m.ItemUid, r =>
            {
                if (p.Hero == null) return; // left meanwhile: the account is stored, the next join shows it
                Zone.Enter(p.Hero.HomeZone);
                if (r.Ok && ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p); // the gold paid with it
                repl.SendRpgState(p, m.ReqId, r.Ok, r.Text);
                p.RpgDirty = false;
            });
        }

        /// <summary>A ball thrown at monster ItemUid (its NetId): PetCaptureService checks the target and the account, the answer comes when stored.</summary>
        static void Capture(ServerReplication repl, NetPlayer p, InvActionMsg m)
        {
            ushort id;
            if (!ushort.TryParse(m.ItemUid, out id)) { repl.SendRpgState(p, m.ReqId, false, "No such monster"); p.RpgDirty = false; return; }
            PetCaptureService.Request(p.Hero, p.Hero.HomeZone, id, m.Slot == 1, r =>
            {
                if (p.Hero == null) return;
                Zone.Enter(p.Hero.HomeZone);
                if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
                repl.SendRpgState(p, m.ReqId, r.Ok, r.Text);
                p.RpgDirty = false;
            });
        }

        /// <summary>A purchase counts only with the seller on the map this hero is on, in reach of where the server has the hero. Never the client's word.</summary>
        static string ShopRefusal(NetPlayer p, InvActionMsg m)
        {
            IGameMap map = HeroMap(p);
            float distance = DistanceToNpc(p, map, m.ItemUid);
            if (m.Op == InvOp.PetBuy) return PetConfig.Refusal(m.ItemUid, map.Id, m.Slot, distance);
            if (m.Op == InvOp.BallBuy) return PetConfig.Refusal(m.ItemUid, map.Id, PetConfig.IndexOf(PetConfig.Get("imp")), distance); // the same merchants sell balls
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
            Item w = p.Hero.Rpg.GetEquipped(Slots.Weapon), o = p.Hero.Rpg.GetEquipped(Slots.Offhand);
            return (w != null ? w.uid + w.enhance : "-") + "|" + (o != null ? o.uid + o.enhance : "-") + "|" + p.Hero.Rpg.PetId;
        }
    }
}
