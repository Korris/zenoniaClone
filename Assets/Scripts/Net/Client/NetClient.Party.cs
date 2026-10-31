using LiteNetLib;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>The client's end of the party: requests go up as they are, and what comes back changes PartyClientState and shows as a toast. The server decides everything.</summary>
    public sealed partial class NetClient
    {
        static readonly Color PartyGood = new Color(0.7f, 1f, 0.7f), PartyBad = new Color(1f, 0.7f, 0.5f);

        /// <summary>This client's party and open invite (empty when not joined).</summary>
        public static readonly PartyClientState Party = new PartyClientState();

        public static void SendParty(PartyOp op, ushort target = 0)
        {
            NetClient c = Active;
            if (c == null || c.server == null || c.State != ClientState.Joined) return;
            new PartyRequestMsg { Op = op, Target = target }.Write(c.w);
            c.server.Send(c.w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        void OnPartyNotice(PartyNoticeMsg m)
        {
            Party.Apply(m, ClientMs);
            stats.Add("partyNoticesSeen", 1);
            string text = PartyText.Of(m);
            if (string.IsNullOrEmpty(text)) return;
            Debug.Log("[ZenoniaProto] party: " + text);
            if (NetMode.RendersFx) RpgHud.Toast(text, PartyText.IsBad(m) ? PartyBad : PartyGood);
        }
    }
}
