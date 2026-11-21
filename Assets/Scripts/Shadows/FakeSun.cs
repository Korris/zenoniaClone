using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Which rule a dynamic ground blob follows (FakeSun.Blob).</summary>
    public enum ShadowKind { Hero, Pet, Monster, Boss, Loot, Npc, Turret }

    /// <summary>Shape of one dynamic blob kind: width multiplier over the entity's old shadow width, peak alpha, sun offset as a share of the width.</summary>
    public struct BlobRule
    {
        public float WidthMul, Alpha, OffsetShare;
        public BlobRule(float widthMul, float alpha, float offsetShare) { WidthMul = widthMul; Alpha = alpha; OffsetShare = offsetShare; }
    }

    /// <summary>
    /// The one fake sun of every outdoor map (user + ChatGPT 21/11/2026, docs/reference/cleric-buff-status-concept/midgard-fx-map-rings-shadows-chatgpt.md "Phần bóng đổ"):
    /// single source of truth for the shadow direction, length, colour per map, alpha cap and the on/off switch. Every shadow reads it:
    /// dynamic blobs (GroundShadowBlobs: hero, pet, monster, boss, loot, NPC, turret) every frame, static prop shadows (StaticPropShadows: trees, houses,
    /// rocks) when a map is built; changing a value here and bumping <see cref="Version"/> moves / recolours them all. Soft, short, cool, never black;
    /// overlaps take the max opacity (GroundShadowRenderer mask), capped at <see cref="AlphaCap"/>.
    /// `--shadows off` brings back the old small black ellipse under units and no prop shadows (comparison). Off in the --view25d prototype (own drop shadows).
    /// </summary>
    public static class FakeSun
    {
        /// <summary>Shadow direction on screen: from the upper-left sun toward the lower right, (0.75, 0.66) down = ~41 degrees under the horizontal. World y is up.</summary>
        public static Vector2 Direction = new Vector2(0.75f, -0.66f).normalized;
        /// <summary>Cast length of a standing object as a share of its drawn height (spec 0.25-0.45; sun elevation ~55-60 degrees).</summary>
        public static float LengthFactor = 0.3f;
        /// <summary>Darkest a pixel may get from any number of overlapping shadows (max-opacity mask, never stacked).</summary>
        public static float AlphaCap = 0.40f;
        /// <summary>Soft rim of every blob as a share of its radius (spec 25-35 %).</summary>
        public static float Feather = 0.32f;
        /// <summary>Ground ellipse depth / width of a unit blob (spec 0.35-0.50).</summary>
        public static float BlobDepth = 0.42f;

        // Colours (spec 2.2): default cool grey-green, dirt / sand brown-grey, snow blue-grey. Never #000.
        public static readonly Color DefaultTint = Hex(0x53, 0x6B, 0x69), GrassTint = Hex(0x5D, 0x62, 0x50), DirtTint = Hex(0x76, 0x6B, 0x69), SnowTint = Hex(0x66, 0x7B, 0x8E);

        static int version = 1;
        /// <summary>Bumped by <see cref="Changed"/>; the renderer rebakes the static prop shadows when it moves.</summary>
        public static int Version { get { return version; } }
        /// <summary>Call after editing Direction / LengthFactor / tint rules at runtime: static prop shadows are rebuilt from the same rules.</summary>
        public static void Changed() { version++; }

        static bool parsed, off;
        static void Parse()
        {
            if (parsed) return;
            parsed = true;
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--shadows");
            off = i >= 0 && i + 1 < a.Length && a[i + 1] == "off";
        }

        /// <summary>The new shadow system draws (else the old per-unit black ellipse sprites do). Views only; the --view25d prototype keeps its own shadows.</summary>
        public static bool Enabled { get { Parse(); return !off && NetMode.RendersFx && !View25D.Active; } }

        /// <summary>Shadow colour of the map on screen: snow maps blue-grey, desert maps brown-grey, the rest the default cool grey-green
        /// (one colour per map; grass-only GrassTint kept for later per-surface tuning).</summary>
        public static Color Tint
        {
            get
            {
                string id = MapRegistry.Current != null ? MapRegistry.Current.Id : "";
                if (id == FrostHollowLayout.MapId || id == FrostfangDenMap.MapId) return SnowTint;
                if (id == AmberDunesMap.MapId || id == SunspireOasisMap.MapId || id == ScorpionGorgeMap.MapId) return DirtTint;
                return DefaultTint;
            }
        }

        /// <summary>Dynamic blob rules (spec 2.3 / 2.4 / 8): hero 0.28, pet 0.22, monster 0.28, boss 0.35 (+ contact), loot 0.24.</summary>
        public static BlobRule Blob(ShadowKind k)
        {
            switch (k)
            {
                case ShadowKind.Hero: return new BlobRule(1.0f, 0.28f, 0.10f);    // old width 12 units = ~0.85 of the body
                case ShadowKind.Pet: return new BlobRule(1.0f, 0.22f, 0.10f);
                case ShadowKind.Monster: return new BlobRule(1.0f, 0.28f, 0.10f); // old width HitW + 2 = ~0.95 of the body
                case ShadowKind.Boss: return new BlobRule(1.0f, 0.35f, 0.08f);    // width from the foot core (Monster.ShadowWidth)
                case ShadowKind.Loot: return new BlobRule(1.0f, 0.24f, 0.08f);
                case ShadowKind.Npc: return new BlobRule(0.9f, 0.28f, 0.10f);
                default: return new BlobRule(1.0f, 0.28f, 0.10f);
            }
        }

        /// <summary>Boss contact patch under the belly: share of the blob width and its alpha (reaches the cap in the middle).</summary>
        public const float BossContactShare = 0.45f, BossContactAlpha = 0.40f;
        /// <summary>A unit off the ground (jump, launch, hovering pet): at LiftFull units up the blob is 35 % wider and 60 % lighter.</summary>
        public const float LiftFull = 30f, LiftGrow = 0.35f, LiftFade = 0.6f;

        // Static prop rules (spec 3): alphas; sizes live with the shapes in StaticPropShadows.
        public const float TreeRootAlpha = 0.34f, TreeCrownAlpha = 0.18f, HouseFootAlpha = 0.32f, HouseCastAlpha = 0.18f,
            RockContactAlpha = 0.26f, RockCastAlpha = 0.17f, DecorContactAlpha = 0.18f, FenceAlpha = 0.16f;

        /// <summary>World offset of a shadow `distance` units along the sun.</summary>
        public static Vector2 Along(float distance) { return Direction * distance; }

        static Color Hex(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f, 1f); }
    }
}
