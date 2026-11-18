namespace ZenoniaProto.Rpg
{
    /// <summary>PlayerState: buying basic gear from a village blacksmith (GearCatalog). Same shape as BuyPotion.</summary>
    public sealed partial class PlayerState
    {
        /// <summary>Pays and puts a new +0 common piece in the bag. Whether this seller may sell this (tier, slot) entry is the caller's check (GearShops.Refusal).</summary>
        public ActionResult BuyGear(int tier, int slotIndex, Rand rand = null)
        {
            if (tier < 0 || tier >= GearShops.Tiers || slotIndex < 0 || slotIndex >= Slots.Generated.Length) return ActionResult.Fail("Not sold here");
            if (Inventory.Count >= InventorySize) return ActionResult.Fail("Inventory full");
            int price = GearCatalog.Price(tier, slotIndex);
            if (Gold < price) return ActionResult.Fail("Not enough gold");
            Item item = GearCatalog.Buy(tier, slotIndex, ClassId, rand ?? RpgRandom.Default);
            Gold -= price;
            Inventory.Add(item);
            return ActionResult.Success("Bought " + item.name);
        }

        /// <summary>Buys a shop hat (HeadgearCatalog, Source = shop) into the bag. Seller and reach are the caller's check (GearShops.Refusal).</summary>
        public ActionResult BuyHeadgear(int hatId, Rand rand = null)
        {
            HeadgearDef hat = HeadgearCatalog.Get(hatId);
            if (hat == null || hat.Source != HeadgearSource.Shop) return ActionResult.Fail("Not sold here");
            if (Inventory.Count >= InventorySize) return ActionResult.Fail("Inventory full");
            if (Gold < hat.Price) return ActionResult.Fail("Not enough gold");
            Gold -= hat.Price;
            Inventory.Add(HeadgearCatalog.CreateItem(hat, rand ?? RpgRandom.Default));
            return ActionResult.Success("Bought " + hat.Name);
        }
    }
}
