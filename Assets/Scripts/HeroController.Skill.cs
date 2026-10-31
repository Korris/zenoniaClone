using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Skill casting of the hero: the 8 tree skills of its class (SkillTable). TryCastSkill(index) checks learned / cooldown / MP / cast lock, asks the
    /// SkillExecutor to run the skill (it refuses when a needed target is missing, which costs nothing), then charges MP, starts the cooldown and
    /// roots the hero for the skill's cast lock. Offline, on the server and on the host this is the rule; the online owner predicts only the skills
    /// SkillPrediction allows and takes the rest from the server (SkillCast event + OwnerSim).
    /// </summary>
    public partial class HeroController
    {
        const float MpRegenPerSecond = 2.2f;
        readonly SkillCooldowns cds = new SkillCooldowns();
        float castLeft, castTotal;
        int castSkill;
        /// <summary>Server: sequence of the input frame being simulated for a remote player (the cast salt every side agrees on).</summary>
        public System.Func<uint> InputSeq;
        /// <summary>Successful casts since spawn (server stats).</summary>
        public int SkillCasts { get; private set; }
        float mp { get { return rpg.Mp; } set { rpg.Mp = value; } }

        public float Mp { get { return mp; } }
        public float MaxMp { get { return rpg.MaxMp; } }
        public bool IsCasting { get { return castLeft > 0f; } }
        /// <summary>Skill the L key / bots / debug tools cast: the first magazine slot, else the lowest learned skill; -1 when nothing is learned.</summary>
        public int PrimarySkillIndex
        {
            get
            {
                int first = rpg.Loadout[0];
                if (first >= 0 && rpg.Skills[first] > 0) return first;
                for (int i = 0; i < SkillDef.PerClass; i++) if (rpg.Skills[i] > 0) return i;
                return -1;
            }
        }
        public float SkillCooldownLeftMs { get { return SkillCdLeftMs(PrimarySkillIndex); } }
        public float SkillCdLeftMs(int index) { return cds.LeftMs(index); }
        public bool AnySkillCooldown { get { for (int i = 0; i < SkillCooldowns.Slots; i++) if (cds.LeftMs(i) > 0f) return true; return false; } }
        public bool PredictsSkillFx(int index) { return SkillPrediction.Predicts(SkillTable.Get(rpg.ClassId, index)); }

        void ResetSkillState() { cds.Reset(); castLeft = 0f; castSkill = 0; }

        /// <summary>Gives the hero i-frames for at least `ms` (Shadow Step).</summary>
        public void AddInvuln(float ms) { invuln = Mathf.Max(invuln, ms); }

        /// <summary>Tree skill whose key went down this step (Skill1..Skill8), the L / bot alias counting as the primary skill; -1 when none.</summary>
        int PressedSkill()
        {
            for (int i = 0; i < SkillDef.PerClass; i++) if (Pressed(GameKey.Skill1 + i)) return i;
            return Pressed(GameKey.Skill) ? PrimarySkillIndex : -1;
        }

        /// <summary>Casts the primary skill (L key, bots, debug capture).</summary>
        public bool TryCastSkill() { int i = PrimarySkillIndex; return i >= 0 && TryCastSkill(i); }

        /// <summary>Starts tree skill `index` (0-7) when learned, off cooldown, affordable and not busy. Returns true when cast.</summary>
        public bool TryCastSkill(int index)
        {
            SkillDef def = SkillTable.Get(rpg.ClassId, index);
            if (def == null || dead || castLeft > 0f || dashLeft > 0f) return false;
            int rank = rpg.Skills[index];
            if (rank <= 0) { SkillCastFeedback.Refused(this, def, SkillCastFeedback.Reason.NotLearned); return false; }
            if (!cds.Ready(index)) return false;
            int cost = SkillCooldowns.DevFreeMp ? 0 : def.MpAt(rank);
            if (mp < cost) { SkillCastFeedback.Refused(this, def, SkillCastFeedback.Reason.NoMp); return false; }
            if (predicted && !SkillPrediction.Predicts(def)) return false; // motion / targeted skills: the server decides, its SkillCast shows it
            int salt = CastSalt();
            // The executor decides first (a skill that needs a target refuses when none is in range) so a refused cast costs nothing.
            if (!replaying && !SkillExecutor.Cast(this, index, rank, salt)) { SkillCastFeedback.Refused(this, def, SkillCastFeedback.Reason.NoTarget); return false; }
            if (HomeZone != null && HomeZone.Pump != null) HomeZone.Pump.Adopt();
            NetEventBus.SkillCast(this, index, rank, salt);
            if (!replaying) mp -= cost;
            cds.Start(index, SkillCooldowns.DevNone ? 0f : def.CooldownMs);
            castLeft = castTotal = Mathf.Max(1f, def.CastLockMs);
            castSkill = index;
            if (!replaying) SkillCasts++;
            combo.Reset();
            animTimer = 0f;
            return true;
        }

        /// <summary>The cast's identity on every side: the input step that pressed the key (low 16 bits), so rain drops land on the same points.</summary>
        int CastSalt()
        {
            if (predicted) return (int)(predictor.Seq & 0xFFFF);
            return InputSeq != null ? (int)(InputSeq() & 0xFFFF) : Time.frameCount & 0xFFFF;
        }

        /// <summary>Regen + cooldown ticks; returns true while the hero is rooted by a cast (no movement / attacks this step).</summary>
        bool TickSkill(float dt, float dtMs)
        {
            if (!predicted) mp = Mathf.Min(MaxMp, mp + MpRegenPerSecond * dt);
            cds.Tick(dtMs);
            if (castLeft <= 0f) return false;
            castLeft = Mathf.Max(0f, castLeft - dtMs);
            moving = false;
            return true;
        }

        /// <summary>Cast pose: the class's overhead attack frames stretched over the root time.</summary>
        bool CastPose(out string anim, out int frame)
        {
            anim = "attack3";
            frame = 0;
            if (castLeft <= 0f) return false;
            frame = ByProgress(Mathf.Max(1, view.FrameCount(anim, facing)), 1f - castLeft / castTotal);
            return true;
        }

        /// <summary>Client: the server's cooldowns for the local hero (SelfState); the predicting hero keeps its own sim copy instead.</summary>
        void ApplySelfCooldowns(ushort[] ms)
        {
            if (ms == null) return;
            for (int i = 0; i < SkillCooldowns.Slots && i < ms.Length; i++) cds.Start(i, ms[i]);
        }

        /// <summary>Server -> SelfState: remaining cooldown of each skill in ms.</summary>
        public ushort[] SkillCooldownsMs()
        {
            var ms = new ushort[SkillCooldowns.Slots];
            for (int i = 0; i < ms.Length; i++) ms[i] = (ushort)Mathf.Clamp(Mathf.CeilToInt(cds.LeftMs(i)), 0, 65535);
            return ms;
        }
    }
}
