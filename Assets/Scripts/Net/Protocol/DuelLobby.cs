using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    public enum LobbyPhase : byte { None = 0, Incoming = 1, Outgoing = 2, Pick = 3, Starting = 4 }

    /// <summary>Why a lobby step was refused, or why a lobby ended (sent to the player as DuelFail.Lobby* codes by the zone).</summary>
    public enum LobbyFail : byte { None = 0, Self = 1, Busy = 2, Cooldown = 3, NoInvite = 4, NoLobby = 5, NoPick = 6, NotLocked = 7, Mismatch = 8, Declined = 9, Cancelled = 10, Expired = 11, Left = 12 }

    /// <summary>What one player sees of their lobby (invite or wager panel). Cards are card indices, NoCard = none yet.</summary>
    public struct LobbyView
    {
        public LobbyPhase Phase;
        public ushort Other;
        public bool Wager, MyLocked, TheirLocked, MyConfirmed, TheirConfirmed;
        public byte MyCard, TheirCard;
        public int MsLeft;
    }

    /// <summary>Both players are ready: A is the inviter. For a wager the two picked cards are final (confirmed by both).</summary>
    public struct LobbyStart { public ushort A, B; public bool Wager; public byte CardA, CardB; }

    /// <summary>A lobby ended without a match (to = the player to tell, `Other` = the other one).</summary>
    public struct LobbyEnd { public ushort To, Other; public LobbyFail Why; }

    /// <summary>
    /// The challenge flow of the card duel between two players, no networking, no clock of its own (`nowMs` is passed in): invite (15 s, one open invite or lobby per player, 30 s before inviting the same player again),
    /// accept / decline / cancel, and for a Card Wager the pick panel: each side picks ONE card, Lock, then Confirm once both are locked. A pick clears the picker's own lock and BOTH confirmations, so nobody
    /// confirms a card they did not see. What a card is worth (rarity, ownership) is the zone's business and checked before Pick; the lobby only compares the two picks through `CardsMatch` at Confirm.
    /// Results are collected in Changed (players whose view changed), Starts and Ends for the service to flush.
    /// </summary>
    public sealed class DuelLobby
    {
        public const long InviteMs = 15000, PairCooldownMs = 30000, PickMs = 60000;
        public const byte NoCard = 255;

        sealed class Room
        {
            public ushort A, B;
            public bool Wager;
            public LobbyPhase Phase;
            public long ExpiresAt;
            public byte CardA = NoCard, CardB = NoCard;
            public bool LockA, LockB, ConfA, ConfB;
        }

        readonly Dictionary<ushort, Room> byPlayer = new Dictionary<ushort, Room>();
        readonly Dictionary<uint, long> lastInvite = new Dictionary<uint, long>();

        public readonly List<ushort> Changed = new List<ushort>();
        public readonly List<LobbyStart> Starts = new List<LobbyStart>();
        public readonly List<LobbyEnd> Ends = new List<LobbyEnd>();
        /// <summary>True when the two picked cards may be wagered against each other (same rarity). Null = always.</summary>
        public Func<byte, byte, bool> CardsMatch;

        public bool IsBusy(ushort id) { return byPlayer.ContainsKey(id); }
        public LobbyPhase PhaseOf(ushort id) { Room r; return byPlayer.TryGetValue(id, out r) ? r.Phase : LobbyPhase.None; }

        static uint Key(ushort from, ushort to) { return ((uint)from << 16) | to; }

        public LobbyFail Invite(ushort from, ushort to, bool wager, long now)
        {
            if (from == to) return LobbyFail.Self;
            if (byPlayer.ContainsKey(from) || byPlayer.ContainsKey(to)) return LobbyFail.Busy;
            long last;
            if (lastInvite.TryGetValue(Key(from, to), out last) && now - last < PairCooldownMs) return LobbyFail.Cooldown;
            lastInvite[Key(from, to)] = now;
            var r = new Room { A = from, B = to, Wager = wager, Phase = LobbyPhase.Incoming, ExpiresAt = now + InviteMs };
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

        public LobbyFail Accept(ushort by, long now)
        {
            Room r;
            if (!byPlayer.TryGetValue(by, out r) || r.Phase != LobbyPhase.Incoming || r.B != by) return LobbyFail.NoInvite;
            if (r.Wager) { r.Phase = LobbyPhase.Pick; r.ExpiresAt = now + PickMs; Changed.Add(r.A); Changed.Add(r.B); }
            else Begin(r);
            return LobbyFail.None;
        }

        public LobbyFail Pick(ushort by, byte card)
        {
            Room r = PickRoom(by);
            if (r == null) return LobbyFail.NoLobby;
            if (card == NoCard) return LobbyFail.NoPick;
            if (by == r.A) { r.CardA = card; r.LockA = false; } else { r.CardB = card; r.LockB = false; }
            r.ConfA = r.ConfB = false;
            Changed.Add(r.A); Changed.Add(r.B);
            return LobbyFail.None;
        }

        public LobbyFail Lock(ushort by)
        {
            Room r = PickRoom(by);
            if (r == null) return LobbyFail.NoLobby;
            if ((by == r.A ? r.CardA : r.CardB) == NoCard) return LobbyFail.NoPick;
            if (by == r.A) r.LockA = true; else r.LockB = true;
            Changed.Add(r.A); Changed.Add(r.B);
            return LobbyFail.None;
        }

        public LobbyFail Confirm(ushort by)
        {
            Room r = PickRoom(by);
            if (r == null) return LobbyFail.NoLobby;
            if (!r.LockA || !r.LockB) return LobbyFail.NotLocked;
            if (CardsMatch != null && !CardsMatch(r.CardA, r.CardB)) return LobbyFail.Mismatch;
            if (by == r.A) r.ConfA = true; else r.ConfB = true;
            if (r.ConfA && r.ConfB) Begin(r);
            else { Changed.Add(r.A); Changed.Add(r.B); }
            return LobbyFail.None;
        }

        /// <summary>Either player backs out of the panel (or the other player left the game).</summary>
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

        Room PickRoom(ushort by) { Room r; return byPlayer.TryGetValue(by, out r) && r.Phase == LobbyPhase.Pick ? r : null; }

        void Begin(Room r)
        {
            r.Phase = LobbyPhase.Starting;
            Starts.Add(new LobbyStart { A = r.A, B = r.B, Wager = r.Wager, CardA = r.CardA, CardB = r.CardB });
            Changed.Add(r.A); Changed.Add(r.B);
        }

        void End(Room r, LobbyFail why, ushort actor)
        {
            byPlayer.Remove(r.A); byPlayer.Remove(r.B);
            if (actor != r.A) Ends.Add(new LobbyEnd { To = r.A, Other = r.B, Why = why });
            if (actor != r.B) Ends.Add(new LobbyEnd { To = r.B, Other = r.A, Why = why });
            Changed.Add(r.A); Changed.Add(r.B);
        }

        /// <summary>The view of `id` (Phase None when they have no lobby). The other player's pick is shown as it is: both look at both cards.</summary>
        public LobbyView ViewOf(ushort id, long now)
        {
            Room r;
            if (!byPlayer.TryGetValue(id, out r)) return default(LobbyView);
            bool a = id == r.A;
            LobbyPhase phase = r.Phase == LobbyPhase.Incoming ? (a ? LobbyPhase.Outgoing : LobbyPhase.Incoming) : r.Phase;
            return new LobbyView
            {
                Phase = phase, Other = a ? r.B : r.A, Wager = r.Wager,
                MyCard = a ? r.CardA : r.CardB, TheirCard = a ? r.CardB : r.CardA,
                MyLocked = a ? r.LockA : r.LockB, TheirLocked = a ? r.LockB : r.LockA,
                MyConfirmed = a ? r.ConfA : r.ConfB, TheirConfirmed = a ? r.ConfB : r.ConfA,
                MsLeft = r.Phase == LobbyPhase.Starting ? 0 : (int)Math.Max(0L, Math.Min(int.MaxValue, r.ExpiresAt - now)),
            };
        }
    }
}
