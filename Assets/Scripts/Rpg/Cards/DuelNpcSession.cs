using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    public enum DuelNpcMode { Tutorial = 0, Practice = 1 }

    /// <summary>
    /// One duel against Card Master Orin: the player is side 0, Orin side 1. Shared by the zone server (DuelService.Npc) and the offline host (LocalDuelHost), so both play the same rules and pace.
    /// Tutorial = the scripted first duel with the borrowed deck (DuelTutorialScript: fixed order, player first, the first 5 turns follow the script). Practice = a normal shuffled duel, the player's saved deck if
    /// it is valid, else the borrowed one. Never a clock, never stakes, never a reward: the owner only reads TakeResult once (Tutorial -> the quest). Orin thinks 0.6 - 1.2 s so the reply does not feel instant.
    /// Time comes only from the `nowMs` given.
    /// </summary>
    public sealed class DuelNpcSession
    {
        public const int Player = 0, Orin = 1, ThinkMinMs = 600, ThinkSpanMs = 601;
        /// <summary>A duel with no player action for this long is dropped by its owner (Idle): there is no turn clock, but no soft-lock either.</summary>
        public const long IdleLimitMs = 10L * 60L * 1000L;

        public readonly DuelMatch Match;
        public readonly DuelNpcMode Mode;
        readonly DuelRng aiRng;
        int pos; // next position in DuelTutorialScript.Steps; Steps.Length = script over or skipped
        long orinMoveAtMs, lastActionMs;
        bool resultTaken;

        public bool Finished { get { return Match.Finished; } }
        /// <summary>The lesson (1 ..) the player is on, 0 = free play.</summary>
        public int ScriptStep { get { return DuelTutorialScript.LessonAt(pos); } }
        /// <summary>The scripted placement due next (either side), null when the script is over.</summary>
        public DuelScriptStep CurrentStep { get { return pos < DuelTutorialScript.Steps.Length ? DuelTutorialScript.Steps[pos] : null; } }
        /// <summary>True when the player gave up (Forfeit) rather than playing it out.</summary>
        public bool PlayerGaveUp { get { return Match.Forfeited == Player; } }

        DuelNpcSession(DuelMatch match, DuelNpcMode mode, ulong seed, bool scripted)
        {
            Match = match; Mode = mode; aiRng = new DuelRng(seed ^ 0xA5A5A5A5DEADBEEFUL);
            pos = scripted ? 0 : DuelTutorialScript.Steps.Length;
        }

        /// <summary>The scripted first duel (fixed decks, the player moves first).</summary>
        public static DuelNpcSession Tutorial(long nowMs)
        {
            var m = DuelMatch.CreateFixed(DuelTutorialScript.PlayerDeck, DuelTutorialScript.OrinDeck, Player, nowMs);
            var s = new DuelNpcSession(m, DuelNpcMode.Tutorial, 1UL, true);
            s.lastActionMs = nowMs;
            return s;
        }

        /// <summary>A free practice duel. `playerDeck` null / not a valid full deck = the borrowed one (use PracticeDeck to pick).</summary>
        public static DuelNpcSession Practice(ulong seed, IList<string> playerDeck, long nowMs)
        {
            IList<string> deck = playerDeck != null && DeckRules.Validate(playerDeck, null, true) == DeckError.None ? playerDeck : DuelTutorialScript.PlayerDeck;
            var s = new DuelNpcSession(DuelMatch.Create(seed, deck, DuelTutorialScript.OrinDeck, nowMs, 0), DuelNpcMode.Practice, seed, false);
            s.lastActionMs = nowMs;
            s.ScheduleOrin(nowMs);
            return s;
        }

        /// <summary>The player's saved deck when it is a valid full deck of cards they own, else the borrowed deck.</summary>
        public static IList<string> PracticeDeck(CardCollection cards)
        {
            return cards != null && DeckRules.Validate(cards.Deck, cards, true) == DeckError.None ? new List<string>(cards.Deck) : new List<string>(DuelTutorialScript.PlayerDeck);
        }

        void ScheduleOrin(long nowMs)
        {
            if (!Match.Finished && Match.ToMove == Orin) orinMoveAtMs = nowMs + ThinkMinMs + aiRng.Next(ThinkSpanMs);
        }

        /// <summary>The player places a hand card. While the script runs only the step's card on the step's cell is accepted (DuelPlayError.Script); otherwise refused like DuelMatch.Place.</summary>
        public DuelPlay PlayerPlace(int handIndex, int cell, long nowMs, int expectTurn = 0, int expectCard = -1)
        {
            lastActionMs = nowMs;
            DuelScriptStep step = CurrentStep;
            if (step != null && !Match.Finished && Match.ToMove == Player)
            {
                IList<int> hand = Match.Hand(Player);
                bool ok = step.Side == Player && handIndex >= 0 && handIndex < hand.Count && hand[handIndex] == CardTable.Index(step.CardId) && cell == step.Cell;
                if (!ok) return DuelPlay.Fail(DuelPlayError.Script, "Follow the lesson: " + step.Title);
            }
            DuelPlay p = Match.Place(Player, handIndex, cell, nowMs, expectTurn, expectCard);
            if (p.Ok) { if (step != null) pos++; ScheduleOrin(nowMs); }
            return p;
        }

        /// <summary>Drops the rest of the script: Orin and the player play freely from the next turn.</summary>
        public bool SkipScript(long nowMs)
        {
            if (CurrentStep == null || Match.Finished) return false;
            lastActionMs = nowMs;
            pos = DuelTutorialScript.Steps.Length;
            ScheduleOrin(nowMs);
            return true;
        }

        /// <summary>Runs Orin's move when it is due (the scripted card, or DuelNpcAi). True when the match changed (the owner sends a new view). The player's turn never times out.</summary>
        public bool Tick(long nowMs)
        {
            if (Match.Finished || Match.ToMove != Orin || nowMs < orinMoveAtMs) return false;
            DuelScriptStep step = CurrentStep;
            int h = -1, cell = -1;
            if (step != null && step.Side == Orin)
            {
                h = Match.Hand(Orin).IndexOf(CardTable.Index(step.CardId));
                cell = step.Cell;
            }
            if (h < 0 && !DuelNpcAi.Best(Match.Board, Match.Hand(Orin), Orin, aiRng, false, out h, out cell)) { Match.Forfeit(Orin); return true; } // cannot happen in a running match; never hang
            DuelPlay p = Match.Place(Orin, h, cell, nowMs);
            if (!p.Ok) { Match.Forfeit(Orin); return true; }
            if (step != null && step.Side == Orin) pos++;
            ScheduleOrin(nowMs);
            return true;
        }

        /// <summary>No player action for IdleLimitMs: the owner drops the duel (Forfeit, the quest does NOT move).</summary>
        public bool Idle(long nowMs) { return !Match.Finished && nowMs - lastActionMs > IdleLimitMs; }

        /// <summary>The player gives up: Orin wins. For a tutorial the quest still counts (the owner decides, TakeResult).</summary>
        public void Forfeit() { Match.Forfeit(Player); }

        /// <summary>The finished match's result from the player's side, handed out once (null while it runs or after it was taken).</summary>
        public DuelResult? TakeResult()
        {
            if (!Match.Finished || resultTaken) return null;
            resultTaken = true;
            return View(0).Result;
        }

        public DuelView View(long nowMs)
        {
            DuelView v = DuelView.From(Match, Player, DuelTutorialScript.OrinName, nowMs);
            v.Npc = true;
            v.ScriptStep = Match.Finished ? 0 : ScriptStep;
            return v;
        }
    }
}
