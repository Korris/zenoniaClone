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
    public sealed partial class PlayerRpg
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
        /// <summary>A quest moved: accepted, a kill counted, handed in.</summary>
        public event Action QuestChanged;
        /// <summary>A Wanted Boss claim was tried (result, boss, day key). Ok = paid and WantedDay set; failed = already claimed that day.</summary>
        public event Action<ActionResult, WantedBossDef, int> WantedClaimed;

        public PlayerRpg(PlayerState state) { State = state; }

        void Touch() { if (Changed != null) Changed(); }

        /// <summary>Adds EXP (level-ups fully heal). Returns levels gained.</summary>
        public int GainExp(int exp, Vector2 at)
        {
            int levels = State.GainExp(exp);
            if (ExpGained != null) ExpGained(exp, levels, at);
            if (levels > 0) AccountLevelReached(); // Spirit at 5, tickets at 10 / 20 ... (account, PlayerRpg.Account.cs)
            Touch();
            return levels;
        }

        public void RaiseBossKilled() { if (BossKilled != null) BossKilled(); }

        /// <summary>This player got credit for a kill. Nothing is raised when no quest wants this monster.</summary>
        public void QuestKill(string monsterType)
        {
            if (!State.Quests.CountKill(monsterType)) return;
            if (QuestChanged != null) QuestChanged();
            Touch();
        }

        /// <summary>Authority only: pays today's Wanted Boss reward once per day (PlayerState.ClaimWanted) and tells the listeners.</summary>
        public ActionResult ClaimWanted(WantedBossDef def, int dayKey)
        {
            ActionResult r = State.ClaimWanted(dayKey, def);
            if (r.Ok) { Touch(); AccountWantedClaimed(); } // the account's first claim of the day: a pet ticket (AccountRewards)
            if (WantedClaimed != null) WantedClaimed(r, def, dayKey);
            return r;
        }

        ActionResult QuestStep(ActionResult r)
        {
            if (r.Ok && QuestChanged != null) QuestChanged();
            return r;
        }

        /// <summary>A trade is being written with the bag room it counted on: new equipment waits on the ground for that moment.</summary>
        public bool BagHeld;

        public ActionResult Pickup(LootEntry e)
        {
            if (BagHeld && e.Kind == LootKind.Equipment) return ActionResult.Fail("");
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

        /// <summary>The bag's Use: this size becomes the quick one even when nothing could be drunk.</summary>
        public ActionResult UsePotion(bool hp, int size)
        {
            ActionResult r = State.UsePotion(hp, size);
            Touch();
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
        /// index into Slots.All; Use drinks a potion and PotionBuy buys one (PotionCatalog.PackSlot); Enhance rolls the stone upgrade; Sort orders the bag.
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
                case InvOp.Use:
                    int useSize = PotionCatalog.SlotSize(slot);
                    return useSize < 0 ? UsePotion(PotionCatalog.SlotIsHp(slot)) : UsePotion(PotionCatalog.SlotIsHp(slot), useSize);
                case InvOp.PotionBuy: r = State.BuyPotion(PotionCatalog.SlotIsHp(slot), PotionCatalog.SlotSize(slot)); break;
                case InvOp.PetEquip:
                    int teamSlot, petIndex;
                    PetRules.DecodeSlot(slot, out teamSlot, out petIndex);
                    r = State.SetTeamSlot(teamSlot, petIndex < 0 ? "" : PetConfig.ByIndex(petIndex) != null ? PetConfig.ByIndex(petIndex).Id : "?");
                    break;
                case InvOp.GearBuy:
                    int gearTier, gearSlot;
                    HeadgearDef hat;
                    r = HeadgearCatalog.TryShopHat(slot, out hat) ? State.BuyHeadgear(hat.Id) : GearShops.Decode(slot, out gearTier, out gearSlot) ? State.BuyGear(gearTier, gearSlot) : ActionResult.Fail("Not sold here");
                    break;
                case InvOp.Discard:
                    r = State.Discard(itemUid); // refused for an item that holds Soul Gems
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
                case InvOp.SkillLearn: r = LearnSkill(slot); break;
                case InvOp.SkillAssign: r = AssignSkill(slot, itemUid); break;
                case InvOp.SkillRespec: r = SkillRespec.Apply(State); break;
                case InvOp.QuestAccept: r = QuestStep(State.AcceptQuest(itemUid)); break;
                case InvOp.QuestTurnIn: r = QuestStep(State.TurnInQuest(itemUid)); break;
                case InvOp.ChestOpen: r = State.OpenChest(itemUid, PlayerState.UnixNowMs, RpgRandom.Default); break; // distance: the caller (ServerDungeonActions / DungeonInteract)
                case InvOp.WaypointTouch:
                    bool fresh = State.UnlockWaypoint(itemUid); // may complete a "reach" quest
                    r = fresh || State.HasWaypoint(itemUid) ? ActionResult.Success() : ActionResult.Fail("No such crystal");
                    if (fresh) QuestStep(r);
                    break;
                case InvOp.GemConvert: r = slot > 1 ? ActionResult.Fail("No such gem kind") : CardOps.Convert(State, itemUid, slot == 1 ? GemKind.Guard : GemKind.Power); break; // ItemUid = card id, Slot 0 Power / 1 Guard
                case InvOp.GemSocket: // ItemUid = item uid, Slot = GemSockets.Code of the gem; it goes into the next free socket
                    string sockCard; GemKind sockKind;
                    r = GemSockets.TryDecode(slot, out sockCard, out sockKind) ? CardOps.Socket(State, itemUid, sockCard, sockKind) : ActionResult.Fail("No such gem");
                    break;
                case InvOp.GemRemove: // ItemUid = item uid, Slot = GemSockets.Code of the gem to take out (not the socket index: a repeated request cannot take the neighbour)
                    string remCard; GemKind remKind;
                    r = GemSockets.TryDecode(slot, out remCard, out remKind) ? CardOps.RemoveGem(State, itemUid, remCard, remKind) : ActionResult.Fail("No such gem");
                    break;
                case InvOp.AlbumClaim: r = AlbumRules.Claim(State, slot); break; // Slot = milestone 0-3
                case InvOp.CardSell: r = CardOps.Sell(State, itemUid, slot); break; // ItemUid = card id, Slot = copies
                case InvOp.CardLock: r = CardOps.SetLock(State, itemUid, slot == 1); break; // ItemUid = card id, Slot 1 lock / 0 unlock
                default: r = ActionResult.Fail("Unknown action"); break;
            }
            Touch();
            return r;
        }

        /// <summary>Skill tree request: spend 1 SP on tree skill `index` (learn or rank up), validated by the same rules as the offline window.</summary>
        ActionResult LearnSkill(int index)
        {
            SkillDef def = SkillTable.Get(State.ClassId, index);
            if (def == null) return ActionResult.Fail("No such skill");
            string why = SkillTreeActions.Reason(State, def);
            if (why != null) return ActionResult.Fail(why);
            State.LearnSkill(index);
            return ActionResult.Success(State.Skills[index] == 1 ? "Learned " + def.Name : def.Name + " rank " + State.Skills[index]);
        }

        /// <summary>Skill tree request: put tree skill `skill` (255 = clear) into magazine slot `slotText` (0-7, sent in the item uid field).</summary>
        ActionResult AssignSkill(byte skill, string slotText)
        {
            int slot;
            if (!int.TryParse(slotText, out slot) || slot < 0 || slot >= SkillLoadout.Slots) return ActionResult.Fail("Invalid slot");
            if (skill == NoSkill) { State.Loadout.Unassign(slot); return ActionResult.Success("Slot cleared"); }
            return State.Loadout.Place(slot, skill, State.Skills) ? ActionResult.Success("Assigned") : ActionResult.Fail("Learn the skill first");
        }

        /// <summary>Slot byte of an Assign request that clears the magazine slot.</summary>
        public const byte NoSkill = 255;

        Item Find(string uid) { return State.Inventory.Find(i => i.uid == uid); }

        Item EquippedByUid(string uid)
        {
            for (int i = 0; i < Slots.All.Length; i++) { Item e = State.GetEquipped(Slots.All[i]); if (e != null && e.uid == uid) return e; }
            return null;
        }
    }
}
