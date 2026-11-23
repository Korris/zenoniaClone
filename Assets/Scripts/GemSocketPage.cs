using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "Gem Socketing" (mockup 04): items with sockets on the left (Equip / Bag), the chosen item with its socket tiles in the middle, the Soul Gems the character owns under it
    /// (filter chips, 6 per row, xN), and on the right the chosen gem's info and option plus the Socket Gem button. Free. The gem goes into the next free socket and is trade-locked
    /// from then on (the lock note says so before the press). Server decides (InvOp.GemSocket: ItemUid = item uid, Slot = GemSockets.Code).
    /// </summary>
    public sealed class GemSocketPage : GemPage
    {
        const int Cols = 6, Rows = 2, PerPage = Cols * Rows;
        const float ListW = 292f, MidX = 300f, MidW = 372f, RightX = 680f, RightW = 271f, Tile = 48f, GridY = 372f, PitchX = 60f, PitchY = 80f;

        GemGearList gear;
        GemItemStage stage;
        GemInfoPanel info;
        U.PieceButton socketButton;
        U.Chip[] chips;
        readonly GemTileView[] tiles = new GemTileView[PerPage];
        readonly GemStock[] tileStock = new GemStock[PerPage];
        UnityEngine.UI.Text pageText, reason;
        GemFilter filter = GemFilter.All;
        string selectedGem; // "cardId:p"
        int page;

        public override void Build(RectTransform parent)
        {
            Root = UiKit.Rect(parent, "SocketPage", 0f, 0f, PageW, PageH);
            U.Panel(Root, "ListBox", 0f, 0f, ListW, PageH, true);
            gear = new GemGearList(Root, 10f, 12f, ListW - 20f, item => Refresh());
            stage = new GemItemStage(Root, MidX, 0f, MidW, i => { });
            U.Heading(Root, "SelectHeading", MidX + 8f, 300f, 200f, "Select Gem");
            chips = U.FilterRow(Root, MidX + 6f, 332f, MidW - 12f, f => { filter = f; page = 0; Refresh(); });
            for (int i = 0; i < PerPage; i++)
            {
                int index = i;
                tiles[i] = new GemTileView(Root, "Gem" + i, MidX + 12f + (i % Cols) * PitchX, GridY + (i / Cols) * PitchY, Tile, () => Pick(tileStock[index]), true, true);
            }
            pageText = U.Txt(Root, "Page", MidX + MidW - 112f, 298f, 56f, 28f, "1 / 1", U.TextSmall, U.Ink, TextAnchor.MiddleCenter);
            Arrow(MidX + MidW - 154f, "btn-page-left", -1); Arrow(MidX + MidW - 54f, "btn-page-right", 1);
            info = new GemInfoPanel(Root, RightX, 0f, RightW, "Gem Info");
            reason = U.Txt(Root, "Reason", RightX, PageH - 88f, RightW, 32f, "", U.TextSmall, U.Bad, TextAnchor.MiddleCenter);
            socketButton = new U.PieceButton(Root, "SocketButton", RightX + (RightW - 220f) * 0.5f, PageH - 52f, 220f, U.ButtonH, GemUiArt.Frame("btn-gold"), "Socket Gem", OnSocket);
        }

        void Arrow(float x, string piece, int dir)
        {
            UnityEngine.UI.Image a = GemUiArt.Img(Root, "Arrow" + dir, x, 298f, 36f, 28f, GemUiArt.Frame(piece), true);
            UiWinKit.MakeButton(a, () => { page += dir; Refresh(); });
        }

        void Pick(GemStock s) { if (s == null) return; selectedGem = GemSockets.Encode(s.Card.Id, s.Kind); Refresh(); }

        public override void DebugPressMain() { OnSocket(); }

        void OnSocket()
        {
            Item item = gear.Selected;
            string id; GemKind kind;
            if (item == null || !GemSockets.TryParse(selectedGem, out id, out kind)) return;
            CardDef card = CardTable.Get(id);
            Confirm("Socket " + card.Name + " " + kind + " Gem into " + item.name + "?\nThe gem becomes <color=#B33A3A><b>Trade Locked</b></color> for good, even after you remove it, and so does the item while it holds a gem.", "Socket",
                () => Send(InvOp.GemSocket, GemSockets.Code(id, kind), item.uid));
        }

        public override void Refresh()
        {
            PlayerState p = Me;
            if (Root == null || p == null) return;
            gear.Refresh(p);
            Item item = gear.Selected;
            stage.Draw(item);
            U.SelectChip(chips, filter);
            List<GemStock> stocks = GemUiModel.Stocks(p, filter);
            int pages = Mathf.Max(1, Mathf.CeilToInt(stocks.Count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            pageText.text = (page + 1) + " / " + pages;
            string id; GemKind kind;
            if (!GemSockets.TryParse(selectedGem, out id, out kind) || !stocks.Exists(s => s.Card.Id == id && s.Kind == kind)) selectedGem = stocks.Count > 0 ? GemSockets.Encode(stocks[0].Card.Id, stocks[0].Kind) : null;
            for (int i = 0; i < PerPage; i++)
            {
                int index = page * PerPage + i;
                tileStock[i] = index < stocks.Count ? stocks[index] : null;
                GemStock s = tileStock[i];
                if (s == null) { tiles[i].Root.gameObject.SetActive(false); continue; }
                tiles[i].Set(s.Card, s.Kind, s.Total, GemSockets.Encode(s.Card.Id, s.Kind) == selectedGem, false);
            }
            bool have = GemSockets.TryParse(selectedGem, out id, out kind);
            info.Set(have ? CardTable.Get(id) : null, kind, true, "Once socketed, this gem is locked to you for good: it cannot be traded, even after you remove it.");
            string why = !CardOps.GemsEnabled ? "Soul Gems are not available" : item == null ? "Pick an item with sockets" : !have ? "Pick a gem (convert a card first)" : GemUiModel.Filled(item) >= GemSockets.SlotCount(item.slot) ? "All sockets of this item are full" : item.sockets != null && item.sockets.Contains(selectedGem) ? "This item already holds that gem" : null;
            reason.text = why ?? "";
            socketButton.Set("Socket Gem", why == null && !Pending);
        }
    }
}
