namespace ZenoniaProto.Net
{
    /// <summary>Wire opcodes (first byte of every packet) and protocol-wide constants. Client -> server 1..9, server -> client 10..29.</summary>
    public static class NetOp
    {
        public const byte Hello = 1, Input = 2, InvAction = 3, Revive = 4;
        public const byte Welcome = 10, Reject = 11, Spawn = 12, Despawn = 13, Snapshot = 14, SelfState = 15, RpgState = 17, EventBatch = 20;
    }

    /// <summary>Protocol constants shared by server and clients (bump Version on any wire change).</summary>
    public static class NetProtocol
    {
        public const ushort Version = 2;
        public const int MaxNameLength = 16, MaxTokenLength = 64, MaxTextLength = 120, MaxItemUidLength = 40, MaxSaveJsonLength = 60000;
        /// <summary>Largest unfragmented snapshot: LiteNetLib refuses an unreliable packet over 1023 B (MTU 1024), so 1000 B.</summary>
        public const int SnapshotMaxBytes = 1000;
        public const int MaxInputFrames = 5, MaxEventsPerBatch = 64, MaxSnapshotEntities = 255;
        public const int InputStaleMs = 250;
        /// <summary>LiteNetLib channels: 0 unreliable, 1 reliable ordered gameplay, 2 reliable ordered chat (P5).</summary>
        public const byte ChannelUnreliable = 0, ChannelReliable = 1, ChannelChat = 2;
    }

    public enum RejectReason : byte { Full = 0, Version = 1, BadToken = 2, AlreadyOnline = 3 }

    public enum InvOp : byte { Equip = 0, Unequip = 1, Use = 2, Discard = 3, Sort = 4, Enhance = 5 }
}
