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
    /// Protocol 22: the zone also sends WantedStatus (boss alive / respawn countdown, its day key), kept in Status. Everything below answers from the server's
    /// status when one arrived and falls back to the device clock otherwise, so the Wanted UI binds to this class alone.
    /// </summary>
    public static class WantedBossTracker
    {
        static int lastDay;

        /// <summary>The server's last WantedStatus of this connection (Known = false before it arrives and after a disconnect).</summary>
        public static readonly WantedClientStatus Status = new WantedClientStatus();
        /// <summary>Status changed (boss died, returned, the day turned, or the connection went). Static: a subscriber must unsubscribe when it is destroyed.</summary>
        public static event Action StatusChanged;
        /// <summary>This character was just paid the daily bounty for that boss: the moment for the reward panel (WantedBossDef holds the gold, stones and potions).</summary>
        public static event Action<WantedBossDef> BountyPaid;

        static long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }
        static double ClockMs { get { return Time.realtimeSinceStartupAsDouble * 1000.0; } }

        /// <summary>Today's day key: the server's when known, else the device clock's.</summary>
        public static int TodayKey { get { return Status.Known ? Status.DayKey : WantedBoss.DayKey(NowMs); } }

        /// <summary>Today's wanted boss: the one the server named (it may be pinned by --wanted-boss), else the rotation by the device clock. MapId + SpawnTx / SpawnTy = where it stands.</summary>
        public static WantedBossDef Today { get { if (!WantedBoss.Enabled) return null; return (Status.Known ? WantedBoss.Find(Status.BossType) : null) ?? WantedBoss.Today(NowMs); } }

        /// <summary>Yesterday, today, tomorrow for the board. A server that pins one boss shows it on all three.</summary>
        public static WantedBossDef[] Schedule()
        {
            WantedBossDef today = Today;
            if (today != WantedBoss.For(TodayKey)) return new[] { today, today, today };
            return WantedBoss.ScheduleFor(TodayKey);
        }

        /// <summary>Milliseconds until the killed boss stands again; 0 when it is alive or nothing is known.</summary>
        public static long RespawnLeftMs { get { return Status.RespawnLeftMs(ClockMs); } }

        /// <summary>Milliseconds until the wanted boss changes (05:00 UTC+7).</summary>
        public static long ResetLeftMs
        {
            get
            {
                if (Status.Known) return Status.ResetLeftMs(ClockMs);
                long now = NowMs;
                return Math.Max(0L, WantedBoss.NextResetUtcMs(now) - now);
            }
        }

        /// <summary>NetClient: a WantedStatus message arrived.</summary>
        public static void OnStatus(WantedStatusMsg m)
        {
            Status.Apply(m, ClockMs);
            Debug.Log("[ZenoniaProto] wanted: status " + m.BossType + " day " + m.DayKey + (m.Alive ? " alive" : " back in " + m.RespawnInMs / 1000u + " s") + ", next boss in " + m.ResetInMs / 60000u + " min");
            if (StatusChanged != null) StatusChanged();
        }

        /// <summary>NetClient: the connection is gone, so is what that server said.</summary>
        public static void ClearStatus()
        {
            if (!Status.Known) return;
            Status.Clear();
            if (StatusChanged != null) StatusChanged();
        }

        /// <summary>This hero's claim covers today (display only).</summary>
        public static bool ClaimedToday(int wantedDay) { return wantedDay >= TodayKey; }

        /// <summary>"6h 12m" until the wanted boss changes.</summary>
        public static string TimeToReset()
        {
            long min = ResetLeftMs / 60000L;
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
                WantedBossDef today = Today;
                if (ClaimedToday(wantedDay)) ChatHub.System("Wanted reward claimed today. Next wanted boss in " + TimeToReset() + ".");
                else ChatHub.System("Wanted today: " + today.BossName + " in " + MapName(today) + ". Reward: " + WantedBoss.RewardText(today) + ". Resets 05:00.");
                return;
            }
            if (wantedDay <= lastDay) { lastDay = Math.Max(lastDay, wantedDay); return; }
            lastDay = wantedDay;
            WantedBossDef def = wantedDay == TodayKey ? Today : WantedBoss.For(wantedDay); // a claim paid in the last second of a day belongs to that day's boss
            string reward = WantedBoss.RewardText(def);
            ChatHub.System("Wanted boss defeated! " + reward);
            // Adventure Paper HUD: the BOUNTY CLAIMED panel (WantedHud, on BountyPaid) replaces the toast.
            if (!UiScreens.UseNew(UiScreen.Hud)) NotificationToast.Show("Wanted Boss!", WantedBoss.ShortText(def), LootSprites.Coin()); // short text: the toast box is narrow
            if (BountyPaid != null) BountyPaid(def);
        }
    }
}
