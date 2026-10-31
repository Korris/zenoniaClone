using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// What the client knows of its own party: the roster and the open invite, only ever changed by the server's messages. The UI reads it, and so does the replay of an ally skill,
    /// which is why a stranger's heal on a third player is drawn on the caster alone here: a client is only told about its own party.
    /// </summary>
    public sealed class PartyClientState
    {
        public struct OpenInvite { public ushort Other; public string Name; public double ExpiresAtMs; public byte HeroClass; }

        readonly List<PartyMemberInfo> members = new List<PartyMemberInfo>(PartyBook.MaxMembers);

        public ushort MyId;
        /// <summary>Goes up with every change: the UI rebuilds its texts only then.</summary>
        public int Version { get; private set; }
        /// <summary>Leader first; empty when not in a party.</summary>
        public IReadOnlyList<PartyMemberInfo> Members { get { return members; } }
        public OpenInvite? Incoming { get; private set; }
        public OpenInvite? Outgoing { get; private set; }

        public bool InParty { get { return members.Count > 0; } }
        public bool IsLeader { get { return members.Count > 0 && members[0].PlayerId == MyId; } }

        public void Clear() { members.Clear(); Incoming = null; Outgoing = null; MyId = 0; Version++; }

        public void Apply(PartyNoticeMsg m, double nowMs)
        {
            Version++;
            switch (m.Kind)
            {
                case PartyNoticeKind.InviteOut: Outgoing = new OpenInvite { Other = m.Other, Name = m.Name, ExpiresAtMs = nowMs + m.MsLeft }; break;
                case PartyNoticeKind.InviteIn: Incoming = new OpenInvite { Other = m.Other, Name = m.Name, ExpiresAtMs = nowMs + m.MsLeft, HeroClass = m.Detail }; break;
                case PartyNoticeKind.OutClosed: Outgoing = null; break;
                case PartyNoticeKind.InClosed: Incoming = null; break;
            }
        }

        public void Apply(PartyRosterMsg m)
        {
            Version++;
            members.Clear();
            if (m.Members != null) members.AddRange(m.Members);
        }

        public bool Contains(ushort id)
        {
            for (int i = 0; i < members.Count; i++) if (members[i].PlayerId == id) return true;
            return false;
        }

        public bool SameParty(ushort a, ushort b) { return Contains(a) && Contains(b); }
    }
}
