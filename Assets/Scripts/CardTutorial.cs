using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The first-time guided hint of the Monster Card system (phase 08): 4 short steps (cards drop from monsters; More > Cards and My Cards; the Album; the first duel with Orin) in a small panel at the top
    /// centre. Offered once per character, once Orin's first-deck quest is Done: when the 5 starter cards arrive (QuestTracker), else after a card drop (CardToast) or the next time the Card window opens (CardWindow). While the window is open
    /// steps 2 and 3 also point an arrow at the My Cards / Monster Album menu entry. Finishing or skipping stores the flag with the character (InvOp.CardHelp -> CardCollection.Help), so it
    /// never shows again, on any device; a session guard stops it popping up again before the server answered.
    /// </summary>
    public static class CardTutorial
    {
        const float W = 480f, H = 214f, PadX = 26f;
        static readonly string[] Titles = { "Monster Cards", "More > Cards > My Cards", "Monster Album", "First Card Duel" };
        static readonly string[] Bodies =
        {
            "Monsters can drop a card of their own kind when they fall. Every card you get is kept for your character.",
            "Open the More column of the HUD and tap Cards. My Cards lists every copy you hold: pick one to see its numbers, lock it or sell spares.",
            "The Monster Album marks each monster the first time you hold its card. Fill it to claim milestone rewards. Spare cards become Soul Gems.",
            "Card Master Orin, at the Card Corner of Willowbrook, lends you 3 cards to fill a deck of 8 for a first practice duel. Win it and pick a card of his as your reward."
        };

        const float ClosedTop = 160f, OpenTop = 6f; // below the toast / menu row over the HUD; over the window's title row when the Card window is open
        static GameObject root;
        static RectTransform box;
        static UnityEngine.UI.Text title, body, counter;
        static U.PieceButton next;
        static int step;
        static bool offeredThisSession;

        public static bool IsOpen { get { return root != null && root.activeSelf; } }

        static bool Pending()
        {
            PlayerState p = PetActions.Me;
            return p != null && (p.Cards.Help & CardCollection.HelpTutorial) == 0 && !offeredThisSession && DeckQuestDone(p);
        }

        /// <summary>Taurus V4: the walkthrough belongs to Orin's first-deck quest, it never shows before the 5 starter cards were handed over.</summary>
        static bool DeckQuestDone(PlayerState p)
        {
            QuestDef q = QuestCatalog.Get(TaurusQuests.FirstDeckId);
            return q != null && p.Quests.StateOf(q) == QuestState.Done;
        }

        /// <summary>A card just dropped: show the hint if the character has not seen it and already holds Orin's deck.</summary>
        public static void OfferAfterDrop() { if (Pending()) Show(); }

        /// <summary>Orin just handed over the 5 starter cards (quest Done): the walkthrough starts now.</summary>
        public static void OfferAfterDeck() { if (Pending()) Show(); }

        /// <summary>The Card window just opened (called after it is raised over the HUD).</summary>
        public static void OfferOnWindowOpen()
        {
            if (IsOpen) { root.transform.SetAsLastSibling(); Point(); }
            else if (Pending()) Show();
        }

        /// <summary>Capture only (--card-tutorial): shows the hint whatever the flag says.</summary>
        public static void DebugShow(int atStep)
        {
            offeredThisSession = false;
            Show();
            step = Mathf.Clamp(atStep, 0, Titles.Length - 1);
            Refresh();
        }

        static void Show()
        {
            if (UiScreens.Root == null) return;
            offeredThisSession = true;
            if (root == null) Build(UiKit.Canvas(UiScreens.Root).transform);
            step = 0;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Refresh();
        }

        static void Build(Transform canvas)
        {
            root = UiKit.Fill(canvas, "CardTutorial").gameObject;
            // A full-screen layer (children anchor to the real screen) that only owns the panel: taps elsewhere reach the game.
            box = UiWinKit.Anchored(root.transform, "Box", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 0f, -ClosedTop, W, H);
            U.Panel(box, "Frame", 0f, 0f, W, H, true);
            title = U.Txt(box, "Title", PadX, 14f, W - PadX * 2f, 32f, "", U.TextTitle + 2, U.Ink);
            body = U.Txt(box, "Body", PadX, 52f, W - PadX * 2f, 84f, "", U.TextNormal + 1, U.Ink, TextAnchor.UpperLeft);
            counter = U.Txt(box, "Counter", PadX, H - 62f, 70f, 40f, "", U.TextSmall + 2, U.InkSoft);
            new U.PieceButton(box, "Skip", W - PadX - 150f - 12f - 110f, H - 64f, 110f, U.ButtonH, GemUiArt.Frame("btn-blue"), "Skip", Finish);
            next = new U.PieceButton(box, "Next", W - PadX - 150f, H - 64f, 150f, U.ButtonH, GemUiArt.Frame("btn-teal"), "Next", Advance);
            root.SetActive(false);
        }

        static void Refresh()
        {
            title.text = Titles[step];
            body.text = Bodies[step];
            counter.text = (step + 1) + " / " + Titles.Length;
            next.Set(step == Titles.Length - 1 ? "Got it" : "Next", true);
            Point();
        }

        /// <summary>Steps 2 and 3 ring the matching menu entry of the Card window when it is open (My Cards = 0, Monster Album = 1).</summary>
        static void Point()
        {
            box.anchoredPosition = new Vector2(0f, -(CardWindow.IsOpen ? OpenTop : ClosedTop));
            CardWindow.Highlight(step == 1 ? 0 : step == 2 ? 1 : -1);
        }

        static void Advance()
        {
            if (step + 1 < Titles.Length) { step++; Refresh(); return; }
            Finish();
        }

        static void Finish()
        {
            if (root != null) root.SetActive(false);
            CardWindow.Highlight(-1);
            PetActions.Send(InvOp.CardHelp, (byte)CardCollection.HelpTutorial); // stored with the character (offline: saved at once)
        }
    }
}
