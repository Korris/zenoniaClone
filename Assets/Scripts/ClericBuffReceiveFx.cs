using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The moment a hero receives Spirit Blessing (+ATK) / Divine Aegis (+DEF) (2026-11-21, concept docs/reference/cleric-buff-status-concept/
    /// tay-buff-concept-1 = ATK, -2 = DEF, -3 = both). ~0.85 s, then ClericBuffStatusFx takes over with the small maintained look.
    ///  - ATK: warm gold light column rising behind the hero, gold compass rune circle on the ground, two gold swirl arcs spiralling up round
    ///    the body (mesh trails), a round gold sword emblem with up-arrows over the head, cream sparks;
    ///  - DEF: cyan-jade light column, compass circle, a see-through dome dropping over the hero (fresnel: rim brighter than the face; in front only the rim, the face stays
    ///    clear), a shield emblem with up-arrows, ice motes;
    ///  - receiving one while the other already runs plays only the received buff's burst; the combined picture is the maintained look.
    /// Columns sit behind the hero, emblems in the FX band above the name. Crowd tiers come in as `dim` (ClericLightFx.DimFor).
    /// 21/11 08:00 bold Midgard colour (ClericBuffStatusLook): layer alphas raised so the solid body reads 0.6-0.75 and the rims 0.75-0.9,
    /// swirl ribbons 40 % wider.
    /// </summary>
    public static class ClericBuffReceiveFx
    {
        public const float Ms = 850f;
        // Emblem: +25 % (14 -> 17.5) and its centre at ~54 units: the name sits ~30 above the feet, 0.3 units / px at 720p, so ~25 px clear of it.
        // Dome: -10 % width, -6.5 % height (review 21/11: it reached the monster next to the hero).
        const float Squash = 0.6f, EmblemY = 49f, EmblemS0 = 11f, EmblemS1 = 17.5f, DomeW = 40f, DomeH = 43f, DomeBaseV = 0.28f;

        /// <summary>`hero` just received the `atk` (else def) buff; `otherRuns` = the other Cleric buff is already on them (combined look).</summary>
        public static void Receive(HeroController hero, bool atk, float dim)
        {
            if (hero == null || !ClericBuffStatusLook.Ready) return;
            // The burst shows only the buff just received (user 21/11: Aegis on a hero that still had Blessing showed the sword emblem too);
            // the combined picture is left to the maintained look (ClericBuffStatusFx) while both run.
            Play(hero, atk, !atk, dim);
            ClericBuffStatusFx.Ensure(hero, ClericBuffStatusFx.HoldAfterReceiveMs);
        }

        static void Play(HeroController t, bool atk, bool def, float dim)
        {
            ClericBuffStatusLook.Set A = ClericBuffStatusLook.Atk, D = ClericBuffStatusLook.Def, s = atk ? A : D;
            var fx = ClericLightFx.Create("BuffReceive", t.Position, t, Ms, dim).Particles(ClericBuffStatusLook.Spark, ClericBuffStatusLook.Mote);

            // Ground: halo pool + compass rune circle (review 21/11: 12 % smaller, centred under the feet)
            fx.Ground(s.Halo, 0f, 780f, 20f, 40f, 1f, Squash)
              .Ground(s.Compass, 0f, Ms, 26f, 40f, 0.95f, Squash, 0f, 5).Arc(atk ? 4.5f : -4.5f, 0f, 2);

            // Light column behind the hero
            fx.Upright(s.Column, ClericLightFx.Band.Behind, true, 0f, 780f, 24f, 19f, 54f, 68f, 1f, new Vector2(0f, -2f), -3); // behind the dome too

            // DEF: the dome drops over the hero (standing quads, base line at 28 % of the height on the feet). Fresnel: the face behind the
            // hero is nearly clear (~0.1), the rim 0.35-0.45; in front of the hero only the rim, so the face is never tinted.
            if (def)
            {
                Vector2 off = new Vector2(0f, -DomeBaseV * DomeH);
                fx.Upright(D.Dome, ClericLightFx.Band.Behind, true, 40f, Ms, DomeW * 0.8f, DomeW, DomeH * 0.8f, DomeH, 0.75f, off, -2)
                  .Upright(D.DomeRim, ClericLightFx.Band.Behind, true, 40f, Ms, DomeW * 0.8f, DomeW, DomeH * 0.8f, DomeH, 0.85f, off, -1)
                  .Upright(D.DomeRim, ClericLightFx.Band.Front, true, 40f, Ms, DomeW * 0.8f, DomeW, DomeH * 0.8f, DomeH, 0.65f, off);
            }

            // Emblem (+25 %, dark outline, low bloom) + up-arrows, well above the name tag and HP bar
            fx.Upright(s.EmblemOutline, ClericLightFx.Band.Over, false, 60f, Ms, EmblemS0, EmblemS1, EmblemS0, EmblemS1, 0.95f, new Vector2(0f, EmblemY), 1).Rise(5f)
              .Upright(s.Emblem, ClericLightFx.Band.Over, false, 60f, Ms, EmblemS0, EmblemS1, EmblemS0, EmblemS1, 1f, new Vector2(0f, EmblemY), 2).Rise(5f);
            Arrow(fx, s, -12f, 100f); Arrow(fx, s, 12f, 170f);

            // Small flash behind the chest, sparks, swirls (feet to waist only)
            fx.Upright(s.Flash, ClericLightFx.Band.Behind, false, 0f, 140f, 6f, 12f, 6f, 12f, 0.6f, new Vector2(0f, 9f), 1);
            if (atk)
            {
                Color cream = A.Light;
                fx.At(0f, f => ClericBlessingLightFx.SideSparks(f, 6, cream, 0.45f));
                if (dim >= 0.5f || fx.Own)
                {
                    float d = dim;
                    fx.At(0f, f => { ClericBuffOrbitTrail.Spawn(f.transform, t, A, 12f, 4.5f, 1f, 12f, 520f, 720f, 0f, 3.1f, 0.28f, d); });
                    fx.At(60f, f => { ClericBuffOrbitTrail.Spawn(f.transform, t, A, 12f, 4.5f, 1f, 12f, 520f, 720f, Mathf.PI, 3.1f, 0.28f, d); });
                }
            }
            else
            {
                Color ice = D.Light, cyan = D.Main;
                fx.At(60f, f => f.Puff(false, f.Anchor + new Vector2(0f, 4f), 6, 11f, 20f, 1.8f, 2.8f, new Color(cyan.r, cyan.g, cyan.b, 0.85f), 0.55f))
                  .At(120f, f => f.Puff(true, f.Anchor + new Vector2(0f, 12f), 5, 14f, 14f, 1.5f, 2.3f, new Color(ice.r, ice.g, ice.b, 0.95f), 0.45f));
            }
        }

        static void Arrow(ClericLightFx fx, ClericBuffStatusLook.Set s, float x, float t0)
        {
            fx.Upright(s.Arrow, ClericLightFx.Band.Over, false, t0, Ms - 40f, 4.2f, 4.9f, 5.4f, 6.3f, 0.9f, new Vector2(x, EmblemY - 6f), 3).Rise(10f);
        }

        /// <summary>Map change: drop the de-dup times (heroes of the old map).</summary>
        public static void Clear() { }
    }
}
