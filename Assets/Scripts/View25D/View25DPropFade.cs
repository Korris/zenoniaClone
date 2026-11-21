using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// 2.5D maps: a tall standing prop (tree, pillar) in front of the local hero fades so the hero stays visible (ChatGPT art direction 21/11/2026,
    /// docs/reference/willowbrook-25d/willowbrook-forest-art-direction-chatgpt.md: fade the occluder to ~20-35 %, out in ~0.18 s, back in ~0.3 s).
    /// Props register when View25D.StandProp stands them up; each frame only the props near the hero are tested on screen: the prop's base is
    /// south of the hero (drawn over it) and the hero's chest (as wide as the body) overlaps the prop's screen rect (narrowed a little, so the thin edge of a crown does not fade it).
    /// Its ground shadow (baked FakeSun mask) does not change.
    /// </summary>
    public static class View25DPropFade
    {
        sealed class Entry { public SpriteRenderer Sr; public float BaseY; public float Alpha = 1f; }

        const float MinHeight = 40f;          // world units (2.5 tiles): lower props never hide a hero
        const float NearRange = 220f;         // only props whose base is this close to the hero are tested
        const float FadedAlpha = 0.32f, FadeOutPerSec = 1f / 0.18f, FadeInPerSec = 1f / 0.3f;
        const float RectShrinkX = 0.06f;      // share of the prop's screen width ignored on each side (the crown's thin outer leaves)
        static readonly List<Entry> Props = new List<Entry>();

        /// <summary>Called for every standing prop of a 2.5D map when it is built.</summary>
        public static void Register(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null || sr.sprite.bounds.size.y < MinHeight) return;
            Props.Add(new Entry { Sr = sr, BaseY = sr.transform.position.y });
        }

        /// <summary>Per frame from View25DWorld while a 2.5D map is on screen.</summary>
        public static void Tick(Camera cam)
        {
            HeroController hero = LocalPlayer.Hero;
            if (cam == null) return;
            Vector3 hp = hero != null ? hero.transform.position : Vector3.zero;
            Vector3 chest = cam.WorldToScreenPoint(View25D.Above(hp.x, hp.y, 14f));
            float halfW = Mathf.Abs(cam.WorldToScreenPoint(View25D.Above(hp.x + 7f, hp.y, 14f)).x - chest.x); // half the hero's body width on screen
            float dt = Time.unscaledDeltaTime;
            for (int i = Props.Count - 1; i >= 0; i--)
            {
                Entry e = Props[i];
                if (e.Sr == null) { Props.RemoveAt(i); continue; } // map rebuilt
                bool hide = hero != null && e.BaseY < hp.y && Mathf.Abs(e.Sr.transform.position.x - hp.x) < NearRange && hp.y - e.BaseY < NearRange && Covers(cam, e.Sr, chest, halfW);
                float target = hide ? FadedAlpha : 1f;
                if (Mathf.Approximately(e.Alpha, target)) continue;
                e.Alpha = Mathf.MoveTowards(e.Alpha, target, (hide ? FadeOutPerSec : FadeInPerSec) * dt);
                Color c = e.Sr.color;
                c.a = e.Alpha;
                e.Sr.color = c;
            }
        }

        /// <summary>The screen point lies inside the prop's screen rect (from its 3D bounds, narrowed on both sides).</summary>
        static bool Covers(Camera cam, SpriteRenderer sr, Vector3 p, float halfW)
        {
            Bounds b = sr.bounds;
            Vector3 lo = cam.WorldToScreenPoint(new Vector3(b.min.x, b.min.y, b.max.z)), hi = cam.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, b.min.z));
            float x0 = Mathf.Min(lo.x, hi.x), x1 = Mathf.Max(lo.x, hi.x), y0 = Mathf.Min(lo.y, hi.y), y1 = Mathf.Max(lo.y, hi.y);
            float shrink = (x1 - x0) * RectShrinkX;
            return p.x + halfW > x0 + shrink && p.x - halfW < x1 - shrink && p.y > y0 && p.y < y1;
        }

        /// <summary>Back to opaque (2.5D off or another map).</summary>
        public static void Reset()
        {
            foreach (Entry e in Props) if (e.Sr != null) { Color c = e.Sr.color; c.a = 1f; e.Sr.color = c; }
            Props.Clear();
        }
    }
}
