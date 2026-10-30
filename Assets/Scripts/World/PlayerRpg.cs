using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One player's RPG progress (non-static): owns the PlayerState and applies every state change (EXP, pickups, potions, death penalty,
    /// inventory actions) while raising events. Offline, RpgSession wraps it and turns the events into toasts / FX / saves; on the server
    /// there is one per connected player and the events become network messages. No Unity components inside, so the rules stay testable.
    /// </summary>
    public sealed class PlayerRpg
    {
        public PlayerState State { get; private set; }

        /// <summary>Any mutation through this object (HUD refresh, online SelfState push).</summary>
        public event Action Changed;
        /// <summary>EXP granted (amount, levels gained, where the monster died).</summary>
        public event Action<int, int, Vector2> ExpGained;
        /// <summary>A pickup attempt: the entry and its result (Ok = applied; failed equipment means the bag is full).</summary>
        public event Action<LootEntry, ActionResult> LootPicked;
        /// <summary>Death penalty applied (EXP lost).</summary>
        public event Action<int> Revived;
        /// <summary>This player contributed to the Frostfang King kill.</summary>
        public event Action BossKilled;

        public PlayerRpg(PlayerState state) { State = state; }

        void Touch() { if (Changed != null) Changed(); }

        /// <summary>Adds EXP (level-ups fully heal). Returns levels gained.</summary>
        public int GainExp(int exp, Vector2 at)
        {
            int levels = State.GainExp(exp);
            if (ExpGained != null) ExpGained(exp, levels, at);
            Touch();
            return levels;
        }

        public void RaiseBossKilled() { if (BossKilled != null) BossKilled(); }

        public ActionResult Pickup(LootEntry e)
        {
            ActionResult r = State.ApplyPickup(e);
            if (LootPicked != null) LootPicked(e, r);
            if (r.Ok) Touch();
            return r;
        }

        public ActionResult UsePotion(bool hp)
        {
            ActionResult r = State.UsePotion(hp);
            if (r.Ok) Touch();
            return r;
        }

        /// <summary>Death: -10% of the current-level EXP, full HP / MP. Returns the EXP lost.</summary>
        public int Revive()
        {
            int lost = State.Revive();
            if (Revived != null) Revived(lost);
            Touch();
            return lost;
        }

        /// <summary>
        /// Server-side inventory request (the same rules the offline bag panel uses). Equip / Discard need the item uid; Unequip uses the slot
        /// index into Slots.All; Use drinks a potion (slot 0 = HP, 1 = MP); Enhance rolls the stone upgrade; Sort orders the bag.
        /// </summary>
        public ActionResult ApplyInvAction(InvOp op, string itemUid, byte slot)
        {
            ActionResult r;
            switch (op)
            {
                case InvOp.Equip:
                    Item item = Find(itemUid);
                    r = item != null ? State.Equip(item) : ActionResult.Fail("No such item");
                    break;
                case InvOp.Unequip:
                    r = slot < Slots.All.Length ? State.Unequip(Slots.All[slot]) : ActionResult.Fail("Invalid slot");
                    break;
                case InvOp.Use: return UsePotion(slot == 0);
                case InvOp.Discard:
                    r = State.RemoveFromInventory(itemUid) ? ActionResult.Success("Discarded") : ActionResult.Fail("No such item");
                    break;
                case InvOp.Sort:
                    State.Inventory.Sort((a, b) => a.slot != b.slot ? Slots.Index(a.slot).CompareTo(Slots.Index(b.slot)) : b.itemLevel.CompareTo(a.itemLevel));
                    r = ActionResult.Success();
                    break;
                case InvOp.Enhance:
                    Item target = Find(itemUid) ?? EquippedByUid(itemUid);
                    if (target == null || !EnhanceStats.IsEnhanceable(target)) { r = ActionResult.Fail("Weapons only"); break; }
                    EnhanceResult er = EnhanceSystem.Attempt(State, target);
                    r = er.Ok ? (er.Success ? ActionResult.Success("Enhanced to +" + er.Level) : ActionResult.Fail("Enhance failed (+" + er.Level + " kept)")) : ActionResult.Fail("Cannot enhance");
                    break;
                default: r = ActionResult.Fail("Unknown action"); break;
            }
            Touch();
            return r;
        }

        Item Find(string uid) { return State.Inventory.Find(i => i.uid == uid); }

        Item EquippedByUid(string uid)
        {
            for (int i = 0; i < Slots.All.Length; i++) { Item e = State.GetEquipped(Slots.All[i]); if (e != null && e.uid == uid) return e; }
            return null;
        }
    }
}
