using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    public enum LobbyPhase : byte { None = 0, Incoming = 1, Outgoing = 2, Starting = 3 }

    /// <summary>Why a lobby step was refused, or why a lobby ended (sent to the player as DuelFail.* codes by the zone).</summary>
    public enum LobbyFail : byte { None = 0, Self = 1, Busy = 2, Cooldown = 3, NoInvite = 4, NoLobby = 5, Declined = 6, Cancelled = 7, Expired = 8, Left = 9 }

    /// <summary>What one player sees of their lobby (the invite they got or sent). Stakes = cards are at stake in this duel.</summary>
    public struct LobbyView
    {
        public LobbyPhase Phase;
        public ushort Other;
        public bool Stakes;
        public int MsLeft;
    }

    /// <summary>Both players are ready: A is the inviter.</summary>
    public struct LobbyStart { public ushort A, B; public bool Stakes; }

    /// <summary>A lobby ended without a match (to = the player to tell, `Other` = the other one).</summary>
    public struct LobbyEnd { public ushort To, Other; public LobbyFail Why; }

    /// <summary>
    /// The challenge flow of the card duel between two players, no networking, no clock of its own (`nowMs` is passed in): invite (15 s, one open invite or lobby per player, 30 s before inviting the same player again)
    /// and accept / decline / cancel. Accept starts the match at once (there is no card picking before a duel any more: the stakes are the whole decks and the winner picks AFTER the match). `Stakes` is only
    /// carried so both players read the same rule on the invite. Results are collected in Changed (players whose view changed), Starts and Ends for the service to flush.
    /// </summary>
    public sealed class DuelLobby
    {
        public const long InviteMs = 15000, PairCooldownMs = 30000;

        sealed class Room
        {
            public ushort A, B;
            public bool Stakes;
            public LobbyPhase Phase;
            public long ExpiresAt;
        }

        readonly Dictionary<ushort, Room> byPlayer = new Dictionary<ushort, Room>();
        readonly Dictionary<uint, long> lastInvite = new Dictionary<uint, long>();

        public readonly List<ushort> Changed = new List<ushort>();
        public readonly List<LobbyStart> Starts = new List<LobbyStart>();
        public readonly List<LobbyEnd> Ends = new List<LobbyEnd>();

        public bool IsBusy(ushort id) { return byPlayer.ContainsKey(id); }
        public LobbyPhase PhaseOf(ushort id) { Room r; return byPlayer.TryGetValue(id, out r) ? r.Phase : LobbyPhase.None; }

        static uint Key(ushort from, ushort to) { return ((uint)from << 16) | to; }

        public LobbyFail Invite(ushort from, ushort to, bool stakes, long now)
        {
            if (from == to) return LobbyFail.Self;
            if (byPlayer.ContainsKey(from) || byPlayer.ContainsKey(to)) return LobbyFail.Busy;
            long last;
            if (lastInvite.TryGetValue(Key(from, to), out last) && now - last < PairCooldownMs) return LobbyFail.Cooldown;
            lastInvite[Key(from, to)] = now;
            var r = new Room { A = from, B = to, Stakes = stakes, Phase = LobbyPhase.Incoming, ExpiresAt = now + InviteMs };
            byPlayer[from] = r; byPlayer[to] = r;
            Changed.Add(from); Changed.Add(to);
            return LobbyFail.None;
        }

        /// <summary>The inviter takes the invite back (before it is answered); they may invite again at once.</summary>
        public LobbyFail Cancel(ushort by)
        {
            Room r;
            if (!byPlayer.TryGetValue(by, out r) || r.Phase != LobbyPhase.Incoming || r.A != by) return LobbyFail.NoInvite;
            lastInvite.Remove(Key(r.A, r.B));
            End(r, LobbyFail.Cancelled, by);
            return LobbyFail.None;
        }

        public LobbyFail Decline(ushort by)
        {
            Room r;
            if (!byPlayer.TryGetValue(by, out r) || r.Phase != LobbyPhase.Incoming || r.B != by) return LobbyFail.NoInvite;
            End(r, LobbyFail.Declined, by);
            return LobbyFail.None;
        }

        public LobbyFail Accept(ushort by)
        {
            Room r;
            if (!byPlayer.TryGetValue(by, out r) || r.Phase != LobbyPhase.Incoming || r.B != by) return LobbyFail.NoInvite;
            r.Phase = LobbyPhase.Starting;
            Starts.Add(new LobbyStart { A = r.A, B = r.B, Stakes = r.Stakes });
            Changed.Add(r.A); Changed.Add(r.B);
            return LobbyFail.None;
        }

        /// <summary>Either player backs out (or the other player left the game).</summary>
        public LobbyFail Leave(ushort by, LobbyFail why = LobbyFail.Left)
        {
            Room r;
            if (!byPlayer.TryGetValue(by, out r)) return LobbyFail.NoLobby;
            if (r.Phase == LobbyPhase.Starting) return LobbyFail.Busy; // the service finishes it
            End(r, why, by);
            return LobbyFail.None;
        }

        /// <summary>The service is done with a started lobby (the match runs, or the escrow failed): both players are free.</summary>
        public void Finish(ushort anyOf)
        {
            Room r;
            if (!byPlayer.TryGetValue(anyOf, out r)) return;
            byPlayer.Remove(r.A); byPlayer.Remove(r.B);
            Changed.Add(r.A); Changed.Add(r.B);
        }

        public void Tick(long now)
        {
            List<Room> over = null;
            foreach (Room r in byPlayer.Values)
                if (r.Phase != LobbyPhase.Starting && now >= r.ExpiresAt && (over == null || !over.Contains(r))) { if (over == null) over = new List<Room>(); over.Add(r); }
            if (over != null) foreach (Room r in over) End(r, LobbyFail.Expired, 0);
            if (lastInvite.Count > 256) PruneInvites(now);
        }

        void PruneInvites(long now)
        {
            var old = new List<uint>();
            foreach (KeyValuePair<uint, long> kv in lastInvite) if (now - kv.Value >= PairCooldownMs) old.Add(kv.Key);
            foreach (uint k in old) lastInvite.Remove(k);
        }

        void End(Room r, LobbyFail why, ushort actor)
        {
            byPlayer.Remove(r.A); byPlayer.Remove(r.B);
            if (actor != r.A) Ends.Add(new LobbyEnd { To = r.A, Other = r.B, Why = why });
            if (actor != r.B) Ends.Add(new LobbyEnd { To = r.B, Other = r.A, Why = why });
            Changed.Add(r.A); Changed.Add(r.B);
        }

        /// <summary>The view of `id` (Phase None when they have no lobby).</summary>
        public LobbyView ViewOf(ushort id, long now)
        {
            Room r;
            if (!byPlayer.TryGetValue(id, out r)) return default(LobbyView);
            bool a = id == r.A;
            LobbyPhase phase = r.Phase == LobbyPhase.Incoming ? (a ? LobbyPhase.Outgoing : LobbyPhase.Incoming) : r.Phase;
            return new LobbyView
            {
                Phase = phase, Other = a ? r.B : r.A, Stakes = r.Stakes,
                MsLeft = r.Phase == LobbyPhase.Starting ? 0 : (int)Math.Max(0L, Math.Min(int.MaxValue, r.ExpiresAt - now)),
            };
        }
    }
}
