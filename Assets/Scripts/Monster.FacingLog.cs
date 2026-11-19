using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Test hook for the "boss keeps spinning" report (2026-11-19): with env ZENONIA_BOSS_FACING_LOG=1 the authority logs, every 10 s per wanted
    /// boss, how many times it turned, re-picked its target and entered Return. ZENONIA_BOSS_FACING_LEGACY=1 restores the old rules (plain axis
    /// facing, no target stickiness through skills, no in-reach retarget) so one build measures before and after. Both are read once at start.
    /// </summary>
    public static class BossFacingLog
    {
        public static readonly bool On = Env("ZENONIA_BOSS_FACING_LOG");
        public static readonly bool Legacy = Env("ZENONIA_BOSS_FACING_LEGACY");
        /// <summary>Capture only: a wanted boss casts Venom Drain whenever it is ready and has a victim (Monster.WantedSkills.cs).</summary>
        public static readonly bool PreferDrain = Env("ZENONIA_PREFER_DRAIN");
        public const float WindowMs = 10000f;

        static bool Env(string name)
        {
            try { return Environment.GetEnvironmentVariable(name) == "1"; } catch { return false; }
        }
    }

    public partial class Monster
    {
        struct FacingLogWindow { public int Turns, Retargets, Returns; public float Ms; }
        FacingLogWindow facingLog;

        void TickFacingLog(float dtMs)
        {
            facingLog.Ms += dtMs;
            if (facingLog.Ms < BossFacingLog.WindowMs) return;
            float s = facingLog.Ms / 1000f;
            Debug.Log("[boss-facing] " + def.Sheet + (BossFacingLog.Legacy ? " LEGACY" : " FIXED") + " turns=" + facingLog.Turns + " (" + (facingLog.Turns / s).ToString("0.00")
                + "/s) retargets=" + facingLog.Retargets + " returns=" + facingLog.Returns + " state=" + state + " facing=" + facing);
            facingLog = default(FacingLogWindow);
        }
    }
}
