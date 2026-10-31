using System;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>What a client asks of the party rules. Target: Invite / Kick = that player; Accept / Decline = the inviter (so an answer can never land on a newer invite).</summary>
    public enum PartyOp : byte { Invite = 0, CancelInvite = 1, Accept = 2, Decline = 3, Leave = 4, Kick = 5 }

    public enum PartyNoticeKind : byte
    {
        /// <summary>You invited Name: pending until OutClosed. MsLeft = time to answer.</summary>
        InviteOut = 0,
        /// <summary>Name invited you: the window stays up until InClosed. MsLeft = time to answer, Detail = the inviter's hero class.</summary>
        InviteIn = 1,
        /// <summary>Your invite to Name ended, Detail = PartyInviteEnd.</summary>
        OutClosed = 2,
        /// <summary>The invite from Name ended, Detail = PartyInviteEnd.</summary>
        InClosed = 3,
        /// <summary>Your request was refused, Detail = PartyFail, Name = who it was about (may be empty).</summary>
        Failed = 4,
        MemberJoined = 5, MemberLeft = 6, MemberKicked = 7,
        YouLeft = 8, YouWereKicked = 9,
        LeaderChanged = 10,
        /// <summary>Nobody else is left: the party is over.</summary>
        Disbanded = 11,
    }

    public enum PartyInviteEnd : byte { Accepted = 0, Declined = 1, Cancelled = 2, Expired = 3, Offline = 4 }

    public enum PartyFail : byte { None = 0, NoSuchPlayer = 1, Self = 2, TargetInParty = 3, TargetBusy = 4, PartyFull = 5, NotLeader = 6, AlreadyInvited = 7, AlreadyPending = 8, AnswerFirst = 9, NoInvite = 10, NotInParty = 11, NotMember = 12 }

    /// <summary>Client -> server: one party request. A semantic refusal comes back as a notice; only an unreadable packet costs the connection.</summary>
    public struct PartyRequestMsg
    {
        public PartyOp Op;
        public ushort Target;

        public void Write(NetDataWriter w) { w.Reset(); w.Put(NetOp.PartyRequest); w.Put((byte)Op); w.Put(Target); }

        public static bool TryRead(NetDataReader r, out PartyRequestMsg m)
        {
            m = default(PartyRequestMsg);
            try
            {
                byte op = r.GetByte(); m.Target = r.GetUShort();
                if (op > (byte)PartyOp.Kick) return false;
                m.Op = (PartyOp)op;
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>Server -> client: one thing that happened to this player's invites or party (a state change plus a toast, see PartyText).</summary>
    public struct PartyNoticeMsg
    {
        public PartyNoticeKind Kind;
        public byte Detail;
        public ushort Other;
        public string Name;
        public ushort MsLeft;

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.PartyNotice); w.Put((byte)Kind); w.Put(Detail); w.Put(Other); w.Put(Name ?? "", NetProtocol.MaxNameLength); w.Put(MsLeft);
        }

        public static bool TryRead(NetDataReader r, out PartyNoticeMsg m)
        {
            m = default(PartyNoticeMsg);
            try
            {
                byte kind = r.GetByte(); m.Detail = r.GetByte(); m.Other = r.GetUShort(); m.Name = r.GetString(NetProtocol.MaxNameLength); m.MsLeft = r.GetUShort();
                if (kind > (byte)PartyNoticeKind.Disbanded) return false;
                m.Kind = (PartyNoticeKind)kind;
                if ((m.Kind == PartyNoticeKind.OutClosed || m.Kind == PartyNoticeKind.InClosed) && m.Detail > (byte)PartyInviteEnd.Offline) return false;
                if (m.Kind == PartyNoticeKind.Failed && m.Detail > (byte)PartyFail.NotMember) return false;
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>One line of the party panel. MapId names the member's zone: a member elsewhere still shows.</summary>
    public struct PartyMemberInfo
    {
        public ushort PlayerId, Hp, MaxHp;
        public byte HeroClass, Level;
        public string Name, MapId;
    }

    /// <summary>Server -> client: the whole party, leader first; no members = this player is not in a party.</summary>
    public struct PartyRosterMsg
    {
        public PartyMemberInfo[] Members;

        public void Write(NetDataWriter w)
        {
            int n = Members != null ? Math.Min(Members.Length, PartyBook.MaxMembers) : 0;
            w.Reset(); w.Put(NetOp.PartyRoster); w.Put((byte)n);
            for (int i = 0; i < n; i++)
            {
                PartyMemberInfo x = Members[i];
                w.Put(x.PlayerId); w.Put(x.Name ?? "", NetProtocol.MaxNameLength); w.Put(x.HeroClass); w.Put(x.Level); w.Put(x.Hp); w.Put(x.MaxHp); w.Put(x.MapId ?? "", NetProtocol.MaxMapIdLength);
            }
        }

        public static bool TryRead(NetDataReader r, out PartyRosterMsg m)
        {
            m = default(PartyRosterMsg);
            try
            {
                int n = r.GetByte();
                if (n > PartyBook.MaxMembers) return false;
                m.Members = new PartyMemberInfo[n];
                for (int i = 0; i < n; i++)
                    m.Members[i] = new PartyMemberInfo { PlayerId = r.GetUShort(), Name = r.GetString(NetProtocol.MaxNameLength), HeroClass = r.GetByte(), Level = r.GetByte(), Hp = r.GetUShort(), MaxHp = r.GetUShort(), MapId = r.GetString(NetProtocol.MaxMapIdLength) };
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
