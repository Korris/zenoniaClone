using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// A skill's shape resolved and locked at the moment its telegraph starts (world units). This is exactly what goes on the wire
    /// (NetEventType.WantedSkillTelegraph): boss id + skill id + Origin + DirDeg; the radius / angle / safe lanes come back from WantedSkillTable,
    /// shared by the server and every client, so changing a number only means rebuilding (sprite-rules.md "Bảng skill dùng chung với server").
    /// </summary>
    public struct WantedShapeInstance
    {
        public float OriginX, OriginY;
        /// <summary>Facing (Cone / Line), or the first safe lane's centre for a Circle with SafeArcCount &gt; 0. Unused by a plain Circle.</summary>
        public float DirDeg;
    }

    /// <summary>
    /// Engine-free hit test for a Wanted Boss skill: point-in-shape, built on SkillGeometry's exact box-vs-shape clipping called with a zero-size
    /// box. The edge of every shape is pulled in by EdgeMarginTiles ("nới 0,25 ô ở rìa" in the design doc) so a player standing right on the line
    /// is not hit by network jitter; a safe lane is widened by the same margin (converted to degrees at its radius) so it stays forgiving too.
    /// </summary>
    public static class WantedSkillArea
    {
        public const float EdgeMarginTiles = 0.25f;
        /// <summary>Mirrors GameConfig.TileUnits: this layer must stay UnityEngine-free so dotnet can test it directly.</summary>
        public const float TileUnits = 16f;

        public static bool Hits(WantedSkillDef skill, WantedShapeInstance shape, float px, float py)
        {
            float margin = EdgeMarginTiles * TileUnits;
            double rad = shape.DirDeg * Math.PI / 180.0;
            float dx = (float)Math.Cos(rad), dy = (float)Math.Sin(rad);
            switch (skill.Shape)
            {
                case WantedAreaShape.Line:
                    float len = Math.Max(0f, skill.LengthTiles * TileUnits - margin);
                    float w = Math.Max(0f, skill.WidthTiles * TileUnits - margin * 2f);
                    return SkillGeometry.InLine(shape.OriginX, shape.OriginY, dx, dy, len, w, px, py, px, py);
                case WantedAreaShape.Cone:
                    float coneR = Math.Max(0f, skill.RadiusTiles * TileUnits - margin);
                    return SkillGeometry.InSector(shape.OriginX, shape.OriginY, dx, dy, coneR, skill.AngleDeg, px, py, px, py);
                default:
                    float r = Math.Max(0f, skill.RadiusTiles * TileUnits - margin);
                    if (!SkillGeometry.InCircle(shape.OriginX, shape.OriginY, r, px, py, px, py)) return false;
                    return skill.SafeArcCount <= 0 || !InAnySafeLane(skill, shape, px, py, r);
            }
        }

        static bool InAnySafeLane(WantedSkillDef skill, WantedShapeInstance shape, float px, float py, float radiusUnits)
        {
            float angle = AngleDeg(px - shape.OriginX, py - shape.OriginY);
            float marginDeg = radiusUnits > 1f ? (float)(EdgeMarginTiles * TileUnits / radiusUnits * 180.0 / Math.PI) : 10f;
            for (int i = 0; i < skill.SafeArcCount; i++)
            {
                float center = shape.DirDeg + i * (360f / skill.SafeArcCount); // lanes spread evenly (2 => opposite)
                if (Math.Abs(Diff(angle, center)) <= skill.SafeArcDeg * 0.5f + marginDeg) return true;
            }
            return false;
        }

        /// <summary>Shortest signed difference a - b in degrees, in (-180, 180].</summary>
        static float Diff(float a, float b)
        {
            float d = (a - b) % 360f;
            if (d <= -180f) d += 360f; else if (d > 180f) d -= 360f;
            return d;
        }

        static float AngleDeg(float x, float y) { return (float)(Math.Atan2(y, x) * 180.0 / Math.PI); }
    }
}
