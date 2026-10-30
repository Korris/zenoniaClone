using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture / restore of the hero's movement-relevant simulation state, for prediction. The server sends its capture (OwnerSim + the snapshot entry)
    /// with every snapshot; the client restores it at the acked input frame and replays its newer inputs.
    /// </summary>
    public partial class HeroController
    {
        /// <summary>Timers may dip below zero after they expire (dash, chain window); nothing reads them there, and the wire only carries 0..60000.</summary>
        static float T(float v) { return v > 0f ? v : 0f; }

        static byte DirIndex(Vector2 d)
        {
            if (d.sqrMagnitude < 1e-6f) return 0;
            int k = Mathf.RoundToInt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg / 45f);
            return (byte)(((k % 8) + 8) % 8 + 1);
        }

        static Vector2 DirFromIndex(int idx)
        {
            float a = (idx - 1) * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        /// <summary>The sim state beyond position / facing / dead (those travel in the snapshot entry).</summary>
        public OwnerSim CaptureSim(bool withSkills = true)
        {
            ComboState c = combo.Capture();
            float hastePct, hasteLeft;
            HeroBuffs.AtkSpeedOf(this, out hastePct, out hasteLeft);
            var sim = new OwnerSim
            {
                DashDir = dashLeft > 0f ? DirIndex(dashDir) : (byte)0, ComboPhase = (byte)c.Phase, ComboStep = (byte)c.Step, ComboLast = (byte)c.LastStep, ComboQueued = c.Queued,
                DashLeft = T(dashLeft), DashCooldown = T(dashCooldown), ComboTimer = T(c.Timer), ChainTimer = T(c.ChainTimer), Invuln = T(invuln), Hurt = T(hurt),
                CastLeft = T(castLeft), StabLeft = T(stabLeft), CastSkill = (byte)castSkill, AtkSpeedLeft = T(hasteLeft), AtkSpeedPct = hastePct,
            };
            if (withSkills && AnySkillCooldown)
            {
                sim.Cd = new float[OwnerSim.CdSlots];
                for (int i = 0; i < sim.Cd.Length; i++) sim.Cd[i] = T(cds.LeftMs(i));
            }
            return sim;
        }

        /// <summary>Puts the hero into the state the server reported: position / facing / dead from the snapshot entry, the rest from the sim block.</summary>
        public void RestoreSim(Vector2 pos, int facingDir, bool isDead, float deathProgress, OwnerSim s)
        {
            x = pos.x; y = pos.y;
            facing = facingDir & 3;
            if (isDead && !dead) deathTimer = deathProgress * RespawnMs;
            dead = isDead;
            dashLeft = s.DashLeft; dashCooldown = s.DashCooldown; invuln = s.Invuln; hurt = s.Hurt;
            castLeft = s.CastLeft; stabLeft = s.StabLeft; castSkill = s.CastSkill;
            for (int i = 0; i < SkillCooldowns.Slots; i++) cds.Start(i, s.CdOf(i));
            HeroBuffs.SetAtkSpeed(this, s.AtkSpeedPct, s.AtkSpeedLeft);
            if (castLeft > 0f)
            {
                SkillDef def = SkillTable.Get(rpg.ClassId, castSkill);
                castTotal = Mathf.Max(castLeft, def != null ? def.CastLockMs : castLeft);
            }
            if (s.DashDir > 0) dashDir = DirFromIndex(s.DashDir);
            combo.Restore(new ComboState { Phase = (ComboPhase)s.ComboPhase, Step = s.ComboStep, LastStep = s.ComboLast, Timer = s.ComboTimer, ChainTimer = s.ChainTimer, Queued = s.ComboQueued });
            if (stabLeft > 0f) { stabHit = combo.CurrentHit ?? stabHit; stabStep = combo.Step > 0 ? combo.Step : stabStep; }
        }

        /// <summary>
        /// Coarse discrete state (dead, dashing, casting, combo phase + step) used to decide whether the server's state disagrees with what was predicted.
        /// Hurt is left out on purpose: the client learns about hits from events, not from its own sim.
        /// </summary>
        public static int Discrete(bool isDead, OwnerSim s)
        {
            return (isDead ? 1 : 0) | (s.DashLeft > 0f ? 2 : 0) | (s.CastLeft > 0f ? 4 : 0) | (s.ComboPhase << 3) | (s.ComboStep << 5) | (s.AtkSpeedLeft > 0f ? 1 << 12 : 0);
        }

        public int DiscreteState() { return Discrete(dead, CaptureSim(false)); }
    }
}
