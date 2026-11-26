using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Which ground indicator a Ground skill shows while aiming (docs/reference/skill-targeting/skill-targeting-design-chatgpt.md, "3 loại indicator").</summary>
    public enum GroundIndicator { Area, Placement, Leap }

    /// <summary>
    /// Ground-targeted skills (SkillTargeting.Ground, ChatGPT "Smart Ground Targeting" 26/11/2026): the player picks the centre of the skill inside
    /// SkillDef.CastRange tiles. Engine-free (server, client and dotnet tests share it): range clamp of a wanted point, the AoE radius the indicator draws,
    /// the indicator kind, and the auto pick (Auto Battle, mobile tap = smart cast, a frame without a point) = the centre that covers the most monsters.
    /// Distances in world units unless the name says tiles.
    /// </summary>
    public static class GroundTargetRules
    {
        public const float TileUnits = 16f;

        public static bool IsGround(SkillDef def) { return def != null && def.Targeting == SkillTargeting.Ground; }

        /// <summary>`(tx, ty)` pulled back onto the cast range circle around the caster `(fx, fy)` when it lies farther out.</summary>
        public static void ClampToRange(float fx, float fy, float tx, float ty, float rangeUnits, out float x, out float y)
        {
            float dx = tx - fx, dy = ty - fy, d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (float.IsNaN(d)) { x = fx; y = fy; return; }
            if (d <= rangeUnits || d < 1e-4f) { x = tx; y = ty; return; }
            float k = rangeUnits / d;
            x = fx + dx * k; y = fy + dy * k;
        }

        /// <summary>True when `(tx, ty)` is within the cast range of a caster at `(fx, fy)` (+ `slackUnits`: the server's hero can stand a hair off the client's).</summary>
        public static bool InRange(float fx, float fy, float tx, float ty, float rangeUnits, float slackUnits = 0f)
        {
            float dx = tx - fx, dy = ty - fy, r = rangeUnits + slackUnits;
            return dx * dx + dy * dy <= r * r;
        }

        /// <summary>Indicator kind: a Turret = Placement, a Leap = Leap Impact, anything else = Area Circle.</summary>
        public static GroundIndicator IndicatorOf(SkillDef def)
        {
            if (def == null) return GroundIndicator.Area;
            foreach (SkillEffect e in def.Effects)
            {
                if (e.Kind == EffectKind.Turret) return GroundIndicator.Placement;
                if (e.Kind == EffectKind.Motion && e.Motion == MotionKind.Leap) return GroundIndicator.Leap;
            }
            return GroundIndicator.Area;
        }

        /// <summary>Turret footprint the Placement ring shows (tiles; ChatGPT: "Vòng đặt trụ R0,65").</summary>
        public const float TurretFootTiles = 0.65f;

        /// <summary>
        /// Radius (tiles) the area ring shows = the real hit area round the chosen centre: a strike / heal / zone circle's Radius, a rain's scatter disc
        /// (ImpactRadius is the extra reach of each drop: see <see cref="OuterRadiusTiles"/>), the hand-made Ice Storm / Earth Break circles.
        /// </summary>
        public static float AreaRadiusTiles(SkillDef def)
        {
            if (def == null) return 0f;
            if (def.Legacy == LegacySkill.IceStorm) return LegacySkillNumbers.IceStormRadius / TileUnits;
            if (def.Legacy == LegacySkill.EarthBreak) return LegacySkillNumbers.EarthBreakRadius / TileUnits;
            if (IndicatorOf(def) == GroundIndicator.Placement) return TurretFootTiles;
            float r = 0f;
            foreach (SkillEffect e in def.Effects)
                if ((e.Kind == EffectKind.Strike || e.Kind == EffectKind.Rain || e.Kind == EffectKind.Zone || e.Kind == EffectKind.Heal) && e.Radius > r) r = e.Radius;
            return r;
        }

        /// <summary>Rain skills: scatter radius + one drop's blast (the faint "max reach" ring ChatGPT asked for); others: the area radius.</summary>
        public static float OuterRadiusTiles(SkillDef def)
        {
            float r = AreaRadiusTiles(def);
            if (def != null) foreach (SkillEffect e in def.Effects) if (e.Kind == EffectKind.Rain) return e.Radius + e.ImpactRadius;
            return r;
        }

        /// <summary>
        /// Auto pick: among the candidate centres (each monster in reach, then clamped into range), the one whose `radiusUnits` circle holds the most
        /// monster centres; `preferIndex` (the current target, -1 = none) counts double and wins ties. xs / ys = monster centres, `count` of them.
        /// Returns false when no monster lies within range + radius (the caller falls back to a point ahead of the hero).
        /// </summary>
        public static bool BestCentre(float fx, float fy, float rangeUnits, float radiusUnits, float[] xs, float[] ys, int count, int preferIndex, out float bx, out float by)
        {
            bx = fx; by = fy;
            float reach = rangeUnits + radiusUnits, best = -1f, r2 = radiusUnits * radiusUnits;
            for (int i = 0; i < count; i++)
            {
                if (!InRange(fx, fy, xs[i], ys[i], reach)) continue;
                float cx, cy;
                ClampToRange(fx, fy, xs[i], ys[i], rangeUnits, out cx, out cy);
                float score = 0f;
                for (int j = 0; j < count; j++)
                {
                    float dx = xs[j] - cx, dy = ys[j] - cy;
                    if (dx * dx + dy * dy <= r2) score += j == preferIndex ? 2f : 1f;
                }
                if (i == preferIndex) score += 0.5f;
                if (score > best) { best = score; bx = cx; by = cy; }
            }
            return best >= 0f;
        }
    }
}
