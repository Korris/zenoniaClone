using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ice-block overlay of a frozen hero (docs/wanted-boss-skills-design.md: Frozen Ground Stomp). Shown for every hero WantedFrozen reports
    /// true, not just the local player, wherever NetMode.RendersFx (sprite-rules.md "Quy tắc VFX skill" #7). Drawn over the whole hero (the art's middle is
    /// hollow: the body shows through it, the base crystals cover the feet); HeroRenderer.SetWantedFreezeTint tints the body
    /// itself, the only cue left once the ice block finishes breaking or when frozen-block.png is not painted yet.
    /// Sheet Sprites/fx/wanted/frozen-block.png: 4 x 2 cells of 384 x 512. Row 0 = freeze-in (0, 1) then hold (2, 3); row 1 = crack + shatter
    /// (4, 5, 6, 7). Feet anchor at (188, 440) px from the cell's top-left; the full ice spans 418 px tall, scaled to 1.3x a hero's own
    /// ~20-unit height (TurretRules.cs's own approximation).
    /// </summary>
    public partial class HeroController
    {
        const string FreezeFxPath = "Sprites/fx/wanted/frozen-block";
        const int FreezeCellW = 384, FreezeCellH = 512, FreezeCols = 4, FreezeRows = 2;
        const float FreezePivotX = 188f / FreezeCellW, FreezePivotY = 1f - 440f / FreezeCellH;
        const float FreezeFullPx = 418f, FreezeHeroHeightUnits = 20f;
        const float FreezePpu = FreezeFullPx / (FreezeHeroHeightUnits * 1.3f); // bakes the 1.3x-hero-height target straight into the sprite's ppu
        const float FreezeInFrameMs = 80f, FreezeHoldFrameMs = 80f, FreezeBreakFrameMs = 70f;

        static Sprite[] freezeFrames;
        static bool freezeFramesTried;

        SpriteRenderer freezeIceSr;
        bool freezeWasOn;
        int freezePhase; // 0 none, 1 freeze-in (frames 0-1), 2 hold loop (2-3), 3 breaking (4-7)
        float freezePhaseMs;

        static Sprite[] FreezeFrames()
        {
            if (freezeFramesTried) return freezeFrames;
            freezeFramesTried = true;
            if (SpriteSheet.TryLoad(FreezeFxPath) == null) return null; // not painted yet: the body tint alone carries the cue
            Sprite[][] grid = SpriteSheet.Grid(FreezeFxPath, FreezeCellW, FreezeCellH, FreezePpu, new Vector2(FreezePivotX, FreezePivotY));
            if (grid == null) return null;
            var f = new Sprite[FreezeCols * FreezeRows];
            int i = 0;
            for (int r = 0; r < FreezeRows && r < grid.Length; r++)
                for (int c = 0; c < FreezeCols && grid[r] != null && c < grid[r].Length; c++) f[i++] = grid[r][c];
            freezeFrames = f;
            return freezeFrames;
        }

        /// <summary>ArriveInZone (zone transfer): drops the ice block right away instead of playing a break it did not earn.</summary>
        void ClearWantedFreeze()
        {
            frozenMs = 0f; freezePhase = 0; freezeWasOn = false;
            if (freezeIceSr != null) freezeIceSr.enabled = false;
        }

        /// <summary>Called once per Render(): the body tint always follows WantedFrozen; the ice block picture plays its freeze / hold / break
        /// cycle on top of that, only while frozen-block.png is painted.</summary>
        void UpdateFrozenFx()
        {
            bool frozen = NetMode.RendersFx && WantedFrozen;
            view.SetWantedFreezeTint(frozen);
            if (!NetMode.RendersFx) return;
            if (frozen && !freezeWasOn) { freezePhase = 1; freezePhaseMs = 0f; }
            else if (!frozen && freezeWasOn && freezePhase != 0) { freezePhase = 3; freezePhaseMs = 0f; }
            freezeWasOn = frozen;
            if (freezePhase == 0) { if (freezeIceSr != null) freezeIceSr.enabled = false; return; }

            Sprite[] frames = FreezeFrames();
            if (frames == null) { freezePhase = frozen ? 2 : 0; return; } // no art: the body tint alone is the cue
            if (freezeIceSr == null) freezeIceSr = ProceduralSprites.Child(transform, "WantedFreeze", frames[0]);

            freezePhaseMs += GameClock.Dt * 1000f;
            int frame;
            if (freezePhase == 1)
            {
                int step = Mathf.FloorToInt(freezePhaseMs / FreezeInFrameMs);
                if (step >= 2) { freezePhase = 2; freezePhaseMs = 0f; frame = 2; }
                else frame = step;
            }
            else if (freezePhase == 3)
            {
                int step = Mathf.FloorToInt(freezePhaseMs / FreezeBreakFrameMs);
                if (step >= 4) { freezePhase = 0; freezeIceSr.enabled = false; return; }
                frame = 4 + step;
            }
            else frame = 2 + (Mathf.FloorToInt(freezePhaseMs / FreezeHoldFrameMs) % 2);
            freezeIceSr.enabled = true;
            freezeIceSr.sprite = frames[frame];
            freezeIceSr.sortingOrder = GameConfig.OrderBase(y) + 7; // over the whole hero (body + 3, hands / weapon glow + 6): the hero is INSIDE the block, seen through its hollow middle
        }
    }
}
