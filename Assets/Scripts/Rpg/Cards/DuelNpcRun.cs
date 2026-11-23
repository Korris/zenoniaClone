using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// A practice duel bound to the character playing it: the deck check at the start (full 8 cards, copies owned, boss / SR+ limits; Lock Card does not matter for the deck), the session, and the one-time payment of the result.
    /// Used by the zone server (DuelService, per connected player) and the offline host (LocalDuelHost), so both pay and refuse identically. A duel that did not finish normally is never paid.
    /// </summary>
    public sealed class DuelNpcRun
    {
        public readonly DuelNpcSession Session;
        readonly PlayerState state;
        public DuelReward Reward { get; private set; }
        bool settled;

        DuelNpcRun(DuelNpcSession session, PlayerState p) { Session = session; state = p; }

        /// <summary>Starts a duel against NPC `npc` with the character's SAVED deck. Null when the deck breaks a rule (the reason in `error`) or the NPC does not exist (error None).</summary>
        public static DuelNpcRun Start(PlayerState p, int npc, ulong seed, long nowMs, out DeckError error)
        {
            error = DeckRules.Validate(p.Cards.Deck, p.Cards, true);
            if (error != DeckError.None) return null;
            DuelNpcSession s = DuelNpcSession.Start(seed, p.Cards.Deck, npc, nowMs);
            return s == null ? null : new DuelNpcRun(s, p);
        }

        public bool Finished { get { return Session.Finished; } }

        /// <summary>Pays the result once the match is over (call after anything that may have ended it). True exactly once, when it paid or settled a no-pay result.</summary>
        public bool Settle(int dayKey)
        {
            if (settled) return false;
            DuelResult? r = Session.TakeResult();
            if (r == null) return false;
            settled = true;
            Reward = DuelRewards.ApplyNpcResult(state, Session.NpcIndex, r.Value, dayKey);
            return true;
        }

        public DuelView View(long nowMs) { return Session.View(nowMs, Reward); }

        /// <summary>WantedBoss day key for "now" (UTC ms): the daily NPC task restarts with it.</summary>
        public static int DayKeyNow() { return WantedBoss.DayKey(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); }
    }
}
