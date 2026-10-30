using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Looping background music from Resources/Audio/music (frost-hollow, oakhaven-village). Play() fades the new track in and the old one
    /// out (two sources), so a map change never cuts the music abruptly. Unscaled time: hit-stop and the paused inventory do not affect it.
    /// </summary>
    public class GameMusic : MonoBehaviour
    {
        public const float Volume = 0.55f;
        const float FadeSeconds = 1.2f;
        static GameMusic instance;
        AudioSource current, previous;
        string playing;

        public static void Init(Transform parent)
        {
            var go = new GameObject("Music");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<GameMusic>();
            instance.current = instance.NewSource();
            instance.previous = instance.NewSource();
        }

        AudioSource NewSource()
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        /// <summary>Crossfades to Resources/Audio/music/&lt;track&gt;; a missing file logs once and keeps the current music.</summary>
        public static void Play(string track)
        {
            if (instance == null || track == instance.playing) return;
            var clip = Resources.Load<AudioClip>("Audio/music/" + track);
            if (clip == null) { Debug.LogWarning("[ZenoniaProto] Missing music Resources/Audio/music/" + track); return; }
            instance.playing = track;
            AudioSource s = instance.previous; // swap: the old track fades out on `previous`
            instance.previous = instance.current;
            instance.current = s;
            s.clip = clip;
            s.volume = 0f;
            s.Play();
        }

        void Update()
        {
            float step = Time.unscaledDeltaTime / FadeSeconds * Volume;
            current.volume = Mathf.MoveTowards(current.volume, Volume, step);
            previous.volume = Mathf.MoveTowards(previous.volume, 0f, step);
            if (previous.isPlaying && previous.volume <= 0f) previous.Stop();
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
