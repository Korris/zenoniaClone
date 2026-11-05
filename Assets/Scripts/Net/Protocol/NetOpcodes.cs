namespace ZenoniaProto.Net
{
    /// <summary>Wire opcodes (first byte of every packet) and protocol-wide constants. Client -> server 1..9, server -> client 10..29.</summary>
    public static class NetOp
    {
        public const byte Hello = 1, Input = 2, InvAction = 3, Revive = 4, PartyRequest = 5, TradeRequest = 6;
        public const byte Welcome = 10, Reject = 11, Spawn = 12, Despawn = 13, Snapshot = 14, SelfState = 15, RpgState = 17, ZoneChanged = 18, EventBatch = 20, PartyNotice = 21, PartyRoster = 22, TradeNotice = 23, TradeState = 24;
    }

    /// <summary>Protocol constants shared by server and clients (bump Version on any wire change).</summary>
    public static class NetProtocol
    {
        public const ushort Version = 14; // 14: Thatchwick Village + Willowbrook Meadows maps (an older client has no data for them)
        public const int MaxSelfBuffs = 8;
        public const int MaxNameLength = 16, MaxTokenLength = 64, MaxTextLength = 120, MaxItemUidLength = 40, MaxSaveJsonLength = 60000, MaxMapIdLength = 32;
        /// <summary>Largest unfragmented snapshot: LiteNetLib refuses an unreliable packet over 1023 B (MTU 1024), so 1000 B.</summary>
        public const int SnapshotMaxBytes = 1000;
        public const int MaxInputFrames = 5, MaxEventsPerBatch = 64, MaxSnapshotEntities = 255;
        public const int InputStaleMs = 250;
        /// <summary>LiteNetLib channels: 0 unreliable, 1 reliable ordered gameplay, 2 reliable ordered chat (P5).</summary>
        public const byte ChannelUnreliable = 0, ChannelReliable = 1, ChannelChat = 2;
    }

    public enum RejectReason : byte { Full = 0, Version = 1, BadToken = 2, AlreadyOnline = 3, Unavailable = 4 }

    public enum InvOp : byte { Equip = 0, Unequip = 1, Use = 2, Discard = 3, Sort = 4, Enhance = 5, SkillLearn = 6, SkillAssign = 7, SkillRespec = 8, PotionBuy = 9, PetBuy = 10, QuestAccept = 11, QuestTurnIn = 12 }
}
