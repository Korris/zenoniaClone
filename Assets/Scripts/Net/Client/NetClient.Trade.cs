using LiteNetLib;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>The client's end of a trade: requests go up as they are, and what comes back changes TradeClientState and shows as a toast, or on the window while it is open. The server decides everything.</summary>
    public sealed partial class NetClient
    {
        /// <summary>This client's open trade request and trade window (empty when not joined).</summary>
        public static readonly TradeClientState Trade = new TradeClientState();

        public static void SendTrade(TradeRequestMsg m)
        {
            NetClient c = Active;
            if (c == null || c.server == null || c.State != ClientState.Joined) return;
            m.Write(c.w);
            c.server.Send(c.w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        public static void SendTrade(TradeOp op, ushort target = 0) { SendTrade(new TradeRequestMsg { Op = op, Target = target }); }

        void OnTradeNotice(TradeNoticeMsg m)
        {
            bool windowUp = Trade.Session.HasValue && m.Kind == TradeNoticeKind.Failed;
            Trade.Apply(m, ClientMs);
            stats.Add("tradeNoticesSeen", 1);
            string text = TradeText.Of(m);
            if (string.IsNullOrEmpty(text)) return;
            Debug.Log("[ZenoniaProto] trade: " + text);
            if (!NetMode.RendersFx) return;
            if (windowUp) TradeWindow.Flash(text); // the window covers the toast strip
            else RpgHud.Toast(text, TradeText.IsBad(m) ? PartyBad : PartyGood);
        }
    }
}
