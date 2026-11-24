using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Talking to Card Master Orin (Oakhaven, by the south gate; TownNpc.Talk -> UiScreens.OpenForNpc). What he says follows the quest "The First Card Duel" (CardDuelQuest, offered once the character owns a card):
    /// Available = offer lines, then the quest window; Accept asks "Ready?". Active = his reminder, then the same question. ReadyToTurnIn = the PvP rules and the reward (turn-in). Done = one greeting line, then a
    /// choice: a practice duel with him, or the Card window. No card yet = the five pages explaining the Monster Card system plus "bring me your first card" (where the duel lesson comes from). The question boxes are the Card window's CardConfirmBox, so the window opens behind them.
    /// Esc on the dialogue leaves without anything else.
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
                case QuestState.Available: NpcDialogueBox.Open(q.GiverName, portrait, q.OfferLines, () => QuestOfferWindow.Open(q, () => { QuestTracker.Accept(q); AskDuel(); })); break;
                case QuestState.Active: NpcDialogueBox.Open(q.GiverName, portrait, q.ActiveLines, AskDuel); break;
                case QuestState.ReadyToTurnIn: NpcDialogueBox.Open(q.GiverName, portrait, q.ReadyLines, () => QuestTracker.TurnIn(q)); break;
                default: NpcDialogueBox.Open(who, portrait, DoneLines, AskPractice); break; // quest done: one line, then straight to the practice question (the 5 rule pages were already read)
            }
        }

        /// <summary>Done-state greeting: short, so the practice duel is one tap away.</summary>
        static readonly string[] DoneLines = { "Back for another round? My table is always open." };

        /// <summary>No card yet: the explanation pages plus the hint that a first card unlocks his duel lesson.</summary>
        static string[] WithComeBack(string[] pages)
        {
            var all = new string[pages.Length + 1];
            pages.CopyTo(all, 0);
            all[pages.Length] = "Bring me your first monster card and I will teach you to duel. I even lend you a deck for it.";
            return all;
        }

        /// <summary>"Ready for your first duel?": Duel starts the scripted duel with the borrowed deck, Later closes the window again.</summary>
        public static void AskDuel()
        {
            CardWindow.Open(CardWindow.Page.MyCards, false); // no first-time hint over Orin's question
            CardConfirmBox.Ask("Ready for your first duel?\nI lend you a deck of 8 common cards.", "Duel", () => DuelClient.PlayNpc(true), "Later", CardWindow.Close);
        }

        /// <summary>After the five pages (quest done): a practice duel with Orin (nothing won or lost, works offline), or just the Card window.</summary>
        public static void AskPractice()
        {
            CardWindow.Open(CardWindow.Page.MyCards, false); // no first-time hint over Orin's question
            CardConfirmBox.Ask("Want a practice duel with me?\nNothing is won or lost.", "Practice Duel", () => DuelClient.PlayNpc(false), "Open Cards");
        }
    }
}
