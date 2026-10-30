using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// The part of a hero's simulation state that position / pose do not already carry, sent to its OWNER in every snapshot so the client can restore the
    /// server state at an acked input frame and replay its newer inputs (reconciliation). Timers are exact floats (ms): a rounded timer would let a
    /// replay cross a phase boundary one step earlier than the server did. Wire: 2 B of small enums, an 8-bit presence mask, then only the non-zero
    /// timers, 4 B each (an idle hero costs 3 B). Skill state (cooldowns of the 8 skills, the skill being cast, the attack-speed buff) travels in an
    /// optional extension block announced by bit 7 of the first byte, so a hero that never casts pays nothing for it.
    /// </summary>
    public struct OwnerSim
    {
        /// <summary>0 = none, 1..8 = 45 degree steps (dash direction).</summary>
        public byte DashDir;
        /// <summary>ComboPhase 0..3 (idle / windup / active / recover).</summary>
        public byte ComboPhase, ComboStep, ComboLast;
        public bool ComboQueued;
        public float DashLeft, DashCooldown, ComboTimer, ChainTimer, Invuln, Hurt, CastLeft, StabLeft;
        /// <summary>Tree index 0-7 of the skill being cast (meaningful while CastLeft &gt; 0).</summary>
        public byte CastSkill;
        /// <summary>Attack-speed buff: remaining ms and percent (0 = none).</summary>
        public float AtkSpeedLeft, AtkSpeedPct;
        /// <summary>Cooldowns (ms) of the 8 skills; null = all ready.</summary>
        public float[] Cd;

        public const int CdSlots = 8;
        const int BaseFields = 8, ExtFields = CdSlots + 2;
        const byte ExtFlag = 128;

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
                default: StabLeft = v; break;
            }
        }

        float ExtField(int i) { return i < CdSlots ? CdOf(i) : i == CdSlots ? AtkSpeedLeft : AtkSpeedPct; }

        void SetExtField(int i, float v)
        {
            if (i < CdSlots) { if (Cd == null) Cd = new float[CdSlots]; Cd[i] = v; }
            else if (i == CdSlots) AtkSpeedLeft = v;
            else AtkSpeedPct = v;
        }

        public float CdOf(int i) { return Cd != null && i >= 0 && i < Cd.Length ? Cd[i] : 0f; }

        ushort ExtMask()
        {
            ushort m = 0;
            for (int i = 0; i < ExtFields; i++) if (ExtField(i) != 0f) m |= (ushort)(1 << i);
            return m;
        }

        public void Write(NetDataWriter w)
        {
            ushort ext = ExtMask();
            bool hasExt = ext != 0 || (CastLeft > 0f && CastSkill != 0);
            w.Put((byte)((DashDir & 15) | ((ComboPhase & 3) << 4) | (ComboQueued ? 64 : 0) | (hasExt ? ExtFlag : 0)));
            w.Put((byte)((ComboStep & 15) | ((ComboLast & 15) << 4)));
            byte mask = 0;
            for (int i = 0; i < BaseFields; i++) if (Field(i) != 0f) mask |= (byte)(1 << i);
            w.Put(mask);
            for (int i = 0; i < BaseFields; i++) if ((mask & (1 << i)) != 0) w.Put(Field(i));
            if (!hasExt) return;
            w.Put((byte)(CastSkill & 7));
            w.Put(ext);
            for (int i = 0; i < ExtFields; i++) if ((ext & (1 << i)) != 0) w.Put(ExtField(i));
        }

        static bool ReadTimer(NetDataReader r, out float v)
        {
            v = 0f;
            if (r.AvailableBytes < 4) return false;
            v = r.GetFloat();
            return !float.IsNaN(v) && v >= 0f && v <= 60000f;
        }

        public static bool TryRead(NetDataReader r, out OwnerSim s)
        {
            s = default(OwnerSim);
            if (r.AvailableBytes < 3) return false;
            byte b0 = r.GetByte(), b1 = r.GetByte(), mask = r.GetByte();
            s.DashDir = (byte)(b0 & 15); s.ComboPhase = (byte)((b0 >> 4) & 3); s.ComboQueued = (b0 & 64) != 0;
            s.ComboStep = (byte)(b1 & 15); s.ComboLast = (byte)(b1 >> 4);
            if (s.DashDir > 8) return false;
            for (int i = 0; i < BaseFields; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                float v;
                if (!ReadTimer(r, out v)) return false;
                s.SetField(i, v);
            }
            if ((b0 & ExtFlag) == 0) return true;
            if (r.AvailableBytes < 3) return false;
            byte cast = r.GetByte();
            ushort ext = r.GetUShort();
            if (cast > 7 || (ext >> ExtFields) != 0) return false;
            s.CastSkill = cast;
            for (int i = 0; i < ExtFields; i++)
            {
                if ((ext & (1 << i)) == 0) continue;
                float v;
                if (!ReadTimer(r, out v)) return false;
                s.SetExtField(i, v);
            }
            return true;
        }
    }
}
