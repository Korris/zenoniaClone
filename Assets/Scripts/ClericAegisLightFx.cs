using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Light picture of Divine Aegis (+DEF ally buff), RO3 Soft Fantasy V1 (spec 4.3 CLERIC_DIVINE_AEGIS_V1, Kyrie Eleison + Assumptio),
    /// Protection palette pale blue #9DDAEC / #B9E5EA with the gold accent #FFF0B8; `--cleric-fx legacy` keeps the old painted blue frames (ClericBuffFx).
    /// ClericBuffFx runs the caster's raise-hand pose; times are its (cast 620 ms, hand up 110 ms, streams 180-460 ms), the buff is skill data (60 s).
    ///  - caster: a faint blue pool + a thin blue-gold hexagon seal under the feet, a small light over the raised hand, 4 sparks;
    ///  - streams to allies like Spirit Blessing (blue ribbon, gold-white head);
    ///  - receive burst: ClericBuffReceiveFx (hexagon 1.4 H, see-through shield in front, two arcs round the body, ivory flash); then
    ///    ClericBuffStatusFx keeps the dim ring and flashes the shield 150 ms when the wearer is hit.
    /// </summary>
    public static class ClericAegisLightFx
    {
        public static void Cast(HeroController caster, List<HeroController> targets, float buffMs)
        {
            if (caster == null || !ClericBuffStatusLook.Ready) return;
            ClericLightLook.Pal p = ClericLightLook.Guard;
            const float raise = ClericBlessingLightFx.RaiseMs, hand = ClericBlessingLightFx.HandLift;
            var fx = ClericLightFx.Create("Aegis", caster.Position, caster, ClericBlessingLightFx.CastMs, ClericLightFx.DimFor(caster, null))
                .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 520f, 16f, 28f, 1f).Kind(LayerKind.Glow)
                .Ground(ClericLightLook.M(ClericLightLook.Hexagon, p, ClericLightLook.Role.Line), raise, 600f, 18f, 27f, 0.85f, ClericLightFx.Squash, 0f, 5).Env(60f, 240f)
                .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, raise, 430f, 4f, 8f, 4f, 8f, 0.6f, new Vector2(0f, hand));
            Color spark = p.Core;
            fx.At(raise + 40f, f => ClericBlessingLightFx.SideSparks(f, 4, spark, 0.45f));
            ClericBlessingLightFx.LaunchAll(fx, caster, targets, false);
        }
    }
}
