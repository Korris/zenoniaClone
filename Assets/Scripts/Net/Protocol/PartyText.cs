namespace ZenoniaProto.Net
{
    /// <summary>The toast for each party notice. Empty = no toast (the invite window speaks for itself).</summary>
    public static class PartyText
    {
        /// <summary>True when the notice is bad news for the reader (drawn in the warning colour).</summary>
        public static bool IsBad(PartyNoticeMsg m)
        {
            switch (m.Kind)
            {
                case PartyNoticeKind.Failed:
                case PartyNoticeKind.YouWereKicked:
                case PartyNoticeKind.Disbanded:
                    return true;
                case PartyNoticeKind.OutClosed:
                case PartyNoticeKind.InClosed:
                    return m.Detail != (byte)PartyInviteEnd.Accepted;
                default: return false;
            }
        }

        public static string Of(PartyNoticeMsg m)
        {
            string name = string.IsNullOrEmpty(m.Name) ? "That player" : m.Name;
            switch (m.Kind)
            {
                case PartyNoticeKind.InviteOut: return "Party invite sent to " + name;
                case PartyNoticeKind.InviteIn: return "";
                case PartyNoticeKind.OutClosed:
                    switch ((PartyInviteEnd)m.Detail)
                    {
                        case PartyInviteEnd.Accepted: return name + " joined your party";
                        case PartyInviteEnd.Declined: return name + " declined your invite";
                        case PartyInviteEnd.Cancelled: return "Invite to " + name + " cancelled";
                        case PartyInviteEnd.Expired: return name + " did not answer your invite";
                        default: return name + " went offline, invite cancelled";
                    }
                case PartyNoticeKind.InClosed:
                    switch ((PartyInviteEnd)m.Detail)
                    {
                        case PartyInviteEnd.Accepted: return "You joined " + name + "'s party";
                        case PartyInviteEnd.Declined: return "You declined " + name + "'s invite";
                        case PartyInviteEnd.Cancelled: return name + " cancelled the party invite";
                        case PartyInviteEnd.Expired: return "The invite from " + name + " expired";
                        default: return name + " went offline, invite cancelled";
                    }
                case PartyNoticeKind.Failed: return OfFail((PartyFail)m.Detail, name);
                case PartyNoticeKind.MemberJoined: return name + " joined the party";
                case PartyNoticeKind.MemberLeft: return name + " left the party";
                case PartyNoticeKind.MemberKicked: return name + " was removed from the party";
                case PartyNoticeKind.YouLeft: return "You left the party";
                case PartyNoticeKind.YouWereKicked: return name + " removed you from the party";
                case PartyNoticeKind.LeaderChanged: return name + " is now the party leader";
                case PartyNoticeKind.Disbanded: return "The party has ended";
                default: return "";
            }
        }

        static string OfFail(PartyFail f, string name)
        {
            switch (f)
            {
                case PartyFail.NoSuchPlayer: return "That player is not online";
                case PartyFail.Self: return "You cannot do that to yourself";
                case PartyFail.TargetInParty: return name + " is already in a party";
                case PartyFail.TargetBusy: return name + " is answering another invite";
                case PartyFail.PartyFull: return "Your party is full (" + PartyBook.MaxMembers + " members)";
                case PartyFail.NotLeader: return "Only the party leader can do that";
                case PartyFail.AlreadyInvited: return name + " has already been invited";
                case PartyFail.AlreadyPending: return "Your invite to " + name + " is still waiting";
                case PartyFail.AnswerFirst: return "Answer your party invite first";
                case PartyFail.NoInvite: return "That invite is no longer open";
                case PartyFail.NotInParty: return "You are not in a party";
                case PartyFail.NotMember: return name + " is not in your party";
                default: return "";
            }
        }
    }
}
