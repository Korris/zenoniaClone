using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;
using P = ZenoniaProto.CardPageWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "Card Duel" page of the Card window (mockup 06a): the cards the character owns on the left (tap one to add it), "My Deck" in the middle (8 slots, tap a card to take it out), the deck rules and
    /// "Duel Info" (power, boss, SR+) on the right with Play NPC. Every change is a full-deck request (DuelClient.SaveDeck) the server validates with DeckRules; the page redraws from the saved deck that
    /// comes back, so it never shows a deck the server did not accept. "Play NPC" opens the three practice opponents.
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
        U.PieceButton playNpc, challenge, clear;
        RectTransform npcRoot;
        readonly Image[] npcFace = new Image[3];
        readonly Text[] npcName = new Text[3], npcSub = new Text[3];
        readonly U.PieceButton[] npcGo = new U.PieceButton[3];
        int rarity = -1, area = -1, page;

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
            playNpc = new U.PieceButton(Root, "PlayNpc", InfoX + 14f, 262f, InfoW - 28f, U.ButtonH, GemUiArt.Frame("btn-blue"), "Play NPC", OpenNpcList);
            challenge = new U.PieceButton(Root, "Challenge", InfoX + 14f, 318f, InfoW - 28f, U.ButtonH, GemUiArt.Frame("btn-blue"), "Challenge Player", () => { });
            clear = new U.PieceButton(Root, "Clear", InfoX + 14f, 374f, InfoW - 28f, U.ButtonH, GemUiArt.Frame("btn-red"), "Clear Deck", OnClear);
            U.Txt(Root, "PvpNote", InfoX + 14f, 428f, InfoW - 28f, 60f, "Player duels and Ranked come later.", U.TextSmall, U.InkSoft, TextAnchor.UpperCenter);
            BuildNpcList();
        }

        void BuildNpcList()
        {
            npcRoot = UiKit.Rect(Root, "NpcList", DeckX, 0f, DeckW + InfoW + 10f, PageH);
            U.Panel(npcRoot, "Box", 0f, 0f, DeckW + InfoW + 10f, PageH, true);
            U.Txt(npcRoot, "Title", 16f, 8f, 300f, 36f, "Choose an Opponent", 24, U.Ink);
            for (int i = 0; i < DuelNpcTable.Count; i++)
            {
                int npc = i;
                float y = 54f + i * 142f, w = DeckW + InfoW + 10f - 32f;
                RectTransform row = UiKit.Rect(npcRoot, "Npc" + i, 16f, y, w, 130f);
                U.Panel(row, "Bg", 0f, 0f, w, 130f);
                npcFace[i] = GemUiArt.Img(row, "Face", 22f, 24f, 74f, 78f, DuelNpcTable.Get(i).Portrait);
                GemUiArt.Img(row, "Ring", 12f, 14f, 94f, 98f, "cards/board/avatar-ring-enemy");
                npcName[i] = U.Txt(row, "Name", 120f, 12f, w - 320f, 36f, DuelNpcTable.Get(i).Name, 22, U.Ink);
                npcSub[i] = U.Txt(row, "Sub", 120f, 50f, w - 320f, 66f, "", U.TextNormal, U.InkSoft, TextAnchor.UpperLeft);
                npcGo[i] = new U.PieceButton(row, "Duel", w - 180f, 40f, 160f, U.ButtonH, GemUiArt.Frame("btn-teal"), "Duel", () => OnPlay(npc));
            }
            new U.PieceButton(npcRoot, "Back", (DeckW + InfoW + 10f - 190f) * 0.5f, PageH - 62f, 190f, U.ButtonH, GemUiArt.Frame("btn-blue"), "Back", () => { npcRoot.gameObject.SetActive(false); Refresh(); });
            npcRoot.gameObject.SetActive(false);
        }

        public override void OnShow() { npcRoot.gameObject.SetActive(false); Refresh(); }

        void OpenNpcList() { npcRoot.gameObject.SetActive(true); Refresh(); }

        void OnPlay(int npc)
        {
            PlayerState p = Me;
            if (p == null || Pending) return;
            Hold();
            DuelClient.PlayNpc(npc);
            Refresh();
        }

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
            playNpc.Set("Play NPC", full == DeckError.None && !locked);
            challenge.Set("Challenge Player", false);
            clear.Set("Clear Deck", deck.Count > 0 && !locked);
            reason.text = DuelClient.Running ? "A duel is running." : full == DeckError.None ? "Tap a deck card to take it out. Tap a card on the left to add it." : DeckRules.Text(full) + ". Tap a card on the left to add it.";
            reason.color = U.InkSoft;
            for (int i = 0; i < DuelNpcTable.Count; i++)
            {
                DuelNpcDef def = DuelNpcTable.Get(i);
                bool beaten = (p.Cards.NpcFirstWins & DuelNpcTable.FirstWinBit(i)) != 0;
                npcSub[i].text = def.Level + "\n" + (beaten ? "First win claimed" : "First win: " + def.FirstWinGold.ToString("N0", CultureInfo.InvariantCulture) + " G");
                npcGo[i].Set("Duel", full == DeckError.None && !locked);
            }
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
