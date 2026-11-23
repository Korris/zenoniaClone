using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// The Card Wager calls of the write queue (phase 06 v2). Escrow needs both characters idle and online (it takes the whole staked deck out of both, rev + 1 each); settle gives both decks back (the winner plus the picked card),
    /// and a side may no longer be here, in which case the API writes the cards straight to the character. Both are retried under the same matchId, which the API applies at most once.
    ///
    /// The rule that keeps a card from being made or lost: while a wager call may have been applied by the API and the zone has no definite answer, NO other write of those characters goes out (SavedCharacter.WagerHold,
    /// checked next to Busy in Tick). A save sent in between would overwrite the API's copy with a state that has not seen the escrow / payout, and the replay would then add the cards a second time.
    /// And a call whose outcome is unknown is NEVER given up (only one that certainly changed nothing is): it is replayed under the same matchId until the API says yes or no.
    /// The caller applies the matching change to its own copy exactly once, from `done`, which runs once with the definite answer, before the queue sends anything else for those characters.
    /// </summary>
    public sealed partial class CharacterSaveQueue
    {
        public const int SettleMaxAttempts = 40;
        public const int EscrowMaxAttempts = 6;
        public const long EscrowGiveUpMs = 25000;

        sealed class WagerJob
        {
            public bool Settle;
            public string MatchId, Result, Pick;
            public readonly string[] Id = new string[2];                    // settle: the character of side a / b (null = not part of it)
            public readonly SavedCharacter[] Ch = new SavedCharacter[2];    // escrow: the two characters
            public readonly SavedCharacter[] Sent = new SavedCharacter[2];  // settle: whose state went with the attempt in flight
            public readonly SavedCharacter[] Pin = new SavedCharacter[2];   // settle: once an attempt had an unknown outcome, the ONLY characters whose state may still go (the API may have written theirs)
            public bool Pinned;
            public readonly List<SavedCharacter> Held = new List<SavedCharacter>();
            public Action<ZoneApiReply> Done;
            public int Attempts;
            public long NextAttemptMs, StartedMs;
            public bool InFlight, Unknown;
        }

        readonly List<WagerJob> wagerJobs = new List<WagerJob>();

        public int WagerJobCount { get { return wagerJobs.Count; } }

        /// <summary>Takes the whole staked deck (state.cardDeck) out of each character at the API. `done` gets the definite answer (Ok: both revs moved on, apply the removal to your copy now); a refusal changed nothing.</summary>
        public void EscrowWager(string matchId, SavedCharacter a, SavedCharacter b, Action<ZoneApiReply> done)
        {
            var j = new WagerJob { MatchId = matchId, Done = done, StartedMs = now };
            j.Ch[0] = a; j.Ch[1] = b;
            wagerJobs.Add(j);
        }

        /// <summary>
        /// Settles a held match: `result` "a" / "b" / "draw", `pick` = the card id the winner takes (null on a draw); `receiverA` / `receiverB` = the character ids of both sides (each gets its own deck back; null = that side is not part of it).
        /// A receiver still here is sent with its state, also one that is leaving (its final write waits for this). `done` gets the definite answer; a credit marked Apply = add these cards to your copy of that character now, once.
        /// </summary>
        public void SettleWager(string matchId, string result, string pick, string receiverA, string receiverB, Action<ZoneApiReply> done)
        {
            var j = new WagerJob { Settle = true, MatchId = matchId, Result = result, Pick = pick, Done = done, StartedMs = now };
            j.Id[0] = receiverA; j.Id[1] = receiverB;
            wagerJobs.Add(j);
        }

        void TickWagers()
        {
            for (int i = 0; i < wagerJobs.Count; i++)
            {
                WagerJob j = wagerJobs[i];
                if (j.InFlight || now < j.NextAttemptMs) continue;
                if (j.Settle) SendSettle(j); else SendEscrow(j);
                if (i < wagerJobs.Count && wagerJobs[i] != j) i--; // the job ended and left the list
            }
        }

        bool Here(SavedCharacter c) { return c != null && !c.Leaving && characters.Contains(c); }

        static WagerSide Side(SavedCharacter c) { return new WagerSide { CharacterId = c.Id, SessionId = c.SessionId, Rev = c.Rev, StateJson = c.Capture() }; }

        void Hold(WagerJob j, SavedCharacter c)
        {
            if (c == null || j.Held.Contains(c)) return;
            j.Held.Add(c);
            c.WagerHold++;
        }

        void ReleaseHolds(WagerJob j)
        {
            foreach (SavedCharacter c in j.Held) c.WagerHold--;
            j.Held.Clear();
        }

        // an attempt that certainly changed nothing frees the characters for their own writes until the next attempt
        void EndWager(WagerJob j, ZoneApiReply r)
        {
            wagerJobs.Remove(j);
            try { j.Done(r); }
            finally { ReleaseHolds(j); }
        }

        static ZoneApiReply Failure(WagerJob j, int status, string code)
        {
            return new ZoneApiReply { Status = status, Code = code, Message = code, Rev = -1, RevB = -1, CurrentRev = -1, MayHaveLanded = j.Unknown };
        }

        // 403 wager_disabled is the API's answer (the switch is off), not a bad key: it is final
        static bool Retryable(ZoneApiReply r) { return r.Transient && r.Code != "wager_disabled"; }

        void RetryWager(WagerJob j, ZoneApiReply r)
        {
            if (r.Status == 0 || r.Status >= 502) ApiHealthy = false;
            if (r.OutcomeUnknown) j.Unknown = true;
            j.Attempts++;
            j.NextAttemptMs = now + Math.Min(BackoffMaxMs, BackoffBaseMs << Math.Min(j.Attempts - 1, 10));
            if (!j.Unknown) ReleaseHolds(j); // nothing was written: the characters may save meanwhile
            Add("apiRetries");
        }

        // ---- escrow ----

        void SendEscrow(WagerJob j)
        {
            SavedCharacter a = j.Ch[0], b = j.Ch[1];
            if (!j.Unknown) // once an attempt may have landed the job goes on whatever happens to the players: the API must be asked until it says
            {
                if (!Here(a) || !Here(b)) { EndWager(j, Failure(j, 409, "not_online")); return; } // a player left: nothing to start
                if (j.Attempts >= EscrowMaxAttempts || now - j.StartedMs > EscrowGiveUpMs) { EndWager(j, Failure(j, 0, "timeout")); return; }
            }
            if (a.Busy || b.Busy || a.Uncertain || b.Uncertain) { j.NextAttemptMs = now + 50; return; } // an unresolved save of either one first: its rev may be behind
            a.Busy = b.Busy = true;
            Hold(j, a); Hold(j, b);
            j.InFlight = true;
            api.EscrowWager(j.MatchId, Side(a), Side(b), r => OnEscrowReply(j, r));
        }

        void OnEscrowReply(WagerJob j, ZoneApiReply r)
        {
            j.InFlight = false;
            j.Ch[0].Busy = j.Ch[1].Busy = false;
            if (Retryable(r)) { RetryWager(j, r); return; }
            ApiHealthy = true;
            if (r.Ok) { j.Ch[0].Rev = r.Rev; j.Ch[1].Rev = r.RevB; Add("wagersEscrowed"); }
            else
            {
                if (r.Code == "wager_not_held" && r.Rev >= 0 && r.RevB >= 0) { j.Ch[0].Rev = r.Rev; j.Ch[1].Rev = r.RevB; } // the replay found the match already over: the API's revs are the current ones
                Add("wagersRefused");
            }
            EndWager(j, r);
        }

        // ---- settle ----

        // who gets its state with this attempt: a character here (a leaving one too: its final write comes after), but after an attempt of unknown outcome only the ones that attempt could have written
        SavedCharacter SideCharacter(WagerJob j, int side)
        {
            if (j.Id[side] == null) return null;
            if (j.Pinned) return j.Pin[side] != null && characters.Contains(j.Pin[side]) ? j.Pin[side] : null;
            foreach (SavedCharacter c in characters) if (c.Id == j.Id[side]) return c;
            return null;
        }

        void SendSettle(WagerJob j)
        {
            if (!j.Unknown && j.Attempts >= SettleMaxAttempts) { EndWager(j, Failure(j, 0, "gave_up")); return; }
            SavedCharacter ra = SideCharacter(j, 0), rb = SideCharacter(j, 1);
            if ((ra != null && (ra.Busy || ra.Uncertain)) || (rb != null && (rb.Busy || rb.Uncertain))) { j.NextAttemptMs = now + 50; return; }
            j.Sent[0] = ra; j.Sent[1] = rb;
            if (ra != null) { ra.Busy = true; Hold(j, ra); }
            if (rb != null) { rb.Busy = true; Hold(j, rb); }
            j.InFlight = true;
            api.SettleWager(j.MatchId, j.Result, j.Pick, ra != null ? Side(ra) : (WagerSide?)null, rb != null ? Side(rb) : (WagerSide?)null, r => OnSettleReply(j, r));
        }

        void OnSettleReply(WagerJob j, ZoneApiReply r)
        {
            j.InFlight = false;
            for (int s = 0; s < 2; s++) if (j.Sent[s] != null) j.Sent[s].Busy = false;
            if (r.OutcomeUnknown && !j.Pinned) { j.Pinned = true; j.Pin[0] = j.Sent[0]; j.Pin[1] = j.Sent[1]; }
            // The API does not own this character's copy the way the zone thinks (another session, or a write that is not ours): that character is not ours to write any more.
            // The match itself is still settled: the next attempt goes without its state and the API writes it directly (or waits for receiver_online).
            if (r.Code == "session_mismatch" || (r.Code == "rev_conflict" && !AdoptRevConflict(j, r)))
            {
                int bad = r.ErrorSide == "a" ? 0 : r.ErrorSide == "b" ? 1 : -1; // the API says which of the two it is (an older API does not: then nobody is dropped here, that character's own save finds out)
                SavedCharacter c = bad >= 0 ? j.Sent[bad] : null;
                if (c != null && characters.Contains(c)) Lose(c, r.Code);
            }
            if (Retryable(r) || r.Code == "receiver_online" || r.Code == "session_mismatch" || r.Code == "rev_conflict") { RetryWager(j, r); return; }
            ApiHealthy = true;
            if (r.Ok) { MarkCredits(j, r); Add("wagersSettled"); }
            else Add("wagersSettleRefused");
            EndWager(j, r);
        }

        // a rev_conflict right after a save whose answer was lost: that save landed, the zone's copy is newer. Only the one Uncertain character it can be.
        bool AdoptRevConflict(WagerJob j, ZoneApiReply r)
        {
            bool adopted = false;
            for (int s = 0; s < 2; s++) if (j.Sent[s] != null && AdoptOwnWrite(j.Sent[s], r)) adopted = true;
            return adopted;
        }

        // The zone applies a payout to its own copy only for a character it sent the state of (the API wrote that state + the cards, so the zone's copy still lacks them), and only
        // here, once, as the job ends: the rev of that character becomes the API's, whatever happened to the rev on the way (a replayed answer carries the current one).
        void MarkCredits(WagerJob j, ZoneApiReply r)
        {
            if (r.Credits == null) return;
            for (int i = 0; i < r.Credits.Length; i++)
            {
                WagerCredit c = r.Credits[i];
                SavedCharacter sc = c.Side >= 0 && c.Side < 2 ? j.Sent[c.Side] : null;
                if (sc != null && sc.Id == c.CharacterId && characters.Contains(sc))
                {
                    if (c.Rev >= 0) sc.Rev = c.Rev;
                    sc.Uncertain = false;
                    c.Apply = true;
                }
                r.Credits[i] = c;
            }
        }
    }
}
