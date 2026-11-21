namespace ZenoniaProto.Net
{
    /// <summary>Wire opcodes (first byte of every packet) and protocol-wide constants. Client -> server 1..9, server -> client 10..29.</summary>
    public static class NetOp
    {
        public const byte Hello = 1, Input = 2, InvAction = 3, Revive = 4, PartyRequest = 5, TradeRequest = 6;
        public const byte Welcome = 10, Reject = 11, Spawn = 12, Despawn = 13, Snapshot = 14, SelfState = 15, RpgState = 17, ZoneChanged = 18, EventBatch = 20, PartyNotice = 21, PartyRoster = 22, TradeNotice = 23, TradeState = 24, WantedStatus = 25;
    }

    /// <summary>Protocol constants shared by server and clients (bump Version on any wire change).</summary>
    public static class NetProtocol
    {
        public const ushort Version = 26; // 26 (unreleased): InvOp ChestOpen / WaypointTouch / WaypointWarp (Rootburrow V5 chests + warp crystals; SaveData chests / waypoints ride in RpgState). 25 (unreleased): NetEventType.HeroStatus (wanted boss poison on a hero: kind + ms left, Venom Drain flash; an older client cannot read an event batch carrying one). 24 (unreleased): equipment slot 7 = headgear (cosmetic hats: RpgState save JSON, InvOp.Unequip slot 7, loot ItemDefId slot 7 with the hat id in the level bits) + GearBuy hat entries (128 + hat id). 23 (unreleased): NetAppearance.HairColor / OutfitColor / Headgear (CharacterAppearanceData, 3 more bytes in every Spawn / AppearanceChanged) + InvOp.Appearance (client stores its look on the server). 22 (unreleased): NetOp.WantedStatus (WantedStatusMsg: today's Wanted Boss alive / respawn countdown for every map; an older client would log it as a malformed packet). 21 (unreleased): pet team / growth / capture InvOps, NetEventType.PetCapturable, SaveData.petTeam. 20 (unreleased): Pet Companion + pet gacha: RpgState carries AccountJson, InvOp PetEquip / PetStarUp / GachaPull / GachaExchange, NetEventType.PetSkill; NetAppearance.PetId = PetConfig index + 1 of 8 pets. 19 (unreleased): 19 (unreleased): NetEventType.MonsterShot (ranged monsters, Cliff Harpy; an older client cannot read the event batch that carries one). 18 (unreleased): InvOp.GearBuy (basic-gear shops, GearCatalog). 17 (unreleased): bag grows 20 -> 50 slots (PlayerState.InventorySize, server-api INVENTORY_SIZE); same bytes, but a 20-slot client would cut the RpgState bag at 20. 16 (unreleased): NetAppearance.HairStyle + InvOp.HairStyle (client picks its head layer style). 15 (unreleased): NetAppearance carries armour / gloves / boots tiers (armour look). 14 (unreleased): Thatchwick Village + Willowbrook Meadows maps, meadow and Frost Hollow ice monsters appended to MonsterDef.All
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

    public enum InvOp : byte { Equip = 0, Unequip = 1, Use = 2, Discard = 3, Sort = 4, Enhance = 5, SkillLearn = 6, SkillAssign = 7, SkillRespec = 8, PotionBuy = 9, PetBuy = 10, QuestAccept = 11, QuestTurnIn = 12, HairStyle = 13, GearBuy = 14, PetEquip = 15, PetStarUp = 16, GachaPull = 17, GachaExchange = 18, PetLevelUp = 19, PetResetLevel = 20, PetResetStars = 21, PetConvertShards = 22, GachaBuyCoins = 23, BallBuy = 24, PetCapture = 25, Appearance = 26, ChestOpen = 27, WaypointTouch = 28, WaypointWarp = 29 } // ChestOpen: ItemUid = DungeonPoints chest id. WaypointTouch: ItemUid = crystal id (attune). WaypointWarp: ItemUid = "fromId>toId" (crystal stood at > target crystal or DungeonPoints.ExitId). HairStyle: Slot = the chosen style, no reply. Appearance: ItemUid = "style,hairColor,outfitColor" (CharacterAppearanceData.ToOpText; -1 = not chosen), stored with the character, no reply. GearBuy: ItemUid = seller npc id, Slot = GearShops entry index (tier * 6 + slot index), or HeadgearCatalog.ShopEntryBase (128) + hat id for a shop hat. PetBuy: ItemUid = seller, Slot = PetConfig index (unlocks it for the account). PetEquip: Slot = PetRules.EncodeSlot(team slot, PetConfig index; index 15 = clear). PetStarUp / PetLevelUp / PetResetLevel / PetResetStars: Slot = PetConfig index. PetConvertShards: Slot = index, ItemUid = how many. GachaPull: Slot = 1 or 10. GachaBuyCoins: Slot = coins. GachaExchange: 120 UR shards for the UR pet. BallBuy: ItemUid = seller, Slot = how many. PetCapture: ItemUid = the monster's NetId, Slot = 1 for a rare ball

    public static class InvOpInfo
    {
        /// <summary>The last InvOp a reader accepts.</summary>
        public const InvOp Last = InvOp.WaypointWarp;
        /// <summary>Answered later, once the account write is stored (ZoneAccountQueue), not by the immediate RpgState.</summary>
        public static bool IsAccountOp(InvOp op) { return op == InvOp.PetBuy || op == InvOp.PetStarUp || op == InvOp.GachaPull || op == InvOp.GachaExchange || op == InvOp.PetLevelUp || op == InvOp.PetResetLevel || op == InvOp.PetResetStars || op == InvOp.PetConvertShards || op == InvOp.GachaBuyCoins || op == InvOp.BallBuy; }
    }
}
