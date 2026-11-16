using System;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// What the client knows of today's Wanted Boss, only ever changed by the server's WantedStatus message (WantedBossTracker.Status holds the one instance; the Wanted UI reads it).
    /// The message's countdowns are relative, so they are pinned to the client's own monotonic clock at arrival and read back against the same clock (NetClient.ClientMs).
    /// </summary>
    public sealed class WantedClientStatus
    {
        double receivedAtMs;
        uint respawnInMs, resetInMs;

        /// <summary>False until the first message of this connection (and after Clear): show nothing, or fall back to the device clock.</summary>
        public bool Known { get; private set; }
        /// <summary>Goes up with every change: the UI rebuilds its texts only then.</summary>
        public int Version { get; private set; }
        public string BossType { get; private set; }
        /// <summary>The server's day key (WantedBoss.DayKey): PlayerState.WantedDay >= DayKey means this character has been paid today.</summary>
        public int DayKey { get; private set; }
        /// <summary>The boss stands on its map; false = killed, RespawnLeftMs counts down to its return.</summary>
        public bool Alive { get; private set; }

        public void Apply(WantedStatusMsg m, double nowMs)
        {
            Known = true; Version++;
            BossType = m.BossType; DayKey = m.DayKey; Alive = m.Alive;
            respawnInMs = m.RespawnInMs; resetInMs = m.ResetInMs; receivedAtMs = nowMs;
        }

        /// <summary>Connection gone: what was known belongs to that server stay.</summary>
        public void Clear()
        {
            Known = false; Version++;
            BossType = null; DayKey = 0; Alive = false; respawnInMs = resetInMs = 0; receivedAtMs = 0.0;
        }

        /// <summary>Milliseconds until the boss stands again: 0 when unknown, alive, or due (the server's "alive" message follows).</summary>
        public long RespawnLeftMs(double nowMs) { return Known && !Alive ? Left(respawnInMs, nowMs) : 0L; }

        /// <summary>Milliseconds until the next boss of the rotation is wanted: 0 when unknown or due.</summary>
        public long ResetLeftMs(double nowMs) { return Known ? Left(resetInMs, nowMs) : 0L; }

        long Left(uint inMs, double nowMs) { return Math.Max(0L, (long)Math.Ceiling(inMs - Math.Max(0.0, nowMs - receivedAtMs))); }
    }
}
