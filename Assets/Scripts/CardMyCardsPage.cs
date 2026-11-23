using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;
using P = ZenoniaProto.CardPageWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "My Cards" (mockup 01): rarity and area pills, the category chips, "Card Collection (n / 40)", the cards the character owns (4 x 3 per page, xN, lock and In Deck marks) and on the
    /// right the chosen card (CardDetailPanel) with Add to Deck (Card Duel phase), Sell (to an NPC, asks first via CardConfirmBox), Lock Card and View in Album. Sell / Lock go to the server
    /// (InvOp.CardSell / CardLock); the page only asks and redraws from the mirrored state.
    /// </summary>
    public sealed class CardMyCardsPage : GemPage
    {
        const int Cols = 4, Rows = 3, PerPage = Cols * Rows;
        const float GridW = 598f, CardW = 83f, CardH = 110f, PitchX = 146f, PitchY = 140f, GridX = 26f, GridY = 86f, DetailX = 606f, DetailW = 345f;

        readonly GemCardView[] cells = new GemCardView[PerPage];
        readonly Image[] lockMark = new Image[PerPage], deckMark = new Image[PerPage];
        readonly CardDef[] cellCard = new CardDef[PerPage];
        U.Chip[] chips;
        P.CyclePill rarityPill, areaPill;
        P.Pager pager;
        CardDetailPanel detail;
        U.PieceButton addToDeck, sell, sellAll, lockButton, viewAlbum;
        Text collection, reason;
        GemFilter filter = GemFilter.All;
        int rarity = -1, area = -1, page; // -1 = all
        string selectedId;

        public override void Build(RectTransform parent)
        {
            Root = UiKit.Rect(parent, "MyCardsPage", 0f, 0f, PageW, PageH);
            U.Panel(Root, "GridBox", 0f, 0f, GridW, PageH, true);
            rarityPill = new P.CyclePill(Root, "Rarity", 14f, 10f, 150f, () => { rarity = rarity >= 3 ? -1 : rarity + 1; page = 0; Refresh(); });
            areaPill = new P.CyclePill(Root, "Area", 172f, 10f, 200f, () => { area = area + 1 >= CardTable.Regions.Length ? -1 : area + 1; page = 0; Refresh(); });
            collection = U.Txt(Root, "Collection", 380f, 8f, GridW - 394f, 34f, "", U.TextNormal, U.Ink, TextAnchor.MiddleRight);
            chips = U.FilterRow(Root, 14f, 46f, GridW - 28f, f => { filter = f; page = 0; Refresh(); });
            for (int i = 0; i < PerPage; i++)
            {
                int index = i;
                float x = GridX + (i % Cols) * PitchX, y = GridY + (i / Cols) * PitchY;
                cells[i] = new GemCardView(Root, "Card" + i, x, y, CardW, CardH, () => Select(cellCard[index]), true);
                lockMark[i] = GemUiArt.Img(cells[i].Root, "Lock", CardW - 28f, 5f, 22f, 25f, GemUiArt.Icon("icon-lock"));
                deckMark[i] = GemUiArt.Img(cells[i].Root, "InDeck", (CardW - 60f) * 0.5f, CardH - 20f, 60f, 26f, GemUiArt.Icon("tag-in-deck"));
            }
            pager = new P.Pager(Root, GridW * 0.5f, PageH - 38f, d => { page += d; Refresh(); });
            detail = new CardDetailPanel(Root, DetailX, 0f, DetailW);
            // three rows of 40 high buttons (the Sell all spare row made the old 48 high rows not fit), then a 14 high reason line
            float bw = 156f, bh = 40f, by = CardDetailPanel.ButtonsY, by2 = by + 42f, by3 = by + 84f, x0 = 14f, x1 = DetailW - 14f - bw;
            addToDeck = new U.PieceButton(detail.Root, "AddToDeck", x0, by, bw, bh, GemUiArt.Frame("btn-teal"), "Add to Deck", OnAddToDeck);
            sell = new U.PieceButton(detail.Root, "Sell", x1, by, bw, bh, GemUiArt.Frame("btn-red"), "Sell", OnSell);
            lockButton = new U.PieceButton(detail.Root, "Lock", x0, by2, bw, bh, GemUiArt.Frame("btn-blue"), "Lock Card", OnLock);
            viewAlbum = new U.PieceButton(detail.Root, "ViewAlbum", x1, by2, bw, bh, GemUiArt.Frame("btn-blue"), "View in Album", () => { if (selectedId != null) CardWindow.Show(CardWindow.Page.Album, selectedId); });
            sellAll = new U.PieceButton(detail.Root, "SellAll", x0, by3, DetailW - 28f, bh, GemUiArt.Frame("btn-red"), "Sell all spare", OnSellAll);
            reason = U.Txt(detail.Root, "Reason", 14f, by3 + bh + 1f, DetailW - 28f, 15f, "", U.TextSmall, U.Bad, TextAnchor.MiddleCenter);
        }

        public void Focus(string cardId)
        {
            CardDef c = CardTable.Get(cardId);
            if (c == null) return;
            filter = GemFilter.All; rarity = -1; area = -1; selectedId = cardId; page = -1; // page -1: Refresh jumps to the page holding the card
        }

        void Select(CardDef c) { if (c == null) return; selectedId = c.Id; Refresh(); }

        void OnSell()
        {
            PlayerState p = Me;
            CardDef c = CardTable.Get(selectedId);
            if (p == null || c == null || p.Cards.Spare(c.Id) < 1) return;
            string id = c.Id;
            Confirm("Sell 1 x " + c.Name + " [" + c.Rarity + "] to the shop for " + CardFees.Sell(c).ToString("N0") + " G?\nCopies in the deck stay.", "Sell", () => Send(InvOp.CardSell, 1, id));
        }

        /// <summary>Sells every spare copy (not locked, not in the deck) after showing the count and the total. Slot 0 on the wire means "all spare", so a stack over 255 still fits a byte.</summary>
        public void PressSellAll() { OnSellAll(); }

        void OnSellAll()
        {
            PlayerState p = Me;
            CardDef c = CardTable.Get(selectedId);
            if (p == null || c == null) return;
            int spare = p.Cards.Spare(c.Id);
            if (spare < 1) return;
            string id = c.Id;
            long total = (long)CardFees.Sell(c) * spare;
            Confirm("Sell all " + spare + " spare x " + c.Name + " [" + c.Rarity + "] to the shop for " + total.ToString("N0") + " G?\nCopies in the deck stay.", "Sell all", () => Send(InvOp.CardSell, 0, id));
        }

        void OnAddToDeck()
        {
            PlayerState p = Me;
            if (p == null || selectedId == null) return;
            string why = DeckRules.WhyNotAdd(p.Cards.Deck, selectedId, p.Cards);
            if (why.Length > 0) { DuelClient.Notify(why, false); return; }
            var next = new List<string>(p.Cards.Deck) { selectedId };
            Hold();
            DuelClient.SaveDeck(next);
            Refresh();
        }

        void OnLock()
        {
            PlayerState p = Me;
            if (p == null || selectedId == null) return;
            Send(InvOp.CardLock, (byte)(p.Cards.IsLocked(selectedId) ? 0 : 1), selectedId);
        }

        List<CardDef> Visible(PlayerState p)
        {
            List<CardDef> list = GemUiModel.OwnedCards(p, filter);
            list.RemoveAll(c => (rarity >= 0 && (int)c.Rarity != rarity) || (area >= 0 && c.Region != CardTable.Regions[area]));
            return list;
        }

        public override void Refresh()
        {
            PlayerState p = Me;
            if (Root == null || p == null) return;
            U.SelectChip(chips, filter);
            string[] rar = { "N", "R", "SR", "SSR" };
            rarityPill.Set(rarity < 0 ? "All Rarity" : "Rarity: " + rar[rarity]);
            areaPill.Set(area < 0 ? "All Areas" : P.ShortRegion(CardTable.Regions[area]));
            collection.text = "Card Collection (" + p.Cards.OwnedKinds + " / " + CardTable.Count + ")";
            List<CardDef> list = Visible(p);
            int pages = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)PerPage));
            if (page < 0) { int at = list.FindIndex(c => c.Id == selectedId); page = at < 0 ? 0 : at / PerPage; }
            page = Mathf.Clamp(page, 0, pages - 1);
            pager.Set(page, pages);
            if (selectedId == null || list.FindIndex(c => c.Id == selectedId) < 0) selectedId = list.Count > 0 ? list[0].Id : null;
            for (int i = 0; i < PerPage; i++)
            {
                int index = page * PerPage + i;
                CardDef c = index < list.Count ? list[index] : null;
                cellCard[i] = c;
                if (c == null) { cells[i].Root.gameObject.SetActive(false); continue; }
                cells[i].Set(c, p.Cards.Count(c.Id), c.Id == selectedId);
                lockMark[i].gameObject.SetActive(p.Cards.IsLocked(c.Id));
                deckMark[i].gameObject.SetActive(p.Cards.InDeck(c.Id) > 0);
            }
            DrawDetail(p, CardTable.Get(selectedId));
        }

        void DrawDetail(PlayerState p, CardDef c)
        {
            detail.Set(c, p, true);
            if (c == null)
            {
                addToDeck.Set("Add to Deck", false); sell.Set("Sell", false); sellAll.Set("Sell all spare", false); lockButton.Set("Lock Card", false); viewAlbum.Set("View in Album", false);
                reason.text = list0Hint(p);
                return;
            }
            int spare = p.Cards.Spare(c.Id), price = CardFees.Sell(c);
            bool dueling = DuelClient.Running;
            addToDeck.Set(p.Cards.Deck.Count >= DeckRules.Size ? "Deck full" : "Add to Deck", !Pending && !dueling && p.Cards.Deck.Count < DeckRules.Size);
            sell.Set("Sell", spare > 0 && !Pending);
            sellAll.Set(spare > 1 ? "Sell all spare (" + spare + ")" : "Sell all spare", spare > 1 && !Pending);
            lockButton.Set(p.Cards.IsLocked(c.Id) ? "Unlock Card" : "Lock Card", !Pending);
            viewAlbum.Set("View in Album", true);
            reason.text = p.Cards.IsLocked(c.Id) ? "Locked: unlock the card to sell or convert it" : spare < 1 ? "Copies in the deck cannot be sold" : "Sell price: " + price.ToString("N0") + " G each" + (p.Cards.InDeck(c.Id) > 0 ? " (" + p.Cards.InDeck(c.Id) + " in deck)" : "");
            reason.color = spare < 1 ? U.Bad : U.InkSoft;
        }

        static string list0Hint(PlayerState p) { return p.Cards.OwnedKinds == 0 ? "Defeat monsters to find cards." : "No card matches the filter."; }
    }
}
