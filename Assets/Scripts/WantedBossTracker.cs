using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Client side of the daily Wanted Boss: tells the player what is wanted when the character arrives in the world, and tells it again when a claim is paid.
    /// There is no wire message for a claim: the zone raises PlayerState.WantedDay inside the normal RpgState push, and this compares it with the last value seen
    /// (like QuestTracker.Observe). Display only: the zone decides who is paid. Online only, offline never calls it.
    /// </summary>
    public static class WantedBossTracker
    {
        static int lastDay;

        static long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }

        /// <summary>This hero's claim covers today (device clock; display only).</summary>
        public static bool ClaimedToday(int wantedDay) { return wantedDay >= WantedBoss.DayKey(NowMs); }

        /// <summary>"6h 12m" until the wanted boss changes.</summary>
        public static string TimeToReset()
        {
            long now = NowMs;
            long min = Math.Max(0L, (WantedBoss.NextResetUtcMs(now) - now) / 60000L);
            return min / 60 + "h " + min % 60 + "m";
        }

        public static string MapName(WantedBossDef d)
        {
            var map = MapRegistry.Get(d.MapId);
            return map != null ? map.Name : d.MapId;
        }

        /// <summary>
        /// `known` is false for the first RpgState of a connection (the character arriving): one chat line with today's boss or the claimed note, no toast.
        /// Later pushes announce only a WantedDay that rose.
        /// </summary>
        public static void Observe(int wantedDay, bool known)
        {
            if (!known)
            {
                lastDay = wantedDay;
                WantedBossDef today = WantedBoss.Today(NowMs);
                if (ClaimedToday(wantedDay)) ChatHub.System("Wanted reward claimed today. Next wanted boss in " + TimeToReset() + ".");
                else ChatHub.System("Wanted today: " + today.BossName + " in " + MapName(today) + ". Reward: " + WantedBoss.RewardText(today) + ". Resets 05:00.");
                return;
            }
            if (wantedDay <= lastDay) { lastDay = Math.Max(lastDay, wantedDay); return; }
            lastDay = wantedDay;
            WantedBossDef def = WantedBoss.Today(NowMs);
            string reward = WantedBoss.RewardText(def);
            ChatHub.System("Wanted boss defeated! " + reward);
            NotificationToast.Show("Wanted Boss!", WantedBoss.ShortText(def), LootSprites.Coin()); // short text: the toast box is narrow
        }
    }
}
