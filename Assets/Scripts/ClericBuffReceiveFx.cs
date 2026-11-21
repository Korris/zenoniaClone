using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The moment a hero receives Spirit Blessing (+ATK) / Divine Aegis (+DEF) (2026-11-21, concept docs/reference/cleric-buff-status-concept/
    /// tay-buff-concept-1 = ATK, -2 = DEF, -3 = both). ~0.85 s, then ClericBuffStatusFx takes over with the small maintained look.
    ///  - ATK: warm gold light column rising behind the hero, gold compass rune circle on the ground, two gold swirl arcs spiralling up round
    ///    the body (mesh trails), a round gold sword emblem with up-arrows over the head, cream sparks;
    ///  - DEF: blue light column, blue compass circle, a see-through blue dome dropping over the hero (front half rim only: the face stays
    ///    clear), a shield emblem with up-arrows, ice motes;
    ///  - receiving one while the other already runs plays only the received buff's burst; the combined picture is the maintained look.
    /// Columns sit behind the hero, emblems in the FX band above the name. Crowd tiers come in as `dim` (ClericLightFx.DimFor).
    /// </summary>
    public static class ClericBuffReceiveFx
    {
        public const float Ms = 850f;
        const float Squash = 0.6f, EmblemY = 40f, DomeW = 44f, DomeH = 46f, DomeBaseV = 0.28f;

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
            ClericBuffStatusLook.Set A = ClericBuffStatusLook.Atk, D = ClericBuffStatusLook.Def;
            bool both = atk && def;
            var fx = ClericLightFx.Create("BuffReceive", t.Position, t, Ms, dim).Particles(ClericBuffStatusLook.Spark, ClericBuffStatusLook.Mote);

            // Ground: halo pool + compass rune circle (gold for ATK; blue when DEF alone, a blue ring round the gold one when both)
            if (atk)
                fx.Ground(A.Halo, 0f, 780f, 22f, 46f, 1f, Squash)
                  .Ground(A.Compass, 0f, Ms, 30f, 46f, 0.95f, Squash, 0f, 5).Arc(4.5f, 0f, 2);
            if (def && !atk)
                fx.Ground(D.Halo, 0f, 780f, 22f, 46f, 1f, Squash)
                  .Ground(D.Compass, 0f, Ms, 30f, 46f, 0.95f, Squash, 0f, 5).Arc(-4.5f, 0f, 2);
            if (both) fx.Ground(D.Ring, 0f, Ms, 44f / 0.78f, 54f / 0.78f, 0.7f, Squash, 0f, 4).Arc(-6f);

            // Light columns behind the hero
            if (both) { Column(fx, A, -6f, 15f); Column(fx, D, 6f, 15f); }
            else Column(fx, atk ? A : D, 0f, 24f);

            // DEF: the dome drops over the hero (standing quad: base line at 28 % of its height on the feet)
            if (def)
            {
                Vector2 off = new Vector2(0f, -DomeBaseV * DomeH);
                fx.Upright(D.Dome, ClericLightFx.Band.Behind, true, 40f, Ms, DomeW * 0.8f, DomeW, DomeH * 0.8f, DomeH, 0.75f, off, -1)
                  .Upright(D.Dome, ClericLightFx.Band.Front, true, 40f, Ms, DomeW * 0.8f, DomeW, DomeH * 0.8f, DomeH, 0.28f, off);
            }

            // Emblems + up-arrows above the head
            if (both)
            {
                Emblem(fx, A, -9f); Emblem(fx, D, 9f);
                Arrow(fx, A, -17f, 100f); Arrow(fx, D, 0f, 160f); Arrow(fx, D, 17f, 220f);
            }
            else
            {
                ClericBuffStatusLook.Set s = atk ? A : D;
                Emblem(fx, s, 0f);
                Arrow(fx, s, -10f, 100f); Arrow(fx, s, 10f, 170f);
            }

            // Small flash behind the chest, sparks, swirls
            ClericBuffStatusLook.Set main = atk ? A : D;
            fx.Upright(main.Flash, ClericLightFx.Band.Behind, false, 0f, 140f, 6f, 12f, 6f, 12f, 0.55f, new Vector2(0f, 9f), 1);
            if (atk)
            {
                Color cream = A.Light;
                fx.At(0f, f => ClericBlessingLightFx.SideSparks(f, 6, cream, 0.45f));
                if (dim >= 0.5f || fx.Own)
                {
                    float d = dim;
                    fx.At(0f, f => { ClericBuffOrbitTrail.Spawn(f.transform, t, A, 13f, 5f, 2f, 24f, 520f, 720f, 0f, 1.8f, 0.28f, d); });
                    fx.At(60f, f => { ClericBuffOrbitTrail.Spawn(f.transform, t, A, 13f, 5f, 2f, 24f, 520f, 720f, Mathf.PI, 1.8f, 0.28f, d); });
                }
            }
            if (def)
            {
                Color ice = D.Light, sky = D.Main;
                fx.At(60f, f => f.Puff(false, f.Anchor + new Vector2(0f, 4f), 6, 11f, 20f, 1.8f, 2.8f, new Color(sky.r, sky.g, sky.b, 0.85f), 0.55f))
                  .At(120f, f => f.Puff(true, f.Anchor + new Vector2(0f, 12f), 5, 14f, 14f, 1.5f, 2.3f, new Color(ice.r, ice.g, ice.b, 0.95f), 0.45f));
            }
        }

        static void Column(ClericLightFx fx, ClericBuffStatusLook.Set s, float x, float w)
        {
            fx.Upright(s.Column, ClericLightFx.Band.Behind, true, 0f, 780f, w, w * 0.8f, 54f, 68f, 0.8f, new Vector2(x, -2f), -2); // behind the dome's back half too
        }

        static void Emblem(ClericLightFx fx, ClericBuffStatusLook.Set s, float x)
        {
            fx.Upright(s.Emblem, ClericLightFx.Band.Over, false, 60f, Ms, 9f, 14f, 9f, 14f, 0.95f, new Vector2(x, EmblemY - 4f), 2).Rise(5f);
        }

        static void Arrow(ClericLightFx fx, ClericBuffStatusLook.Set s, float x, float t0)
        {
            fx.Upright(s.Arrow, ClericLightFx.Band.Over, false, t0, Ms - 40f, 3.6f, 4.2f, 4.6f, 5.4f, 0.9f, new Vector2(x, EmblemY - 9f), 3).Rise(10f);
        }

        /// <summary>Map change: drop the de-dup times (heroes of the old map).</summary>
        public static void Clear() { }
    }
}
