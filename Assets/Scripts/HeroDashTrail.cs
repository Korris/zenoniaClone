using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Afterimages of a dash, view only: while the hero dashes, a copy of the figure as drawn (body, gear, fist) is left behind every few ms, up to four at once, each fading out.
    /// Copies never move, so the newest stands next to the hero and the oldest is the faintest. Enhance glow is not copied.
    /// </summary>
    public sealed class HeroDashTrail : MonoBehaviour
    {
        const int GhostCount = 4;
        const float EveryMs = 40f, FadeMs = 240f, StartAlpha = 0.55f;
        const int BehindHero = 8; // one entity stride of the draw order

        SpriteRenderer[] sources;
        SpriteRenderer[][] ghosts;
        readonly float[] ages = new float[GhostCount];
        Transform holder;
        float sinceLast = EveryMs;
        int next;

        public void Init(SpriteRenderer[] figure)
        {
            sources = figure;
            holder = new GameObject("DashTrail").transform;
            holder.SetParent(transform.parent, false);
            ghosts = new SpriteRenderer[GhostCount][];
            for (int g = 0; g < GhostCount; g++)
            {
                ages[g] = FadeMs;
                ghosts[g] = new SpriteRenderer[figure.Length];
                for (int i = 0; i < figure.Length; i++)
                {
                    ghosts[g][i] = new GameObject("Ghost").AddComponent<SpriteRenderer>();
                    ghosts[g][i].transform.SetParent(holder, false);
                    ghosts[g][i].enabled = false;
                }
            }
        }

        /// <summary>Called after the figure is drawn for this frame.</summary>
        public void Tick(bool dashing, float dtMs)
        {
            sinceLast += dtMs;
            if (dashing && sinceLast >= EveryMs) { sinceLast = 0f; Leave(); }
            if (!dashing) sinceLast = EveryMs; // the next dash leaves its first image at once
            for (int g = 0; g < GhostCount; g++)
            {
                if (ages[g] >= FadeMs) continue;
                ages[g] += dtMs;
                float t = Mathf.Clamp01(ages[g] / FadeMs);
                for (int i = 0; i < ghosts[g].Length; i++)
                {
                    SpriteRenderer sr = ghosts[g][i];
                    if (t >= 1f) { sr.enabled = false; continue; }
                    Color c = sr.color; c.a = StartAlpha * (1f - t); sr.color = c;
                }
            }
        }

        void Leave()
        {
            int g = next;
            next = (next + 1) % GhostCount;
            ages[g] = 0f;
            for (int i = 0; i < sources.Length; i++)
            {
                SpriteRenderer from = sources[i], to = ghosts[g][i];
                to.enabled = from.sprite != null;
                if (!to.enabled) continue;
                to.sprite = from.sprite;
                to.flipX = from.flipX;
                to.color = new Color(1f, 1f, 1f, StartAlpha);
                to.sortingOrder = from.sortingOrder - BehindHero;
                Transform a = from.transform, b = to.transform;
                b.SetPositionAndRotation(a.position, a.rotation);
                b.localScale = a.lossyScale;
            }
        }

        void OnDestroy() { if (holder != null) Destroy(holder.gameObject); }
    }
}
