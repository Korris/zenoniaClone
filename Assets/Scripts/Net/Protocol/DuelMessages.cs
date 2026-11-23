using System;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// What a client asks of the card duel (NetOp.DuelRequest). SaveDeck: Cards = card indices (CardTable order), at most 8, replaces the saved deck. PlayNpc: A = NPC index. Place: A = index in the
    /// player's hand, B = cell (row * 4 + column), Turn = the turn the client is looking at (moves so far + 1), Card = the card index it believes sits at that hand index: the server refuses the placement
    /// when either is stale (a timeout placement or a quick opponent changed the hand under the tap). Forfeit: leaves the running duel (the opponent wins). A client never describes a card, a hand or a result.
    /// PvP (phase 06): Challenge: Other = the player id, A = 1 for a Card Wager (the inviter decides); CancelInvite (inviter), Accept, Decline (receiver); WagerPick: A = card index; WagerLock, WagerConfirm;
    /// LeaveLobby: backs out of the invite or the wager panel.
    /// </summary>
    public enum DuelOp : byte { SaveDeck = 0, PlayNpc = 1, Place = 2, Forfeit = 3, Challenge = 4, CancelInvite = 5, Accept = 6, Decline = 7, WagerPick = 8, WagerLock = 9, WagerConfirm = 10, LeaveLobby = 11 }

    public enum DuelNoticeKind : byte { Refused = 0, DeckSaved = 1 }

    /// <summary>Why a request was refused (DuelNoticeMsg.Detail). DeckInvalid carries the DeckError in Extra.</summary>
    public enum DuelFail : byte
    {
        None = 0, Busy = 1, NotSafe = 2, Combat = 3, Dead = 4, Trading = 5, DeckInvalid = 6, NoSuchNpc = 7, NoMatch = 8, NotYourTurn = 9, BadMove = 10, Disabled = 11, InDuel = 12,
        NoTarget = 13, TargetBusy = 14, InviteCooldown = 15, NoInvite = 16, NoLobby = 17, NoPick = 18, NotLocked = 19, CardMismatch = 20, Declined = 21, Cancelled = 22, Expired = 23, OpponentLeft = 24,
        WagerOff = 25, WagerLevel = 26, WagerCard = 27, WagerDaily = 28, ApiBusy = 29, WagerRefused = 30, StaleMove = 31, SelfInvite = 32, TargetNotReady = 33
    }

    public static class DuelFailInfo { public const DuelFail Last = DuelFail.TargetNotReady; }

    public struct DuelRequestMsg
    {
        public const int MaxDeck = 8;
        public DuelOp Op;
        public byte A, B, Turn, Card;
        public ushort Other;
        public byte[] Cards;

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.DuelRequest); w.Put((byte)Op); w.Put(A); w.Put(B); w.Put(Turn); w.Put(Card); w.Put(Other);
            int n = Cards == null ? 0 : Math.Min(Cards.Length, MaxDeck);
            w.Put((byte)n);
            for (int i = 0; i < n; i++) w.Put(Cards[i]);
        }

        public static bool TryRead(NetDataReader r, out DuelRequestMsg m)
        {
            m = default(DuelRequestMsg);
            try
            {
                byte op = r.GetByte(); m.A = r.GetByte(); m.B = r.GetByte(); m.Turn = r.GetByte(); m.Card = r.GetByte(); m.Other = r.GetUShort();
                int n = r.GetByte();
                if (op > (byte)DuelOp.LeaveLobby || n > MaxDeck) return false;
                m.Op = (DuelOp)op;
                m.Cards = new byte[n];
                for (int i = 0; i < n; i++) m.Cards[i] = r.GetByte();
                return r.AvailableBytes == 0;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>Server -> client: a refusal (Detail = DuelFail, Extra = DeckError for DeckInvalid) or "deck saved".</summary>
    public struct DuelNoticeMsg
    {
        public DuelNoticeKind Kind;
        public byte Detail, Extra;

        public void Write(NetDataWriter w) { w.Reset(); w.Put(NetOp.DuelNotice); w.Put((byte)Kind); w.Put(Detail); w.Put(Extra); }

        public static bool TryRead(NetDataReader r, out DuelNoticeMsg m)
        {
            m = default(DuelNoticeMsg);
            try
            {
                byte kind = r.GetByte(); m.Detail = r.GetByte(); m.Extra = r.GetByte();
                if (kind > (byte)DuelNoticeKind.DeckSaved || (kind == (byte)DuelNoticeKind.Refused && m.Detail > (byte)DuelFailInfo.Last)) return false;
                m.Kind = (DuelNoticeKind)kind;
                return r.AvailableBytes == 0;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>
    /// Server -> client: everything one player may know of their duel, as the move list (Side 0 = the receiver) plus their own hand: the board, score, cards left and log are derived from the moves on
    /// the client (DuelView), so there is no second copy of any of them to disagree. MsLeft = time left of the turn when sent; Result 0 playing / 1 win / 2 lose / 3 draw; EndReason 0 played out / 1 you left / 2 they left.
    /// Npc = PlayerOpponent (255) for a PvP duel. Wager* = the Card Wager of a PvP duel: the cards each side put in (NoCard = none) and where the payout is: 0 no wager, 1 held, 2 settling, 3 paid, 4 failed.
    /// OppAwayMs = how long the opponent has been disconnected (0 = connected); at 60 s they forfeit.
    /// </summary>
    public struct DuelStateMsg
    {
        public const int MaxMoves = 16, MaxHand = 5, MaxCardIndex = 63;
        public const byte PlayerOpponent = 255, NoCard = 255;
        public byte Npc, Result, EndReason;
        public byte WagerMine, WagerTheirs, WagerState;
        public bool YourTurn;
        public ushort MsLeft, OppAwayMs;
        public int RewardFirst, RewardDaily;
        public string OppName;
        public byte[] MoveCard, MoveCell, MoveSide, MoveAuto;
        public ushort[] MoveFlips;
        public byte[] Hand;

        public int MoveCount { get { return MoveCard == null ? 0 : MoveCard.Length; } }

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.DuelState);
            w.Put(Npc); w.Put(Result); w.Put(EndReason); w.Put(YourTurn); w.Put(MsLeft); w.Put(RewardFirst); w.Put(RewardDaily);
            w.Put(WagerMine); w.Put(WagerTheirs); w.Put(WagerState); w.Put(OppAwayMs);
            w.Put(OppName ?? "", NetProtocol.MaxNameLength);
            int n = Math.Min(MoveCount, MaxMoves);
            w.Put((byte)n);
            for (int i = 0; i < n; i++) { w.Put(MoveCard[i]); w.Put((byte)(MoveCell[i] | (MoveSide[i] << 4) | (MoveAuto[i] << 5))); w.Put(MoveFlips[i]); }
            int h = Hand == null ? 0 : Math.Min(Hand.Length, MaxHand);
            w.Put((byte)h);
            for (int i = 0; i < h; i++) w.Put(Hand[i]);
        }

        public static bool TryRead(NetDataReader r, out DuelStateMsg m)
        {
            m = default(DuelStateMsg);
            try
            {
                m.Npc = r.GetByte(); m.Result = r.GetByte(); m.EndReason = r.GetByte(); m.YourTurn = r.GetBool(); m.MsLeft = r.GetUShort(); m.RewardFirst = r.GetInt(); m.RewardDaily = r.GetInt();
                m.WagerMine = r.GetByte(); m.WagerTheirs = r.GetByte(); m.WagerState = r.GetByte(); m.OppAwayMs = r.GetUShort();
                m.OppName = r.GetString(NetProtocol.MaxNameLength);
                int n = r.GetByte();
                if (m.Result > 3 || m.EndReason > 2 || m.WagerState > 4 || n > MaxMoves || m.RewardFirst < 0 || m.RewardDaily < 0) return false;
                if ((m.WagerMine != NoCard && m.WagerMine > MaxCardIndex) || (m.WagerTheirs != NoCard && m.WagerTheirs > MaxCardIndex)) return false;
                m.MoveCard = new byte[n]; m.MoveCell = new byte[n]; m.MoveSide = new byte[n]; m.MoveAuto = new byte[n]; m.MoveFlips = new ushort[n];
                for (int i = 0; i < n; i++)
                {
                    m.MoveCard[i] = r.GetByte(); byte cs = r.GetByte(); m.MoveFlips[i] = r.GetUShort();
                    m.MoveCell[i] = (byte)(cs & 15); m.MoveSide[i] = (byte)((cs >> 4) & 1); m.MoveAuto[i] = (byte)((cs >> 5) & 1);
                    if (m.MoveCard[i] > MaxCardIndex || (cs & 0xC0) != 0) return false;
                }
                int h = r.GetByte();
                if (h > MaxHand) return false;
                m.Hand = new byte[h];
                for (int i = 0; i < h; i++) { m.Hand[i] = r.GetByte(); if (m.Hand[i] > MaxCardIndex) return false; }
                return r.AvailableBytes == 0;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>
    /// Server -> client: the player's challenge lobby (NetOp.DuelLobby): Phase 0 none (closes the window), 1 an invite for you, 2 your invite is waiting, 3 the wager panel, 4 starting (the server is
    /// holding the cards). Cards are card indices (NoCard = none picked). Sent on every change and when a lobby ends (Phase 0).
    /// </summary>
    public struct DuelLobbyMsg
    {
        public byte Phase, MyCard, TheirCard;
        public bool Wager, MyLocked, TheirLocked, MyConfirmed, TheirConfirmed;
        public ushort Other, MsLeft;
        public string OtherName;

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.DuelLobby);
            w.Put(Phase); w.Put(MyCard); w.Put(TheirCard);
            w.Put((byte)((Wager ? 1 : 0) | (MyLocked ? 2 : 0) | (TheirLocked ? 4 : 0) | (MyConfirmed ? 8 : 0) | (TheirConfirmed ? 16 : 0)));
            w.Put(Other); w.Put(MsLeft); w.Put(OtherName ?? "", NetProtocol.MaxNameLength);
        }

        public static bool TryRead(NetDataReader r, out DuelLobbyMsg m)
        {
            m = default(DuelLobbyMsg);
            try
            {
                m.Phase = r.GetByte(); m.MyCard = r.GetByte(); m.TheirCard = r.GetByte();
                byte f = r.GetByte();
                m.Other = r.GetUShort(); m.MsLeft = r.GetUShort(); m.OtherName = r.GetString(NetProtocol.MaxNameLength);
                if (m.Phase > 4 || f > 31) return false;
                if ((m.MyCard != DuelLobby.NoCard && m.MyCard > DuelStateMsg.MaxCardIndex) || (m.TheirCard != DuelLobby.NoCard && m.TheirCard > DuelStateMsg.MaxCardIndex)) return false;
                m.Wager = (f & 1) != 0; m.MyLocked = (f & 2) != 0; m.TheirLocked = (f & 4) != 0; m.MyConfirmed = (f & 8) != 0; m.TheirConfirmed = (f & 16) != 0;
                return r.AvailableBytes == 0;
            }
            catch (Exception) { return false; }
        }
    }
}
