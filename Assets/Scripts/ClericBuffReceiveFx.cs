using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The moment a hero receives Spirit Blessing (+ATK) / Divine Aegis (+DEF), RO3 Soft Fantasy V1 treated as a "strong" skill (user 21/11:
    /// "skill buff dam và defense hiện tại đơn giản quá"): 5-6 layers within ~1.8 H, four clear phases, the richness of the concept images the
    /// user liked (docs/reference/cleric-buff-status-concept/tay-buff-concept-1.png / -2.png) in RO3 pastel colours and shapes. Then
    /// ClericBuffStatusFx takes over with the small dim emblem (rule 10). The buff itself applies at the cast (SkillTable atMs 0, 60 s);
    /// this only follows it.
    ///  - ATK (950 ms, Holy gold #F3D16B / ivory #FFF4C9): lead-in 0-200 ms: the 4-point-star rune circle (1.75 H) draws itself in at ~40 %,
    ///    light rays rise behind the body, motes gather; burst 200-420: the circle flashes and turns, a wave runs out, a short soft column
    ///    behind the hero, a chest flash, the sword badge pops over the head on a star flash, sparkles fly out; hold 420-700: two gold
    ///    ribbons spiral up round the body (mesh trails), up-arrows rise, motes float, the circle at ~60 %; fade 700-950.
    ///  - DEF (1050 ms, pale blue #9DDAEC + gold #FFF0B8): lead-in 0-220: the hexagon rune circle draws itself in, rays, motes; burst
    ///    220-460: circle flash + wave, pale-blue column, ivory flash, a see-through dome drops over the hero and settles (rim in front,
    ///    faint face behind, so the hero stays clear), the shield badge pops with up-arrows, gold sparks ring out where the dome lands;
    ///    hold 460-780: two blue arcs circle the body, a bright arc runs round the dome rim; fade 780-1050.
    ///  - receiving one while the other already runs plays only the received buff's burst (user 21/11); no skill name text.
    /// Crowd tiers come in as `dim` (ClericLightFx.DimFor): strangers get no ribbons, no rays / inner runes / arrows, faint glow.
    /// </summary>
    public static class ClericBuffReceiveFx
    {
        public const float AtkMs = 950f, DefMs = 1050f;
        const float AtkBurst = 200f, DefBurst = 220f, AtkSigil = 36f, DefSigil = 34f, DomeW = 30f, DomeH = DomeW * 1.06f, DomeBaseV = 0.28f;

        public static void Receive(HeroController hero, bool atk, float dim)
        {
            if (hero == null || !ClericBuffStatusLook.Ready) return;
            if (atk) Atk(hero, dim); else Def(hero, dim);
            ClericBuffStatusFx.Ensure(hero, ClericBuffStatusFx.HoldAfterReceiveMs);
        }

        static void Atk(HeroController t, float dim)
        {
            ClericLightLook.Pal p = ClericLightLook.Holy;
            var fx = ClericLightFx.Create("BlessingReceive", t.Position, t, AtkMs, dim);
            ClericBuffReceiveParts.Sigil(fx, ClericLightLook.SigilStar, p, AtkMs, AtkBurst, AtkSigil, 16f);
            ClericBuffReceiveParts.Rays(fx, p, 30f);
            ClericBuffReceiveParts.Column(fx, p, AtkBurst);
            fx.Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, AtkBurst, AtkBurst + 220f, 6f, 15f, 6f, 15f, 0.6f, new Vector2(0f, 10f), 1);
            ClericBuffReceiveParts.Emblem(fx, ClericLightLook.Sword, ClericLightLook.Star, p, AtkBurst + 20f, AtkMs - 30f);
            Color gather = p.Light;
            fx.At(0f, f => ClericBuffReceiveParts.Gather(f, gather, AtkSigil, 8))
              .At(90f, f => ClericBuffReceiveParts.Gather(f, gather, AtkSigil * 0.8f, 6))
              .At(AtkBurst, f => ClericBuffReceiveParts.Burst(f, p, 10, 8));
            if (fx.Lod == FxTier.Far) return; // strangers: no swirl ribbons
            ClericBuffStatusLook.Set A = ClericBuffStatusLook.Atk;
            float d = dim;
            fx.At(AtkBurst - 40f, f => ClericBuffOrbitTrail.Spawn(f.transform, t, A, 12f, 4.2f, 0f, 25f, 520f, 600f, 0f, 2.4f, 0.22f, d))
              .At(AtkBurst, f => ClericBuffOrbitTrail.Spawn(f.transform, t, A, 12f, 4.2f, 0f, 25f, 520f, 600f, Mathf.PI, 2.4f, 0.22f, d));
        }

        static void Def(HeroController t, float dim)
        {
            ClericLightLook.Pal p = ClericLightLook.Guard;
            var fx = ClericLightFx.Create("AegisReceive", t.Position, t, DefMs, dim);
            ClericBuffReceiveParts.Sigil(fx, ClericLightLook.SigilHex, p, DefMs, DefBurst, DefSigil, -12f);
            ClericBuffReceiveParts.Rays(fx, p, 40f);
            ClericBuffReceiveParts.Column(fx, p, DefBurst);
            fx.Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, DefBurst, DefBurst + 200f, 6f, 14f, 6f, 14f, 0.65f, new Vector2(0f, 10f), 1);
            // Dome: standing quads, base line at 28 % of the height on the feet; drops 14 units and settles. Face behind the hero (faint), rim in front.
            Vector2 dome = new Vector2(0f, -DomeBaseV * DomeH);
            float d0 = DefBurst - 40f, end = DefMs - 40f;
            fx.Upright(ClericLightLook.M(ClericLightLook.Dome, p, ClericLightLook.Role.Line), ClericLightFx.Band.Behind, true, d0, end, DomeW * 0.86f, DomeW, DomeH * 0.86f, DomeH, 0.24f, dome, -2)
                  .Drop(14f, 260f).Kind(LayerKind.Glow).Env(70f, 280f)
              .Upright(ClericLightLook.M(ClericLightLook.DomeRim, p, ClericLightLook.Role.Arc), ClericLightFx.Band.Front, true, d0, end, DomeW * 0.86f, DomeW, DomeH * 0.86f, DomeH, 0.62f, dome)
                  .Drop(14f, 260f).Arc(5.5f, 0f, 1).Env(70f, 280f);
            ClericBuffReceiveParts.Emblem(fx, ClericLightLook.Shield, ClericLightLook.Core, p, DefBurst + 30f, DefMs - 40f);
            Color gather = p.Light, gold = p.Core;
            fx.At(0f, f => ClericBuffReceiveParts.Gather(f, gather, DefSigil, 8))
              .At(100f, f => ClericBuffReceiveParts.Gather(f, gather, DefSigil * 0.8f, 6))
              .At(DefBurst, f => ClericBuffReceiveParts.Burst(f, p, 8, 6))
              .At(DefBurst + 170f, f => f.Ring(true, f.Anchor, 10, 26f, 1.6f, 2.4f, ClericLightLook.A(gold, 0.95f), 0.4f)) // the dome lands
              .At(DefBurst + 60f, f => ClericBlessingLightFx.SideSparks(f, 6, gold, 0.5f));
            if (fx.Lod == FxTier.Far) return;
            ClericBuffStatusLook.Set D = ClericBuffStatusLook.Def;
            float d = dim;
            fx.At(DefBurst + 40f, f => ClericBuffOrbitTrail.Spawn(f.transform, t, D, 14f, 4.8f, 5f, 16f, 640f, 620f, 0f, 2.2f, 0.22f, d))
              .At(DefBurst + 40f, f => ClericBuffOrbitTrail.Spawn(f.transform, t, D, 14f, 4.8f, 16f, 5f, 640f, 620f, Mathf.PI, 2.2f, 0.22f, d));
        }

        /// <summary>Map change: nothing kept per hero any more (kept for ProxySpawner.ClearForTransfer).</summary>
        public static void Clear() { }
    }
}
