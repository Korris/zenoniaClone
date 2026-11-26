using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Where a Ground skill lands (GroundTargetRules has the engine-free maths). The authority clamps a wanted point into SkillDef.CastRange (and, for a
    /// leap / a turret, short of walls: ChatGPT "nhảy / đặt tháp chỉ chỗ hợp lệ"; rain and blasts may fall over a wall or water). Without a wanted point
    /// (Auto Battle, a mobile tap = smart cast, an old client) the centre covering the most monsters is picked, the current target counting double,
    /// else a point ahead of the hero at the skill's old fixed distance.
    /// </summary>
    public static class SkillGroundAim
    {
        static float[] xs = new float[64], ys = new float[64];

        /// <summary>The centre of this cast. `strict` (authority): clamp the wanted point; a replay takes the point it was given.</summary>
        public static Vector2 Point(SkillDef def, ISkillBody body, Vector2? wanted, bool strict)
        {
            Vector2 feet = body.Feet;
            if (wanted.HasValue && !strict) return wanted.Value;
            Vector2 p = wanted ?? Auto(def, feet, HeroSkillBody.DirOf(body.Facing), PreferredTarget(body.Hero));
            return Clamp(def, feet, p);
        }

        /// <summary>`p` pulled into the cast range of a caster at `feet` (and short of walls for a leap / a placement), inside the map.</summary>
        public static Vector2 Clamp(SkillDef def, Vector2 feet, Vector2 p)
        {
            float x, y;
            GroundTargetRules.ClampToRange(feet.x, feet.y, p.x, p.y, def.CastRange * GameConfig.TileUnits, out x, out y);
            var c = new Vector2(Mathf.Clamp(x, 0f, GameConfig.MapW), Mathf.Clamp(y, 0f, GameConfig.MapH));
            if (GroundTargetRules.IndicatorOf(def) != GroundIndicator.Area && MapRegistry.Current != null) c = SkillAim.ClampToWalls(feet, c);
            return c;
        }

        /// <summary>True when a leap / a turret could not reach the wanted point (a wall in between): the indicator shows the invalid ring.</summary>
        public static bool Blocked(SkillDef def, Vector2 feet, Vector2 wantedClampedToRange)
        {
            if (GroundTargetRules.IndicatorOf(def) == GroundIndicator.Area || MapRegistry.Current == null) return false;
            return (SkillAim.ClampToWalls(feet, wantedClampedToRange) - wantedClampedToRange).sqrMagnitude > 4f;
        }

        /// <summary>Smart point: densest monster group in reach (prefer = current target), else ahead of the hero.</summary>
        public static Vector2 Auto(SkillDef def, Vector2 feet, Vector2 dir, Monster prefer)
        {
            float u = GameConfig.TileUnits, range = def.CastRange * u, radius = Mathf.Max(0.5f, GroundTargetRules.AreaRadiusTiles(def)) * u;
            IReadOnlyList<Monster> all = Monster.All;
            int n = 0, preferIndex = -1;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m == null || m.IsDying) continue;
                if (n == xs.Length) { System.Array.Resize(ref xs, n * 2); System.Array.Resize(ref ys, n * 2); }
                if (m == prefer) preferIndex = n;
                xs[n] = m.Center.x; ys[n] = m.Center.y; n++;
            }
            float bx, by;
            if (GroundTargetRules.BestCentre(feet.x, feet.y, range, radius, xs, ys, n, preferIndex, out bx, out by)) return new Vector2(bx, by);
            return feet + dir * Mathf.Min(range, DefaultTiles(def) * u);
        }

        /// <summary>How far ahead the skill used to land (its fixed Facing distance), for the fallback point and the first indicator position.</summary>
        public static float DefaultTiles(SkillDef def)
        {
            if (def.Legacy == LegacySkill.IceStorm) return LegacySkillNumbers.IceStormRange / GameConfig.TileUnits;
            if (def.Legacy == LegacySkill.EarthBreak) return LegacySkillNumbers.EarthBreakRange / GameConfig.TileUnits;
            foreach (SkillEffect e in def.Effects)
            {
                if (e.Kind == EffectKind.Motion) return e.MoveTiles;
                if ((e.Kind == EffectKind.Rain || e.Kind == EffectKind.Strike) && e.Range > 0f) return e.Range;
            }
            return 1f; // turret, zone: just ahead of the feet
        }

        /// <summary>The monster the player is fighting: the clicked one on a client (ClickMove), the AimHint one on the zone server.</summary>
        public static Monster PreferredTarget(HeroController hero)
        {
            if (hero == null) return null;
            if (hero.AimPreferId != 0 && Zone.Main != null) { Monster m = Zone.Main.Ids.Get(hero.AimPreferId) as Monster; if (m != null && !m.IsDying) return m; }
            Monster c = hero == LocalPlayer.Hero ? ClickMove.Target : null;
            return c != null && !c.IsDying ? c : null;
        }
    }
}
