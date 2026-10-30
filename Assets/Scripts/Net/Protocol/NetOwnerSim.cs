using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// The part of a hero's simulation state that position / pose do not already carry, sent to its OWNER in every snapshot so the client can restore the
    /// server state at an acked input frame and replay its newer inputs (reconciliation). Timers are exact floats (ms): a rounded timer would let a
    /// replay cross a phase boundary one step earlier than the server did. Wire: 2 B of small enums, a 9-bit presence mask, then only the non-zero
    /// timers, 4 B each (an idle hero costs 4 B).
    /// </summary>
    public struct OwnerSim
    {
        /// <summary>0 = none, 1..8 = 45 degree steps (dash direction).</summary>
        public byte DashDir;
        /// <summary>ComboPhase 0..3 (idle / windup / active / recover).</summary>
        public byte ComboPhase, ComboStep, ComboLast;
        public bool ComboQueued;
        public float DashLeft, DashCooldown, ComboTimer, ChainTimer, Invuln, Hurt, CastLeft, SkillCd, StabLeft;

        const int FieldCount = 9;

        float Field(int i)
        {
            switch (i)
            {
                case 0: return DashLeft;
                case 1: return DashCooldown;
                case 2: return ComboTimer;
                case 3: return ChainTimer;
                case 4: return Invuln;
                case 5: return Hurt;
                case 6: return CastLeft;
                case 7: return SkillCd;
                default: return StabLeft;
            }
        }

        void SetField(int i, float v)
        {
            switch (i)
            {
                case 0: DashLeft = v; break;
                case 1: DashCooldown = v; break;
                case 2: ComboTimer = v; break;
                case 3: ChainTimer = v; break;
                case 4: Invuln = v; break;
                case 5: Hurt = v; break;
                case 6: CastLeft = v; break;
                case 7: SkillCd = v; break;
                default: StabLeft = v; break;
            }
        }

        public void Write(NetDataWriter w)
        {
            w.Put((byte)((DashDir & 15) | ((ComboPhase & 3) << 4) | (ComboQueued ? 64 : 0)));
            w.Put((byte)((ComboStep & 15) | ((ComboLast & 15) << 4)));
            ushort mask = 0;
            for (int i = 0; i < FieldCount; i++) if (Field(i) != 0f) mask |= (ushort)(1 << i);
            w.Put(mask);
            for (int i = 0; i < FieldCount; i++) if ((mask & (1 << i)) != 0) w.Put(Field(i));
        }

        public static bool TryRead(NetDataReader r, out OwnerSim s)
        {
            s = default(OwnerSim);
            if (r.AvailableBytes < 4) return false;
            byte b0 = r.GetByte(), b1 = r.GetByte();
            s.DashDir = (byte)(b0 & 15); s.ComboPhase = (byte)((b0 >> 4) & 3); s.ComboQueued = (b0 & 64) != 0;
            s.ComboStep = (byte)(b1 & 15); s.ComboLast = (byte)(b1 >> 4);
            ushort mask = r.GetUShort();
            if (s.DashDir > 8 || (mask >> FieldCount) != 0) return false;
            for (int i = 0; i < FieldCount; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                if (r.AvailableBytes < 4) return false;
                float v = r.GetFloat();
                if (float.IsNaN(v) || v < 0f || v > 60000f) return false;
                s.SetField(i, v);
            }
            return true;
        }
    }
}
