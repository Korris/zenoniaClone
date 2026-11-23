using LiteNetLib;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>The client's end of the card duel: requests go up as they are; DuelState / DuelNotice come back through DuelClient. The server decides everything.</summary>
    public sealed partial class NetClient
    {
        public static void SendDuel(DuelRequestMsg m)
        {
            NetClient c = Active;
            if (c == null || c.server == null || c.State != ClientState.Joined) return;
            m.Write(c.w);
            c.server.Send(c.w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }
    }
}
