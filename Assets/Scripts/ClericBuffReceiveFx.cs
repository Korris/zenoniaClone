using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The moment a hero receives Spirit Blessing (+ATK) / Divine Aegis (+DEF), RO3 Soft Fantasy V1 (spec 4.2 / 4.3). Then ClericBuffStatusFx
    /// takes over with the dim maintained look.
    ///  - ATK (650 ms, Holy gold / ivory): a 4-point gold star over the head, a thin gold ring 1.3 H under the feet, two gold light lines running
    ///    up round the body (mesh trails), 6 gold motes. No column, no arrows: one symbol (rule 1).
    ///  - DEF (750 ms, pale blue + gold): a blue-gold hexagon 1.4 H under the feet, a see-through shield in front of the chest (only its rim and a
    ///    faint face, the hero stays visible), two blue arcs circling the body (1.4 H), an ivory flash behind the chest at the start.
    ///  - receiving one while the other already runs plays only the received buff's burst (user 21/11); the combined picture is the maintained look.
    /// Emblems sit in the FX band above the name tag; crowd tiers come in as `dim` (ClericLightFx.DimFor).
    /// </summary>
    public static class ClericBuffReceiveFx
    {
        public const float AtkMs = 650f, DefMs = 750f;
        const float StarY = 44f, ShieldY = 11f; // star centre ~14 units over the name tag (~30), shield on the chest

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
            fx.Ground(ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Line), 0f, AtkMs, 18f / 0.955f, 27f / 0.955f, 0.85f, ClericLightFx.Squash, 18f, 5).Env(50f, 260f)
              .Upright(ClericLightLook.M(ClericLightLook.Star, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 40f, AtkMs, 7f, 12f, 7f, 12f, 1f, new Vector2(0f, StarY), 2).Rise(3f).Env(70f, 240f)
              .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 40f, 220f, 5f, 12f, 5f, 12f, 0.55f, new Vector2(0f, StarY), 1)
              .At(60f, f => f.Puff(false, f.Anchor + new Vector2(0f, 4f), 6, 10f, 22f, 1.6f, 2.4f, ClericLightLook.A(p.Main, 0.9f), 0.5f));
            ClericBuffStatusLook.Set A = ClericBuffStatusLook.Atk;
            if (fx.Lod == FxTier.Far) return; // strangers: no swirl ribbons
            float d = dim;
            fx.At(0f, f => ClericBuffOrbitTrail.Spawn(f.transform, t, A, 11f, 4f, 1f, 20f, 480f, 560f, 0f, 1.8f, 0.18f, d))
              .At(50f, f => ClericBuffOrbitTrail.Spawn(f.transform, t, A, 11f, 4f, 1f, 20f, 480f, 560f, Mathf.PI, 1.8f, 0.18f, d));
        }

        static void Def(HeroController t, float dim)
        {
            ClericLightLook.Pal p = ClericLightLook.Guard;
            var fx = ClericLightFx.Create("AegisReceive", t.Position, t, DefMs, dim);
            fx.Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 620f, 16f, 28f, 1f).Kind(LayerKind.Glow)
              .Ground(ClericLightLook.M(ClericLightLook.Hexagon, p, ClericLightLook.Role.Line), 0f, DefMs, 20f, 29f, 0.9f, ClericLightFx.Squash, -14f, 5).Env(50f, 300f)
              .Upright(ClericLightLook.M(ClericLightLook.Shield, p, ClericLightLook.Role.Line), ClericLightFx.Band.Front, false, 80f, DefMs, 12f, 15f, 13f, 16.5f, 0.42f, new Vector2(0f, ShieldY), 1).Env(90f, 300f)
              .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, 0f, 170f, 6f, 13f, 6f, 13f, 0.65f, new Vector2(0f, 10f), 1)
              .At(120f, f => ClericBlessingLightFx.SideSparks(f, 6, p.Core, 0.45f));
            ClericBuffStatusLook.Set D = ClericBuffStatusLook.Def;
            if (fx.Lod == FxTier.Far) return;
            float d = dim;
            fx.At(40f, f => ClericBuffOrbitTrail.Spawn(f.transform, t, D, 13f, 4.5f, 6f, 16f, 600f, 640f, 0f, 1.8f, 0.2f, d))
              .At(40f, f => ClericBuffOrbitTrail.Spawn(f.transform, t, D, 13f, 4.5f, 16f, 6f, 600f, 640f, Mathf.PI, 1.8f, 0.2f, d));
        }

        /// <summary>Map change: nothing kept per hero any more (kept for ProxySpawner.ClearForTransfer).</summary>
        public static void Clear() { }
    }
}
