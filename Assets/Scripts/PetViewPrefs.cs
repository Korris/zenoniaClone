using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Settings window switch "Other players' pets" (Pet Companion, user 16/11/2026): off hides the pets of other heroes and their skill pictures on this device only.
    /// The server still runs their skills (the damage and shields are real); one's own pet always shows. PlayerPrefs, default on.
    /// </summary>
    public static class PetViewPrefs
    {
        const string Key = "zenonia.view.otherPets";
        static bool loaded, showOthers = true;

        public static bool ShowOthers
        {
            get { Load(); return showOthers; }
            set
            {
                Load();
                showOthers = value;
                try { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
                catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] could not save the pet view setting: " + e.Message); } // still applies for this run
            }
        }

        /// <summary>Whether this hero's pet (and its skill pictures) are drawn here.</summary>
        public static bool Shows(HeroController hero) { return hero != null && (hero == LocalPlayer.Hero || ShowOthers); }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try { showOthers = PlayerPrefs.GetInt(Key, 1) != 0; }
            catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] could not read the pet view setting: " + e.Message); }
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--hide-other-pets") >= 0) showOthers = false; // capture / e2e runs: as if switched off, nothing saved
        }
    }
}
