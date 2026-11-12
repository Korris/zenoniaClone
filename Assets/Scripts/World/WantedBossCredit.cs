using System;
using System.Collections.Generic;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Daily Wanted Boss payout, authority only (dedicated server and dev host; the offline game never claims, its clock and save belong to the player).
    /// Every hero that took part in the kill (same set as the boss EXP: PartyKillShare.CollectParticipants) claims once per character per day.
    /// The day and the boss are read once per kill, so a kill at the reset second belongs to exactly one day.
    /// </summary>
    public static class WantedBossCredit
    {
        static readonly List<HeroController> Participants = new List<HeroController>(16);

        /// <summary>Call after a boss died. `winners` are the heroes that hit it and are alive in the zone (KillRewards.Collect).</summary>
        public static void Pay(Zone zone, Monster m, List<HeroController> winners) { Pay(zone, m, winners, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); }

        public static void Pay(Zone zone, Monster m, List<HeroController> winners, long utcMs)
        {
            if (NetMode.IsOffline || !NetMode.IsAuthority || m == null || !m.IsWanted) return;
            WantedBossDef def = WantedBoss.Today(utcMs);
            if (def.BossType != m.Type) return;
            int day = WantedBoss.DayKey(utcMs);
            PartyKillShare.CollectParticipants(zone, m, winners, Participants);
            for (int i = 0; i < Participants.Count; i++) Participants[i].Owner.ClaimWanted(def, day);
        }
    }
}
