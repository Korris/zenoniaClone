using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// The Card Wager calls of the write queue (phase 06 v2). Escrow needs both characters idle and online (it takes the whole staked deck out of both, rev + 1 each); settle gives both decks back (the winner plus the picked card), and a side may no longer be here,
    /// in which case the API writes the cards straight to the character. A wager call does not sit in a character's job line: it goes out when every character it touches is idle (so the rev it carries is the
    /// current one) and marks them busy until the answer, like any other write. Both are retried under the same matchId, which the API applies at most once:
    /// escrow gives up after EscrowGiveUpMs (the owner then settles it as a draw, because the card may be gone), settle keeps trying (receiver_online = the winner's session is still alive somewhere: wait for it).
    /// </summary>
    public sealed partial class CharacterSaveQueue
    {
        public const int SettleMaxAttempts = 40;
        public const long EscrowGiveUpMs = 25000;

        sealed class WagerJob
        {
            public bool Settle;
            public string MatchId, Result, Pick, IdA, IdB;
            public SavedCharacter A, B, SentA, SentB;
            public Action<ZoneApiReply> Done;
            public int Attempts;
            public long NextAttemptMs, StartedMs;
            public bool InFlight, Unknown;
        }

        readonly List<WagerJob> wagerJobs = new List<WagerJob>();

        public int WagerJobCount { get { return wagerJobs.Count; } }

        /// <summary>Takes the whole staked deck (state.cardDeck) out of each character at the API. `done` gets the definite answer (Ok: both revs moved on, apply the removal now); a refusal changed nothing.</summary>
        public void EscrowWager(string matchId, SavedCharacter a, SavedCharacter b, Action<ZoneApiReply> done)
        {
            wagerJobs.Add(new WagerJob { MatchId = matchId, A = a, B = b, Done = done, StartedMs = now });
        }

        /// <summary>
        /// Settles a held match: `result` "a" / "b" / "draw", `pick` = the card id the winner takes (null on a draw); `receiverA` / `receiverB` = the character ids of both sides (each gets its own deck back; null = that side is not part of it).
        /// Receivers still online here are sent with their state. `done` gets the definite answer, Credits marked Apply = add these cards to your copy of the character.
        /// </summary>
        public void SettleWager(string matchId, string result, string pick, string receiverA, string receiverB, Action<ZoneApiReply> done)
        {
            wagerJobs.Add(new WagerJob { Settle = true, MatchId = matchId, Result = result, Pick = pick, IdA = receiverA, IdB = receiverB, Done = done, StartedMs = now });
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

        SavedCharacter Online(string id)
        {
            if (id == null) return null;
            foreach (SavedCharacter c in characters) if (c.Id == id && !c.Leaving) return c;
            return null;
        }

        bool Here(SavedCharacter c) { return c != null && !c.Leaving && characters.Contains(c); }

        static WagerSide Side(SavedCharacter c) { return new WagerSide { CharacterId = c.Id, SessionId = c.SessionId, Rev = c.Rev, StateJson = c.Capture() }; }

        void EndWager(WagerJob j, ZoneApiReply r)
        {
            wagerJobs.Remove(j);
            j.Done(r);
        }

        static ZoneApiReply Failure(WagerJob j, int status, string code)
        {
            return new ZoneApiReply { Status = status, Code = code, Message = code, Rev = -1, RevB = -1, CurrentRev = -1, MayHaveLanded = j.Unknown };
        }

        void SendEscrow(WagerJob j)
        {
            if (!Here(j.A) || !Here(j.B)) { EndWager(j, Failure(j, 409, "not_online")); return; } // a player left: nothing to start
            if (j.Attempts >= 6 || now - j.StartedMs > EscrowGiveUpMs) { EndWager(j, Failure(j, 0, "timeout")); return; }
            if (j.A.Busy || j.B.Busy) { j.NextAttemptMs = now + 50; return; }
            j.A.Busy = j.B.Busy = true;
            j.InFlight = true;
            api.EscrowWager(j.MatchId, Side(j.A), Side(j.B), r => OnEscrowReply(j, r));
        }

        void OnEscrowReply(WagerJob j, ZoneApiReply r)
        {
            j.InFlight = false;
            j.A.Busy = j.B.Busy = false;
            if (r.Transient) { RetryWager(j, r, j.A, j.B); return; }
            ApiHealthy = true;
            j.A.Uncertain = j.B.Uncertain = false;
            if (r.Ok) { j.A.Rev = r.Rev; j.B.Rev = r.RevB; Add("wagersEscrowed"); }
            else Add("wagersRefused");
            EndWager(j, r);
        }

        void SendSettle(WagerJob j)
        {
            if (j.Attempts >= SettleMaxAttempts) { EndWager(j, Failure(j, 0, "gave_up")); return; }
            SavedCharacter ra = Online(j.IdA), rb = Online(j.IdB);
            if ((ra != null && ra.Busy) || (rb != null && rb.Busy)) { j.NextAttemptMs = now + 50; return; }
            j.SentA = ra; j.SentB = rb;
            if (ra != null) ra.Busy = true;
            if (rb != null) rb.Busy = true;
            j.InFlight = true;
            api.SettleWager(j.MatchId, j.Result, j.Pick, ra != null ? Side(ra) : (WagerSide?)null, rb != null ? Side(rb) : (WagerSide?)null, r => OnSettleReply(j, r));
        }

        void OnSettleReply(WagerJob j, ZoneApiReply r)
        {
            j.InFlight = false;
            if (j.SentA != null) j.SentA.Busy = false;
            if (j.SentB != null) j.SentB.Busy = false;
            bool wait = r.Code == "receiver_online" || r.Code == "rev_conflict" || r.Code == "session_mismatch";
            if (r.Transient || wait)
            {
                if (r.Code == "rev_conflict") { if (j.SentA != null) AdoptOwnWrite(j.SentA, r); if (j.SentB != null) AdoptOwnWrite(j.SentB, r); }
                RetryWager(j, r, j.SentA, j.SentB);
                return;
            }
            ApiHealthy = true;
            if (r.Ok)
            {
                if (j.SentA != null) j.SentA.Uncertain = false;
                if (j.SentB != null) j.SentB.Uncertain = false;
                MarkCredits(r);
                Add("wagersSettled");
            }
            else Add("wagersSettleRefused");
            EndWager(j, r);
        }

        // a payout is applied by the owner only when this zone's rev was behind the API's (a replayed answer for a payout it already applied must not add the card twice)
        void MarkCredits(ZoneApiReply r)
        {
            if (r.Credits == null) return;
            for (int i = 0; i < r.Credits.Length; i++)
            {
                WagerCredit c = r.Credits[i];
                SavedCharacter sc = null;
                foreach (SavedCharacter x in characters) if (x.Id == c.CharacterId) { sc = x; break; }
                if (sc != null && c.Rev > sc.Rev) { sc.Rev = c.Rev; c.Apply = true; }
                r.Credits[i] = c;
            }
        }

        void RetryWager(WagerJob j, ZoneApiReply r, SavedCharacter x, SavedCharacter y)
        {
            if (r.Status == 0 || r.Status >= 502) ApiHealthy = false;
            if (r.OutcomeUnknown) { j.Unknown = true; if (x != null) x.Uncertain = true; if (y != null) y.Uncertain = true; }
            j.Attempts++;
            j.NextAttemptMs = now + Math.Min(BackoffMaxMs, BackoffBaseMs << Math.Min(j.Attempts - 1, 10));
            Add("apiRetries");
        }
    }
}
