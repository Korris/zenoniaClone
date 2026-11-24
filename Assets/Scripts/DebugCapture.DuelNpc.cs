using System;
using System.Collections;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the duel with Card Master Orin and the first-PvP question (docs/reference/card-gem-ui/card-tutorial-shots, docs/reference/world-design/greybox-shots/phase03-*). Offline, in Willowbrook next to Orin
    /// (--map tauWillowbrookVillage --tp 36,19), after Rowan's trial (the quest chain of Taurus V4 is walked through the real QuestLog / PlayerState calls):
    ///   --quest-state deck-offer|deck-active|deck-ready|available|active|ready|done   deck-*: Orin's First Deck; the rest: "The First Card Duel" (available = deck quest done and paid, ready = duel won, done = reward picked)
    ///   --quest-offer / --quest-offer-deck           the quest window of the duel / first-deck offer;   --reward-pick   the "Choose Your Reward Card" screen
    ///   --taurus-cards N                             owns the first N Taurus cards (Album shot);   --orin-ask duel|practice   Orin's question boxes
    ///   --duel-npc tutorial:N | free | hint | end | end-talk   starts the duel through DuelClient (tutorial when the quest is not done, else a practice duel) and plays on: tutorial:N = the script up to lesson N,
    ///                                                free = the script skipped, hint = free + the Hint button, end = skipped and played to the result (DuelNpcAi, careful: the Hint's move)
    ///   --orin-losses N / --guidance                  the loss streak (2 = --orin-ask duel offers Guidance first) / Guidance accepted (auto Hint)
    ///   --duel-risk                                  the first-PvP "cards at stake" question with the deck's 8 cards
    /// </summary>
    public partial class DebugCapture
    {
        static void DebugDuelNpc(string[] args, PlayerState p)
        {
            if (Array.IndexOf(args, "--quest-offer") >= 0) QuestOfferWindow.Open(CardDuelQuest.First, () => { });
            if (Array.IndexOf(args, "--quest-offer-deck") >= 0) QuestOfferWindow.Open(TaurusQuests.FirstDeck, () => { });
            if (Array.IndexOf(args, "--reward-pick") >= 0)
            {
                CardRewardPickPanel.Open(CardDuelQuest.First);
                int pickAt;
                if (int.TryParse(Arg(args, "--reward-pick"), out pickAt)) CardRewardPickPanel.DebugSelect(pickAt); // --reward-pick 2 = with card 2 selected
            }
            string taurus = Arg(args, "--taurus-cards");
            if (taurus != null && p != null) for (int i = 0; i < int.Parse(taurus); i++) p.Cards.Add(CardTable.All[CardTable.LiveStart + i].Id, 1 + i % 3);
            string losses = Arg(args, "--orin-losses");
            if (losses != null && p != null) p.Cards.OrinLosses = int.Parse(losses); // Orin's Guidance offer shows from 2
            if (Array.IndexOf(args, "--guidance") >= 0 && p != null) p.Cards.Help |= CardCollection.HelpGuidance; // guidance accepted: free turns hint by themselves
            string ask = Arg(args, "--orin-ask");
            if (ask == "duel") CardMasterTalk.AskDuel(); else if (ask == "practice") CardMasterTalk.AskPractice();
            if (Array.IndexOf(args, "--duel-risk") >= 0) CardDuelPvpRiskConfirm.Ask(() => { });
            if (Array.IndexOf(args, "--talk-orin") >= 0) TownNpc.Talk(); // what E does beside Orin, AFTER --duel-seed / --quest-state (--talk-npc and --talk run before the cards are seeded)
            string npc = Arg(args, "--duel-npc");
            if (npc != null && p != null) new GameObject("DuelNpcCapture").AddComponent<DuelNpcCapture>().Begin(npc, p);
        }

        /// <summary>Walks the character along Orin's chain to `state` with the calls the game makes (no shortcuts on the quest log), so the cards, markers and banners are the real ones.</summary>
        static void SetQuestState(PlayerState p, string state)
        {
            QuestLog log = p.Quests;
            p.Cards.Help |= CardCollection.HelpTutorial; // a character that gets this quest has seen the first-time card hint
            p.AcceptQuest(TaurusQuests.TrialId);
            for (int i = 0; i < 5; i++) log.CountKill("tauCloverSlime");
            p.TurnInQuest(TaurusQuests.TrialId);
            if (state == "deck-offer") return;
            p.AcceptQuest(TaurusQuests.FirstDeckId);
            if (state == "deck-active") return;
            for (int i = 0; i < 5; i++) log.CountKill("tauCloverSlime");
            if (state == "deck-ready") return;
            p.TurnInQuest(TaurusQuests.FirstDeckId);
            if (state == "available") return;
            p.AcceptQuest(CardDuelQuest.Id);
            if (state == "active") return;
            log.CountCardDuel();
            if (state == "done") p.TurnInQuest(CardDuelQuest.Id, 5);
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
                if (part[0] == "forfeit") { yield return new WaitForSecondsRealtime(1.5f); DuelClient.Forfeit(); } // a duel not won: the result says "Try again", the quest stays Active
                else if (part[0] == "tutorial") yield return PlayLessons(part.Length > 1 ? int.Parse(part[1]) : 1);
                else
                {
                    if (part[0] == "win-pick") yield return PlayLessons(4); else DuelClient.SkipScript(); // win-pick plays the three lessons for real (the first capture shows its two numbers)
                    if (part[0] == "hint") { yield return new WaitForSecondsRealtime(1f); CardWindow.DebugPressHint(); }
                    if (part[0] == "end" || part[0] == "end-talk" || part[0] == "win-pick") yield return PlayOut();
                    if (part[0] == "win-pick") // the won duel's result, then what the end of Orin's ReadyLines opens (CardMasterTalk): the reward screen
                    {
                        Debug.Log("[ZenoniaProto] duel-npc capture: result " + (View != null ? View.Result.ToString() : "none") + ", quest " + player.Quests.StateOf(CardDuelQuest.First));
                        yield return new WaitForSecondsRealtime(3f);
                        CardWindow.RequestClose();
                        CardRewardPickPanel.Open(CardDuelQuest.First);
                    }
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
