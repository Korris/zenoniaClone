using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// When a normal monster shows its RO foot label (HP bar + "Lv.N Name", Monster.RoFootLabel.cs). User 22/11: "cái ống máu và tên quái không hiện
    /// trước mặt lính nữa mà khi nào ấn vào mới hiện": by default only
    ///  - the monster you target (TargetMarker.Target: locked / walked to / clicked on / hit last online), and
    ///  - any monster your own hero damaged in the last <see cref="RecentHitSeconds"/> s.
    /// Settings "All monster names / HP" (PlayerPrefs, default off) brings back the always-on labels. Bosses / Wanted keep their own bars.
    /// `--monster-labels all|focus` (test runs, not saved) forces either mode; `all-nooutline` = all without the 8 RoTextOutline copies (FPS runs).
    /// </summary>
    public static class MonsterLabelPrefs
    {
        public const float RecentHitSeconds = 2f;
        const string Key = "zenonia.view.monsterLabelsAll";
        static bool loaded, showAll;

        /// <summary>Test only (`--monster-labels all-nooutline`): labels built without their 8 dark outline copies, to measure what the outline costs.</summary>
        public static bool NoOutline { get { Load(); return noOutline; } }
        static bool noOutline;

        public static bool ShowAll
        {
            get { Load(); return showAll; }
            set
            {
                Load();
                showAll = value;
                try { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
                catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] could not save the monster label setting: " + e.Message); } // still applies for this run
            }
        }

        /// <summary>Whether `m` (a normal monster under the RO look) draws its foot label this frame.</summary>
        public static bool Shows(Monster m, float lastHitByMeAt)
        {
            if (ShowAll) return true;
            if (Time.time - lastHitByMeAt <= RecentHitSeconds) return true;
            return m == TargetMarker.Shown; // the ring's target (picked once per frame in TargetMarker.LateUpdate)
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try { showAll = PlayerPrefs.GetInt(Key, 0) != 0; }
            catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] could not read the monster label setting: " + e.Message); }
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "--monster-labels");
            if (i >= 0 && i + 1 < args.Length) { showAll = args[i + 1].StartsWith("all"); noOutline = args[i + 1] == "all-nooutline"; }
        }
    }
}
