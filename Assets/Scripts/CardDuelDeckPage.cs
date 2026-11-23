using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;
using P = ZenoniaProto.CardPageWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "Card Duel" page of the Card window (mockup 06a): the cards the character owns on the left (tap one to add it), "My Deck" in the middle (8 slots, tap a card to take it out), the deck rules and
    /// "Duel Info" (power, boss, SR+) on the right with Challenge Player and the stakes warning (every duel stakes the whole deck: the winner takes 1 card of the loser's deck, Locked cards included).
    /// Every change is a full-deck request (DuelClient.SaveDeck) the server validates with DeckRules; the page redraws from the saved deck that comes back, so it never shows a deck the server did not accept.
    /// Card Duel is online only: offline the whole page is covered by a notice that says why it is disabled.
    /// </summary>
    public sealed class CardDuelDeckPage : GemPage
    {
        const int Cols = 3, Rows = 3, PerPage = Cols * Rows;
        const float ListW = 300f, DeckX = 310f, DeckW = 372f, InfoX = 692f, InfoW = 259f;
        const float CardW = 76f, CardH = 101f;

        readonly GemCardView[] list = new GemCardView[PerPage], slot = new GemCardView[DeckRules.Size];
        readonly CardDef[] listCard = new CardDef[PerPage];
        readonly Text[] rules = new Text[3], info = new Text[5];
        P.CyclePill rarityPill, areaPill;
        P.Pager pager;
        Text deckSize, reason, power;
        U.PieceButton challenge, clear;
        RectTransform offlineRoot;
        Text stakesNote;
        int rarity = -1, area = -1, page;
        // "Challenge Player": the players nearby, each with one Card Duel button (the stakes are the same for every duel)
        const int PvpRows = 5;
        RectTransform pvpRoot;
        readonly Text[] pvpName = new Text[PvpRows];
        readonly U.PieceButton[] pvpDuel = new U.PieceButton[PvpRows];
        readonly ushort[] pvpId = new ushort[PvpRows];
        Text pvpEmpty, pvpNote;
        float nextPvpAt;

        public override void Build(RectTransform parent)
        {
            Root = UiKit.Rect(parent, "DuelDeckPage", 0f, 0f, PageW, PageH);
            U.Panel(Root, "ListBox", 0f, 0f, ListW, PageH, true);
            rarityPill = new P.CyclePill(Root, "Rarity", 10f, 10f, 126f, () => { rarity = rarity >= 3 ? -1 : rarity + 1; page = 0; Refresh(); });
            areaPill = new P.CyclePill(Root, "Area", 142f, 10f, 148f, () => { area = area + 1 >= CardTable.Regions.Length ? -1 : area + 1; page = 0; Refresh(); });
            for (int i = 0; i < PerPage; i++)
            {
                int index = i;
                float x = 14f + (i % Cols) * 94f, y = 52f + (i / Cols) * 148f;
                list[i] = new GemCardView(Root, "List" + i, x, y, CardW, CardH, () => OnAdd(listCard[index]), true);
            }
            pager = new P.Pager(Root, ListW * 0.5f, PageH - 40f, d => { page += d; Refresh(); });

            U.Panel(Root, "DeckBox", DeckX, 0f, DeckW, PageH, true);
            U.Txt(Root, "DeckTitle", DeckX + 14f, 8f, 180f, 34f, "My Deck", 24, U.Ink);
            GemUiArt.Img(Root, "DeckSizePill", DeckX + DeckW - 150f, 8f, 136f, 34f, GemUiArt.Frame("btn-teal"));
            deckSize = U.Txt(Root, "DeckSize", DeckX + DeckW - 140f, 10f, 126f, 30f, "", U.TextNormal, U.OnButton, TextAnchor.MiddleCenter);
            for (int i = 0; i < DeckRules.Size; i++)
            {
                int index = i;
                float x = DeckX + 14f + (i % 4) * 91f, y = 50f + (i / 4) * 150f;
                slot[i] = new GemCardView(Root, "Slot" + i, x, y, CardW, CardH, () => OnRemove(index), true);
            }
            RectTransform rulesBox = UiKit.Rect(Root, "Rules", DeckX + 12f, 360f, DeckW - 24f, 120f);
            U.Panel(rulesBox, "Box", 0f, 0f, DeckW - 24f, 120f);
            U.Txt(rulesBox, "Title", 12f, 6f, 160f, 26f, "Deck Rules", U.TextNormal + 1, U.Ink);
            for (int i = 0; i < 3; i++) rules[i] = U.Txt(rulesBox, "Rule" + i, 12f, 34f + i * 26f, DeckW - 48f, 26f, "", U.TextNormal, U.Ink);
            reason = U.Txt(Root, "Reason", DeckX + 12f, 488f, DeckW - 24f, 44f, "", U.TextSmall, U.InkSoft, TextAnchor.UpperCenter);

            U.Panel(Root, "InfoBox", InfoX, 0f, InfoW, PageH, true);
            U.Txt(Root, "InfoTitle", InfoX + 14f, 8f, InfoW - 28f, 34f, "Duel Info", 24, U.Ink);
            power = U.Txt(Root, "Power", InfoX + 14f, 48f, InfoW - 28f, 40f, "", 22, U.Ink);
            string[] labels = { "Deck", "Boss", "SR+", "Area" };
            for (int i = 0; i < 4; i++)
            {
                float y = 96f + i * 34f;
                GemUiArt.Img(Root, "InfoRow" + i, InfoX + 14f, y, InfoW - 28f, 30f, GemUiArt.Frame("panel-inner"));
                U.Txt(Root, "InfoLabel" + i, InfoX + 24f, y, 100f, 30f, labels[i], U.TextNormal, U.Ink);
                info[i] = U.Txt(Root, "InfoValue" + i, InfoX + InfoW - 110f, y, 90f, 30f, "", U.TextNormal, U.Ink, TextAnchor.MiddleRight);
            }
            challenge = new U.PieceButton(Root, "Challenge", InfoX + 14f, 252f, InfoW - 28f, U.ButtonH, GemUiArt.Frame("btn-blue"), "Challenge Player", OpenPvpList);
            clear = new U.PieceButton(Root, "Clear", InfoX + 14f, 308f, InfoW - 28f, U.ButtonH, GemUiArt.Frame("btn-red"), "Clear Deck", OnClear);
            BuildStakesWarning();
            BuildPvpList();
            BuildOfflineNotice();
        }

        const float WarnY = 366f, WarnH = 170f, WarnPad = 12f;

        // the stakes warning under the buttons: a box with a heading and the rule in plain words (the ChatGPT panel frame, text only)
        void BuildStakesWarning()
        {
            RectTransform box = UiKit.Rect(Root, "StakesWarning", InfoX + 14f, WarnY, InfoW - 28f, WarnH);
            U.Panel(box, "Box", 0f, 0f, InfoW - 28f, WarnH);
            U.Txt(box, "Title", WarnPad, 8f, InfoW - 28f - 2f * WarnPad, 26f, "Cards at stake", U.TextTitle, U.Bad);
            stakesNote = U.Txt(box, "Note", WarnPad, 36f, InfoW - 28f - 2f * WarnPad, WarnH - 44f,
                "Every duel puts your whole deck at risk. The winner takes 1 card of the loser's deck: any card, Locked, SR, SSR and Boss cards too. A draw takes nothing.",
                U.TextSmall, U.Ink, TextAnchor.UpperLeft);
        }

        // offline: the page is disabled and says why (no art is drawn here, only the kit panel and text)
        void BuildOfflineNotice()
        {
            offlineRoot = UiKit.Rect(Root, "OfflineNotice", 0f, 0f, PageW, PageH);
            U.Panel(offlineRoot, "Box", 0f, 0f, PageW, PageH, true).raycastTarget = true; // taps stop here
            U.Txt(offlineRoot, "Title", 40f, 150f, PageW - 80f, 44f, "Card Duel is offline", 30, U.Ink, TextAnchor.MiddleCenter);
            U.Txt(offlineRoot, "Reason", 80f, 206f, PageW - 160f, 70f, DuelClient.OfflineReason, 20, U.InkSoft, TextAnchor.UpperCenter);
            U.Txt(offlineRoot, "Hint", 80f, 290f, PageW - 160f, 60f, "Your cards, album and gems still work in the other tabs.", U.TextNormal, U.InkSoft, TextAnchor.UpperCenter);
            offlineRoot.gameObject.SetActive(false);
        }

        void BuildPvpList()
        {
            float w = DeckW + InfoW + 10f;
            pvpRoot = UiKit.Rect(Root, "PvpList", DeckX, 0f, w, PageH);
            U.Panel(pvpRoot, "Box", 0f, 0f, w, PageH, true);
            U.Txt(pvpRoot, "Title", 16f, 8f, 360f, 36f, "Challenge a Player", 24, U.Ink);
            for (int i = 0; i < PvpRows; i++)
            {
                int row = i;
                RectTransform r = UiKit.Rect(pvpRoot, "Player" + i, 16f, 54f + i * 76f, w - 32f, 68f);
                U.Panel(r, "Bg", 0f, 0f, w - 32f, 68f);
                pvpName[i] = U.Txt(r, "Name", 18f, 10f, w - 32f - 18f - 200f, 48f, "", 20, U.Ink);
                pvpDuel[i] = new U.PieceButton(r, "Duel", w - 32f - 172f, 10f, 160f, U.ButtonH, GemUiArt.Frame("btn-teal"), "Card Duel", () => Challenge(row));
            }
            pvpEmpty = U.Txt(pvpRoot, "Empty", 16f, 150f, w - 32f, 60f, "No other player is nearby.", U.TextNormal + 2, U.InkSoft, TextAnchor.MiddleCenter);
            pvpNote = U.Txt(pvpRoot, "Note", 16f, PageH - 118f, w - 32f, 48f, "", U.TextSmall, U.InkSoft, TextAnchor.UpperCenter);
            new U.PieceButton(pvpRoot, "Back", (w - 190f) * 0.5f, PageH - 62f, 190f, U.ButtonH, GemUiArt.Frame("btn-blue"), "Back", () => { pvpRoot.gameObject.SetActive(false); Refresh(); });
            pvpRoot.gameObject.SetActive(false);
        }

        void OpenPvpList() { pvpRoot.gameObject.SetActive(true); RefreshPvp(); }

        void Challenge(int row)
        {
            if (Pending || DuelClient.InLobby || DuelClient.Running || pvpId[row] == 0) return;
            Hold();
            DuelClient.Challenge(pvpId[row]);
            pvpRoot.gameObject.SetActive(false);
        }

        /// <summary>The players in this zone, nearest first (the server only accepts a target on the same map). Offline there is nobody to challenge.</summary>
        void RefreshPvp()
        {
            nextPvpAt = Time.unscaledTime + 0.5f;
            var near = new List<HeroController>();
            Zone zone = Zone.Main;
            HeroController me = LocalPlayer.Hero;
            if (NetMode.IsClient && zone != null && me != null)
                foreach (HeroController h in zone.Heroes.All)
                    if (h != null && h != me && h.PlayerId != 0 && !h.IsDead) near.Add(h);
            near.Sort((a, b) => (a.Position - me.Position).sqrMagnitude.CompareTo((b.Position - me.Position).sqrMagnitude));
            string problem = DuelClient.EntryProblem();
            DeckError full = Me == null ? DeckError.NotFull : DeckRules.Validate(Me.Cards.Deck, Me.Cards, true);
            bool ready = full == DeckError.None && !DuelClient.InLobby && !DuelClient.Running;
            for (int i = 0; i < PvpRows; i++)
            {
                HeroController h = i < near.Count ? near[i] : null;
                pvpId[i] = h != null ? h.PlayerId : (ushort)0;
                pvpName[i].transform.parent.gameObject.SetActive(h != null);
                if (h == null) continue;
                pvpName[i].text = h.PlayerName;
                pvpDuel[i].Set("Card Duel", ready && problem.Length == 0);
            }
            pvpEmpty.gameObject.SetActive(near.Count == 0);
            pvpEmpty.text = NetMode.IsClient ? "No other player is nearby." : "Player duels need an online game.";
            pvpNote.text = full != DeckError.None ? DeckRules.Text(full) + "." : problem.Length > 0 ? problem + "." : "Winner takes 1 card from the loser's deck. Lv30+, 10 duels a day, 2 a day per player.";
        }

        public override void Tick()
        {
            base.Tick();
            if (pvpRoot != null && pvpRoot.gameObject.activeSelf && Time.unscaledTime >= nextPvpAt) RefreshPvp();
        }

        public override void OnShow() { pvpRoot.gameObject.SetActive(false); Refresh(); }

        void OnAdd(CardDef c)
        {
            PlayerState p = Me;
            if (p == null || c == null || Pending || DuelClient.Running) return;
            string why = DeckRules.WhyNotAdd(p.Cards.Deck, c.Id, p.Cards);
            if (why.Length > 0) { DuelClient.Notify(why, false); return; }
            var next = new List<string>(p.Cards.Deck) { c.Id };
            Hold(); DuelClient.SaveDeck(next); Refresh();
        }

        void OnRemove(int i)
        {
            PlayerState p = Me;
            if (p == null || Pending || DuelClient.Running || i >= p.Cards.Deck.Count) return;
            var next = new List<string>(p.Cards.Deck); next.RemoveAt(i);
            Hold(); DuelClient.SaveDeck(next); Refresh();
        }

        void OnClear()
        {
            PlayerState p = Me;
            if (p == null || Pending || DuelClient.Running || p.Cards.Deck.Count == 0) return;
            Hold(); DuelClient.SaveDeck(new List<string>()); Refresh();
        }

        public override void Refresh()
        {
            PlayerState p = Me;
            if (Root == null || p == null) return;
            IList<string> deck = p.Cards.Deck;
            string[] rar = { "N", "R", "SR", "SSR" };
            rarityPill.Set(rarity < 0 ? "All Rarity" : "Rarity: " + rar[rarity]);
            areaPill.Set(area < 0 ? "All Areas" : P.ShortRegion(CardTable.Regions[area]));
            List<CardDef> cards = GemUiModel.OwnedCards(p, GemFilter.All);
            cards.RemoveAll(c => (rarity >= 0 && (int)c.Rarity != rarity) || (area >= 0 && c.Region != CardTable.Regions[area]));
            int pages = Mathf.Max(1, Mathf.CeilToInt(cards.Count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            pager.Set(page, pages);
            bool locked = Pending || DuelClient.Running;
            for (int i = 0; i < PerPage; i++)
            {
                int index = page * PerPage + i;
                CardDef c = index < cards.Count ? cards[index] : null;
                listCard[i] = c;
                if (c == null) { list[i].Root.gameObject.SetActive(false); continue; }
                int free = Mathf.Max(0, p.Cards.Count(c.Id) - p.Cards.InDeck(c.Id));
                list[i].Set(c, free, false, free < 1);
            }
            for (int i = 0; i < DeckRules.Size; i++)
            {
                CardDef c = i < deck.Count ? CardTable.Get(deck[i]) : null;
                slot[i].Set(c, 0, false);
                if (c == null) slot[i].Root.gameObject.SetActive(false);
            }
            int bosses = DeckRules.Bosses(deck), sr = DeckRules.SrPlus(deck);
            deckSize.text = "Deck " + deck.Count + " / " + DeckRules.Size;
            Rule(0, "Deck size: " + deck.Count + " / " + DeckRules.Size + " cards", deck.Count == DeckRules.Size, deck.Count <= DeckRules.Size);
            Rule(1, "Boss cards: " + bosses + " / " + DeckRules.MaxBoss, bosses <= DeckRules.MaxBoss, true);
            Rule(2, "SR / SSR, no boss: " + sr + " / " + DeckRules.MaxSrPlus, sr <= DeckRules.MaxSrPlus, true);
            power.text = "Deck Power  " + DeckRules.Power(deck).ToString(CultureInfo.InvariantCulture);
            info[0].text = deck.Count + " / " + DeckRules.Size; info[1].text = bosses + " / " + DeckRules.MaxBoss; info[2].text = sr + " / " + DeckRules.MaxSrPlus; info[3].text = AreaText(deck);
            DeckError full = DeckRules.Validate(deck, p.Cards, true);
            offlineRoot.gameObject.SetActive(!DuelClient.Online);
            challenge.Set("Challenge Player", DuelClient.Online && full == DeckError.None && !locked && !DuelClient.InLobby);
            clear.Set("Clear Deck", deck.Count > 0 && !locked);
            reason.text = DuelClient.Running ? "A duel is running." : full == DeckError.None ? "Tap a deck card to take it out. Tap a card on the left to add it." : DeckRules.Text(full) + ". Tap a card on the left to add it.";
            reason.color = U.InkSoft;
        }

        void Rule(int i, string text, bool met, bool within)
        {
            rules[i].text = (met ? "[OK]  " : within ? "[ ..]  " : "[X]  ") + text;
            rules[i].color = met ? U.Good : within ? U.InkSoft : U.Bad;
        }

        static string AreaText(IList<string> deck)
        {
            string first = null;
            foreach (string id in deck)
            {
                CardDef c = CardTable.Get(id);
                if (c == null) continue;
                if (first == null) first = c.Region; else if (first != c.Region) return "Mix";
            }
            return first == null ? "-" : P.ShortRegion(first);
        }
    }
}
