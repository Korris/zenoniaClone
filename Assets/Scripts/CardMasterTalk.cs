using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Talking to Card Master Orin (Willowbrook Card Corner; TownNpc.Talk -> UiScreens.OpenForNpc). What he says follows his two quests (Taurus V4): "Orin's First Deck" (after Rowan's meadow trial: 5 kills -> 5 cards)
    /// then "The First Card Duel" (CardDuelQuest). Available = offer lines, then the quest window. Active = his reminder; the duel quest then asks "Ready?". ReadyToTurnIn = his lines, then the cards: the first quest hands
    /// them over, the duel quest opens the reward pick (CardRewardPickPanel: one of his 8 cards). Done (both) = one greeting line, then a practice duel or the Card window. No quest yet = the five pages explaining the Monster Card
    /// system plus "prove yourself first". The question boxes are the Card window's CardConfirmBox, so the window opens behind them. Esc on the dialogue leaves without anything else.
    /// </summary>
    public static class CardMasterTalk
    {
        public const string NpcId = "cardMaster";
        static Sprite face;

        /// <summary>Orin's 152 px face (cached; null when the file is missing): the dialogue portrait and the opponent portrait of the duel board.</summary>
        public static Sprite Face()
        {
            if (face != null) return face;
            var tex = Resources.Load<Texture2D>("Sprites/village/npcs/" + NpcId + "_face");
            if (tex != null) face = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return face;
        }

        /// <summary>"Talk to Orin" on the duel result: the same conversation as walking up to him.</summary>
        public static void Open()
        {
            NpcDef d = NpcDefs.Get(NpcId);
            if (d != null) Open(d.Name, Face(), d.Lines);
        }

        public static void Open(string name, Sprite portrait, string[] pages)
        {
            if (pages == null || pages.Length == 0) return;
            string who = string.IsNullOrEmpty(name) ? "Card Master" : name;
            if (portrait == null) portrait = Face();
            QuestLog log = QuestTracker.Log;
            QuestDef q = log != null ? log.TalkAbout(NpcId) : null;
            if (q == null) { NpcDialogueBox.Open(who, portrait, WithComeBack(pages), null); return; } // no card yet: the pages, then where the duel lesson comes from (the Card window would be empty)
            switch (log.StateOf(q))
            {
                case QuestState.Available: NpcDialogueBox.Open(q.GiverName, portrait, q.OfferLines, () => QuestOfferWindow.Open(q, () => { QuestTracker.Accept(q); if (q.CardDuel) AskDuel(); })); break;
                case QuestState.Active: NpcDialogueBox.Open(q.GiverName, portrait, q.ActiveLines, q.CardDuel ? (System.Action)AskDuel : null); break;
                case QuestState.ReadyToTurnIn: NpcDialogueBox.Open(q.GiverName, portrait, q.ReadyLines, () => { if (q.RewardPickCards != null) CardRewardPickPanel.Open(q); else QuestTracker.TurnIn(q); }); break;
                default: NpcDialogueBox.Open(who, portrait, DoneLines, AskPractice); break; // quest done: one line, then straight to the practice question (the 5 rule pages were already read)
            }
        }

        /// <summary>Done-state greeting: short, so the practice duel is one tap away.</summary>
        static readonly string[] DoneLines = { "Back for another round? My table is always open." };

        /// <summary>No quest yet: the explanation pages plus the hint that Rowan's trial comes first.</summary>
        static string[] WithComeBack(string[] pages)
        {
            var all = new string[pages.Length + 1];
            pages.CopyTo(all, 0);
            all[pages.Length] = "Prove yourself to Elder Rowan on the meadows first. Then come back: I will give you a starter deck and teach you to duel.";
            return all;
        }

        /// <summary>"Ready for your first duel?": Duel starts the scripted duel (starter deck + 3 borrowed cards), Later closes the window again.</summary>
        public static void AskDuel()
        {
            CardWindow.Open(CardWindow.Page.MyCards, false); // no first-time hint over Orin's question
            PlayerState me = PetActions.Me;
            if (me != null && OrinGuidance.ShouldOffer(me.Cards)) { OfferGuidance(me); return; } // 2 duels not won in a row: his offer comes first, then the same duel question
            AskDuelQuestion();
        }

        static void AskDuelQuestion()
        {
            CardConfirmBox.Ask("Ready for your first duel?\nI lend you 3 cards to fill your deck of 8.", "Duel", () => DuelClient.PlayNpc(true), "Later", CardWindow.Close);
        }

        /// <summary>Orin's Guidance (OrinGuidance): accept = the Hint shows by itself on every free turn (stored with the character), try alone = he asks again only after 2 more duels not won. Both go on to the duel question.</summary>
        static void OfferGuidance(PlayerState me)
        {
            CardConfirmBox.Ask("You have grasped how to capture cards, but a few weak sides are still easy to hit back.\nShall I show you how to guard them?", "Accept guidance",
                () => { me.Cards.Help |= CardCollection.HelpGuidance; PetActions.Send(InvOp.CardHelp, (byte)CardCollection.HelpGuidance); AskDuelQuestion(); },
                "Try alone", () => { OrinGuidance.Decline(me.Cards); AskDuelQuestion(); });
        }

        /// <summary>After the five pages (quest done): a practice duel with Orin (nothing won or lost, works offline), or just the Card window.</summary>
        public static void AskPractice()
        {
            CardWindow.Open(CardWindow.Page.MyCards, false); // no first-time hint over Orin's question
            CardConfirmBox.Ask("Want a practice duel with me?\nNothing is won or lost.", "Practice Duel", () => DuelClient.PlayNpc(false), "Open Cards");
        }
    }
}
