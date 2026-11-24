using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Engine-free rules of a boss CHARGE (WantedSkillDef.ChargeMs &gt; 0; first user: Brambleback Matriarch's Bramble Charge). The warning is the locked
    /// Line shape (WantedShapeInstance: origin + direction, never re-aimed); when it ends the boss dashes along that line and each hero the body reaches
    /// while still inside the rectangle takes the hit once. Pure so the timing and the hit test are unit-tested without Unity (Monster.WantedCharge.cs applies them).
    /// </summary>
    public static class WantedChargeRules
    {
        /// <summary>Dash speed in world units per second: the whole line in ChargeMs.</summary>
        public static float SpeedUnitsPerSec(WantedSkillDef skill)
        {
            return skill.ChargeMs <= 0 ? 0f : skill.LengthTiles * WantedSkillArea.TileUnits / (skill.ChargeMs / 1000f);
        }

        /// <summary>How far along the line (world units) the boss has dashed after `elapsedMs` of the charge (clamped to the line).</summary>
        public static float Travelled(WantedSkillDef skill, float elapsedMs)
        {
            float full = skill.LengthTiles * WantedSkillArea.TileUnits;
            return Math.Max(0f, Math.Min(full, SpeedUnitsPerSec(skill) * elapsedMs / 1000f));
        }

        /// <summary>
        /// True when the dashing body, `travelled` units from the origin along the locked direction, has reached the point (px, py) AND the point lies in the
        /// warned rectangle (the same edge margin as every other shape). `reach` = how far ahead of the origin point the body's front edge sits (half its length).
        /// </summary>
        public static bool Reached(WantedSkillDef skill, WantedShapeInstance shape, float travelled, float reach, float px, float py)
        {
            if (!WantedSkillArea.Hits(skill, shape, px, py)) return false;
            double rad = shape.DirDeg * Math.PI / 180.0;
            float along = (px - shape.OriginX) * (float)Math.Cos(rad) + (py - shape.OriginY) * (float)Math.Sin(rad);
            return along <= travelled + reach;
        }
    }
}
