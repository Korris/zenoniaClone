using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>The Wanted notices, lowest priority first (wanted-notifications.png, sections 2-7). Bounty is the reward panel, not a banner.</summary>
    public enum WantedNotice { Already, Login, Area, Respawn, Bounty, Warning }

    /// <summary>
    /// Raises the Wanted notices from what the client sees and shows them one at a time by priority:
    ///   WARNING (near the boss, 2 s) > bounty panel (until OK) > "X has returned!" (4 s) > "A Wanted Boss lurks in this area." (3 s)
    ///   > "Already claimed today - no bounty." (2.5 s). "Today's Wanted" (Login) is no longer raised (user 20/11): the WANTED card shows it.
    /// One banner occupies the toast area; a higher one arriving replaces it, lower ones wait (one pending per kind). While the bounty panel is
    /// open only WARNING may show. Ordinary toasts (pickups ...) wait in their own queue meanwhile (NotificationToast.Suspended), so nothing overlaps.
    /// WARNING swallows a waiting / showing area notice: walking in beside the boss gives one message, not two.
    /// Triggers, all read from the client's own state (nothing is sent):
    ///   Respawn  status Alive false -> true (hero not paid yet)
    ///   Area     the hero arrived on the boss's map while it stands (hero not paid yet)
    ///   Warning  Hud.BarBoss() became the Wanted boss: the same radius as the boss bar
    ///   Bounty   WantedBossTracker.BountyPaid
    ///   Already  the boss died beside a hero who had been paid before this kill
    /// </summary>
    public sealed partial class WantedNotices
    {
        const int Kinds = 6;
        const float MapTitleSeconds = 4.3f; // MapNameBanner: settle 0.5 + fade in 0.45 + hold 2.4 + fade out 0.8, in the same top-centre place
        const float WarningEverySeconds = 10f, NearMemorySeconds = 5f, FadeIn = 0.15f, FadeOut = 0.3f;
        static readonly float[] Seconds = { 2.5f, 5f, 3f, 4f, 0f, 2f }; // by WantedNotice

        readonly bool[] pending = new bool[Kinds];
        readonly WantedBossDef[] pendingDef = new WantedBossDef[Kinds];
        readonly WantedRewardPanel panel;
        int current = -1;
        float age;
        // What the triggers compare against.
        bool known, lastAlive, wasNear;
        string lastBoss, lastMap;
        int lastDay;
        float mapTitleUntil;
        float lastWarningAt = -100f, lastNearAt = -100f, lastPaidAt = -100f;

        public WantedNotices(RectTransform safe)
        {
            if (RoHud.Active) BuildRoBanners(safe); else BuildBanners(safe); // RO Soft HUD: WantedNotices.RoSoft.cs
            panel = new WantedRewardPanel(safe);
        }

        public void Enqueue(WantedNotice kind, WantedBossDef def)
        {
            pending[(int)kind] = true;
            pendingDef[(int)kind] = def;
            if (kind == WantedNotice.Warning) pending[(int)WantedNotice.Area] = false;
        }

        public void OnBountyPaid(WantedBossDef def)
        {
            lastPaidAt = Time.unscaledTime;
            pending[(int)WantedNotice.Already] = false;
            Enqueue(WantedNotice.Bounty, def);
        }

        /// <summary>HUD switch off or the status gone: hide everything and forget, so the next status counts as an arrival.</summary>
        public void Reset()
        {
            if (!known && current < 0 && !panel.IsOpen) return;
            known = false; wasNear = false; lastMap = null;
            for (int i = 0; i < Kinds; i++) pending[i] = false;
            if (current >= 0) End();
            panel.Close();
            NotificationToast.Suspended = false;
        }

        public void Tick(HeroController hero, float dt)
        {
            Detect(hero);
            if (pending[(int)WantedNotice.Bounty])
            {
                pending[(int)WantedNotice.Bounty] = false;
                Debug.Log("[ZenoniaProto] wanted notice: Bounty (panel)");
                panel.Open(pendingDef[(int)WantedNotice.Bounty]);
            }
            int next = -1;
            for (int i = Kinds - 1; i >= 0 && next < 0; i--) if (pending[i]) next = i;
            if (panel.IsOpen && next < (int)WantedNotice.Bounty) next = -1; // only WARNING shows over the panel
            if (Time.unscaledTime < mapTitleUntil) next = -1;               // the area title of a map just entered is on screen there: wait for it
            if (current >= 0)
            {
                age += dt;
                if (next > current || age >= Seconds[current] + FadeOut) End();
                else SetAlpha(Mathf.Min(Mathf.Clamp01(age / FadeIn), Mathf.Clamp01((Seconds[current] + FadeOut - age) / FadeOut)));
            }
            if (current < 0 && next >= 0) Begin(next);
            if (current >= 0) Place(); // the boss bar may grow a cast row under the banner while it shows
            if (current == (int)WantedNotice.Area) RefreshDistance(hero);
            NotificationToast.Suspended = current >= 0 || panel.IsOpen; // one message at a time while a notice or the bounty is up
        }

        void Begin(int kind)
        {
            pending[kind] = false;
            current = kind; age = 0f;
            Debug.Log("[ZenoniaProto] wanted notice: " + (WantedNotice)kind);
            Show((WantedNotice)kind, pendingDef[kind]);
            SetAlpha(0f);
        }

        void End()
        {
            Hide((WantedNotice)current);
            current = -1;
        }

        void Detect(HeroController hero)
        {
            var s = WantedBossTracker.Status;
            WantedBossDef today = WantedBossTracker.Today;
            bool claimed = PaperWanted.Claimed;
            float now = Time.unscaledTime;
            if (!known || s.BossType != lastBoss || s.DayKey != lastDay)
            {
                // No "Today's Wanted" card on arrival any more (user 20/11: "bỏ hiện cái popup ở giữa today wanted đi"): today's boss stays
                // on the WANTED card under the minimap (RoHudWanted) and the Wanted Board; the Login look is kept but never raised.
                known = true; lastBoss = s.BossType; lastDay = s.DayKey; lastAlive = s.Alive;
            }
            else if (s.Alive != lastAlive)
            {
                lastAlive = s.Alive;
                if (s.Alive) { if (!claimed) Enqueue(WantedNotice.Respawn, today); }
                // Paid before this kill (not by it: the claim of this kill arrives as BountyPaid within the same moments) and standing there.
                else if (claimed && now - lastNearAt < NearMemorySeconds && now - lastPaidAt > NearMemorySeconds) Enqueue(WantedNotice.Already, today);
            }
            string map = MapRegistry.Current != null ? MapRegistry.Current.Id : null;
            if (map != lastMap)
            {
                lastMap = map;
                mapTitleUntil = now + MapTitleSeconds;
                if (map == today.MapId && s.Alive && !claimed) Enqueue(WantedNotice.Area, today);
            }
            Monster boss = Hud.BarBoss();
            bool near = boss != null && boss.IsWanted;
            if (near) lastNearAt = now;
            if (near && !wasNear && now - lastWarningAt > WarningEverySeconds)
            {
                lastWarningAt = now;
                Enqueue(WantedNotice.Warning, today);
            }
            wasNear = near;
        }
    }
}
