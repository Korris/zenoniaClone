using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The two sound switches of the Settings window: background music and sound effects (hero swings, hits, skills, loot).
    /// A per-device preference kept in PlayerPrefs (not in the save or on the server); both default to on. GameMusic fades to silence
    /// when music is off, GameSfx / SkillSfx / skill loops skip their clips when sound effects are off.
    /// </summary>
    public static class AudioPrefs
    {
        const string MusicKey = "zenonia.audio.music", SfxKey = "zenonia.audio.sfx";
        static bool loaded, musicOn = true, sfxOn = true;

        /// <summary>Background music plays (GameMusic).</summary>
        public static bool MusicOn { get { Load(); return musicOn; } set { Load(); musicOn = value; Store(MusicKey, value); } }

        /// <summary>Sound effects play (GameSfx, SkillSfx, skill loop sources). Turning them off also silences what is playing now.</summary>
        public static bool SfxOn
        {
            get { Load(); return sfxOn; }
            set { Load(); sfxOn = value; Store(SfxKey, value); GameSfx.ApplyMute(!value); }
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                musicOn = PlayerPrefs.GetInt(MusicKey, 1) != 0;
                sfxOn = PlayerPrefs.GetInt(SfxKey, 1) != 0;
            }
            catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] could not read the sound settings: " + e.Message); } // keep both on
        }

        static void Store(string key, bool on)
        {
            try { PlayerPrefs.SetInt(key, on ? 1 : 0); PlayerPrefs.Save(); }
            catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] could not save the sound settings: " + e.Message); } // still applies for this run
        }
    }
}
