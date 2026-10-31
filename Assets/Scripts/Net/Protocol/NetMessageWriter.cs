using System.Collections.Generic;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    public struct HelloMsg { public ushort Version; public string Name; public byte HeroClass; public string Token; }
    public struct InvActionMsg { public uint ReqId; public InvOp Op; public string ItemUid; public byte Slot; }
    public struct WelcomeMsg { public ushort PlayerId, HeroNetId; public byte Epoch; public string MapId; public uint ServerTimeMs; public byte TickRate, SnapDiv; public bool PortalsLive; }
    /// <summary>
    /// The server moved this player's hero to another zone. Epoch counts the player's zone changes: snapshots and input packets carry it, so neither side ever acts on a packet of
    /// the zone before. Map ids travel as text: a new map needs no number assigned, and a client too old to know it says so instead of showing another map.
    /// </summary>
    public struct ZoneChangedMsg { public byte Epoch; public ushort HeroNetId; public string MapId; public short X, Y; public byte Facing; }
    public struct RejectMsg { public RejectReason Reason; public string Text; }
    public struct DespawnMsg { public ushort Id; public DespawnCause Cause; }
    /// <summary>One buff on the player's own hero: Source = the skill it came from (the client picks the icon), times in ms.</summary>
    public struct NetBuff { public byte Source, Kind; public ushort LeftMs, TotalMs; }
    public struct SelfStateMsg { public ushort Hp, MaxHp, Mp, MaxMp; public uint Exp; public byte Level; public ushort[] SkillCds; public NetBuff[] Buffs; }
    public struct RpgStateMsg { public uint ReqId; public byte Result; public string Text, SaveJson; }

    /// <summary>
    /// Serialises every message into a (reset) LiteNetLib writer: opcode first, little endian. Strings are length capped. Snapshots go through
    /// SnapshotPacker, Spawn through SpawnMsg.Write. The writer is reused by the caller, so one packet = one Write call.
    /// </summary>
    public static class NetMessageWriter
    {
        public static void Hello(NetDataWriter w, HelloMsg m)
        {
            w.Reset(); w.Put(NetOp.Hello); w.Put(m.Version); w.Put(m.Name ?? "", NetProtocol.MaxNameLength); w.Put(m.HeroClass); w.Put(m.Token ?? "", NetProtocol.MaxTokenLength);
        }

        /// <summary>
        /// Input packet (13 B for 3 frames): zone epoch, ack (low 16 bits of the newest decoded snapshot seq), seq of the newest frame (low 16 bits), count,
        /// then the frame bits oldest first. Frames are consecutive, so only the newest seq travels.
        /// </summary>
        public static void Input(NetDataWriter w, byte epoch, ushort ackSnapshotSeq, InputFrame[] frames, int count)
        {
            if (count > NetProtocol.MaxInputFrames) count = NetProtocol.MaxInputFrames;
            w.Reset(); w.Put(NetOp.Input); w.Put(epoch); w.Put(ackSnapshotSeq); w.Put((ushort)frames[count - 1].Seq); w.Put((byte)count);
            for (int i = 0; i < count; i++) w.Put(frames[i].Bits);
        }

        public static void InvAction(NetDataWriter w, InvActionMsg m)
        {
            w.Reset(); w.Put(NetOp.InvAction); w.Put(m.ReqId); w.Put((byte)m.Op); w.Put(m.ItemUid ?? "", NetProtocol.MaxItemUidLength); w.Put(m.Slot);
        }

        public static void Revive(NetDataWriter w) { w.Reset(); w.Put(NetOp.Revive); }

        public static void Welcome(NetDataWriter w, WelcomeMsg m)
        {
            w.Reset(); w.Put(NetOp.Welcome); w.Put(m.PlayerId); w.Put(m.HeroNetId); w.Put(m.Epoch); w.Put(m.MapId ?? "", NetProtocol.MaxMapIdLength); w.Put(m.ServerTimeMs); w.Put(m.TickRate); w.Put(m.SnapDiv); w.Put(m.PortalsLive);
        }

        public static void ZoneChanged(NetDataWriter w, ZoneChangedMsg m)
        {
            w.Reset(); w.Put(NetOp.ZoneChanged); w.Put(m.Epoch); w.Put(m.HeroNetId); w.Put(m.MapId ?? "", NetProtocol.MaxMapIdLength); w.Put(m.X); w.Put(m.Y); w.Put(m.Facing);
        }

        public static void Reject(NetDataWriter w, RejectMsg m)
        {
            w.Reset(); w.Put(NetOp.Reject); w.Put((byte)m.Reason); w.Put(m.Text ?? "", NetProtocol.MaxTextLength);
        }

        public static void Despawn(NetDataWriter w, DespawnMsg m)
        {
            w.Reset(); w.Put(NetOp.Despawn); w.Put(m.Id); w.Put((byte)m.Cause);
        }

        public static void SelfState(NetDataWriter w, SelfStateMsg m)
        {
            w.Reset(); w.Put(NetOp.SelfState); w.Put(m.Hp); w.Put(m.MaxHp); w.Put(m.Mp); w.Put(m.MaxMp); w.Put(m.Exp); w.Put(m.Level); for (int i = 0; i < OwnerSim.CdSlots; i++) w.Put(m.SkillCds != null && i < m.SkillCds.Length ? m.SkillCds[i] : (ushort)0);
            int buffs = m.Buffs != null ? System.Math.Min(m.Buffs.Length, NetProtocol.MaxSelfBuffs) : 0;
            w.Put((byte)buffs);
            for (int i = 0; i < buffs; i++) { w.Put(m.Buffs[i].Source); w.Put(m.Buffs[i].Kind); w.Put(m.Buffs[i].LeftMs); w.Put(m.Buffs[i].TotalMs); }
        }

        public static void RpgState(NetDataWriter w, RpgStateMsg m)
        {
            w.Reset(); w.Put(NetOp.RpgState); w.Put(m.ReqId); w.Put(m.Result); w.Put(m.Text ?? "", NetProtocol.MaxTextLength); w.Put(m.SaveJson ?? "", NetProtocol.MaxSaveJsonLength);
        }

        /// <summary>Writes the first `count` (max 64) events; returns how many were written so the caller can send the rest in the next batch.</summary>
        public static int EventBatch(NetDataWriter w, uint serverTimeMs, IList<NetEvent> events, int start, int maxCount = NetProtocol.MaxEventsPerBatch)
        {
            int n = System.Math.Min(maxCount, System.Math.Min(NetProtocol.MaxEventsPerBatch, events.Count - start));
            if (n < 0) n = 0;
            w.Reset(); w.Put(NetOp.EventBatch); w.Put(serverTimeMs); w.Put((byte)n);
            for (int i = 0; i < n; i++) events[start + i].Write(w);
            return n;
        }
    }
}
