using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// When a hero standing on a portal tile may use it: never right after arriving (the arrival tile can be a trigger, and a client that is still fading has no say), so the
    /// gate opens only once the feet have left every trigger and the cooldown has run out. Per hero, ticked by the server once per sim tick.
    /// </summary>
    public sealed class PortalGate
    {
        public const float CooldownMs = 1000f;
        float leftMs = CooldownMs;
        bool armed;

        /// <summary>Advances the cooldown; true when a hero on a trigger may go through it this tick.</summary>
        public bool Tick(bool onTrigger, float dtMs)
        {
            if (leftMs > 0f) leftMs -= dtMs;
            if (!onTrigger) { armed = true; return false; }
            return armed && leftMs <= 0f;
        }

        /// <summary>The hero was just placed (join, arrival): it has to step off and wait the cooldown again.</summary>
        public void Disarm() { armed = false; leftMs = CooldownMs; }
    }

    /// <summary>
    /// Boss kills of one hero, the same two flags as an offline save: maps with a boss of their own are listed by id, every other map shares the Frostfang King's flag.
    /// Per hero, not per zone: a zone's boss comes back for everyone, and a hero who never hit it keeps the gate shut.
    /// </summary>
    public sealed class BossProgress
    {
        public bool KingDefeated;
        public readonly List<string> OwnBossMaps = new List<string>();

        public bool IsDefeatedOn(string mapId, bool mapHasOwnBoss) { return mapHasOwnBoss ? OwnBossMaps.Contains(mapId) : KingDefeated; }

        /// <summary>Records a kill; true when it is new (the gate may just have opened).</summary>
        public bool Mark(string mapId, bool mapHasOwnBoss)
        {
            if (IsDefeatedOn(mapId, mapHasOwnBoss)) return false;
            if (mapHasOwnBoss) OwnBossMaps.Add(mapId);
            else KingDefeated = true;
            return true;
        }
    }

    /// <summary>
    /// A zone's idle bookkeeping (pure, no engine): an awake zone without heroes pauses after a short grace, so a monster finishes dying and a hero hopping through a portal
    /// does not make it stop and start. Wake says how long it slept, which the zone pays back to its respawn clocks in one go.
    /// </summary>
    public sealed class ZoneIdleClock
    {
        public const float GraceMs = 2000f;
        float emptyMs, pausedAtMs;

        public bool Paused { get; private set; }

        /// <summary>Once per tick while awake; true when the zone should pause now.</summary>
        public bool TickAwake(int heroCount, float dtMs)
        {
            if (Paused) return false;
            if (heroCount > 0) { emptyMs = 0f; return false; }
            emptyMs += dtMs;
            return emptyMs >= GraceMs;
        }

        public void MarkPaused(float nowMs) { Paused = true; pausedAtMs = nowMs; }

        /// <summary>Back to awake (no grace left over); returns the milliseconds slept, 0 when it was not paused.</summary>
        public float Wake(float nowMs)
        {
            emptyMs = 0f;
            if (!Paused) return 0f;
            Paused = false;
            return Math.Max(0f, nowMs - pausedAtMs);
        }
    }

    /// <summary>Respawn delays that ran while a zone slept: each is shortened by the sleep and never goes below 0, so the next tick places everything that came due.</summary>
    public static class RespawnClock
    {
        public static void FastForward(List<float> timersMs, float sleptMs)
        {
            for (int i = 0; i < timersMs.Count; i++) timersMs[i] = Math.Max(0f, timersMs[i] - sleptMs);
        }
    }

    /// <summary>
    /// The portal trigger tile under a hero's feet, y-up world units to the maps' top-down rows. The web samples 4 units above the feet for portals and walkability, so the same
    /// rule runs offline (MapTransfer) and on the server (ZoneTransferService).
    /// </summary>
    public static class PortalTile
    {
        public const float FeetSampleUp = 4f;

        public static void OfFeet(float x, float y, float tileUnits, int mapRows, out int tx, out int ty)
        {
            tx = (int)Math.Floor(x / tileUnits);
            ty = mapRows - 1 - (int)Math.Floor((y + FeetSampleUp) / tileUnits);
        }
    }
}
