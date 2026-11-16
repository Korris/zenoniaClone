using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Outcome of a bag action. Message = what to tell the player (null = nothing). Pending = the request went to the server: its RpgState
    /// reply redraws the bag, so the view must not redraw or announce anything itself. Ok = it worked (for a pending Equip: it can work).
    /// </summary>
    public struct BagResult
    {
        public bool Ok, Pending;
        public string Message;
        public static BagResult Sent(bool ok = true) { return new BagResult { Ok = ok, Pending = true }; }
        public static BagResult Done(bool ok, string message) { return new BagResult { Ok = ok, Message = message }; }
    }

    /// <summary>The next +level of an item, its success chance and the stat it raises, or why it cannot be enhanced.</summary>
    public struct BagEnhanceView { public bool Can, Max; public string Reason, StatLabel; public int From, To, StatFrom, StatTo, Owned, Chance; }

    /// <summary>
    /// Everything the bag can do to the player, shared by both looks of the bag (InventoryPanel, ArmoryWindow). Offline and on the host the
    /// PlayerState changes here (RpgSession autosaves it); an online client only sends the InvOp and the server answers with a new RpgState.
    /// The rules themselves live in Rpg/ (PlayerState.Equip, EnhanceSystem ...): this class only calls them.
    /// </summary>
    public static class BagActions
    {
        /// <summary>TEMPORARY "-1" enhance test button (InventoryPanel.DevDowngrade.cs): test builds, offline only.</summary>
        public static bool DowngradeAvailable { get { return !ReleaseBuild.IsRelease && !NetMode.IsClient; } }

        /// <summary>Drinks one potion of this stack; it also becomes the quick-button size.</summary>
        public static BagResult UsePotion(PlayerState p, BagSupply kind)
        {
            bool hp = BagModel.PotionIsHp(kind);
            int size = BagModel.PotionSize(kind);
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Use, "", PotionCatalog.PackSlot(hp, size)); return BagResult.Sent(); }
            ActionResult r = p.UsePotion(hp, size);
            return BagResult.Done(r.Ok, r.Text);
        }

        /// <summary>Wears a bag item (the piece it replaces goes to the bag). The level gate is checked here too so the player gets the reason at once; the server checks again.</summary>
        public static BagResult Equip(PlayerState p, Item item)
        {
            if (p.CanEquip(item) && !p.MeetsLevel(item)) return BagResult.Done(false, "Requires level " + EquipLevel.Required(item));
            if (NetMode.IsClient)
            {
                NetClient.SendInvAction(InvOp.Equip, item.uid, (byte)System.Math.Max(0, Slots.Index(item.slot)));
                return BagResult.Sent(p.CanEquip(item));
            }
            ActionResult r = p.Equip(item);
            if (r.Ok) RpgSession.SyncGear();
            return BagResult.Done(r.Ok, r.Ok ? "Equipped " + item.name : r.Text);
        }

        /// <summary>Takes a worn item off, back into the bag.</summary>
        public static BagResult Unequip(PlayerState p, Item item)
        {
            if (NetMode.IsClient)
            {
                NetClient.SendInvAction(InvOp.Unequip, item.uid, (byte)System.Math.Max(0, Slots.Index(item.slot)));
                return BagResult.Sent();
            }
            ActionResult r = p.Unequip(item.slot);
            if (r.Ok) RpgSession.SyncGear();
            return BagResult.Done(r.Ok, r.Ok ? "Unequipped" : r.Text);
        }

        /// <summary>One enhance attempt on a worn or bag item (costs a stone, also when it fails).</summary>
        public static BagResult Enhance(PlayerState p, Item item)
        {
            BagEnhanceView view = Preview(p, item);
            if (!view.Can) return BagResult.Done(false, view.Reason);
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Enhance, item.uid, 0); return BagResult.Sent(); }
            int bandBefore = EnhanceStats.VisualBand(item);
            EnhanceResult r = EnhanceSystem.Attempt(p, item);
            if (!r.Ok) return BagResult.Done(false, "Cannot enhance");
            if (!r.Success) return BagResult.Done(false, "Failed... stone lost (+" + r.Level + " kept)");
            if (p.GetEquipped(item.slot) == item) RpgSession.SyncGear();
            return BagResult.Done(true, "Success! " + EnhanceStats.DisplayName(item) + (EnhanceStats.VisualBand(item) > bandBefore ? "  New look!" : ""));
        }

        /// <summary>Throws a bag item away for good. The view asks "sure?" first.</summary>
        public static BagResult Discard(PlayerState p, Item item)
        {
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Discard, item.uid, 0); return BagResult.Sent(); }
            p.RemoveFromInventory(item.uid);
            return BagResult.Done(true, "Discarded " + item.name);
        }

        /// <summary>The old "Sort" button: gear by slot then level, supplies by count. Online the server reorders the bag.</summary>
        public static BagResult TidyBag(BagModel model, PlayerState p)
        {
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Sort, "", 0); return BagResult.Sent(); }
            model.TidyBag(p);
            return BagResult.Done(true, null);
        }

        /// <summary>TEMPORARY test tool: lowers the item's + by one, free and always succeeding.</summary>
        public static BagResult DevDowngrade(PlayerState p, Item item)
        {
            if (item == null || !DowngradeAvailable || !EnhanceStats.IsEnhanceable(item) || item.enhance <= 0) return BagResult.Done(false, null);
            item.enhance--;
            p.RecomputeStats();
            if (p.GetEquipped(item.slot) == item) RpgSession.SyncGear();
            return BagResult.Done(true, "Test: " + EnhanceStats.DisplayName(item));
        }

        /// <summary>What the next enhance attempt on `item` would do.</summary>
        public static BagEnhanceView Preview(PlayerState p, Item item)
        {
            var v = new BagEnhanceView { Owned = p != null ? p.UpgradeStone : 0, StatLabel = "ATK" };
            if (p == null || !EnhanceStats.IsEnhanceable(item)) { v.Reason = "Weapons only"; return v; }
            string key = EnhanceStats.StatKey(item);
            int baseValue = item.stats.GetInt(key);
            v.StatLabel = key == "atk" ? "ATK" : key == "def" ? "DEF" : key == "maxHp" ? "Max HP" : key == "maxMp" ? "Max MP" : key;
            v.From = EnhanceStats.Level(item);
            v.StatFrom = baseValue + EnhanceStats.Bonus(baseValue, v.From, EnhanceStats.Scale(item));
            if (v.From >= EnhanceConfig.MaxEnhance) { v.Max = true; v.To = v.From; v.StatTo = v.StatFrom; v.Reason = "Max +" + EnhanceConfig.MaxEnhance; return v; }
            IUpgradeMethod method = StoneUpgradeMethod.Instance;
            v.To = v.From + 1;
            v.StatTo = baseValue + EnhanceStats.Bonus(baseValue, v.To, EnhanceStats.Scale(item));
            v.Chance = RpgMath.Round(method.SuccessChance(v.From) * 100.0);
            v.Can = method.CanPay(p, v.From);
            if (!v.Can) v.Reason = "Need Upgrade Stone";
            return v;
        }
    }
}
