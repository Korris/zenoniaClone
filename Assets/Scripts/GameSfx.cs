using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One-shot sound effects from Resources/Audio/sfx (2D, no spatialisation). Clips load on first use and are cached; a missing clip is
    /// skipped silently (logged once). A small pool of AudioSources lets swings, shouts and hits overlap.
    /// </summary>
    public static class GameSfx
    {
        const int Voices = 8;
        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        static readonly List<AudioSource> Pool = new List<AudioSource>(Voices);
        static int next;

        public static void Init(Transform parent)
        {
            Clips.Clear();
            Pool.Clear();
            next = 0;
            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            for (int i = 0; i < Voices; i++)
            {
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                Pool.Add(s);
            }
        }

        static AudioClip Clip(string path)
        {
            AudioClip c;
            if (Clips.TryGetValue(path, out c)) return c;
            c = Resources.Load<AudioClip>("Audio/sfx/" + path);
            if (c == null) Debug.LogWarning("[ZenoniaProto] Missing sound Resources/Audio/sfx/" + path);
            Clips[path] = c;
            return c;
        }

        /// <summary>Plays a clip (path under Resources/Audio/sfx, no extension) with a small random pitch so repeats don't sound identical.</summary>
        public static void Play(string path, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (Pool.Count == 0) return;
            AudioClip clip = Clip(path);
            if (clip == null) return;
            AudioSource s = Pool[next];
            next = (next + 1) % Pool.Count;
            s.clip = clip;
            s.volume = volume;
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            s.Play();
        }

        /// <summary>Plays one of `variants` (e.g. "warrior/hit-a", "warrior/hit-b") at random.</summary>
        public static void PlayAny(string[] variants, float volume = 1f)
        {
            if (variants != null && variants.Length > 0) Play(variants[Random.Range(0, variants.Length)], volume);
        }
    }
}
