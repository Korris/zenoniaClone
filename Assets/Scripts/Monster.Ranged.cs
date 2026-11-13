using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Ranged attack of a normal monster (def.Ranged, MonsterRangedProfile; only the Cliff Harpy has one). Monster.Ai.cs's Chase asks CanShoot
    /// instead of the melee reach check; the ordinary Windup / Cooldown states and attack frames are reused, and when the windup ends
    /// Monster.Strike calls FireRangedShot instead of hitting directly. The flight and the hit are MonsterProjectile's.
    /// </summary>
    public partial class Monster
    {
        /// <summary>The shot leaves from / is aimed at this height above the feet: the middle of a hero's 8-unit body box.</summary>
        const float RangedAimLift = 4f;
        /// <summary>World point -> solid tile (map edge, wall, water, prop footprint): blocks the line of sight and the projectile alike.</summary>
        public static readonly Func<float, float, bool> SolidAt = (px, py) => FrostHollowMap.RectBlocked(px - 0.5f, py - 0.5f, 1f, 1f);

        /// <summary>Chase: close enough and nothing solid in between (else it keeps walking closer).</summary>
        bool CanShoot(HeroController hero)
        {
            Vector2 to = hero.Position;
            return MonsterRangedRules.ShouldShoot(def.Ranged, x, y + RangedAimLift, to.x, to.y + RangedAimLift, SolidAt);
        }

        /// <summary>Authority, windup just ended: one straight shot at where the hero stands now. The angle is rounded to its wire step first so
        /// the server's hit box and every client's picture fly the same line.</summary>
        void FireRangedShot(HeroController hero)
        {
            var from = new Vector2(NetQuantize.Pos(NetQuantize.Pos(x)), NetQuantize.Pos(NetQuantize.Pos(y + RangedAimLift)));
            Vector2 to = hero.Position + new Vector2(0f, RangedAimLift);
            Vector2 d = to - from;
            float angleDeg = d.sqrMagnitude > 0.0001f ? Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg : new[] { -90f, 90f, 180f, 0f }[facing];
            angleDeg = NetQuantize.Angle(NetQuantize.Angle(angleDeg));
            NetEventBus.MonsterShot(this, from, angleDeg);
            PlayRangedShot(from, angleDeg);
        }

        /// <summary>Launches the projectile: the real one on the authority, a picture on a client (NetEventPlayer, MonsterShot event).</summary>
        public void PlayRangedShot(Vector2 from, float angleDeg)
        {
            if (def.Ranged == null) return;
            MonsterProjectile.Fire(transform.parent, homeZone, def.Ranged, from, angleDeg, def.Atk);
        }
    }
}
