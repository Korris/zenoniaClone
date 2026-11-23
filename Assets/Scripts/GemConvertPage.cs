using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "Convert to Soul Gem" (mockup 03): the cards the character owns (filter chips, 4 x 3 per page), and on the right the chosen card, Power / Guard choice with the option each gives,
    /// the cost (1 card + gold, by rarity) and the Convert button. Online the server decides (InvOp.GemConvert); this page only asks and redraws from the mirrored state.
    /// </summary>
    public sealed class GemConvertPage : GemPage
    {
        const int Cols = 4, Rows = 3, PerPage = Cols * Rows;
        const float GridX = 6f, GridY = 46f, CellW = 100f, CellH = 122f, PitchX = 146f, PitchY = 148f, DetailX = 606f, DetailW = 345f;

        readonly GemCardView[] cells = new GemCardView[PerPage];
        readonly CardDef[] cellCard = new CardDef[PerPage];
        U.Chip[] chips;
        U.PieceButton convert;
        GemCardView big, costCard;
        GemTileView[] kindTiles = new GemTileView[2];
        Image[] kindRing = new Image[2];
        Text title, owned, area, element, costGold, description, pageText, optionPower, optionGuard, reason;
        Image elementIcon;
        GemFilter filter = GemFilter.All;
        GemKind kind = GemKind.Power;
        string selectedId;
        int page;

        public override void Build(RectTransform parent)
        {
            Root = UiKit.Rect(parent, "ConvertPage", 0f, 0f, PageW, PageH);
            U.Panel(Root, "GridBox", 0f, 0f, 598f, PageH, true);
            chips = U.FilterRow(Root, GridX + 8f, 8f, 574f, f => { filter = f; page = 0; Refresh(); });
            for (int i = 0; i < PerPage; i++)
            {
                int index = i;
                cells[i] = new GemCardView(Root, "Card" + i, GridX + 20f + (i % Cols) * PitchX, GridY + (i / Cols) * PitchY, CellW, CellH, () => Select(cellCard[index]), true);
            }
            pageText = U.Txt(Root, "Page", 299f - 40f, PageH - 38f, 80f, 28f, "1 / 1", U.TextNormal, U.Ink, TextAnchor.MiddleCenter);
            Arrow(299f - 96f, "btn-page-left", -1); Arrow(299f + 56f, "btn-page-right", 1);
            BuildDetail();
        }

        void Arrow(float x, string piece, int dir)
        {
            Image a = GemUiArt.Img(Root, "Arrow" + dir, x, PageH - 40f, 40f, 30f, GemUiArt.Frame(piece), true);
            UiWinKit.MakeButton(a, () => { page += dir; Refresh(); });
        }

        void BuildDetail()
        {
            RectTransform d = UiKit.Rect(Root, "Detail", DetailX, 0f, DetailW, PageH);
            U.Panel(d, "DetailBox", 0f, 0f, DetailW, PageH, true);
            title = U.Txt(d, "Title", 16f, 10f, DetailW - 32f, 30f, "Pick a card", U.TextTitle, U.Ink, TextAnchor.MiddleCenter);
            big = new GemCardView(d, "BigCard", 18f, 48f, 118f, 145f, null, false);
            element = U.Txt(d, "Element", 176f, 56f, DetailW - 190f, 24f, "", U.TextNormal, U.Ink);
            elementIcon = UiKit.Rect(d, "ElementIcon", 150f, 57f, 22f, 22f).gameObject.AddComponent<Image>();
            elementIcon.raycastTarget = false;
            area = U.Txt(d, "Area", 150f, 86f, DetailW - 164f, 40f, "", U.TextSmall + 1, U.Ink, TextAnchor.UpperLeft);
            owned = U.Txt(d, "Owned", 150f, 134f, DetailW - 164f, 40f, "", U.TextSmall + 1, U.Ink, TextAnchor.UpperLeft);
            U.Heading(d, "OptionHeading", 16f, 200f, 200f, "Convert Option");
            for (int i = 0; i < 2; i++)
            {
                int k = i;
                float x = 14f + i * 164f;
                Image back = GemUiArt.Img(d, "KindBack" + i, x, 232f, 156f, 62f, GemUiArt.Frame("panel-detail"), true);
                UiWinKit.MakeButton(back, () => { kind = (GemKind)k; Refresh(); });
                kindTiles[i] = new GemTileView(d, "KindGem" + i, x + 6f, 238f, 50f, null, false, false);
                U.Txt(d, "KindName" + i, x + 60f, 238f, 94f, 24f, i == 0 ? "Power Gem" : "Guard Gem", U.TextNormal, U.Ink);
                Text opt = U.Txt(d, "KindOption" + i, x + 60f, 262f, 94f, 24f, "", U.TextSmall + 1, U.InkSoft);
                if (i == 0) optionPower = opt; else optionGuard = opt;
                kindRing[i] = GemUiArt.Img(d, "KindRing" + i, x - 3f, 229f, 162f, 68f, null);
            }
            U.Heading(d, "CostHeading", 16f, 304f, 200f, "Cost");
            costCard = new GemCardView(d, "CostCard", 18f, 336f, 60f, 74f, null, false, false);
            U.Txt(d, "Plus", 84f, 350f, 24f, 40f, "+", 28, U.Ink, TextAnchor.MiddleCenter);
            GemUiArt.Img(d, "GoldIcon", 112f, 355f, 26f, 26f, GemUiArt.Icon("icon-gold"));
            costGold = U.Txt(d, "CostGold", 142f, 350f, 90f, 36f, "", U.TextNormal + 2, U.Ink);
            U.Txt(d, "Arrow", 224f, 350f, 28f, 40f, ">", 28, U.Ink, TextAnchor.MiddleCenter);
            kindTiles[0].Root.SetAsLastSibling();
            result = new GemTileView(d, "Result", 258f, 336f, 70f, null, false, true);
            description = U.Txt(d, "Description", 16f, 424f, DetailW - 32f, 44f, "Convert 1 Monster Card and gold into 1 Soul Gem. The Album keeps the card.", U.TextSmall + 1, U.InkSoft, TextAnchor.UpperLeft);
            reason = U.Txt(d, "Reason", 16f, 468f, DetailW - 32f, 22f, "", U.TextSmall, U.Bad, TextAnchor.MiddleCenter);
            convert = new U.PieceButton(d, "ConvertButton", (DetailW - 220f) * 0.5f, PageH - 58f, 220f, U.ButtonH, GemUiArt.Frame("btn-gold"), "Convert", OnConvert);
        }

        GemTileView result;

        void Select(CardDef c) { if (c == null) return; selectedId = c.Id; Refresh(); }

        void OnConvert()
        {
            CardDef c = CardTable.Get(selectedId);
            if (c == null) return;
            byte k = (byte)(kind == GemKind.Guard ? 1 : 0);
            string id = selectedId;
            if (c.Rarity < CardRarity.SR) { Send(InvOp.GemConvert, k, id); return; }
            Confirm("Convert " + c.Name + " [" + c.Rarity + "] into a " + kind + " Gem?\nIt costs 1 card and " + CardFees.Convert(c).ToString("N0") + " G. The Album keeps the card.", "Convert", () => Send(InvOp.GemConvert, k, id));
        }

        public override void Refresh()
        {
            PlayerState p = Me;
            if (Root == null || p == null) return;
            U.SelectChip(chips, filter);
            List<CardDef> list = GemUiModel.OwnedCards(p, filter);
            int pages = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            pageText.text = (page + 1) + " / " + pages;
            if (selectedId == null || p.Cards.Count(selectedId) == 0) selectedId = list.Count > 0 ? list[0].Id : null;
            for (int i = 0; i < PerPage; i++)
            {
                int index = page * PerPage + i;
                cellCard[i] = index < list.Count ? list[index] : null;
                if (cellCard[i] == null) { cells[i].Root.gameObject.SetActive(false); continue; }
                cells[i].Set(cellCard[i], p.Cards.Count(cellCard[i].Id), cellCard[i].Id == selectedId, p.Cards.Spare(cellCard[i].Id) == 0);
            }
            DrawDetail(p, CardTable.Get(selectedId));
        }

        void DrawDetail(PlayerState p, CardDef c)
        {
            big.Root.gameObject.SetActive(c != null); costCard.Root.gameObject.SetActive(c != null); result.Root.gameObject.SetActive(c != null);
            for (int i = 0; i < 2; i++) kindTiles[i].Root.gameObject.SetActive(c != null);
            if (c == null)
            {
                title.text = "No cards yet"; element.text = area.text = owned.text = costGold.text = optionPower.text = optionGuard.text = ""; GemUiArt.SetPiece(elementIcon, null);
                reason.text = "Defeat monsters to find cards."; convert.Set("Convert", false);
                return;
            }
            title.text = c.Name;
            big.Set(c, 0, false);
            element.text = c.Element;
            GemUiArt.SetPiece(elementIcon, GemUiArt.ElementIcon(c));
            area.text = "Area: " + GemUiModel.RegionName(c.Region);
            owned.text = "Owned: " + p.Cards.Count(c.Id) + "   Free: " + p.Cards.Spare(c.Id) + (p.Cards.Locked(c.Id) > 0 ? "\nLocked: " + p.Cards.Locked(c.Id) : "");
            kindTiles[0].Set(c, GemKind.Power, 0, false, false); kindTiles[1].Set(c, GemKind.Guard, 0, false, false);
            optionPower.text = GemSockets.OptionText(c.Option(GemKind.Power)); optionGuard.text = GemSockets.OptionText(c.Option(GemKind.Guard));
            GemUiArt.SetPiece(kindRing[0], kind == GemKind.Power ? "cards/card/select-frame-gold" : null);
            GemUiArt.SetPiece(kindRing[1], kind == GemKind.Guard ? "cards/card/select-frame-gold" : null);
            int fee = CardFees.Convert(c);
            costCard.Set(c, 0, false);
            costGold.text = fee.ToString("N0") + " G";
            result.Set(c, kind, 1, false, false);
            string why = !CardOps.GemsEnabled ? "Soul Gems are not available" : p.Cards.Spare(c.Id) < 1 ? "This card is locked or in your deck" : p.Gold < fee ? "Need " + fee.ToString("N0") + " G" : !p.Cards.CanAddGem(c.Id, kind, false) ? "No room for another gem of this kind" : null;
            reason.text = why ?? "";
            convert.Set("Convert", why == null && !Pending);
        }
    }
}
