using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// One practice duel: the player (side 0) against a DuelNpcTable opponent (side 1). Shared by the zone server (DuelService) and the offline host (LocalDuelHost), so online and offline play
    /// the same rules and the same pace. It owns the NPC's thinking time (0.6 - 1.2 s so the reply does not feel instant) and the timeout of the player's turn, driven only by the `nowMs` it is given.
    /// Rewards are paid ONCE by the owner when Finished turns true (TakeResult).
    /// </summary>
    public sealed class DuelNpcSession
    {
        public const int Player = 0, Npc = 1, ThinkMinMs = 600, ThinkSpanMs = 601;

        public readonly DuelMatch Match;
        public readonly int NpcIndex;
        public readonly DuelNpcDef Def;
        readonly DuelRng aiRng;
        long npcMoveAtMs;
        bool resultTaken;

        public bool Finished { get { return Match.Finished; } }

        DuelNpcSession(DuelMatch match, int npc, ulong seed)
        {
            Match = match; NpcIndex = npc; Def = DuelNpcTable.Get(npc); aiRng = new DuelRng(seed ^ 0xA5A5A5A5DEADBEEFUL);
        }

        /// <summary>Starts a duel. `playerDeck` must already be a valid 8-card deck (DeckRules). Null when the NPC does not exist.</summary>
        public static DuelNpcSession Start(ulong seed, IList<string> playerDeck, int npc, long nowMs)
        {
            DuelNpcDef def = DuelNpcTable.Get(npc);
            if (def == null) return null;
            var s = new DuelNpcSession(DuelMatch.Create(seed, playerDeck, def.Deck, nowMs), npc, seed);
            s.ScheduleNpc(nowMs);
            return s;
        }

        void ScheduleNpc(long nowMs)
        {
            if (!Match.Finished && Match.ToMove == Npc) npcMoveAtMs = nowMs + ThinkMinMs + aiRng.Next(ThinkSpanMs);
        }

        /// <summary>The player places a hand card. Refused (nothing changes) like DuelMatch.Place.</summary>
        public DuelPlay PlayerPlace(int handIndex, int cell, long nowMs)
        {
            DuelPlay p = Match.Place(Player, handIndex, cell, nowMs);
            if (p.Ok) ScheduleNpc(nowMs);
            return p;
        }

        /// <summary>Runs whatever is due: the NPC's move, or the player's timeout placement. True when the match changed (the owner sends a new view).</summary>
        public bool Tick(long nowMs)
        {
            if (Match.Finished) return false;
            if (Match.ToMove == Npc)
            {
                if (nowMs < npcMoveAtMs) return false;
                int h, cell;
                if (!DuelNpcAi.Pick(Match, Npc, Def.Level, aiRng, out h, out cell)) { Match.Forfeit(Npc); return true; } // cannot happen in a running match; never hang
                DuelPlay p = Match.Place(Npc, h, cell, nowMs);
                if (!p.Ok) { Match.Forfeit(Npc); return true; }
                return true;
            }
            DuelPlay t = Match.Tick(nowMs);
            if (t.Ok) ScheduleNpc(nowMs);
            return t.Ok;
        }

        /// <summary>The player gives up, or was dropped (left the game, died): the NPC wins, nothing is paid.</summary>
        public void Forfeit() { Match.Forfeit(Player); }

        /// <summary>The finished match's result from the player's side, handed out once (null while it runs or after it was taken) so a reward cannot be paid twice.</summary>
        public DuelResult? TakeResult()
        {
            if (!Match.Finished || resultTaken) return null;
            resultTaken = true;
            return DuelView.From(Match, Player, NpcIndex, Def.Name, 0).Result;
        }

        public DuelView View(long nowMs, DuelReward reward = default(DuelReward))
        {
            return DuelView.From(Match, Player, NpcIndex, Def.Name, nowMs, reward.FirstWinGold, reward.DailyGold);
        }
    }
}
