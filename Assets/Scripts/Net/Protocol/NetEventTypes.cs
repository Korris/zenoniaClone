using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    public enum NetEventType : byte
    {
        AttackStep = 1, BoltFired = 2, BoltEnd = 3, MonsterHit = 4, HeroHit = 5, SkillCast = 6, HeroDied = 7, HeroRespawned = 8,
        ExpGain = 9, LevelUp = 10, PotionUsed = 11, AppearanceChanged = 12, Toast = 13, BossSpawned = 14, BossDied = 15,
    }

    /// <summary>Hero paper-doll data replicated to every viewer: weapon / offhand tier, enhance glow band and flag bits (1 = mechanic blue).</summary>
    public struct NetAppearance
    {
        public byte WeaponTier, OffTier, EnhanceBand, Flags;
        public void Write(NetDataWriter w) { w.Put(WeaponTier); w.Put(OffTier); w.Put(EnhanceBand); w.Put(Flags); }
        public static NetAppearance Read(NetDataReader r) { return new NetAppearance { WeaponTier = r.GetByte(), OffTier = r.GetByte(), EnhanceBand = r.GetByte(), Flags = r.GetByte() }; }
    }

    /// <summary>MonsterHit flag bits.</summary>
    public static class HitFlags { public const byte Crit = 1, Finisher = 2, Quiet = 4; }

    /// <summary>
    /// One gameplay event (reliable, AOI filtered). A single union struct: every type uses a subset of the fields (see Write); the batch
    /// carries the server time so clients play events at render time.
    /// </summary>
    public struct NetEvent
    {
        public NetEventType Type;
        /// <summary>Subject entity: hero / monster / bolt id depending on Type.</summary>
        public ushort Id;
        /// <summary>Second entity: source hero (MonsterHit), source monster (HeroHit), owner hero (BoltFired), target (SkillCast).</summary>
        public ushort Other;
        public ushort Dmg, HpLeft;
        public short X, Y;
        public byte Step, Facing, Angle, Flags;
        public sbyte KnockX, KnockY;
        public uint Value;
        public NetAppearance Look;
        public string Text;

        public void Write(NetDataWriter w)
        {
            w.Put((byte)Type);
            switch (Type)
            {
                case NetEventType.AttackStep: w.Put(Id); w.Put(Step); w.Put(Facing); break;
                case NetEventType.BoltFired: w.Put(Id); w.Put(Other); w.Put(Step); w.Put(X); w.Put(Y); w.Put(Angle); w.Put(Flags); break;
                case NetEventType.BoltEnd: w.Put(Id); w.Put(X); w.Put(Y); w.Put(Flags); break;
                case NetEventType.MonsterHit: w.Put(Id); w.Put(Other); w.Put(Dmg); w.Put(HpLeft); w.Put(Flags); w.Put(KnockX); w.Put(KnockY); break;
                case NetEventType.HeroHit: w.Put(Id); w.Put(Dmg); w.Put(Other); break;
                case NetEventType.SkillCast: w.Put(Id); w.Put(Step); w.Put(X); w.Put(Y); w.Put(Facing); w.Put(Other); break;
                case NetEventType.HeroDied:
                case NetEventType.HeroRespawned: w.Put(Id); w.Put(X); w.Put(Y); break;
                case NetEventType.ExpGain: w.Put(Value); w.Put(Step); break;
                case NetEventType.LevelUp: w.Put(Id); w.Put(Step); break;
                case NetEventType.PotionUsed: w.Put(Id); w.Put(Step); break;
                case NetEventType.AppearanceChanged: w.Put(Id); Look.Write(w); break;
                case NetEventType.Toast: w.Put(Text ?? "", NetProtocol.MaxTextLength); w.Put(Value); break;
                case NetEventType.BossSpawned:
                case NetEventType.BossDied: w.Put(Id); break;
            }
        }

        /// <summary>Reads one event; returns false for an unknown type (the rest of the batch is then unreadable).</summary>
        public static bool TryRead(NetDataReader r, out NetEvent e)
        {
            e = new NetEvent { Type = (NetEventType)r.GetByte() };
            switch (e.Type)
            {
                case NetEventType.AttackStep: e.Id = r.GetUShort(); e.Step = r.GetByte(); e.Facing = r.GetByte(); break;
                case NetEventType.BoltFired: e.Id = r.GetUShort(); e.Other = r.GetUShort(); e.Step = r.GetByte(); e.X = r.GetShort(); e.Y = r.GetShort(); e.Angle = r.GetByte(); e.Flags = r.GetByte(); break;
                case NetEventType.BoltEnd: e.Id = r.GetUShort(); e.X = r.GetShort(); e.Y = r.GetShort(); e.Flags = r.GetByte(); break;
                case NetEventType.MonsterHit: e.Id = r.GetUShort(); e.Other = r.GetUShort(); e.Dmg = r.GetUShort(); e.HpLeft = r.GetUShort(); e.Flags = r.GetByte(); e.KnockX = r.GetSByte(); e.KnockY = r.GetSByte(); break;
                case NetEventType.HeroHit: e.Id = r.GetUShort(); e.Dmg = r.GetUShort(); e.Other = r.GetUShort(); break;
                case NetEventType.SkillCast: e.Id = r.GetUShort(); e.Step = r.GetByte(); e.X = r.GetShort(); e.Y = r.GetShort(); e.Facing = r.GetByte(); e.Other = r.GetUShort(); break;
                case NetEventType.HeroDied:
                case NetEventType.HeroRespawned: e.Id = r.GetUShort(); e.X = r.GetShort(); e.Y = r.GetShort(); break;
                case NetEventType.ExpGain: e.Value = r.GetUInt(); e.Step = r.GetByte(); break;
                case NetEventType.LevelUp: e.Id = r.GetUShort(); e.Step = r.GetByte(); break;
                case NetEventType.PotionUsed: e.Id = r.GetUShort(); e.Step = r.GetByte(); break;
                case NetEventType.AppearanceChanged: e.Id = r.GetUShort(); e.Look = NetAppearance.Read(r); break;
                case NetEventType.Toast: e.Text = r.GetString(NetProtocol.MaxTextLength); e.Value = r.GetUInt(); break;
                case NetEventType.BossSpawned:
                case NetEventType.BossDied: e.Id = r.GetUShort(); break;
                default: return false;
            }
            return true;
        }
    }
}
