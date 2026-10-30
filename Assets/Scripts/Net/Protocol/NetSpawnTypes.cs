using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    public enum EntityKind : byte { Hero = 0, Monster = 1, Loot = 2 }

    public enum DespawnCause : byte { LeftAoi = 0, Died = 1, Removed = 2, PickedUp = 3, Logout = 4 }

    /// <summary>
    /// Spawn message (reliable): creates a proxy on a client. Common header + a payload chosen by Kind (union struct, unused fields stay 0):
    /// Hero = PlayerId, Name, Level, Look; Monster = Hp, MaxHp, State; Loot = Amount, ItemDefId, Rarity, Enhance, OwnerPlayerId, FreeAtMs.
    /// </summary>
    public struct SpawnMsg
    {
        public ushort Id;
        public EntityKind Kind;
        /// <summary>Hero class index / MonsterDef index / LootKind.</summary>
        public byte Type;
        public short X, Y;
        public byte Facing;
        public ushort PlayerId;
        public string Name;
        public byte Level;
        public NetAppearance Look;
        public ushort Hp, MaxHp;
        public byte State;
        public int Amount;
        public ushort ItemDefId, OwnerPlayerId;
        public byte Rarity, Enhance;
        public uint FreeAtMs;

        public void Write(NetDataWriter w)
        {
            w.Reset();
            w.Put(NetOp.Spawn); w.Put(Id); w.Put((byte)Kind); w.Put(Type); w.Put(X); w.Put(Y); w.Put(Facing);
            switch (Kind)
            {
                case EntityKind.Hero: w.Put(PlayerId); w.Put(Name ?? "", NetProtocol.MaxNameLength); w.Put(Level); Look.Write(w); break;
                case EntityKind.Monster: w.Put(Hp); w.Put(MaxHp); w.Put(State); break;
                case EntityKind.Loot: w.Put(Amount); w.Put(ItemDefId); w.Put(Rarity); w.Put(Enhance); w.Put(OwnerPlayerId); w.Put(FreeAtMs); break;
            }
        }

        /// <summary>Reads the body after the opcode.</summary>
        public static bool TryRead(NetDataReader r, out SpawnMsg m)
        {
            m = default(SpawnMsg);
            try
            {
                m.Id = r.GetUShort(); byte kind = r.GetByte(); m.Type = r.GetByte(); m.X = r.GetShort(); m.Y = r.GetShort(); m.Facing = r.GetByte();
                if (kind > (byte)EntityKind.Loot) return false;
                m.Kind = (EntityKind)kind;
                switch (m.Kind)
                {
                    case EntityKind.Hero: m.PlayerId = r.GetUShort(); m.Name = r.GetString(NetProtocol.MaxNameLength); m.Level = r.GetByte(); m.Look = NetAppearance.Read(r); break;
                    case EntityKind.Monster: m.Hp = r.GetUShort(); m.MaxHp = r.GetUShort(); m.State = r.GetByte(); break;
                    case EntityKind.Loot: m.Amount = r.GetInt(); m.ItemDefId = r.GetUShort(); m.Rarity = r.GetByte(); m.Enhance = r.GetByte(); m.OwnerPlayerId = r.GetUShort(); m.FreeAtMs = r.GetUInt(); break;
                }
                return true;
            }
            catch (System.Exception) { return false; }
        }
    }
}
