namespace ZenoniaProto.Net
{
    /// <summary>What the client knows of its own trade: the open request and the open window, only ever changed by the server's messages. The UI reads it.</summary>
    public sealed class TradeClientState
    {
        public struct OpenRequest { public ushort Other; public string Name; public double ExpiresAtMs; public byte HeroClass; }

        /// <summary>Goes up with every change: the UI redraws only then.</summary>
        public int Version { get; private set; }
        public OpenRequest? Incoming { get; private set; }
        public OpenRequest? Outgoing { get; private set; }
        /// <summary>The open trade as the server last described it; null when there is none.</summary>
        public TradeStateMsg? Session { get; private set; }
        /// <summary>Goes up every time the other side's offer changes or a lock is lost: the window raises its warning on it.</summary>
        public int WarningCount { get; private set; }
        /// <summary>What the last warning was about.</summary>
        public TradeChange LastChange { get; private set; }

        public void Clear() { Incoming = null; Outgoing = null; Session = null; Version++; }

        public void Apply(TradeNoticeMsg m, double nowMs)
        {
            Version++;
            switch (m.Kind)
            {
                case TradeNoticeKind.RequestOut: Outgoing = new OpenRequest { Other = m.Other, Name = m.Name, ExpiresAtMs = nowMs + m.MsLeft }; break;
                case TradeNoticeKind.RequestIn: Incoming = new OpenRequest { Other = m.Other, Name = m.Name, ExpiresAtMs = nowMs + m.MsLeft, HeroClass = m.Detail }; break;
                case TradeNoticeKind.OutClosed: Outgoing = null; break;
                case TradeNoticeKind.InClosed: Incoming = null; break;
                case TradeNoticeKind.Changed:
                    LastChange = (TradeChange)m.Detail;
                    if (LastChange != TradeChange.YouUnlocked) WarningCount++;
                    break;
                case TradeNoticeKind.Ended: Session = null; break;
            }
        }

        public void Apply(TradeStateMsg m)
        {
            Version++;
            Session = m;
        }
    }
}
