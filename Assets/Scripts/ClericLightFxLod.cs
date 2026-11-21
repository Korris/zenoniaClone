using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Crowd tier of one Cleric light FX (RO3 rule 12).</summary>
    public enum FxTier { Own, Near, Far }

    /// <summary>What a layer is for the LOD: Main = the skill's shape (rune, ring, emblem), Glow = soft secondary light, Detail = extra rune detail.</summary>
    public enum LayerKind { Main, Glow, Detail }

    /// <summary>
    /// Crowd LOD of the Cleric light FX, RO3 Soft Fantasy rule 12 (40 players on a boss): the local player's own cast (or one landing on them) full;
    /// a party member's: main shapes 90 %, soft glow 50 %, particles 50 %; anyone else's: main shapes 55 %, glow 15 %, detail layers dropped,
    /// particles 20 %, no trail. Boss warnings are not Cleric FX and are never dimmed. Also the ground ellipse rule (rule 7 / section 5.3).
    /// </summary>
    public sealed partial class ClericLightFx
    {
        /// <summary>Height / width of every Cleric ring lying on the ground (one projection for all skills). Areas keep their x radius = the gameplay radius.</summary>
        public const float Squash = 0.7f;

        /// <summary>Crowd dim for a cast by `caster` landing on `target` (either may be null): 1 own / on me, 0.7 party, 0.35 anyone else.</summary>
        public static float DimFor(HeroController caster, HeroController target)
        {
            HeroController me = LocalPlayer.Hero;
            if (me == null || caster == me || target == me) return 1f;
            return PartyAllies.Are(me, caster) || target != null && PartyAllies.Are(me, target) ? 0.7f : 0.35f;
        }

        public static FxTier TierOf(float dim) { return dim >= 0.99f ? FxTier.Own : dim >= 0.6f ? FxTier.Near : FxTier.Far; }

        static float KindAlpha(LayerKind k, FxTier t)
        {
            if (t == FxTier.Own) return 1f;
            if (k == LayerKind.Glow) return t == FxTier.Near ? 0.5f : 0.15f;
            if (k == LayerKind.Detail) return t == FxTier.Near ? 0.9f : 0f;
            return t == FxTier.Near ? 0.9f : 0.55f;
        }

        static float ParticleAlpha(FxTier t) { return t == FxTier.Own ? 1f : t == FxTier.Near ? 0.85f : 0.6f; }

        static float TrailAlpha(FxTier t) { return t == FxTier.Own ? 1f : t == FxTier.Near ? 0.6f : 0f; }

        /// <summary>Count of particles for this cast: all for the local player, half for a party member, a fifth for anyone else (0 when that rounds away).</summary>
        public int Count(int n)
        {
            FxTier t = Lod;
            if (t == FxTier.Own) return n;
            return t == FxTier.Near ? Mathf.Max(1, Mathf.RoundToInt(n * 0.5f)) : Mathf.RoundToInt(n * 0.2f);
        }
    }
}
