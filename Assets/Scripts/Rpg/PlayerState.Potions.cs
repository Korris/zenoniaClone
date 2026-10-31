namespace ZenoniaProto.Rpg
{
    /// <summary>PlayerState: potions by size. HpPotions / MpPotions are the Small stacks, the same fields old saves and drops use.</summary>
    public sealed partial class PlayerState
    {
        public int HpPotionsMedium, HpPotionsLarge, MpPotionsMedium, MpPotionsLarge;
        public int HpPotionPick, MpPotionPick; // the size the quick buttons drink

        public int PotionCount(bool hp, int size)
        {
            switch (size)
            {
                case PotionCatalog.Small: return hp ? HpPotions : MpPotions;
                case PotionCatalog.Medium: return hp ? HpPotionsMedium : MpPotionsMedium;
                case PotionCatalog.Large: return hp ? HpPotionsLarge : MpPotionsLarge;
                default: return 0;
            }
        }

        void AddPotions(bool hp, int size, int n)
        {
            switch (size)
            {
                case PotionCatalog.Small: if (hp) HpPotions += n; else MpPotions += n; break;
                case PotionCatalog.Medium: if (hp) HpPotionsMedium += n; else MpPotionsMedium += n; break;
                case PotionCatalog.Large: if (hp) HpPotionsLarge += n; else MpPotionsLarge += n; break;
            }
        }

        public int PotionPick(bool hp) { return hp ? HpPotionPick : MpPotionPick; }

        public void SetPotionPick(bool hp, int size)
        {
            if (!PotionCatalog.ValidSize(size)) return;
            if (hp) HpPotionPick = size; else MpPotionPick = size;
        }

        /// <summary>The size a quick button drinks: the picked one, else the next size up that is owned, else the nearest smaller one. The pick itself when none are owned.</summary>
        public int QuickPotionSize(bool hp)
        {
            int pick = PotionPick(hp);
            for (int s = pick; s < PotionCatalog.Sizes; s++) if (PotionCount(hp, s) > 0) return s;
            for (int s = pick - 1; s >= 0; s--) if (PotionCount(hp, s) > 0) return s;
            return pick;
        }

        /// <summary>Drinks the quick size, only when it can restore something.</summary>
        public ActionResult UsePotion(bool hp) { return DrinkPotion(hp, QuickPotionSize(hp)); }

        /// <summary>Picked from the bag: this size becomes the quick one, then it is drunk.</summary>
        public ActionResult UsePotion(bool hp, int size)
        {
            if (!PotionCatalog.ValidSize(size)) return ActionResult.Fail("No potion");
            SetPotionPick(hp, size);
            return DrinkPotion(hp, size);
        }

        ActionResult DrinkPotion(bool hp, int size)
        {
            if (PotionCount(hp, size) <= 0) return ActionResult.Fail("No potion");
            float cur = hp ? Hp : Mp, max = hp ? MaxHp : MaxMp;
            if (cur >= max) return ActionResult.Fail(hp ? "HP full" : "MP full");
            int amount = RpgMath.Round(max * PotionCatalog.Fraction(size));
            float v = System.Math.Min(max, cur + amount);
            if (hp) Hp = v; else Mp = v;
            AddPotions(hp, size, -1);
            return ActionResult.Success((hp ? "HP +" : "MP +") + amount);
        }

        /// <summary>Pays and adds one potion. Whether this seller may sell it is the caller's check (PotionShops).</summary>
        public ActionResult BuyPotion(bool hp, int size)
        {
            if (!PotionCatalog.ValidSize(size)) return ActionResult.Fail("Not sold here");
            if (PotionCount(hp, size) >= PotionCatalog.MaxStack) return ActionResult.Fail("You cannot carry more");
            int price = PotionCatalog.Price(size);
            if (Gold < price) return ActionResult.Fail("Not enough gold");
            Gold -= price;
            AddPotions(hp, size, 1);
            return ActionResult.Success("Bought " + PotionCatalog.Name(hp, size));
        }
    }
}
