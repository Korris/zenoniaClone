using System;
using System.Collections;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the duel with Card Master Orin and the first-PvP question (docs/reference/card-gem-ui/card-tutorial-shots). Offline, in Oakhaven next to Orin (--map oakhaven-village --tp 16,22), with cards (--duel-seed):
    ///   --quest-state available|active|ready|done   puts "The First Card Duel" in that state (through the real QuestLog calls)
    ///   --quest-offer                                the quest window of the offer; --orin-ask duel|practice   Orin's question boxes
    ///   --duel-npc tutorial:N | free | hint | end | end-talk   starts the duel through DuelClient (tutorial when the quest is not done, else a practice duel) and plays on: tutorial:N = the script up to lesson N,
    ///                                                free = the script skipped, hint = free + the Hint button, end = skipped and played to the result (DuelNpcAi, careful: the Hint's move)
    ///   --duel-risk                                  the first-PvP "cards at stake" question with the deck's 8 cards
    /// </summary>
    public partial class DebugCapture
    {
        static void DebugDuelNpc(string[] args, PlayerState p)
        {
            if (Array.IndexOf(args, "--quest-offer") >= 0) QuestOfferWindow.Open(CardDuelQuest.First, () => { });
            string ask = Arg(args, "--orin-ask");
            if (ask == "duel") CardMasterTalk.AskDuel(); else if (ask == "practice") CardMasterTalk.AskPractice();
            if (Array.IndexOf(args, "--duel-risk") >= 0) CardDuelPvpRiskConfirm.Ask(() => { });
            if (Array.IndexOf(args, "--talk-orin") >= 0) TownNpc.Talk(); // what E does beside Orin, AFTER --duel-seed / --quest-state (--talk-npc and --talk run before the cards are seeded)
            string npc = Arg(args, "--duel-npc");
            if (npc != null && p != null) new GameObject("DuelNpcCapture").AddComponent<DuelNpcCapture>().Begin(npc, p);
        }

        static void SetQuestState(PlayerState p, string state)
        {
            QuestLog log = p.Quests;
            QuestDef q = CardDuelQuest.First;
            p.Cards.Help |= CardCollection.HelpTutorial; // a character that gets this quest has seen the first-time card hint
            if (state == "available") return;
            log.Accept(q);
            if (state == "active") return;
            log.CountCardDuel();
            if (state == "done") log.Close(q);
        }

        sealed class DuelNpcCapture : MonoBehaviour
        {
            PlayerState player;

            public void Begin(string mode, PlayerState p) { player = p; StartCoroutine(Run(mode)); }

            static DuelView View { get { return DuelClient.View; } }

            IEnumerator Until(Func<bool> done, float timeout)
            {
                float end = Time.realtimeSinceStartup + timeout;
                while (!done() && Time.realtimeSinceStartup < end) yield return null;
            }

            IEnumerator Run(string mode)
            {
                yield return new WaitForSecondsRealtime(2f);
                bool tutorial = player.Quests.StateOf(CardDuelQuest.First) != QuestState.Done;
                if (tutorial) player.Quests.Accept(CardDuelQuest.First); // no-op when already taken
                DuelClient.PlayNpc(tutorial);
                yield return Until(() => View != null && View.Npc, 5f);
                string[] part = mode.Split(':');
                if (part[0] == "tutorial") yield return PlayLessons(part.Length > 1 ? int.Parse(part[1]) : 1);
                else
                {
                    DuelClient.SkipScript();
                    if (part[0] == "hint") { yield return new WaitForSecondsRealtime(1f); CardWindow.DebugPressHint(); }
                    if (part[0] == "end" || part[0] == "end-talk") yield return PlayOut();
                    if (part[0] == "end-talk") { yield return new WaitForSecondsRealtime(3f); CardDuelBattleView.TalkToOrin(); } // the result's "Talk to Orin" button
                }
            }

            // plays the player's scripted moves of lessons 1 .. lesson-1 (each followed by Orin's scripted reply), so lesson `lesson` is on screen
            IEnumerator PlayLessons(int lesson)
            {
                for (int m = 0; m < 2 * (lesson - 1); m += 2)
                {
                    int moves = m;
                    yield return Until(() => View != null && View.YourTurn && View.Moves.Count == moves, 10f);
                    yield return new WaitForSecondsRealtime(1.2f);
                    DuelScriptStep s = DuelTutorialScript.Steps[m];
                    DuelClient.Place(View.Hand.IndexOf(CardTable.Index(s.CardId)), s.Cell);
                    yield return Until(() => View != null && View.Moves.Count == moves + 2 && View.YourTurn, 10f);
                    yield return new WaitForSecondsRealtime(1.5f);
                }
            }

            IEnumerator PlayOut()
            {
                float end = Time.realtimeSinceStartup + 120f;
                while (View != null && !View.Finished && Time.realtimeSinceStartup < end)
                {
                    int h, c;
                    if (View.YourTurn && DuelNpcAi.Best(View.Board(), View.Hand, DuelView.Me, null, true, out h, out c)) { DuelClient.Place(h, c); yield return new WaitForSecondsRealtime(2.2f); }
                    else yield return null;
                }
            }
        }
    }
}
