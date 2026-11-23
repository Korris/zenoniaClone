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
    /// The Card Wager panel (phase 06): both players see both slots. Each picks ONE of their spare N / R cards, Locks it, and once both are locked Confirms; picking again clears your lock and BOTH confirmations
    /// (the server does that, this only draws). The match starts when both have confirmed and the server has the two cards in escrow. Marks (waiting / locked / confirmed) and the slot are the ChatGPT ranked
    /// pieces; nothing is drawn in code. Reads DuelClient.Lobby, sends DuelClient.WagerPick / WagerLock / WagerConfirm / LeaveLobby.
    /// </summary>
    public sealed class CardWagerPanel
    {
        const float W = 800f, H = 512f, SlotW = 142f, SlotH = 196f, SlotY = 112f, CardW = 70f, CardH = 86f;
        const int Cols = 4, PerPage = 8;

        readonly RectTransform veil, box;
        readonly GemCardView mineCard, theirCard;
        readonly Image mineMark, theirMark;
        readonly Text mineState, theirState, theirName, hint, secs;
        readonly GemCardView[] grid = new GemCardView[PerPage];
        readonly CardDef[] gridCard = new CardDef[PerPage];
        readonly U.PieceButton main;
        readonly P.Pager pager;
        readonly List<CardDef> spare = new List<CardDef>();
        int page;

        public bool Visible { get { return veil.gameObject.activeSelf; } }

        public CardWagerPanel(RectTransform overlay)
        {
            veil = UiKit.Fill(overlay, "WagerVeil");
            UiKit.Panel(veil, new Color(0.16f, 0.22f, 0.3f, 0.45f), true); // taps stop here: a veil, not art
            box = UiWinKit.Anchored(veil, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, W, H);
            U.Panel(box, "Frame", 0f, 0f, W, H, true).raycastTarget = true;
            U.Txt(box, "Title", 30f, 12f, 300f, 40f, "Card Wager", 30, U.Ink);
            U.Txt(box, "Sub", 30f, 54f, W - 60f, 28f, "Pick 1 spare N or R card of the same rarity as your opponent's. Lock it, then Confirm.", U.TextNormal, U.InkSoft);
            TradeArt.Plain(UiKit.Rect(box, "Clock", W - 98f, 18f, 24f, 24f), "clock");
            secs = U.Txt(box, "Secs", W - 70f, 14f, 50f, 32f, "", U.TextNormal + 2, U.Ink, TextAnchor.MiddleRight);

            U.Txt(box, "MineLabel", 34f, 84f, SlotW, 26f, "Your card", U.TextNormal, U.Ink, TextAnchor.MiddleCenter);
            theirName = U.Txt(box, "TheirLabel", W - 34f - SlotW, 84f, SlotW, 26f, "", U.TextNormal, U.Ink, TextAnchor.MiddleCenter);
            mineCard = Slot(34f, out mineMark, out mineState);
            theirCard = Slot(W - 34f - SlotW, out theirMark, out theirState);

            float gx = 196f, gw = W - 2f * gx;
            U.Panel(box, "PickBox", gx, 84f, gw, 292f);
            U.Txt(box, "PickTitle", gx + 12f, 90f, gw - 24f, 26f, "Your spare N / R cards", U.TextNormal, U.Ink);
            for (int i = 0; i < PerPage; i++)
            {
                int index = i;
                float x = gx + (gw - (Cols * CardW + (Cols - 1) * 24f)) * 0.5f + (i % Cols) * (CardW + 24f), y = 120f + (i / Cols) * (CardH + 22f);
                grid[i] = new GemCardView(box, "Pick" + i, x, y, CardW, CardH, () => OnPick(gridCard[index]), true); // the name goes under the card: the count tag covers the frame's bar
            }
            pager = new P.Pager(box, W * 0.5f, 344f, d => { page += d; Refresh(); });
            hint = U.Txt(box, "Hint", gx, 384f, gw, 44f, "", U.TextSmall, U.InkSoft, TextAnchor.UpperCenter);

            new U.PieceButton(box, "Leave", 34f, H - 74f, 190f, U.ButtonH, GemUiArt.Frame("btn-red"), "Leave", DuelClient.LeaveLobby);
            main = new U.PieceButton(box, "Main", (W - 260f) * 0.5f, H - 74f, 260f, U.ButtonH, GemUiArt.Frame("btn-gold"), "Lock Card", OnMain);
            NameTags.Occlude(box);
            veil.gameObject.SetActive(false);
        }

        GemCardView Slot(float x, out Image mark, out Text state)
        {
            GemUiArt.Img(box, "Slot", x, SlotY, SlotW, SlotH, "cards/ranked/wager-slot");
            var view = new GemCardView(box, "SlotCard", x + (SlotW - 100f) * 0.5f, SlotY + (SlotH - 124f) * 0.5f, 100f, 124f, null, false);
            view.Root.gameObject.SetActive(false);
            mark = GemUiArt.Img(box, "Mark", x + 10f, SlotY + SlotH + 10f, 30f, 30f, "cards/ranked/wager-waiting");
            state = U.Txt(box, "State", x + 46f, SlotY + SlotH + 8f, SlotW - 46f, 34f, "", U.TextNormal, U.Ink);
            return view;
        }

        public void Show(bool on)
        {
            if (veil.gameObject.activeSelf == on) return;
            veil.gameObject.SetActive(on);
            if (on) { page = 0; veil.SetAsLastSibling(); }
        }

        public void Fit(float w, float h)
        {
            float scale = Mathf.Min(1f, w * 0.96f / W, (h - 8f) / H);
            box.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        /// <summary>Why this player cannot take part in a wager (Lv30, switch, daily limit, no spare N / R card); empty = they can. Only to hide or disable: the server and server-api check again.</summary>
        public static string EntryProblem()
        {
            PlayerState p = PetActions.Me;
            if (p == null) return "";
            WagerError e = WagerRules.CanEnter(p, DuelNpcRun.DayKeyNow());
            if (e != WagerError.None) return WagerRules.Text(e);
            foreach (CardDef c in CardTable.All) if (WagerRules.CheckCard(p, c.Id) == WagerError.None) return "";
            return "You have no spare N or R card to wager";
        }

        void OnPick(CardDef c)
        {
            if (c == null || DuelClient.Lobby.Phase != 3) return;
            DuelClient.WagerPick(CardTable.Index(c.Id));
        }

        void OnMain()
        {
            DuelLobbyMsg m = DuelClient.Lobby;
            if (m.Phase != 3) return;
            if (!m.MyLocked) DuelClient.WagerLock(); else DuelClient.WagerConfirm();
        }

        /// <summary>Rebuilds the list of spare cards and every label from the last lobby message.</summary>
        public void Refresh()
        {
            PlayerState p = PetActions.Me;
            DuelLobbyMsg m = DuelClient.Lobby;
            if (p == null) return;
            spare.Clear();
            foreach (CardDef c in CardTable.All) if (WagerRules.CheckCard(p, c.Id) == WagerError.None) spare.Add(c);
            int pages = Mathf.Max(1, Mathf.CeilToInt(spare.Count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            pager.Set(page, pages);
            CardDef mine = CardTable.ByIndex(m.MyCard == DuelLobby.NoCard ? -1 : m.MyCard), theirs = CardTable.ByIndex(m.TheirCard == DuelLobby.NoCard ? -1 : m.TheirCard);
            for (int i = 0; i < PerPage; i++)
            {
                int index = page * PerPage + i;
                CardDef c = index < spare.Count ? spare[index] : null;
                gridCard[i] = c;
                if (c == null) { grid[i].Root.gameObject.SetActive(false); continue; }
                grid[i].Set(c, p.Cards.Spare(c.Id), mine != null && mine.Id == c.Id);
            }
            mineCard.Set(mine, 0, false); theirCard.Set(theirs, 0, false);
            if (mine == null) mineCard.Root.gameObject.SetActive(false);
            if (theirs == null) theirCard.Root.gameObject.SetActive(false);
            string other = string.IsNullOrEmpty(m.OtherName) ? "Opponent" : m.OtherName;
            theirName.text = other + "'s card";
            Mark(mineMark, mineState, mine != null, m.MyLocked, m.MyConfirmed);
            Mark(theirMark, theirState, theirs != null, m.TheirLocked, m.TheirConfirmed);
            bool same = mine == null || theirs == null || WagerRules.Matches(mine, theirs);
            hint.text = m.Phase == 4 ? "Both confirmed. The server is holding the two cards..."
                : !same ? "Rarity must match: yours is " + mine.Rarity + ", theirs is " + theirs.Rarity + ". Pick another card."
                : spare.Count == 0 ? "You have no spare N or R card (locked cards and cards your deck needs do not count)."
                : "Changing your card clears both confirmations.";
            hint.color = same && spare.Count > 0 ? U.InkSoft : U.Bad;
            if (m.Phase == 4) main.Set("Holding the cards...", false);
            else if (mine == null) main.Set("Lock Card", false);
            else if (!m.MyLocked) main.Set("Lock Card", true);
            else if (!m.TheirLocked) main.Set("Waiting for " + other + " to lock", false);
            else if (!m.MyConfirmed) main.Set("Confirm Wager", same);
            else main.Set("Waiting for " + other + "...", false);
        }


        static void Mark(Image mark, Text text, bool picked, bool locked, bool confirmed)
        {
            GemUiArt.SetPiece(mark, confirmed ? "cards/ranked/wager-confirmed" : locked ? "cards/ranked/wager-locked" : "cards/ranked/wager-waiting");
            text.text = confirmed ? "Confirmed" : locked ? "Locked" : picked ? "Not locked" : "Choosing...";
        }

        string shownSecs = "";

        public void Tick()
        {
            string s = Mathf.CeilToInt(DuelClient.LobbySecondsLeft).ToString(CultureInfo.InvariantCulture);
            if (s != shownSecs) { shownSecs = s; secs.text = s; }
        }
    }
}
