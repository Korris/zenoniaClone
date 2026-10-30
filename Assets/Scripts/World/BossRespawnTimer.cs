namespace ZenoniaProto
{
    /// <summary>
    /// Online world-boss respawn clock (the Frostfang King returns 30 minutes after its death). Pure logic: the zone starts it on the kill and
    /// ticks it with the sim delta; Tick returns true once when it runs out. Offline never starts it (the save's bossDefeated flag rules there).
    /// </summary>
    public sealed class BossRespawnTimer
    {
        public const float DefaultRespawnMs = 30f * 60f * 1000f;
        float leftMs = -1f;

        public bool Active { get { return leftMs >= 0f; } }
        public float LeftMs { get { return leftMs; } }

        public void Start(float ms = DefaultRespawnMs) { leftMs = ms < 0f ? 0f : ms; }
        public void Cancel() { leftMs = -1f; }

        /// <summary>Advances the clock; true exactly once when the timer elapses.</summary>
        public bool Tick(float dtMs)
        {
            if (leftMs < 0f) return false;
            leftMs -= dtMs;
            if (leftMs > 0f) return false;
            leftMs = -1f;
            return true;
        }
    }
}
