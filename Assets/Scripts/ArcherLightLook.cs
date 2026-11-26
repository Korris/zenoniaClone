using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Colours, masks and switches of the Archer's skill FX (2026-11-26, user testing the build: "skill của archer sao ko thấy hiệu ứng gì vậy").
    /// RO3 Soft Fantasy V1 like every class (docs/sprite-rules.md top block): Particle System + mesh trail + shader quads on the Cleric light toolkit
    /// (ClericLightFx timeline, SkillLightMaterials roles), no FX frames; only the arrow itself (a solid object) stays the painted arrow sprite.
    /// Class colour = leaf lime (ClassPalette.Leaf, the skill icons' green) + gold as the one accent (rule 8). Wind / nature / starlight theme.
    /// Every stroke keeps a darker rim of its own hue so it reads on snow (rule 6, Sổ lỗi 2026-11-20 Cleric on snow); only Flash cores bloom.
    /// </summary>
    public static class ArcherLightLook
    {
        /// <summary>Main class colour: leaf lime body, olive-green rim (readable on snow and grass), cream core.</summary>
        public static readonly ClericLightLook.Pal Leaf = new ClericLightLook.Pal("ArcLeaf", 0xA6D45A, 0xE2F5A8, 0xFBFFE8, 0x4C7A1F, new Color(0.08f, 0.14f, 0.03f));
        /// <summary>Wind streams: paler mint-lime, a clear grey-green rim (wind arcs, dash trail, arrow spiral).</summary>
        public static readonly ClericLightLook.Pal Wind = new ClericLightLook.Pal("ArcWind", 0xC4E8A0, 0xEEFADC, 0xFFFFF4, 0x5E8A45, new Color(0.06f, 0.12f, 0.05f));
        /// <summary>Vines of Binding Shot: deeper green.</summary>
        public static readonly ClericLightLook.Pal Vine = new ClericLightLook.Pal("ArcVine", 0x78B043, 0xC8E88E, 0xF2FFD8, 0x35581A, new Color(0.05f, 0.1f, 0.02f));
        /// <summary>The one accent: warm gold (bow flash, Hunter's Focus reticle, Starfall stars).</summary>
        public static readonly ClericLightLook.Pal Gold = new ClericLightLook.Pal("ArcGold", 0xF2CF66, 0xFFEFB2, 0xFFFBE6, 0x96701F, new Color(0.15f, 0.11f, 0.03f));
        /// <summary>Dust kicked up by landing arrows / the dash (alpha-blended smoke, never additive).</summary>
        public static readonly ClericLightLook.Pal Dust = new ClericLightLook.Pal("ArcDust", 0xB9A88C, 0xDCCFB8, 0xF4ECDC, 0x6B5B45, Color.black);

        /// <summary>Particle colours: lime sparks, gold glints, leaf motes.</summary>
        public static readonly Color Spark = new Color(0.86f, 0.97f, 0.55f, 1f), Glint = new Color(1f, 0.88f, 0.45f, 1f), Mote = new Color(0.66f, 0.86f, 0.36f, 0.95f);

        // Masks (Resources/Sprites/fx/light-skill). Archer ones: Tools/light_skill_textures_archer.py
        public const string Eye = "archer-eye", VineRing = "archer-vine-ring", LeafMask = "archer-leaf", WindArc = "archer-wind-arc",
            Reticle = "tech-reticle", Band = "burst-ring", RuneRing = "holy-rune-ring", Compass = "buff-compass", Star = "holy-star4", Core = "meteor-core",
            Streak = "holy-streak", Chevron = "buff-arrow", SigilStar = "holy-sigil-star", Puff = "smoke-puff";

        /// <summary>Burst-ring band sits at 78 % of its quad radius: quad diameter that puts the band on radius r.</summary>
        public static float BandQuad(float r) { return 2f * r / 0.78f; }

        /// <summary>True when this process draws the Archer light FX (renders FX, shaders and masks present).</summary>
        public static bool Active { get { return SkillLightMaterials.Ready && Masks(); } }

        static int masks; // 0 unknown, 1 ok, -1 missing
        static bool Masks()
        {
            if (masks == 0) masks = SkillLightMaterials.Mask(Eye) != null && SkillLightMaterials.Mask(VineRing) != null && SkillLightMaterials.Mask(LeafMask) != null
                && SkillLightMaterials.Mask(WindArc) != null && SkillLightMaterials.Mask(Reticle) != null ? 1 : -1;
            return masks > 0;
        }

        /// <summary>Loads shaders / masks / materials ahead of the first cast (class spawn), so the first cast never hitches.</summary>
        public static void Load()
        {
            if (!Active) return;
            foreach (ClericLightLook.Pal p in new[] { Leaf, Wind, Gold })
            {
                Line(Band, p); Line(WindArc, p); Glyph(Star, p); Flash(p); Pool(p);
            }
            Glyph(Eye, Gold); Line(Reticle, Gold); Line(VineRing, Vine); Glyph(LeafMask, Vine); Glyph(LeafMask, Leaf); Line(RuneRing, Leaf);
            _ = DustMat;
        }

        public static Material M(string mask, ClericLightLook.Pal p, ClericLightLook.Role role) { return SkillLightMaterials.M(mask, p, role); }
        public static Material Line(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Line); }
        public static Material Glyph(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Glyph); }
        public static Material Arc(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Arc); }
        public static Material Flash(ClericLightLook.Pal p) { return M(Core, p, ClericLightLook.Role.Flash); }
        public static Material Pool(ClericLightLook.Pal p) { return M(Band, p, ClericLightLook.Role.Fill); }
        /// <summary>Soft dust puff (smoke-puff mask, alpha only): landing arrows, dash start / end.</summary>
        public static Material DustMat { get { return SkillLightMaterials.Smoke(Puff, Dust, 0.6f); } }

        /// <summary>Crowd dim of a cast by `caster` (ClericLightFx.DimFor: 1 own, 0.7 party, 0.35 anyone else).</summary>
        public static float Dim(HeroController caster) { return ClericLightFx.DimFor(caster, null); }

        public static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }
    }
}
