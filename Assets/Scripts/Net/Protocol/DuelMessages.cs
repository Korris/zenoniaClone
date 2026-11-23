using System;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// What a client asks of the card duel (NetOp.DuelRequest). SaveDeck: Cards = card indices (CardTable order), at most 8, replaces the saved deck. Place: A = index in the player's hand, B = cell (row * 4 + column),
    /// Turn = the turn the client is looking at (moves so far + 1), Card = the card index it believes sits at that hand index: the server refuses the placement when either is stale (a timeout placement or a quick
    /// opponent changed the hand under the tap). Forfeit: leaves the running duel (the opponent wins). Challenge: Other = the player id (the stakes are the server's rule: every casual PvP duel stakes both decks);
    /// CancelInvite (inviter), Accept, Decline (receiver); LeaveLobby: backs out of the invite. QueueRanked / LeaveQueue: join / leave the Ranked waiting line (the server checks level, deck and map; a client never says its rating). TakeCard: A = card index, the winner's pick from the loser's deck. A client never describes a card, a hand or a result.
    /// </summary>
    public enum DuelOp : byte { SaveDeck = 0, Place = 1, Forfeit = 2, Challenge = 3, CancelInvite = 4, Accept = 5, Decline = 6, TakeCard = 7, LeaveLobby = 8, QueueRanked = 9, LeaveQueue = 10 }

    public enum DuelNoticeKind : byte { Refused = 0, DeckSaved = 1 }

    /// <summary>Why a request was refused (DuelNoticeMsg.Detail). DeckInvalid carries the DeckError in Extra.</summary>
    public enum DuelFail : byte
    {
        None = 0, Busy = 1, NotSafe = 2, Combat = 3, Dead = 4, Trading = 5, DeckInvalid = 6, NoMatch = 7, NotYourTurn = 8, BadMove = 9, Disabled = 10, InDuel = 11,
        NoTarget = 12, TargetBusy = 13, InviteCooldown = 14, NoInvite = 15, NoLobby = 16, Declined = 17, Cancelled = 18, Expired = 19, OpponentLeft = 20,
        WagerLevel = 21, WagerDaily = 22, ApiBusy = 23, WagerRefused = 24, StaleMove = 25, SelfInvite = 26, TargetNotReady = 27, NoPick = 28, AccountAge = 29, SameAccount = 30, RankedLevel = 31, NoStakes = 32
    }

    public static class DuelFailInfo { public const DuelFail Last = DuelFail.NoStakes; }

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
                if (op > (byte)DuelOp.LeaveQueue || n > MaxDeck) return false;
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
    /// WagerState = the card stakes: 0 none, 1 both decks held, 2 the winner is picking, 3 settling, 4 paid, 5 failed. PickedCard = the card the winner took (NoCard = none yet / a draw), PickAtRandom = the 30 s ran out,
    /// PickMsLeft = the winner's time left, PickCards = the loser's 8 staked cards, sent only to the winner while picking. OppAwayMs = how long the opponent has been disconnected (0 = connected); at 60 s they forfeit.
    /// Ranked = a Ranked match (no cards at stake); once it is over LpChange = the league points gained / lost and RankNotCounted = this match did not count (the same pair too often today, so LpChange is 0).
    /// </summary>
    public struct DuelStateMsg
    {
        public const int MaxMoves = 16, MaxHand = 5, MaxCardIndex = 63, MaxPick = 8;
        public const byte NoCard = 255;
        public byte Result, EndReason, WagerState, PickedCard;
        public bool YourTurn, PickAtRandom, Ranked, RankNotCounted;
        public short LpChange;
        public ushort MsLeft, OppAwayMs, PickMsLeft;
        public string OppName;
        public byte[] MoveCard, MoveCell, MoveSide, MoveAuto;
        public ushort[] MoveFlips;
        public byte[] Hand, PickCards;

        public int MoveCount { get { return MoveCard == null ? 0 : MoveCard.Length; } }

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.DuelState);
            w.Put(Result); w.Put(EndReason); w.Put((byte)((YourTurn ? 1 : 0) | (PickAtRandom ? 2 : 0) | (Ranked ? 4 : 0) | (RankNotCounted ? 8 : 0))); w.Put(MsLeft); w.Put(LpChange);
            w.Put(WagerState); w.Put(PickedCard); w.Put(PickMsLeft); w.Put(OppAwayMs);
            w.Put(OppName ?? "", NetProtocol.MaxNameLength);
            int n = Math.Min(MoveCount, MaxMoves);
            w.Put((byte)n);
            for (int i = 0; i < n; i++) { w.Put(MoveCard[i]); w.Put((byte)(MoveCell[i] | (MoveSide[i] << 4) | (MoveAuto[i] << 5))); w.Put(MoveFlips[i]); }
            int h = Hand == null ? 0 : Math.Min(Hand.Length, MaxHand);
            w.Put((byte)h);
            for (int i = 0; i < h; i++) w.Put(Hand[i]);
            int k = PickCards == null ? 0 : Math.Min(PickCards.Length, MaxPick);
            w.Put((byte)k);
            for (int i = 0; i < k; i++) w.Put(PickCards[i]);
        }

        public static bool TryRead(NetDataReader r, out DuelStateMsg m)
        {
            m = default(DuelStateMsg);
            try
            {
                m.Result = r.GetByte(); m.EndReason = r.GetByte(); byte flags = r.GetByte(); m.MsLeft = r.GetUShort(); m.LpChange = r.GetShort();
                m.WagerState = r.GetByte(); m.PickedCard = r.GetByte(); m.PickMsLeft = r.GetUShort(); m.OppAwayMs = r.GetUShort();
                m.OppName = r.GetString(NetProtocol.MaxNameLength);
                int n = r.GetByte();
                if (m.Result > 3 || m.EndReason > 2 || m.WagerState > 5 || flags > 15 || n > MaxMoves) return false;
                if (m.PickedCard != NoCard && m.PickedCard > MaxCardIndex) return false;
                m.YourTurn = (flags & 1) != 0; m.PickAtRandom = (flags & 2) != 0; m.Ranked = (flags & 4) != 0; m.RankNotCounted = (flags & 8) != 0;
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
                int k = r.GetByte();
                if (k > MaxPick) return false;
                m.PickCards = new byte[k];
                for (int i = 0; i < k; i++) { m.PickCards[i] = r.GetByte(); if (m.PickCards[i] > MaxCardIndex) return false; }
                return r.AvailableBytes == 0;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>
    /// Server -> client: the player's challenge lobby (NetOp.DuelLobby): Phase 0 none (closes the window), 1 an invite for you, 2 your invite is waiting, 3 starting (the server is holding the decks).
    /// Stakes = true when cards are at stake (winner takes 1 card of the loser's deck); false while the emergency switch is off. Sent on every change and when a lobby ends (Phase 0).
    /// </summary>
    public struct DuelLobbyMsg
    {
        public byte Phase;
        public bool Stakes;
        public ushort Other, MsLeft;
        public string OtherName;

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.DuelLobby);
            w.Put(Phase); w.Put(Stakes); w.Put(Other); w.Put(MsLeft); w.Put(OtherName ?? "", NetProtocol.MaxNameLength);
        }

        public static bool TryRead(NetDataReader r, out DuelLobbyMsg m)
        {
            m = default(DuelLobbyMsg);
            try
            {
                m.Phase = r.GetByte(); m.Stakes = r.GetBool();
                m.Other = r.GetUShort(); m.MsLeft = r.GetUShort(); m.OtherName = r.GetString(NetProtocol.MaxNameLength);
                if (m.Phase > 3) return false;
                return r.AvailableBytes == 0;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>
    /// Server -> client (NetOp.DuelRanked): where the player stands in the Ranked line and the season clock. Queued = in the waiting line, WaitSec = seconds waited when sent (the client counts on from there),
    /// Season = the season number, MinutesLeft = minutes until it ends (the server's clock). Sent on join, on every change of the line and when a season rolls over. The player's LP / tier / record are in the
    /// RpgState (CardRankData); the hidden MMR is never put in this message.
    /// </summary>
    public struct DuelRankedMsg
    {
        public bool Queued;
        public ushort WaitSec, Season, MinutesLeft;

        public void Write(NetDataWriter w) { w.Reset(); w.Put(NetOp.DuelRanked); w.Put(Queued); w.Put(WaitSec); w.Put(Season); w.Put(MinutesLeft); }

        public static bool TryRead(NetDataReader r, out DuelRankedMsg m)
        {
            m = default(DuelRankedMsg);
            try
            {
                m.Queued = r.GetBool(); m.WaitSec = r.GetUShort(); m.Season = r.GetUShort(); m.MinutesLeft = r.GetUShort();
                return r.AvailableBytes == 0;
            }
            catch (Exception) { return false; }
        }
    }
}
