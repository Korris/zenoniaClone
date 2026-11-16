using System;
using LiteNetLib.Utils;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// Server -> client (reliable, channel 1, protocol 22): the state of today's Wanted Boss for the whole world, so a client on any map knows whether the boss
    /// stands or when it comes back. Sent once after the join and again whenever the boss dies, returns, or the day turns (WantedStatusService).
    /// Both countdowns are RELATIVE to the moment of sending: the client adds them to its own clock at arrival (WantedClientStatus), so a wrong device clock
    /// cannot shift them. Where the boss is (map, tile), its level and its reward are not sent: WantedBoss.Find(BossType) has them on both ends.
    /// Wire: opcode, BossType (string, at most MaxBossTypeLength), DayKey (int), Alive (byte 0 / 1), RespawnInMs (uint), ResetInMs (uint).
    /// </summary>
    public struct WantedStatusMsg
    {
        public const int MaxBossTypeLength = 32;

        /// <summary>WantedBossDef.BossType of the boss the zone server hunts today (its own clock, or its --wanted-boss pin).</summary>
        public string BossType;
        /// <summary>The server's WantedBoss.DayKey: a character whose WantedDay reaches it has been paid today.</summary>
        public int DayKey;
        /// <summary>True = the boss stands on its map (or will the moment somebody walks in); false = killed, back in RespawnInMs.</summary>
        public bool Alive;
        /// <summary>Milliseconds until the boss stands again; 0 while Alive.</summary>
        public uint RespawnInMs;
        /// <summary>Milliseconds until the day turns (05:00 UTC+7) and the next boss of the rotation is wanted.</summary>
        public uint ResetInMs;

        public void Write(NetDataWriter w)
        {
            w.Reset(); w.Put(NetOp.WantedStatus);
            w.Put(BossType ?? "", MaxBossTypeLength); w.Put(DayKey); w.Put((byte)(Alive ? 1 : 0)); w.Put(RespawnInMs); w.Put(ResetInMs);
        }

        public static bool TryRead(NetDataReader r, out WantedStatusMsg m)
        {
            m = default(WantedStatusMsg);
            try
            {
                m.BossType = r.GetString(MaxBossTypeLength);
                m.DayKey = r.GetInt();
                byte alive = r.GetByte();
                m.Alive = alive == 1;
                m.RespawnInMs = r.GetUInt();
                m.ResetInMs = r.GetUInt();
                return !string.IsNullOrEmpty(m.BossType) && m.DayKey > 0 && alive <= 1;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>The zone server's Wanted status rules, free of engine types so the tests can run them: what to tell the clients, and when it is news.</summary>
    public static class WantedStatusRules
    {
        /// <summary>
        /// The status of today's boss from what its zone holds. `standing` = the boss object of today's type is there and not dying. `timerActive` / `timerLeftMs` = the zone's
        /// respawn clock (Zone.WantedTimer); a paused zone does not tick it, so `sleptMs` (how long the zone has been asleep) is taken off here, exactly what waking the zone will do.
        /// Neither standing nor waiting (a day that just turned, a zone asleep since before the boss was due) counts as alive: the spawner places the boss as soon as the zone runs.
        /// </summary>
        public static WantedStatusMsg Build(string bossType, int dayKey, bool standing, bool timerActive, float timerLeftMs, float sleptMs, long resetInMs)
        {
            float left = timerActive ? timerLeftMs - Math.Max(0f, sleptMs) : 0f;
            bool respawning = !standing && left > 0f;
            return new WantedStatusMsg
            {
                BossType = bossType, DayKey = dayKey, Alive = !respawning,
                RespawnInMs = respawning ? (uint)Math.Ceiling(left) : 0u,
                ResetInMs = (uint)Math.Max(0L, Math.Min(resetInMs, uint.MaxValue)),
            };
        }

        /// <summary>True when `now` must be sent to everybody: another boss, another day, or the boss died / returned. The countdowns alone are not news (clients count down themselves).</summary>
        public static bool IsNews(WantedStatusMsg last, WantedStatusMsg now)
        {
            return last.BossType != now.BossType || last.DayKey != now.DayKey || last.Alive != now.Alive;
        }
    }
}
