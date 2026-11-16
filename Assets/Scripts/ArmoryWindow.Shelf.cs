using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: the carousel shelf, ten standing item cards a page between two page arrows, with the "Drag to Equip" tip over the
    /// selected card, and the footer plaques under it ("Bag 50/50", "Page 1/3" with dots). Cards show real data only: the bag's item name,
    /// the level needed to wear it, one star per rarity rank; a card the hero cannot wear is locked with the reason in red.
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        const int ShelfCards = 10;
        const float ShelfX = 10f, ShelfY = 436f, ShelfW = 1118f, ShelfH = 160f, CardX = 84f, CardStep = 98f, CardY = 461f, CardLift = 7f;

        readonly PaperCard[] cards = new PaperCard[ShelfCards];
        GameObject shelf, hint, pagePlaque;
        RectTransform bagPlaque;
        Text emptyText, bagText, pageText;
        PaperDots pageDots;
        int dotCount;

        /// <summary>Top edge and middle of the list in use (shelf or grid board): where the toast sits.</summary>
        float ListTop { get { return gridView ? GridY : ShelfY; } }
        float ListCentreX { get { return gridView ? GridX + GridW * 0.5f : ShelfX + ShelfW * 0.5f; } }

        void BuildShelf()
        {
            RectTransform root = UiKit.Rect(content, "Shelf", 0f, 0f, 10f, 10f);
            shelf = root.gameObject;
            if (PaperArt.Has(PaperPiece.BagShelf)) PaperArt.Draw(UiKit.Rect(root, "Board", ShelfX, ShelfY, ShelfW, ShelfH), PaperPiece.BagShelf, PaperTheme.Wood, PaperTheme.WoodDark, 10f, true);
            else PaperKit.Frame(root, "Board", ShelfX, ShelfY, ShelfW, ShelfH, PaperFrame.Wood);
            PaperKit.PageArrow(root, "Prev", CardX - 54f, ShelfY + 58f, 44f, true, () => TurnPage(-1));
            PaperKit.PageArrow(root, "Next", CardX + ShelfCards * CardStep, ShelfY + 58f, 44f, false, () => TurnPage(1));
            for (int i = 0; i < ShelfCards; i++)
            {
                int index = i;
                cards[i] = new PaperCard(root, "Card" + i, CardX + i * CardStep, CardY, () => OnCellTap(model.Page * ShelfCards + index));
                cards[i].Root.gameObject.AddComponent<ArmoryCardDrag>().Init(this, index);
            }
            emptyText = PaperTheme.Title(root, "Empty", ShelfX, ShelfY + 60f, ShelfW, 40f, "", PaperTheme.SectionSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            RectTransform tip = UiKit.Rect(root, "Hint", 0f, ShelfY - 20f, 128f, 44f); // speech bubble: a hand on the left, the tail below
            PaperArt.Draw(tip, PaperPiece.BagHint, PaperTheme.Cream, PaperTheme.BtnGoldEdge, 8f);
            Text tipText = PaperTheme.Caption(tip, "Drag to Equip", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain);
            tipText.rectTransform.offsetMin = new Vector2(30f, 12f); tipText.rectTransform.offsetMax = new Vector2(-4f, -2f);
            hint = tip.gameObject;
        }

        /// <summary>The plaques under the list. The grid view shows every slot on one page, so it only keeps the bag count, centred under its board.</summary>
        void BuildFooter()
        {
            bagPlaque = Plaque("BagCount", 0f, 170f, PaperPiece.BagPlaque, out bagText);
            bagText.rectTransform.offsetMin = new Vector2(40f, 0f); // the sign carries a pouch on its left end
            RectTransform page = Plaque("Page", 548f, 220f, null, out pageText);
            pageText.rectTransform.offsetMax = new Vector2(-104f, 0f); // the dots take the right part
            pagePlaque = page.gameObject;
        }

        RectTransform Plaque(string name, float x, float w, string piece, out Text text)
        {
            RectTransform rt = UiKit.Rect(content, name, x, 599f, w, 36f);
            PaperArt.Draw(rt, piece, PaperTheme.WoodDark, PaperTheme.Wood, 12f);
            text = PaperTheme.Caption(rt, "", PaperTheme.SectionSize, PaperTheme.TextLight);
            return rt;
        }

        void TurnPage(int by)
        {
            confirmDiscard = false;
            model.TurnPage(by);
            Refresh();
        }

        void RefreshFooter(PlayerState p)
        {
            bagText.text = "Bag   " + p.Inventory.Count + "/" + PlayerState.InventorySize;
            bagPlaque.anchoredPosition = new Vector2(gridView ? GridX + (GridW - 170f) * 0.5f : 360f, -599f);
            pagePlaque.SetActive(!gridView);
            if (gridView) return;
            int pages = model.PageCount;
            pageText.text = "Page " + (model.Page + 1) + "/" + pages;
            if (pages != dotCount) // a row of dots per page count: the bag changes it rarely
            {
                if (pageDots != null) Destroy(pageDots.Root.gameObject);
                dotCount = pages;
                pageDots = new PaperDots(pagePlaque.transform, "Dots", 220f - 20f - pages * 13.5f, 13f, pages, 9f);
            }
            pageDots.Set(model.Page);
        }

        void RefreshShelf(PlayerState p)
        {
            emptyText.text = model.Cells.Count > 0 ? "" : model.Filter == BagFilter.All ? "The bag is empty." : "Nothing of this type in the bag.";
            bool tip = false;
            for (int i = 0; i < ShelfCards; i++)
            {
                int cell = model.Page * ShelfCards + i;
                PaperCard card = cards[i];
                bool shown = cell < model.Cells.Count, selected = shown && model.Worn < 0 && cell == model.Cell;
                card.Root.gameObject.SetActive(shown);
                if (!shown) continue;
                card.Root.anchoredPosition = new Vector2(CardX + i * CardStep, -(selected ? CardY - CardLift : CardY));
                BagCell c = model.Cells[cell];
                if (c.IsSupply)
                {
                    card.Set(0, BagText.SupplySprite(c.Supply), BagText.SupplyName(c.Supply), "x " + BagModel.SupplyCount(p, c.Supply), 0, selected);
                    card.Stars.Root.gameObject.SetActive(false); // potions and stones have no rarity
                    ItemEnhanceSlotFx.Show(card.Icon, null);
                    continue;
                }
                card.Stars.Root.gameObject.SetActive(true);
                string why = WhyLocked(p, c.Item);
                card.Set(PaperTheme.RarityTier(c.Item.rarity), LootSprites.Icon(c.Item), BagText.ItemName(c.Item), why ?? ("Lv. " + EquipLevel.Required(c.Item)), RarityStars(c.Item), selected, why != null);
                ItemEnhanceSlotFx.Show(card.Icon, c.Item);
                if (selected && why == null && dragItem == null)
                {
                    tip = true;
                    ((RectTransform)hint.transform).anchoredPosition = new Vector2(CardX + i * CardStep - 20f, -(ShelfY - 20f));
                }
            }
            hint.SetActive(tip);
        }

        /// <summary>Why this hero cannot wear the item ("Req. Lv 70", "Wizard only"), null when he can.</summary>
        static string WhyLocked(PlayerState p, Item item)
        {
            if (!p.CanEquip(item)) return RpgClasses.Get(item.classId).Name + " only";
            if (item.slot == Slots.Pet) return "Old pet"; // a pet item of an old save: it becomes an account pet at the next log in
            return p.MeetsLevel(item) ? null : EquipLevel.Label(item);
        }

        /// <summary>Stars of a card: the rarity rank (Common 1 ... the top rarity 4 of 5). The game has no other star value for gear.</summary>
        static int RarityStars(Item item) { return Mathf.Max(0, System.Array.IndexOf(ItemDefs.RarityOrder, item.rarity)) + 1; }
    }
}
