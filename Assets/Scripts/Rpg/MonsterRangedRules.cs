using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Optional ranged attack of a normal monster (MonsterDef.Ranged; null = melee). The monster stops once its target is within Range with a
    /// clear line, plays its ordinary windup and, when the windup ends, fires one straight projectile at where the target stands at that moment.
    /// Damage is the monster's normal Atk. Shared by client and server: changing a number means shipping both.
    /// </summary>
    public sealed class MonsterRangedProfile
    {
        /// <summary>Stops and shoots when the target is this close (world units, 16 = one tile).</summary>
        public float Range;
        /// <summary>Projectile speed, units per second.</summary>
        public float ProjectileSpeed;
        /// <summary>The projectile expires after flying this far (longer than Range, so backing straight away does not dodge it).</summary>
        public float MaxTravel;
        /// <summary>Side of the square hit box centred on the projectile.</summary>
        public float HitSize;
        /// <summary>0xRRGGBB tint of the projectile picture.</summary>
        public uint ColorRgb;
        /// <summary>Painted art, Resources/Sprites/fx/monster-shots/&lt;Sprite&gt;.png, drawn flying to screen-right; while the file is missing a small procedural quill is drawn.</summary>
        public string Sprite;

        /// <summary>Cliff Harpy (Lv 18): about 6 tiles of reach, a feather slow enough to sidestep (the hero walks about 60 units / s).</summary>
        public static readonly MonsterRangedProfile CliffHarpyFeather = new MonsterRangedProfile
            { Range = 100f, ProjectileSpeed = 130f, MaxTravel = 150f, HitSize = 6f, ColorRgb = 0xF2E3B0, Sprite = "cliff-harpy-feather" };
    }

    public enum MonsterShotEnd { Flying, Hit, Wall, Expired }

    /// <summary>Engine-free rules of a monster's ranged attack: when to stop and shoot, and how its projectile flies (dotnet-tested).</summary>
    public static class MonsterRangedRules
    {
        /// <summary>Sampling step of the line of sight and of the flight (a tile is 16 units, a hero box 10 x 8).</summary>
        public const float StepUnits = 2f;

        /// <summary>True when the shooter at (ax, ay) should stop and wind up: the target (bx, by) is within range and no solid tile lies between
        /// them (otherwise it keeps walking closer, like a melee monster, instead of shooting a wall).</summary>
        public static bool ShouldShoot(MonsterRangedProfile p, float ax, float ay, float bx, float by, Func<float, float, bool> solidAt)
        {
            float dx = bx - ax, dy = by - ay;
            return dx * dx + dy * dy <= p.Range * p.Range && ClearLine(ax, ay, bx, by, solidAt);
        }

        public static bool ClearLine(float ax, float ay, float bx, float by, Func<float, float, bool> solidAt)
        {
            float dx = bx - ax, dy = by - ay;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            int steps = Math.Max(1, (int)Math.Ceiling(dist / StepUnits));
            for (int i = 1; i <= steps; i++)
                if (solidAt(ax + dx * i / steps, ay + dy * i / steps)) return false;
            return true;
        }

        /// <summary>
        /// Flies the projectile `dist` further along (dirX, dirY) (unit vector) in sub-steps, so a slow tick cannot carry it through a hero or a
        /// thin wall. Stops at the first thing met, in this order per sub-step: a solid tile, a target (`hitAt`), the end of MaxTravel.
        /// </summary>
        public static MonsterShotEnd Advance(ref float x, ref float y, float dirX, float dirY, float dist, ref float travelled, float maxTravel,
            Func<float, float, bool> solidAt, Func<float, float, bool> hitAt)
        {
            int steps = Math.Max(1, (int)Math.Ceiling(dist / StepUnits));
            float step = dist / steps;
            for (int i = 0; i < steps; i++)
            {
                x += dirX * step; y += dirY * step; travelled += step;
                if (solidAt(x, y)) return MonsterShotEnd.Wall;
                if (hitAt != null && hitAt(x, y)) return MonsterShotEnd.Hit;
                if (travelled >= maxTravel) return MonsterShotEnd.Expired;
            }
            return MonsterShotEnd.Flying;
        }

        /// <summary>The projectile's square (side `size`, centred on px, py) overlaps a y-up box (bottom-left boxX, boxY).</summary>
        public static bool Touches(float px, float py, float size, float boxX, float boxY, float boxW, float boxH)
        {
            float h = size * 0.5f;
            return px + h > boxX && px - h < boxX + boxW && py + h > boxY && py - h < boxY + boxH;
        }
    }
}
