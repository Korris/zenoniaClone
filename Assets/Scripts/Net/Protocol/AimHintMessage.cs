using System;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// Client -> server (NetOp.AimHint, reliable, only when it changes): the monster the player clicked, so the zone server aims the hero's ranged combo
    /// bolts at it instead of the nearest one in front (RangedAimRules.Pick preferIndex). MonsterId = the monster's NetId, 0 = no pick (clicked elsewhere).
    /// Epoch = the zone epoch the id belongs to (ids are per zone: a hint of the zone before is dropped). 4 B + header.
    /// </summary>
    public struct AimHintMsg
    {
        public byte Epoch;
        public ushort MonsterId;

        public void Write(NetDataWriter w) { w.Reset(); w.Put(NetOp.AimHint); w.Put(Epoch); w.Put(MonsterId); }

        public static bool TryRead(NetDataReader r, out AimHintMsg m)
        {
            m = default(AimHintMsg);
            try { m.Epoch = r.GetByte(); m.MonsterId = r.GetUShort(); return true; }
            catch (Exception) { return false; }
        }
    }
}
