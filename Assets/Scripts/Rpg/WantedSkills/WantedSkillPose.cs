namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Which of the 4 poses of a skill's own sheet to show (ChatGPT animation spec 24/11): F1-F2 while the warning runs (wind-up, first half / second half), F3 = the impact or
    /// the launch for the first <see cref="ImpactHoldMs"/> of the Execute window, F4 = the held dash / recovery pose for the rest. The sprite only shows the pose, Unity moves the body.
    /// Pure (no Unity) so the timing is tested.
    /// </summary>
    public static class WantedSkillPose
    {
        /// <summary>How long F3 (impact / launch) stays before F4 takes over, ms (Elder Awakening holds F3 0.2-0.3 s for the climax).</summary>
        public const float ImpactHoldMs = 250f;

        /// <summary>Pose index 0..3. telegraphing: progress 0..1 of the warning; otherwise executeElapsedMs since the Execute state began.</summary>
        public static int Frame(bool telegraphing, float telegraphProgress, float executeElapsedMs)
        {
            if (telegraphing) return telegraphProgress < 0.5f ? 0 : 1;
            return executeElapsedMs < ImpactHoldMs ? 2 : 3;
        }
    }
}
