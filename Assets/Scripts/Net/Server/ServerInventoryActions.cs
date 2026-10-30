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
            string before = LookKey(p);
            ActionResult r = p.Rpg.ApplyInvAction(m.Op, m.ItemUid, m.Slot);
            if (LookKey(p) != before)
            {
                p.Hero.SyncGearView();
                NetEventBus.Appearance(p.Hero);
            }
            repl.SendRpgState(p, m.ReqId, r.Ok, r.Text);
            p.RpgDirty = false;
        }

        /// <summary>Weapon / offhand identity + enhance level: changes when the visible gear changes.</summary>
        static string LookKey(NetPlayer p)
        {
            Item w = p.Hero.Rpg.GetEquipped(Slots.Weapon), o = p.Hero.Rpg.GetEquipped(Slots.Offhand);
            return (w != null ? w.uid + w.enhance : "-") + "|" + (o != null ? o.uid + o.enhance : "-");
        }
    }
}
