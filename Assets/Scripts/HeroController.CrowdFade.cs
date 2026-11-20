using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// 40-player readability (ChatGPT RO3 advice, 2026-11-20): another hero drawn in front of the local hero and covering it fades to ~50 %, so
    /// your own hero stays readable in a crowd. Your own hero is never faded. One rectangle test per hero per frame: cheap with 40 heroes.
    /// </summary>
    public partial class HeroController
    {
        /// <summary>Alpha of a hero that covers the local hero (0.45-0.6 suggested).</summary>
        const float CoverAlpha = 0.5f;
        /// <summary>Painted hero box around the feet, world units (hero ~20 tall at figure scale 1.15, ~14 wide).</summary>
        const float CoverHalfWidth = 11f, CoverHeight = 22f;
        /// <summary>Fade in / out speed (alpha per second): no pop when someone walks past.</summary>
        const float CoverFadeSpeed = 6f;

        float crowdAlpha = 1f;

        float CrowdFadeAlpha()
        {
            HeroController me = LocalPlayer.Hero;
            float goal = 1f;
            if (me != null && me != this && !dead && !me.IsDead)
            {
                Vector2 a = Position, b = me.Position;
                float front = b.y - a.y; // this hero stands lower on screen = drawn over the local one
                if (front > 0f && front < CoverHeight && Mathf.Abs(a.x - b.x) < CoverHalfWidth * 2f * 0.8f) goal = CoverAlpha;
            }
            crowdAlpha = Mathf.MoveTowards(crowdAlpha, goal, CoverFadeSpeed * GameClock.Dt);
            return crowdAlpha;
        }
    }
}
