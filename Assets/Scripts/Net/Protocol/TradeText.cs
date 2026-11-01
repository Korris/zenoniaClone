namespace ZenoniaProto.Net
{
    /// <summary>The toast for each trade notice. Empty = no toast (the windows speak for themselves).</summary>
    public static class TradeText
    {
        /// <summary>What a notice with no name says instead. A refusal of your own gold offer names nobody, so that case reads "You" (OfFail).</summary>
        const string NoName = "That player";

        /// <summary>True when the notice is bad news for the reader (drawn in the warning colour).</summary>
        public static bool IsBad(TradeNoticeMsg m)
        {
            switch (m.Kind)
            {
                case TradeNoticeKind.Failed: return true;
                case TradeNoticeKind.OutClosed:
                case TradeNoticeKind.InClosed: return m.Detail != (byte)TradeRequestEnd.Accepted;
                case TradeNoticeKind.Ended: return m.Detail != (byte)TradeEnd.Completed;
                default: return false;
            }
        }

        /// <summary>The line of the warning banner in the trade window.</summary>
        public static string Banner(TradeChange c, string name)
        {
            switch (c)
            {
                case TradeChange.YouChangedCleared:
                case TradeChange.TheyChangedCleared: return "Offer changed: both locks cleared";
                case TradeChange.YouUnlocked:
                case TradeChange.TheyUnlocked: return "Unlocked: both locks cleared";
                default: return (string.IsNullOrEmpty(name) ? "The other player" : name) + " changed their offer";
            }
        }

        public static string Of(TradeNoticeMsg m)
        {
            string name = string.IsNullOrEmpty(m.Name) ? NoName : m.Name;
            switch (m.Kind)
            {
                case TradeNoticeKind.RequestOut: return "Trade request sent to " + name;
                case TradeNoticeKind.RequestIn: return "";
                case TradeNoticeKind.OutClosed:
                    switch ((TradeRequestEnd)m.Detail)
                    {
                        case TradeRequestEnd.Accepted: return "";
                        case TradeRequestEnd.Declined: return name + " declined your trade request";
                        case TradeRequestEnd.Cancelled: return "Trade request to " + name + " cancelled";
                        case TradeRequestEnd.Expired: return name + " did not answer your trade request";
                        case TradeRequestEnd.Apart: return name + " is out of reach, trade request cancelled";
                        default: return name + " went offline, trade request cancelled";
                    }
                case TradeNoticeKind.InClosed:
                    switch ((TradeRequestEnd)m.Detail)
                    {
                        case TradeRequestEnd.Accepted: return "";
                        case TradeRequestEnd.Declined: return "You declined " + name + "'s trade request";
                        case TradeRequestEnd.Cancelled: return name + " cancelled the trade request";
                        case TradeRequestEnd.Expired: return "The trade request from " + name + " expired";
                        case TradeRequestEnd.Apart: return name + " is out of reach, trade request cancelled";
                        default: return name + " went offline, trade request cancelled";
                    }
                case TradeNoticeKind.Failed: return OfFail((TradeFail)m.Detail, name);
                case TradeNoticeKind.Changed: return "";
                case TradeNoticeKind.Ended:
                    switch ((TradeEnd)m.Detail)
                    {
                        case TradeEnd.Completed: return "Trade with " + name + " complete";
                        case TradeEnd.YouCancelled: return "You cancelled the trade";
                        case TradeEnd.TheyCancelled: return name + " cancelled the trade";
                        case TradeEnd.TooFar: return "Trade cancelled: too far from " + name;
                        case TradeEnd.MapChanged: return "Trade cancelled: you are no longer on the same map";
                        case TradeEnd.Died: return "Trade cancelled: someone was defeated";
                        default: return "Trade cancelled: " + name + " went offline";
                    }
                default: return "";
            }
        }

        static string OfFail(TradeFail f, string name)
        {
            switch (f)
            {
                case TradeFail.NoSuchPlayer: return "That player is not online";
                case TradeFail.Self: return "You cannot trade with yourself";
                case TradeFail.TargetBusy: return name + " is busy with another trade";
                case TradeFail.AlreadyTrading: return "You are already trading";
                case TradeFail.AlreadyPending: return "Your trade request to " + name + " is still waiting";
                case TradeFail.AnswerFirst: return "Answer your trade request first";
                case TradeFail.NoRequest: return "That trade request is no longer open";
                case TradeFail.TooFar: return "Too far away to trade";
                case TradeFail.OtherMap: return "That player is on another map";
                case TradeFail.Dead: return "Cannot trade while defeated";
                case TradeFail.NotTrading: return "You are not trading";
                case TradeFail.NotOwned: return "That item is not in your bag";
                case TradeFail.Equipped: return "Take that item off before you trade it";
                case TradeFail.Starter: return "Starter gear cannot be traded";
                case TradeFail.DuplicateItem: return "That item is already in your offer";
                case TradeFail.OfferFull: return "An offer holds at most " + TradeBook.MaxItems + " items";
                case TradeFail.NotOffered: return "That item is not in your offer";
                case TradeFail.BadGold: return "That is not a valid gold amount";
                case TradeFail.NotEnoughGold: return name == NoName ? "You do not have that much gold" : name + " does not have that much gold";
                case TradeFail.GoldOverflow: return name + " cannot carry that much gold";
                case TradeFail.NoRoom: return name + "'s bag has no room for this trade";
                case TradeFail.ItemGone: return name + " no longer has an offered item";
                case TradeFail.UidClash: return name + " already has an item with the same id: trade refused";
                case TradeFail.AlreadyLocked: return "Your offer is already locked";
                case TradeFail.NotLocked: return "Your offer is not locked";
                case TradeFail.NotBothLocked: return "Both offers must be locked first";
                case TradeFail.AlreadyConfirmed: return "You have already confirmed";
                case TradeFail.StaleOffer: return "The offers just changed: check them again";
                case TradeFail.EmptyTrade: return "Add an item or gold first";
                case TradeFail.Committing: return "The trade is being saved";
                case TradeFail.SaveFailed: return "The trade could not be saved: nothing moved";
                default: return "";
            }
        }
    }
}
