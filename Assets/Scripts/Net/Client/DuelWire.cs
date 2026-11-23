using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>DuelView (what the UI reads) to and from the DuelStateMsg bytes. A straight copy of the fields: the board, score and log are derived from the moves on each end, never sent.</summary>
    public static class DuelWire
    {
        public static DuelStateMsg ToMsg(DuelView v)
        {
            int n = v.Moves.Count;
            var m = new DuelStateMsg
            {
                Result = (byte)v.Result, EndReason = (byte)v.EndReason, YourTurn = v.YourTurn, MsLeft = (ushort)System.Math.Min(60000, v.MsLeft),
                OppName = v.OppName, WagerState = (byte)v.WagerState, OppAwayMs = (ushort)System.Math.Min(60000, v.OppAwayMs),
                PickedCard = (byte)v.PickedCard, PickMsLeft = (ushort)System.Math.Min(60000, v.PickMsLeft), PickAtRandom = v.PickedAtRandom,
                Ranked = v.Ranked, RankNotCounted = v.RankNotCounted, LpChange = (short)System.Math.Max(-999, System.Math.Min(999, v.LpChange)),
                MoveCard = new byte[n], MoveCell = new byte[n], MoveSide = new byte[n], MoveAuto = new byte[n], MoveFlips = new ushort[n], Hand = new byte[v.Hand.Count], PickCards = new byte[v.PickCards.Count],
            };
            for (int i = 0; i < n; i++)
            {
                DuelMove mv = v.Moves[i];
                m.MoveCard[i] = (byte)mv.Card; m.MoveCell[i] = (byte)mv.Cell; m.MoveSide[i] = (byte)mv.Side; m.MoveAuto[i] = (byte)(mv.Auto ? 1 : 0); m.MoveFlips[i] = (ushort)mv.FlipMask;
            }
            for (int i = 0; i < v.Hand.Count; i++) m.Hand[i] = (byte)v.Hand[i];
            for (int i = 0; i < v.PickCards.Count; i++) m.PickCards[i] = (byte)v.PickCards[i];
            return m;
        }

        public static DuelView ToView(DuelStateMsg m)
        {
            var v = new DuelView
            {
                Result = (DuelResult)m.Result, EndReason = m.EndReason, YourTurn = m.YourTurn, MsLeft = m.MsLeft, OppName = m.OppName ?? "",
                WagerState = m.WagerState, OppAwayMs = m.OppAwayMs, PickedCard = m.PickedCard, PickMsLeft = m.PickMsLeft, PickedAtRandom = m.PickAtRandom,
                Ranked = m.Ranked, RankNotCounted = m.RankNotCounted, LpChange = m.LpChange,
            };
            for (int i = 0; i < m.MoveCount; i++)
                v.Moves.Add(new DuelMove { Turn = i + 1, Side = m.MoveSide[i], Card = m.MoveCard[i], Cell = m.MoveCell[i], FlipMask = m.MoveFlips[i], Auto = m.MoveAuto[i] != 0 });
            if (m.Hand != null) foreach (byte c in m.Hand) v.Hand.Add(c);
            if (m.PickCards != null) foreach (byte c in m.PickCards) v.PickCards.Add(c);
            return v;
        }
    }
}
