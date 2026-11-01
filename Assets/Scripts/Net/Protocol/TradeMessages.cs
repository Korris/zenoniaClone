using System;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// What a client asks of the trade rules. Target: Request = that player; Accept / Decline = the requester (so an answer can never land on a newer request). AddItem / RemoveItem: Uid.
    /// SetGold: Gold. Lock / Confirm: Revision, the offers the client was looking at, so a press cannot count for offers that changed while it was on the way.
    /// </summary>
    public enum TradeOp : byte { Request = 0, CancelRequest = 1, Accept = 2, Decline = 3, AddItem = 4, RemoveItem = 5, SetGold = 6, Lock = 7, Unlock = 8, Confirm = 9, Cancel = 10 }

    public enum TradeNoticeKind : byte
    {
        /// <summary>You asked Name to trade: pending until OutClosed. MsLeft = time to answer.</summary>
        RequestOut = 0,
        /// <summary>Name asks you to trade: the window stays up until InClosed. MsLeft = time to answer, Detail = the requester's hero class.</summary>
        RequestIn = 1,
        /// <summary>Your request to Name ended, Detail = TradeRequestEnd.</summary>
        OutClosed = 2,
        /// <summary>The request from Name ended, Detail = TradeRequestEnd.</summary>
        InClosed = 3,
        /// <summary>Your request was refused, or the trade could not execute. Detail = TradeFail, Name = who it was about (may be empty).</summary>
        Failed = 4,
        /// <summary>An offer changed or someone unlocked, Detail = TradeChange, Name = the other player.</summary>
        Changed = 5,
        /// <summary>The trade window closes, Detail = TradeEnd, Name = the other player.</summary>
        Ended = 6,
    }

    public enum TradeRequestEnd : byte { Accepted = 0, Declined = 1, Cancelled = 2, Expired = 3, Offline = 4, Apart = 5 }

    public enum TradeEnd : byte { Completed = 0, YouCancelled = 1, TheyCancelled = 2, TooFar = 3, MapChanged = 4, Died = 5, Offline = 6 }

    public enum TradeChange : byte { TheyChanged = 0, YouChangedCleared = 1, TheyChangedCleared = 2, YouUnlocked = 3, TheyUnlocked = 4 }

    public enum TradeFail : byte
    {
        None = 0, NoSuchPlayer = 1, Self = 2, TargetBusy = 3, AlreadyTrading = 4, AlreadyPending = 5, AnswerFirst = 6, NoRequest = 7, TooFar = 8, OtherMap = 9, Dead = 10, NotTrading = 11,
        NotOwned = 12, Equipped = 13, Starter = 14, DuplicateItem = 15, OfferFull = 16, NotOffered = 17, BadGold = 18, NotEnoughGold = 19, GoldOverflow = 20, NoRoom = 21, ItemGone = 22, UidClash = 23,
        AlreadyLocked = 24, NotLocked = 25, NotBothLocked = 26, AlreadyConfirmed = 27, StaleOffer = 28, EmptyTrade = 29, Committing = 30, SaveFailed = 31,
    }

    /// <summary>Client -> server: one trade request. Only ids and amounts: a client never describes an item. A semantic refusal comes back as a notice; only an unreadable packet costs the connection.</summary>
    public struct TradeRequestMsg
    {
        public TradeOp Op;
        public ushort Target;
        public string Uid;
        public int Gold;
        public uint Revision;

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.TradeRequest); w.Put((byte)Op); w.Put(Target); w.Put(Uid ?? "", NetProtocol.MaxItemUidLength); w.Put(Gold); w.Put(Revision);
        }

        public static bool TryRead(NetDataReader r, out TradeRequestMsg m)
        {
            m = default(TradeRequestMsg);
            try
            {
                byte op = r.GetByte(); m.Target = r.GetUShort(); m.Uid = r.GetString(NetProtocol.MaxItemUidLength) ?? ""; m.Gold = r.GetInt(); m.Revision = r.GetUInt();
                if (op > (byte)TradeOp.Cancel) return false;
                m.Op = (TradeOp)op;
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>Server -> client: one thing that happened to this player's request or trade (a state change plus a toast, see TradeText).</summary>
    public struct TradeNoticeMsg
    {
        public TradeNoticeKind Kind;
        public byte Detail;
        public ushort Other;
        public string Name;
        public ushort MsLeft;

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.TradeNotice); w.Put((byte)Kind); w.Put(Detail); w.Put(Other); w.Put(Name ?? "", NetProtocol.MaxNameLength); w.Put(MsLeft);
        }

        public static bool TryRead(NetDataReader r, out TradeNoticeMsg m)
        {
            m = default(TradeNoticeMsg);
            try
            {
                byte kind = r.GetByte(); m.Detail = r.GetByte(); m.Other = r.GetUShort(); m.Name = r.GetString(NetProtocol.MaxNameLength); m.MsLeft = r.GetUShort();
                if (kind > (byte)TradeNoticeKind.Ended) return false;
                m.Kind = (TradeNoticeKind)kind;
                switch (m.Kind)
                {
                    case TradeNoticeKind.OutClosed:
                    case TradeNoticeKind.InClosed: return m.Detail <= (byte)TradeRequestEnd.Apart;
                    case TradeNoticeKind.Failed: return m.Detail <= (byte)TradeFail.SaveFailed;
                    case TradeNoticeKind.Changed: return m.Detail <= (byte)TradeChange.TheyUnlocked;
                    case TradeNoticeKind.Ended: return m.Detail <= (byte)TradeEnd.Offline;
                    default: return true;
                }
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>What the server says an offered item is, enough to draw it and judge it. Bounded on the wire; the client reads this and never sends it back.</summary>
    public struct TradeItemView
    {
        public string Uid, TemplateId, Slot, Name, Rarity, ClassId;
        public int ItemLevel, Enhance, Atk, Def, MaxHp, MaxMp, Durability;
        public float CritChance, SpeedMult;

        const int MaxShortText = 16, MaxNameText = 40;

        public void Write(NetDataWriter w)
        {
            w.Put(Uid ?? "", NetProtocol.MaxItemUidLength); w.Put(TemplateId ?? "", NetProtocol.MaxItemUidLength); w.Put(Slot ?? "", MaxShortText); w.Put(Name ?? "", MaxNameText); w.Put(Rarity ?? "", MaxShortText); w.Put(ClassId ?? "", MaxShortText);
            w.Put(ItemLevel); w.Put(Enhance); w.Put(Atk); w.Put(Def); w.Put(MaxHp); w.Put(MaxMp); w.Put(CritChance); w.Put(SpeedMult); w.Put(Durability);
        }

        public static TradeItemView Read(NetDataReader r)
        {
            return new TradeItemView
            {
                Uid = r.GetString(NetProtocol.MaxItemUidLength), TemplateId = r.GetString(NetProtocol.MaxItemUidLength), Slot = r.GetString(MaxShortText), Name = r.GetString(MaxNameText), Rarity = r.GetString(MaxShortText), ClassId = r.GetString(MaxShortText),
                ItemLevel = r.GetInt(), Enhance = r.GetInt(), Atk = r.GetInt(), Def = r.GetInt(), MaxHp = r.GetInt(), MaxMp = r.GetInt(), CritChance = r.GetFloat(), SpeedMult = r.GetFloat(), Durability = r.GetInt(),
            };
        }
    }

    /// <summary>One side of the open trade.</summary>
    public struct TradeOfferView
    {
        public TradeItemView[] Items;
        public int Gold;
        public bool Locked, Confirmed;

        public void Write(NetDataWriter w)
        {
            int n = Items != null ? Math.Min(Items.Length, TradeBook.MaxItems) : 0;
            w.Put((byte)n);
            for (int i = 0; i < n; i++) Items[i].Write(w);
            w.Put(Gold); w.Put(Locked); w.Put(Confirmed);
        }

        public static bool TryRead(NetDataReader r, out TradeOfferView o)
        {
            o = default(TradeOfferView);
            int n = r.GetByte();
            if (n > TradeBook.MaxItems) return false;
            o.Items = new TradeItemView[n];
            for (int i = 0; i < n; i++) o.Items[i] = TradeItemView.Read(r);
            o.Gold = r.GetInt(); o.Locked = r.GetBool(); o.Confirmed = r.GetBool();
            return true;
        }
    }

    /// <summary>Server -> client: the whole open trade as this player sees it, after every change. Other = who with, Revision = what a Lock or Confirm must name.</summary>
    public struct TradeStateMsg
    {
        public ushort Other;
        public string Name;
        public byte HeroClass;
        public uint Revision;
        public TradeOfferView Mine, Theirs;

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.TradeState); w.Put(Other); w.Put(Name ?? "", NetProtocol.MaxNameLength); w.Put(HeroClass); w.Put(Revision);
            Mine.Write(w); Theirs.Write(w);
        }

        public static bool TryRead(NetDataReader r, out TradeStateMsg m)
        {
            m = default(TradeStateMsg);
            try
            {
                m.Other = r.GetUShort(); m.Name = r.GetString(NetProtocol.MaxNameLength); m.HeroClass = r.GetByte(); m.Revision = r.GetUInt();
                return TradeOfferView.TryRead(r, out m.Mine) && TradeOfferView.TryRead(r, out m.Theirs);
            }
            catch (Exception) { return false; }
        }
    }
}
