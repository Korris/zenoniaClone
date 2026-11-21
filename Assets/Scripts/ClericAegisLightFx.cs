using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Light picture of Divine Aegis (+DEF ally buff); `--cleric-fx legacy` keeps the old painted blue frames (ClericBuffFx).
    /// ClericBuffFx runs the caster's raise-hand pose; times are its (cast 620 ms, hand up 110 ms, streams 180-460 ms), the buff is skill data.
    /// 2026-11-21: clear sky / ice blue after the concept the user picked (docs/reference/cleric-buff-status-concept/tay-buff-concept-2.png,
    /// ClericBuffStatusLook.Def), replacing the teal shield rim of 20/11:
    ///  - caster: blue halo pool + blue ring with a running bright arc, a small light over the raised hand, white sparks;
    ///  - streams to allies like Spirit Blessing (sky ribbon, white head);
    ///  - receive burst: ClericBuffReceiveFx (blue column, compass circle, dome, shield emblem); then ClericBuffStatusFx keeps a small dome for
    ///    the buff's real time (bright arc over it when the wearer takes a hit).
    /// 21/11 08:00: bold cyan-jade (ClericBuffStatusLook, Midgard colour); the maintained look is a cyan ring round the low chest (ClericBuffStatusFx).
    /// Crowd: ClericLightFx.DimFor (own full, party 70 %, others 35 %).
    /// </summary>
    public static class ClericAegisLightFx
    {
        public static void Cast(HeroController caster, List<HeroController> targets, float buffMs)
        {
            if (caster == null || !ClericBuffStatusLook.Ready) return;
            ClericBuffStatusLook.Set b = ClericBuffStatusLook.Def;
            const float raise = ClericBlessingLightFx.RaiseMs, hand = ClericBlessingLightFx.HandLift;
            var fx = ClericLightFx.Create("Aegis", caster.Position, caster, ClericBlessingLightFx.CastMs, ClericLightFx.DimFor(caster, null))
                .Particles(ClericBuffStatusLook.Spark, ClericBuffStatusLook.Mote)
                .Ground(b.Halo, 0f, 520f, 20f, 36f, 1f, 0.6f)
                .Ground(b.Ring, raise, 560f, 14f / 0.78f, 36f / 0.78f, 0.9f, 0.6f, 0f, 5).Arc(9f)
                .Upright(b.Flash, ClericLightFx.Band.Over, false, raise, 430f, 4f, 8f, 4f, 8f, 0.55f, new Vector2(0f, hand));
            Color rim = b.Light;
            fx.At(raise + 40f, f => ClericBlessingLightFx.SideSparks(f, 4, rim, 0.45f));
            ClericBlessingLightFx.LaunchAll(fx, caster, targets, false);
        }
    }
}
