using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// Parses packets defensively: every Try* returns false on truncated, oversized or out-of-range data instead of throwing, so a hostile
    /// packet can only cost its sender the connection (the caller disconnects on false). ReadOpcode first, then the matching Try*.
    /// </summary>
    public static class NetMessageReader
    {
        /// <summary>Reads the opcode byte; false on an empty packet.</summary>
        public static bool ReadOpcode(NetDataReader r, out byte op)
        {
            op = 0;
            if (r.AvailableBytes < 1) return false;
            op = r.GetByte();
            return true;
        }

        public static bool TryHello(NetDataReader r, out HelloMsg m)
        {
            m = default(HelloMsg);
            try
            {
                m.Version = r.GetUShort(); m.Name = r.GetString(NetProtocol.MaxNameLength); m.HeroClass = r.GetByte(); m.Token = r.GetString(NetProtocol.MaxTokenLength);
                return true;
            }
            catch (Exception) { return false; }
        }

        /// <summary>Fills `frames` (capacity 3); count 0 or &gt; 3 is rejected.</summary>
        /// <summary>Reads an input packet; frame Seq values are the low 16 bits (the server expands them against its own counter).</summary>
        public static bool TryInput(NetDataReader r, out ushort ackSnapshotSeq, InputFrame[] frames, out int count)
        {
            ackSnapshotSeq = 0; count = 0;
            try
            {
                ackSnapshotSeq = r.GetUShort();
                ushort newest = r.GetUShort();
                int n = r.GetByte();
                if (n < 1 || n > NetProtocol.MaxInputFrames || n > frames.Length || r.AvailableBytes < n * InputFrame.WireBytes) return false;
                for (int i = 0; i < n; i++)
                    frames[i] = new InputFrame { Seq = (ushort)(newest - (n - 1 - i)), Bits = (ushort)(r.GetUShort() & InputBits.AllMask) };
                count = n;
                return true;
            }
            catch (Exception) { return false; }
        }

        public static bool TryInvAction(NetDataReader r, out InvActionMsg m)
        {
            m = default(InvActionMsg);
            try
            {
                m.ReqId = r.GetUInt(); byte op = r.GetByte(); m.ItemUid = r.GetString(NetProtocol.MaxItemUidLength); m.Slot = r.GetByte();
                if (op > (byte)InvOp.SkillRespec) return false;
                m.Op = (InvOp)op;
                return true;
            }
            catch (Exception) { return false; }
        }

        public static bool TryWelcome(NetDataReader r, out WelcomeMsg m)
        {
            m = default(WelcomeMsg);
            try
            {
                m.PlayerId = r.GetUShort(); m.HeroNetId = r.GetUShort(); m.ZoneId = r.GetByte(); m.MapId = r.GetByte(); m.ServerTimeMs = r.GetUInt(); m.TickRate = r.GetByte(); m.SnapDiv = r.GetByte();
                return m.TickRate > 0 && m.SnapDiv > 0;
            }
            catch (Exception) { return false; }
        }

        public static bool TryReject(NetDataReader r, out RejectMsg m)
        {
            m = default(RejectMsg);
            try
            {
                byte reason = r.GetByte(); m.Text = r.GetString(NetProtocol.MaxTextLength);
                if (reason > (byte)RejectReason.AlreadyOnline) return false;
                m.Reason = (RejectReason)reason;
                return true;
            }
            catch (Exception) { return false; }
        }

        public static bool TryDespawn(NetDataReader r, out DespawnMsg m)
        {
            m = default(DespawnMsg);
            try
            {
                m.Id = r.GetUShort(); byte cause = r.GetByte();
                if (cause > (byte)DespawnCause.Logout) return false;
                m.Cause = (DespawnCause)cause;
                return true;
            }
            catch (Exception) { return false; }
        }

        public static bool TrySelfState(NetDataReader r, out SelfStateMsg m)
        {
            m = default(SelfStateMsg);
            try
            {
                m.Hp = r.GetUShort(); m.MaxHp = r.GetUShort(); m.Mp = r.GetUShort(); m.MaxMp = r.GetUShort(); m.Exp = r.GetUInt(); m.Level = r.GetByte();
                m.SkillCds = new ushort[OwnerSim.CdSlots];
                for (int i = 0; i < m.SkillCds.Length; i++) m.SkillCds[i] = r.GetUShort();
                return true;
            }
            catch (Exception) { return false; }
        }

        public static bool TryRpgState(NetDataReader r, out RpgStateMsg m)
        {
            m = default(RpgStateMsg);
            try
            {
                m.ReqId = r.GetUInt(); m.Result = r.GetByte(); m.Text = r.GetString(NetProtocol.MaxTextLength); m.SaveJson = r.GetString(NetProtocol.MaxSaveJsonLength);
                return true;
            }
            catch (Exception) { return false; }
        }

        /// <summary>Appends the batch's events to `into`; false (and `into` possibly partly filled) on bad data.</summary>
        public static bool TryEventBatch(NetDataReader r, out uint serverTimeMs, List<NetEvent> into)
        {
            serverTimeMs = 0;
            try
            {
                serverTimeMs = r.GetUInt();
                int n = r.GetByte();
                if (n > NetProtocol.MaxEventsPerBatch) return false;
                for (int i = 0; i < n; i++)
                {
                    NetEvent e;
                    if (!NetEvent.TryRead(r, out e)) return false;
                    into.Add(e);
                }
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
