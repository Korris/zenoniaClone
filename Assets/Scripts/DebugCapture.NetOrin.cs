using System;
using System.Collections;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --net-bot orin [--orin-drop] [--orin-forfeit] [--orin-skip] [--orin-peek]: the end-to-end script of "The First Card Duel" (Taurus V4) on an ONLINE character that already holds Orin's 5 starter cards (Tools/net-orin-test.ps1: real
    /// accounts + API + zone). Everything goes through the calls the buttons make (QuestTracker.Accept / TurnIn, DuelClient.PlayNpc / Place / Forfeit / SkipScript). It walks to Orin, then follows the quest state it finds: Available =
    /// accept and ask for the duel AT ONCE (no wait for the answer: proves the two packets keep their order); Active = ask for the duel; ReadyToTurnIn = pick a card (a bad index first: refused) and hand in; Done = a practice duel.
    /// A duel that is NOT won (loss, draw, give-up) must leave the quest Active and the character unchanged: the bot then plays again (up to 8 times, the free replay). The scripted duel sends one wrong move first (it must be refused),
    /// then the lesson moves, then free play with the greedy pick. --orin-drop stops after the first lesson move (the script kills the process), --orin-forfeit gives up at once (first attempt only), --orin-skip skips the script,
    /// --orin-peek only logs. Every fact is an "orin-e2e:" line (quest state, gold, the cards); the script compares them with what the API stores.
    /// </summary>
    public partial class DebugCapture
    {
        static void Orin(string line) { Debug.Log("[ZenoniaProto] orin-e2e: " + line); }

        bool orinWalk; Vector2 orinGoal;

        static QuestState OrinState() { QuestLog l = QuestTracker.Log; return l == null ? QuestState.Available : l.StateOf(CardDuelQuest.First); }

        static string OrinLine()
        {
            PlayerState me = PetActions.Me;
            CardCollection c = me.Cards;
            return "quest=" + OrinState() + " gold=" + me.Gold + " boar=" + c.Count("tauBrambleBoar") + " rootling=" + c.Count("tauRootling") + " sporeling=" + c.Count("tauAmberSporeling") + " slime=" + c.Count("tauCloverSlime") + " cards=" + CardTotal() + " help=" + c.Help;
        }

        void StartOrinBot() { StartCoroutine(OrinScript()); }

        /// <summary>--orin-shots (a real window, not -nographics): a PNG of the screen into --shot-path at the milestones.</summary>
        IEnumerator OrinShot(string name, float wait = 1.5f)
        {
            if (!NetLaunchArgs.HasFlag("--orin-shots") || !NetMode.RendersFx) yield break;
            yield return new WaitForSeconds(wait);
            yield return new WaitForEndOfFrame();
            Save(System.IO.Path.Combine(shotPath, name + ".png"));
        }

        IEnumerator OrinUntil(Func<bool> done, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - start < timeout) yield return null;
        }

        IEnumerator OrinWalkToOrin()
        {
            Vector2? orin = null;
            foreach (MapNpc n in MapRegistry.Current.Npcs) if (n.Id == CardDuelQuest.GiverId) orin = FrostHollowMap.TileFeet(n.Tx, n.Ty, 2f);
            if (!orin.HasValue) { Orin("FAIL no Orin on map " + MapRegistry.Current.Id); yield break; }
            HeroController hero = GameSession.Hero;
            InputAdapter.BotHeld = k =>
            {
                if (!orinWalk || hero == null || hero.IsDead) return false;
                Vector2 move = StepToward(hero.Position, orinGoal);
                if (move == Vector2.zero) { Vector2 d = orinGoal - hero.Position; move = new Vector2(Mathf.Abs(d.x) > 1f ? Mathf.Sign(d.x) : 0f, Mathf.Abs(d.y) > 1f ? Mathf.Sign(d.y) : 0f); }
                return (k == GameKey.Left && move.x < 0f) || (k == GameKey.Right && move.x > 0f) || (k == GameKey.Down && move.y < 0f) || (k == GameKey.Up && move.y > 0f);
            };
            orinGoal = orin.Value; orinWalk = true;
            yield return OrinUntil(() => Vector2.Distance(hero.Position, orin.Value) <= 36f, 60f); // the server lets a quest talk reach 44 units
            orinWalk = false;
            Orin((Vector2.Distance(hero.Position, orin.Value) <= 42f ? "OK " : "FAIL ") + "stands " + Vector2.Distance(hero.Position, orin.Value).ToString("F0") + " units from Orin on " + MapRegistry.Current.Id);
        }

        IEnumerator OrinScript()
        {
            string[] a = Environment.GetCommandLineArgs();
            yield return OrinUntil(() => GameSession.Hero != null && GameSession.Hero.Rpg != null && DuelClient.RankedSeason > 0, 30f); // the first pushed state (and the duel service) has arrived
            yield return new WaitForSeconds(1.5f);
            Orin("start: " + OrinLine());
            if (NetLaunchArgs.HasFlag("--orin-peek")) { Application.Quit(); yield break; }
            yield return OrinWalkToOrin();
            QuestDef q = CardDuelQuest.First;
            yield return OrinShot("online-01-near-orin", 0.5f);
            for (int attempt = 1; attempt <= 8 && (OrinState() == QuestState.Available || OrinState() == QuestState.Active); attempt++)
            {
                if (attempt > 1) DuelClient.Reset(); // the result screen of the last try is closed, as the player would
                if (OrinState() == QuestState.Available)
                {
                    QuestTracker.Accept(q);
                    DuelClient.PlayNpc(true); // straight away: the accept has to be handled first (same reliable-ordered channel)
                    yield return OrinUntil(() => DuelClient.View != null && DuelClient.View.Npc, 6f);
                    Orin((DuelClient.View != null && DuelClient.View.Npc ? "OK " : "FAIL ") + "accept + duel request sent back to back: duel " + (DuelClient.View != null ? "started" : "NOT started") + ", quest=" + OrinState());
                }
                else
                {
                    string before = OrinLine();
                    DuelClient.PlayNpc(true);
                    yield return OrinUntil(() => DuelClient.View != null && DuelClient.View.Npc, 6f);
                    Orin((DuelClient.View != null ? "OK " : "FAIL ") + "tutorial duel started from Active (attempt " + attempt + ", replay is free: " + before + ")");
                }
                if (attempt == 1) yield return OrinShot("online-02-tutorial-lesson1");
                if (DuelClient.Running) yield return OrinPlayTutorial(a, attempt == 1);
                if (OrinState() == QuestState.Active) Orin("OK attempt " + attempt + " was not won: the quest stays Active, nothing paid: " + OrinLine());
            }
            if (OrinState() == QuestState.ReadyToTurnIn) yield return OrinHandIn(q);
            if (OrinState() == QuestState.Done) yield return OrinPractice();
            Orin("end: " + OrinLine());
            Application.Quit();
        }

        IEnumerator OrinPlayTutorial(string[] a, bool first)
        {
            bool forfeit = first && Array.IndexOf(a, "--orin-forfeit") >= 0, skip = first && Array.IndexOf(a, "--orin-skip") >= 0, drop = first && Array.IndexOf(a, "--orin-drop") >= 0;
            if (forfeit) { yield return new WaitForSeconds(1f); DuelClient.Forfeit(); Orin("gave up at once"); }
            if (skip) { yield return new WaitForSeconds(1f); DuelClient.SkipScript(); Orin("script skipped"); }
            bool wrongTried = false;
            float t0 = Time.realtimeSinceStartup;
            while (DuelClient.View != null && !DuelClient.View.Finished && Time.realtimeSinceStartup - t0 < 120f)
            {
                DuelView v = DuelClient.View;
                if (v.YourTurn)
                {
                    if (v.ScriptStep > 0 && v.Moves.Count < DuelTutorialScript.Steps.Length)
                    {
                        DuelScriptStep s = DuelTutorialScript.Steps[v.Moves.Count];
                        int hand = v.Hand.IndexOf(CardTable.Index(s.CardId));
                        if (!wrongTried)
                        {
                            wrongTried = true;
                            int before = v.Moves.Count, wrongCell = (s.Cell + 5) % DuelBoard.Cells;
                            DuelClient.Place(hand, wrongCell);
                            yield return new WaitForSeconds(1.5f);
                            Orin((DuelClient.View.Moves.Count == before ? "OK " : "FAIL ") + "a move off the script (cell " + wrongCell + ") was refused, moves " + before + " -> " + DuelClient.View.Moves.Count);
                            continue;
                        }
                        if (v.Moves.Count == 2) yield return OrinShot("online-02b-tutorial-lesson2", 0.5f);
                        DuelClient.Place(hand, s.Cell);
                        Orin("lesson move " + (v.Moves.Count + 1) + ": " + s.CardId + " at cell " + s.Cell + ", lesson " + v.ScriptStep);
                        if (drop) { yield return new WaitForSeconds(2f); Orin("DROP: quest=" + OrinState() + " (the script kills this client now)"); while (true) yield return null; }
                    }
                    else DuelBotPick(v);
                }
                yield return new WaitForSeconds(0.8f);
            }
            DuelView end = DuelClient.View;
            if (end == null || !end.Finished) { Orin("FAIL the duel never finished"); yield break; }
            Orin("duel over: " + end.Result + " score " + end.Score(0) + "-" + end.Score(1) + " reason " + end.EndReason + " | at the final view: " + OrinLine() + " (a win must show ReadyToTurnIn: the quest state arrives before the view; anything else stays Active)");
            yield return OrinShot("online-03-end-banner", 3f); // the last card has to turn before the result shows
        }

        IEnumerator OrinHandIn(QuestDef q)
        {
            Orin("hand in, before: " + OrinLine());
            QuestTracker.TurnIn(q, 9); // an index past Orin's 8 cards: refused, nothing closes
            yield return new WaitForSeconds(2f);
            Orin((OrinState() == QuestState.ReadyToTurnIn ? "OK " : "FAIL ") + "a bad pick index was refused, quest=" + OrinState());
            yield return OrinShot("online-04-reward-pick", 0.1f);
            QuestTracker.TurnIn(q, (byte)System.Array.IndexOf(q.RewardPickCards, "tauBrambleBoar"));
            yield return OrinUntil(() => OrinState() == QuestState.Done, 6f);
            yield return new WaitForSeconds(1f);
            Orin((OrinState() == QuestState.Done ? "OK " : "FAIL ") + "after turn-in (picked tauBrambleBoar): " + OrinLine());
            string once = OrinLine();
            QuestTracker.TurnIn(q, (byte)System.Array.IndexOf(q.RewardPickCards, "tauRootling")); // a second hand-in with another pick must change nothing
            yield return new WaitForSeconds(2f);
            Orin((OrinLine() == once ? "OK " : "FAIL ") + "second turn-in changed nothing: " + OrinLine());
        }

        IEnumerator OrinPractice()
        {
            DuelClient.Reset();
            DuelClient.PlayNpc(true); // the tutorial again: the quest is Done, the server must refuse
            yield return new WaitForSeconds(2.5f);
            Orin((DuelClient.View == null ? "OK " : "FAIL ") + "a second tutorial duel is refused");
            string before = OrinLine();
            DuelClient.PlayNpc(false);
            yield return OrinUntil(() => DuelClient.View != null && DuelClient.View.Npc, 6f);
            Orin((DuelClient.View != null && DuelClient.View.Npc ? "OK " : "FAIL ") + "practice duel started, scripted step " + (DuelClient.View != null ? DuelClient.View.ScriptStep : -1));
            if (DuelClient.View == null) yield break;
            yield return new WaitForSeconds(1f);
            float t0 = Time.realtimeSinceStartup;
            int placed = 0;
            while (DuelClient.View != null && !DuelClient.View.Finished && placed < 3 && Time.realtimeSinceStartup - t0 < 30f)
            {
                if (DuelClient.View.YourTurn && DuelBotPick(DuelClient.View)) placed++;
                yield return new WaitForSeconds(1.2f);
            }
            DuelClient.Forfeit();
            yield return new WaitForSeconds(2f);
            Orin((OrinLine() == before ? "OK " : "FAIL ") + "practice changed nothing: " + OrinLine());
        }
    }
}
