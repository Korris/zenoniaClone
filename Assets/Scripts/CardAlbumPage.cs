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
    /// "Monster Album" (mockup 02): progress bar x / 40 with the four milestones (10 / 20 / 30 / 40) and a Claim button, region tabs (All + the 10 map regions, two rows), the 40 monsters
    /// (6 x 2 per page; a card never owned is a dark silhouette with "?"), and on the right the chosen monster (CardDetailPanel). The Album keeps a card forever, even after it is sold
    /// or turned into a gem. Claim = InvOp.AlbumClaim (the server checks the milestone and that it was not taken).
    /// </summary>
    public sealed class CardAlbumPage : GemPage
    {
        const int Cols = 6, Rows = 2, PerPage = Cols * Rows, TabsPerRow = 6;
        const float MainW = 640f, DetailX = 648f, DetailW = 303f, CardW = 81f, CardH = 108f, PitchX = 100f, PitchY = 130f, GridX = 28f, GridY = 190f;
        const float BarX = 176f, BarW = 330f, BarY = 14f;
        static readonly string[] Reward = { "5,000 G", "Title", "Frame", "Master" };

        readonly GemCardView[] cells = new GemCardView[PerPage];
        readonly CardDef[] cellCard = new CardDef[PerPage];
        readonly Image[] milestone = new Image[4];
        P.TextTab[] tabs;
        P.Pager pager;
        CardDetailPanel detail;
        RectTransform fill;
        Text progress, hint;
        U.PieceButton claim, viewCards;
        int region = -1, page; // region -1 = All
        string selectedId;

        public override void Build(RectTransform parent)
        {
            Root = UiKit.Rect(parent, "AlbumPage", 0f, 0f, PageW, PageH);
            U.Panel(Root, "MainBox", 0f, 0f, MainW, PageH, true);
            U.Txt(Root, "ProgressLabel", 16f, 8f, 156f, 34f, "Collection", U.TextTitle, U.Ink);
            GemUiArt.Img(Root, "Track", BarX, BarY, BarW, 24f, GemUiArt.Frame("progress-track"));
            fill = GemUiArt.Img(Root, "Fill", BarX, BarY, 48f, 24f, GemUiArt.Frame("progress-fill")).rectTransform;
            progress = U.Txt(Root, "Progress", BarX + BarW + 10f, 8f, MainW - BarX - BarW - 20f, 34f, "", U.TextNormal, U.Ink);
            for (int m = 0; m < 4; m++)
            {
                float cx = BarX + BarW * (AlbumRules.Milestones[m] / (float)CardTable.Count);
                milestone[m] = GemUiArt.Img(Root, "Milestone" + m, cx - 19f, 46f, 38f, 38f, GemUiArt.Frame("milestone-locked"));
                U.Txt(Root, "MilestoneNum" + m, cx - 41f, 84f, 82f, 18f, AlbumRules.Milestones[m] + " cards", U.TextSmall, U.Ink, TextAnchor.MiddleCenter);
                U.Txt(Root, "MilestoneReward" + m, cx - 41f, 101f, 82f, 18f, Reward[m], U.TextSmall - 1, U.InkSoft, TextAnchor.MiddleCenter);
            }
            claim = new U.PieceButton(Root, "Claim", 16f, 50f, 124f, U.ButtonH, GemUiArt.Frame("btn-gold"), "Claim", OnClaim);
            BuildTabs();
            for (int i = 0; i < PerPage; i++)
            {
                int k = i;
                cells[i] = new GemCardView(Root, "Card" + i, GridX + (i % Cols) * PitchX, GridY + (i / Cols) * PitchY, CardW, CardH, () => { if (cellCard[k] != null) { selectedId = cellCard[k].Id; Refresh(); } }, true);
            }
            pager = new P.Pager(Root, MainW * 0.5f, PageH - 38f, d => { page += d; Refresh(); });
            detail = new CardDetailPanel(Root, DetailX, 0f, DetailW);
            viewCards = new U.PieceButton(detail.Root, "ViewCards", (DetailW - 220f) * 0.5f, CardDetailPanel.ButtonsY + 30f, 220f, U.ButtonH, GemUiArt.Frame("btn-blue"), "View in My Cards", () => { if (selectedId != null) CardWindow.Show(CardWindow.Page.MyCards, selectedId); });
            hint = U.Txt(detail.Root, "Hint", 14f, CardDetailPanel.ButtonsY + 84f, DetailW - 28f, 22f, "", U.TextSmall, U.InkSoft, TextAnchor.MiddleCenter);
        }

        /// <summary>All + the 10 map regions: 6 + 5 tabs on two rows (the mockup has 7 regions; the game has 10, so the tabs wrap instead of scrolling).</summary>
        void BuildTabs()
        {
            int n = CardTable.Regions.Length + 1;
            tabs = new P.TextTab[n];
            float gap = 6f, w = (MainW - 28f - gap * (TabsPerRow - 1)) / TabsPerRow;
            for (int i = 0; i < n; i++)
            {
                int r = i - 1;
                string label = i == 0 ? "All" : P.ShortRegion(CardTable.Regions[r]);
                tabs[i] = new P.TextTab(Root, "Tab" + i, 14f + (i % TabsPerRow) * (w + gap), 126f + (i / TabsPerRow) * 34f, w, label, () => { region = r; page = 0; Refresh(); });
            }
        }

        public void Focus(string cardId)
        {
            if (!CardTable.IsCardId(cardId)) return;
            region = -1; selectedId = cardId; page = -1; // page -1: Refresh jumps to the page holding it
        }

        void OnClaim()
        {
            PlayerState p = Me;
            if (p == null) return;
            for (int m = 0; m < 4; m++) if (AlbumRules.Reached(p.Cards, m) && !AlbumRules.Claimed(p.Cards, m)) { Send(InvOp.AlbumClaim, (byte)m); return; }
        }

        List<CardDef> Visible()
        {
            var list = new List<CardDef>();
            foreach (CardDef c in CardTable.All) if (region < 0 || c.Region == CardTable.Regions[region]) list.Add(c);
            return list;
        }

        public override void Refresh()
        {
            PlayerState p = Me;
            if (Root == null || p == null) return;
            CardCollection cc = p.Cards;
            int have = cc.AlbumCount;
            progress.text = have + " / " + CardTable.Count + " (" + (have * 100 / CardTable.Count) + "%)";
            fill.sizeDelta = new Vector2(Mathf.Max(48f, BarW * have / CardTable.Count), 24f);
            fill.gameObject.SetActive(have > 0);
            bool anyReady = false;
            for (int m = 0; m < 4; m++)
            {
                bool done = AlbumRules.Claimed(cc, m), ready = !done && AlbumRules.Reached(cc, m);
                anyReady |= ready;
                GemUiArt.SetPiece(milestone[m], GemUiArt.Frame(done ? "milestone-claimed" : ready ? "milestone-ready" : "milestone-locked"));
            }
            claim.Set("Claim", anyReady && !Pending);
            for (int i = 0; i < tabs.Length; i++) tabs[i].Set(i - 1 == region);
            List<CardDef> list = Visible();
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
                if (cc.InAlbum(c.Id)) cells[i].Set(c, 0, c.Id == selectedId); else cells[i].SetUnknown(c, c.Id == selectedId);
            }
            CardDef sel = CardTable.Get(selectedId);
            bool known = sel != null && cc.InAlbum(sel.Id);
            detail.Set(sel, p, known);
            viewCards.Set("View in My Cards", sel != null && cc.Count(sel.Id) > 0);
            hint.text = sel == null ? "" : cc.Count(sel.Id) > 0 ? "" : known ? "Not in your bag now (the Album remembers it)." : "Not collected yet.";
        }
    }
}
