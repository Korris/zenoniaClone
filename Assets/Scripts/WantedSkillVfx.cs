using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Impact picture of a Wanted Boss skill, played once the telegraph's locked strike instant arrives (WantedSkillTelegraphFx). One declared
    /// piece per boss + skill id is the single place to change when new art lands (sprite-rules.md "Skill mới phải nối đủ" / "một chỗ khai báo
    /// đường dẫn + bố cục ô"): Resources/Sprites/fx/wanted/&lt;bossId&gt;-skill&lt;N&gt;.png (N = skill id + 1), 1536x1024, a 2x2 grid of 768x512
    /// cells read left-to-right / top-to-bottom as (start, mid, peak, end), transparent background. AnchorPx is the point every frame shares
    /// (apex of a cone / centre of a circle, measured from the cell's top-left); RefPxX/Y is the pixel distance that maps to the skill's own
    /// RadiusTiles, independently per axis (the ground is drawn a little flat, so circles use an ellipse). A boss + skill with no entry here
    /// falls back to the older combined "&lt;bossId&gt;-skills.png" sheet (4x4 cells of 384x256, row = skill id, centred) while its own picture has
    /// not landed yet, then to nothing at all -- the ground telegraph alone still carries the warning.
    /// </summary>
    public static class WantedSkillVfx
    {
        const int NewCellW = 768, NewCellH = 512, NewCols = 2, NewRows = 2;
        const int OldCellW = 384, OldCellH = 256;
        const float FrameMs = 130f;

        struct Piece { public float AnchorPxX, AnchorPxY, RefPxX, RefPxY; public bool Cone, Line, RotatesWithDir; } // Line: anchor = start of the line, RefPxX = half its width, RefPxY = its length

        static readonly Dictionary<string, Piece> Pieces = new Dictionary<string, Piece>
        {
            // Rimeclaw Tyrant (coordinator numbers, 2026-11-12): skill1 cone apex + reach; skill2/3 plain circles (never rotate: skill2's chain
            // always hangs up); skill4 (enrage) rotates -- its own art already draws the safe gap, pointing south by default like the cone.
            ["rimeclawTyrant:0"] = new Piece { AnchorPxX = 384f, AnchorPxY = 35f, RefPxX = 445f, RefPxY = 445f, Cone = true },
            ["rimeclawTyrant:1"] = new Piece { AnchorPxX = 384f, AnchorPxY = 284f, RefPxX = 248f, RefPxY = 197f },
            ["rimeclawTyrant:2"] = new Piece { AnchorPxX = 380f, AnchorPxY = 255f, RefPxX = 325f, RefPxY = 240f },
            ["rimeclawTyrant:3"] = new Piece { AnchorPxX = 380f, AnchorPxY = 237f, RefPxX = 275f, RefPxY = 215f, RotatesWithDir = true },
            // Glacial Colossus (2026-11-12): skill1 a straight strip from the top centre down (RefPxX = half its width); skill2 a cone drawn a
            // little narrower than 100 degrees, so X stretches more than Y.
            ["glacialColossus:0"] = new Piece { AnchorPxX = 380f, AnchorPxY = 10f, RefPxX = 90f, RefPxY = 490f, Line = true },
            ["glacialColossus:1"] = new Piece { AnchorPxX = 384f, AnchorPxY = 15f, RefPxX = 437f, RefPxY = 470f, Cone = true },
            ["glacialColossus:2"] = new Piece { AnchorPxX = 386f, AnchorPxY = 300f, RefPxX = 230f, RefPxY = 165f }, // one per targeted player
            ["glacialColossus:3"] = new Piece { AnchorPxX = 383f, AnchorPxY = 230f, RefPxX = 300f, RefPxY = 215f, RotatesWithDir = true }, // two empty wedges, south + north
            // Mire Hydra Queen (2026-11-12): skill2 and skill4 share one venom bloom picture (Tools/build-wanted-boss-sheets.py FX_SHARED).
            ["mireHydraQueen:0"] = new Piece { AnchorPxX = 384f, AnchorPxY = 29f, RefPxX = 440f, RefPxY = 440f, Cone = true },
            ["mireHydraQueen:1"] = new Piece { AnchorPxX = 380f, AnchorPxY = 290f, RefPxX = 265f, RefPxY = 172f },
            ["mireHydraQueen:2"] = new Piece { AnchorPxX = 384f, AnchorPxY = 245f, RefPxX = 330f, RefPxY = 215f }, // ring, empty middle for the queen
            ["mireHydraQueen:3"] = new Piece { AnchorPxX = 380f, AnchorPxY = 290f, RefPxX = 265f, RefPxY = 172f }, // same picture as skill 2
        };

        public static void Play(string bossId, WantedSkillDef skill, Vector2 at, float dirDeg)
        {
            int skillId = skill.Id;
            WantedAreaShape shape = skill.Shape;
            float u = WantedSkillArea.TileUnits, radiusUnits = skill.RadiusTiles * u;
            if (!NetMode.RendersFx || !FeelConfig.FxOn) return;
            Piece piece;
            string newPath = "Sprites/fx/wanted/" + bossId + "-skill" + (skillId + 1);
            if (Pieces.TryGetValue(bossId + ":" + skillId, out piece) && SpriteSheet.TryLoad(newPath) != null)
            {
                Vector2 pivot = new Vector2(piece.AnchorPxX / NewCellW, 1f - piece.AnchorPxY / NewCellH);
                Sprite[][] grid = SpriteSheet.Grid(newPath, NewCellW, NewCellH, 1f, pivot); // ppu 1: 1 source px = 1 world unit before the per-axis scale below
                Sprite[] frames = Flatten(grid, NewRows, NewCols);
                if (frames == null) return;
                float angle = piece.Cone || piece.Line || piece.RotatesWithDir ? dirDeg + 90f : 0f; // the art points "down" (world -Y) unrotated, like the ground cone
                var anim = SkillFxAnim.Play(frames, 1000f / FrameMs, at, angle, 1f, UltimateFx.GroundFxOrder + 2); // on the ground, under every hero and the boss: the burst must not hide who stands in it
                float sx = piece.Line ? skill.WidthTiles * u * 0.5f : radiusUnits, sy = piece.Line ? skill.LengthTiles * u : radiusUnits;
                if (anim != null) anim.transform.localScale = new Vector3(sx / piece.RefPxX, sy / piece.RefPxY, 1f);
                return;
            }
            string oldPath = "Sprites/fx/wanted/" + bossId + "-skills";
            if (SpriteSheet.TryLoad(oldPath) == null) return; // nothing painted yet: the ground telegraph alone still shows
            Sprite[][] oldGrid = SpriteSheet.Grid(oldPath, OldCellW, OldCellH, OldCellH, new Vector2(0.5f, 0.5f));
            Sprite[] oldFrames = SpriteSheet.Row(oldGrid, skillId);
            if (oldFrames == null) return;
            float oldScale = (shape == WantedAreaShape.Circle ? radiusUnits * 2f : radiusUnits) * 0.6f; // small centred icon, not a full-size shape
            SkillFxAnim.Play(oldFrames, 1000f / FrameMs, at, 0f, oldScale, UltimateFx.GroundFxOrder + 2); // on the ground, under every hero and the boss: the burst must not hide who stands in it
        }

        static Sprite[] Flatten(Sprite[][] grid, int rows, int cols)
        {
            if (grid == null) return null;
            var f = new Sprite[rows * cols];
            int i = 0;
            for (int r = 0; r < rows && r < grid.Length; r++)
                for (int c = 0; c < cols && grid[r] != null && c < grid[r].Length; c++) f[i++] = grid[r][c];
            return f;
        }
    }
}
