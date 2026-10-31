using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>A notice waiting to go to one player.</summary>
    public struct PartyNote { public ushort To; public PartyNoticeMsg Msg; }

    /// <summary>
    /// The party rules, with no engine and no network: who is in which party, and every pending invite. Parties live in memory on the server. Each change queues notices in Notes for the
    /// caller to send. Rules: max 5, the inviter leads a new party, only the leader of an existing party invites, the first member listed is the leader (so leadership passes to the
    /// longest-serving member), the party ends when one member remains. A player has at most one invite open, sent or received: that one rule keeps answers unambiguous and a full party
    /// from being overbooked. Going offline removes the player and ends their invites both ways.
    /// </summary>
    public sealed class PartyBook
    {
        public const int MaxMembers = 5;
        public const int InviteMs = 30000;

        sealed class Party { public int Id; public readonly List<ushort> Members = new List<ushort>(MaxMembers); }
        sealed class Invite { public ushort From, To; public long ExpiresAt; }

        static readonly List<ushort> NoMembers = new List<ushort>();

        readonly Dictionary<ushort, string> names = new Dictionary<ushort, string>();
        readonly Dictionary<ushort, byte> classes = new Dictionary<ushort, byte>();
        readonly Dictionary<ushort, Party> partyOf =new Dictionary<ushort, Party>();
        readonly List<Invite> invites = new List<Invite>();
        int nextPartyId = 1;
        bool rosterChanged;

        /// <summary>Notices queued since the caller last emptied this list.</summary>
        public readonly List<PartyNote> Notes = new List<PartyNote>();

        /// <summary>A player came online. `heroClass` travels with the player's invites, for the icon in the invitee's window.</summary>
        public void AddPlayer(ushort id, string name, byte heroClass = 0) { names[id] = name ?? ""; classes[id] = heroClass; }

        /// <summary>The player went offline: invites both ways end, and the party carries on without them.</summary>
        public void RemovePlayer(ushort id)
        {
            for (int i = invites.Count - 1; i >= 0; i--)
                if (invites[i].From == id || invites[i].To == id) Close(invites[i], PartyInviteEnd.Offline);
            Party p;
            if (partyOf.TryGetValue(id, out p)) RemoveMember(p, id, PartyNoticeKind.MemberLeft, null);
            names.Remove(id);
            classes.Remove(id);
        }

        /// <summary>Runs one client request; every refusal is also queued as a Failed notice to `from`.</summary>
        public PartyFail Apply(ushort from, PartyRequestMsg m, long nowMs)
        {
            PartyFail fail;
            string about = "";
            switch (m.Op)
            {
                case PartyOp.Invite: fail = TryInvite(from, m.Target, nowMs, ref about); break;
                case PartyOp.CancelInvite: fail = CancelInvite(from); break;
                case PartyOp.Accept: fail = Accept(from, m.Target); break;
                case PartyOp.Decline: fail = Decline(from, m.Target); break;
                case PartyOp.Leave: fail = Leave(from); break;
                case PartyOp.Kick: fail = Kick(from, m.Target, ref about); break;
                default: fail = PartyFail.NoInvite; break;
            }
            if (fail != PartyFail.None) Note(from, PartyNoticeKind.Failed, (byte)fail, m.Target, about);
            return fail;
        }

        /// <summary>Ends the invites whose 30 s ran out.</summary>
        public void Tick(long nowMs)
        {
            for (int i = invites.Count - 1; i >= 0; i--)
                if (nowMs >= invites[i].ExpiresAt) Close(invites[i], PartyInviteEnd.Expired);
        }

        // ---- questions ----

        /// <summary>The player's party id, 0 when not in one.</summary>
        public int PartyIdOf(ushort id) { Party p; return partyOf.TryGetValue(id, out p) ? p.Id : 0; }

        public bool SameParty(ushort a, ushort b)
        {
            Party p, q;
            return partyOf.TryGetValue(a, out p) && partyOf.TryGetValue(b, out q) && p == q;
        }

        /// <summary>The player's party, leader first (empty when not in one). The list is the book's own: read it, do not keep it.</summary>
        public IReadOnlyList<ushort> MembersOf(ushort id) { Party p; return partyOf.TryGetValue(id, out p) ? p.Members : NoMembers; }

        public bool HasOpenInvite(ushort id) { return FindOf(id) != null; }

        /// <summary>True once after any membership change: the caller pushes rosters right away instead of waiting for its timer.</summary>
        public bool TakeRosterChanged() { bool c = rosterChanged; rosterChanged = false; return c; }

        // ---- requests ----

        PartyFail TryInvite(ushort from, ushort to, long nowMs, ref string about)
        {
            if (!names.ContainsKey(from)) return PartyFail.NoSuchPlayer;
            if (to == from) return PartyFail.Self;
            string toName;
            if (!names.TryGetValue(to, out toName)) return PartyFail.NoSuchPlayer;
            about = toName;
            Party mine;
            bool inParty = partyOf.TryGetValue(from, out mine);
            if (inParty && mine.Members[0] != from) return PartyFail.NotLeader;
            Invite open = FindOf(from);
            if (open != null)
            {
                if (open.To == from) return PartyFail.AnswerFirst;
                if (open.To != to) about = names[open.To];
                return open.To == to ? PartyFail.AlreadyInvited : PartyFail.AlreadyPending;
            }
            if (inParty && mine.Members.Count >= MaxMembers) return PartyFail.PartyFull;
            if (partyOf.ContainsKey(to)) return PartyFail.TargetInParty;
            if (FindOf(to) != null) return PartyFail.TargetBusy;
            invites.Add(new Invite { From = from, To = to, ExpiresAt = nowMs + InviteMs });
            Note(from, PartyNoticeKind.InviteOut, 0, to, toName, InviteMs);
            Note(to, PartyNoticeKind.InviteIn, classes[from], from, names[from], InviteMs);
            return PartyFail.None;
        }

        PartyFail CancelInvite(ushort from)
        {
            Invite open = FindOf(from);
            if (open == null || open.From != from) return PartyFail.NoInvite;
            Close(open, PartyInviteEnd.Cancelled);
            return PartyFail.None;
        }

        PartyFail Decline(ushort to, ushort from)
        {
            Invite open = FindOf(to);
            if (open == null || open.To != to || open.From != from) return PartyFail.NoInvite;
            Close(open, PartyInviteEnd.Declined);
            return PartyFail.None;
        }

        PartyFail Accept(ushort to, ushort from)
        {
            Invite open = FindOf(to);
            if (open == null || open.To != to || open.From != from) return PartyFail.NoInvite;
            Party p;
            if (!partyOf.TryGetValue(from, out p))
            {
                p = new Party { Id = nextPartyId++ };
                p.Members.Add(from);
                partyOf[from] = p;
            }
            for (int i = 1; i < p.Members.Count; i++) Note(p.Members[i], PartyNoticeKind.MemberJoined, 0, to, names[to]);
            p.Members.Add(to);
            partyOf[to] = p;
            rosterChanged = true;
            Close(open, PartyInviteEnd.Accepted);
            return PartyFail.None;
        }

        PartyFail Leave(ushort id)
        {
            Party p;
            if (!partyOf.TryGetValue(id, out p)) return PartyFail.NotInParty;
            Note(id, PartyNoticeKind.YouLeft, 0, id, "");
            RemoveMember(p, id, PartyNoticeKind.MemberLeft, null);
            return PartyFail.None;
        }

        PartyFail Kick(ushort leader, ushort target, ref string about)
        {
            Party p;
            if (!partyOf.TryGetValue(leader, out p)) return PartyFail.NotInParty;
            if (p.Members[0] != leader) return PartyFail.NotLeader;
            if (target == leader) return PartyFail.Self;
            string targetName;
            if (names.TryGetValue(target, out targetName)) about = targetName;
            if (!p.Members.Contains(target)) return PartyFail.NotMember;
            Note(target, PartyNoticeKind.YouWereKicked, 0, leader, names[leader]);
            RemoveMember(p, target, PartyNoticeKind.MemberKicked, targetName);
            return PartyFail.None;
        }

        // ---- shared steps ----

        /// <summary>Takes one member out, tells the rest, and ends the party or hands the lead on as needed. A leader's open invite goes with the lead.</summary>
        void RemoveMember(Party p, ushort id, PartyNoticeKind told, string shownName)
        {
            bool wasLeader = p.Members[0] == id;
            string name = shownName ?? names[id];
            if (wasLeader)
            {
                Invite open = FindOf(id);
                if (open != null && open.From == id) Close(open, PartyInviteEnd.Cancelled);
            }
            p.Members.Remove(id);
            partyOf.Remove(id);
            rosterChanged = true;
            for (int i = 0; i < p.Members.Count; i++) Note(p.Members[i], told, 0, id, name);
            if (p.Members.Count == 1)
            {
                Note(p.Members[0], PartyNoticeKind.Disbanded, 0, 0, "");
                partyOf.Remove(p.Members[0]);
                p.Members.Clear();
            }
            else if (wasLeader)
                for (int i = 0; i < p.Members.Count; i++) Note(p.Members[i], PartyNoticeKind.LeaderChanged, 0, p.Members[0], names[p.Members[0]]);
        }

        void Close(Invite inv, PartyInviteEnd end)
        {
            invites.Remove(inv);
            Note(inv.From, PartyNoticeKind.OutClosed, (byte)end, inv.To, names[inv.To]);
            Note(inv.To, PartyNoticeKind.InClosed, (byte)end, inv.From, names[inv.From]);
        }

        Invite FindOf(ushort id)
        {
            for (int i = 0; i < invites.Count; i++) if (invites[i].From == id || invites[i].To == id) return invites[i];
            return null;
        }

        void Note(ushort to, PartyNoticeKind kind, byte detail, ushort other, string name, int msLeft = 0)
        {
            Notes.Add(new PartyNote { To = to, Msg = new PartyNoticeMsg { Kind = kind, Detail = detail, Other = other, Name = name, MsLeft = (ushort)msLeft } });
        }
    }
}
