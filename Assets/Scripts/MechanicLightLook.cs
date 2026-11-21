using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Colours, masks and switches of the Mechanic's RO3 Soft Fantasy V1 FX (2026-11-21, user: "làm lại hết skill của warrior, wizard, mechanic đổi
    /// qua kiểu mới"; rules at the top of docs/sprite-rules.md). Class colour = honey-gold / brass (sprite-rules "Mechanic vàng", same family as the
    /// soft fantasy icons docs/reference/skill-icons-soft-fantasy/mechanic-after-soft-fantasy-icons.png) + cyan tech energy as the one accent
    /// (rule 8: at most 2 main colours + 1 core per skill). Explosions stay honey with neutral warm smoke. Every stroke keeps a darker rim of its
    /// own hue (rule 6, snow) and a low additive part; only Flash cores bloom (rule 5). No sprite frames anywhere.
    /// </summary>
    public static class MechanicLightLook
    {
        public static readonly ClericLightLook.Pal Honey = new ClericLightLook.Pal("MechHoney", 0xF0B53F, 0xFFDD85, 0xFFF7DC, 0x8F5B17, new Color(0.17f, 0.11f, 0.02f));
        public static readonly ClericLightLook.Pal Tech = new ClericLightLook.Pal("MechTech", 0x2DB6D6, 0xA8EEF7, 0xF0FDFF, 0x135A6B, new Color(0.03f, 0.14f, 0.18f));
        /// <summary>Explosions (user 21/11: "skill tên lửa sao nhìn nó bé tí vậy, không có độ ngầu gì hết"): hot white-yellow core -> orange fireball, the
        /// warm end of the class family; the orange body carries the shape on snow (low additive part, Sổ lỗi 2026-11-19 hot colours on bright ground).</summary>
        public static readonly ClericLightLook.Pal Fire = new ClericLightLook.Pal("MechFire", 0xFF9431, 0xFFD467, 0xFFF6D2, 0x9C3A0C, new Color(0.3f, 0.13f, 0.02f));
        /// <summary>Dark smoke after a blast (dissolves), reads on snow and sand.</summary>
        public static readonly ClericLightLook.Pal Soot = new ClericLightLook.Pal("MechSoot", 0x5E5149, 0x8E7E72, 0xB8A898, 0x2B231F, Color.black);
        public static readonly ClericLightLook.Pal Smoke = new ClericLightLook.Pal("MechSmoke", 0xCDBFAE, 0xEDE4D6, 0xFFFFFF, 0x6E6255, Color.black);

        /// <summary>Spark particle colours: warm brass embers, cyan crackle.</summary>
        public static readonly Color Ember = new Color(1f, 0.78f, 0.36f, 1f), Crackle = new Color(0.55f, 0.92f, 1f, 1f);

        // Masks (Resources/Sprites/fx/light-skill). Mechanic ones: Tools/light_skill_textures_mechanic_ro3.py
        public const string Ring = "tech-ring", Reticle = "tech-reticle", Puff = "smoke-puff", Flare = "muzzle-flare",
            Band = "burst-ring", Core = "meteor-core", Spark = "holy-cross", Streak = "holy-streak";

        /// <summary>Burst-ring band sits at 78 % of its quad radius, tech-ring's main band at 95.5 %: quad diameters that put the band on radius r.</summary>
        public static float BandQuad(float r) { return 2f * r / 0.78f; }
        public static float RingQuad(float r) { return 2f * r / 0.955f; }

        /// <summary>True when this process draws the Mechanic light FX (renders FX, shaders and masks present). The skills have no painted art left.</summary>
        public static bool Active { get { return SkillLightMaterials.Ready && Masks(); } }

        static int masks; // 0 unknown, 1 ok, -1 missing
        static bool Masks()
        {
            if (masks == 0) masks = SkillLightMaterials.Mask(Ring) != null && SkillLightMaterials.Mask(Reticle) != null && SkillLightMaterials.Mask(Puff) != null && SkillLightMaterials.Mask(Flare) != null ? 1 : -1;
            return masks > 0;
        }

        /// <summary>Loads shaders / masks / materials ahead of the first shot (class spawn), so the first cast never hitches.</summary>
        public static void Load()
        {
            if (!Active) return;
            foreach (ClericLightLook.Pal p in new[] { Honey, Tech })
            {
                M(Ring, p, ClericLightLook.Role.Line); M(Reticle, p, ClericLightLook.Role.Glyph); M(Flare, p, ClericLightLook.Role.Glyph);
                M(Band, p, ClericLightLook.Role.Line); M(Core, p, ClericLightLook.Role.Flash); M(Spark, p, ClericLightLook.Role.Glyph);
            }
            SkillLightMaterials.Smoke(Puff, Smoke); SkillLightMaterials.Smoke(Puff, Soot, 0.82f);
            M(Puff, Fire, ClericLightLook.Role.Glyph); M(Core, Fire, ClericLightLook.Role.Flash); M(Band, Fire, ClericLightLook.Role.Line);
        }

        public static Material M(string mask, ClericLightLook.Pal p, ClericLightLook.Role role) { return SkillLightMaterials.M(mask, p, role); }
        public static Material Line(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Line); }
        public static Material Glyph(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Glyph); }
        public static Material Flash(ClericLightLook.Pal p) { return M(Core, p, ClericLightLook.Role.Flash); }
        public static Material Pool(ClericLightLook.Pal p) { return M(Band, p, ClericLightLook.Role.Fill); }
        public static Material SmokeMat { get { return SkillLightMaterials.Smoke(Puff, Smoke); } }
        public static Material SootMat { get { return SkillLightMaterials.Smoke(Puff, Soot, 0.82f); } }
        /// <summary>Solid orange fireball puff (smoke-puff mask, Glyph tone: light core, dark orange rim).</summary>
        public static Material FireballMat { get { return M(Puff, Fire, ClericLightLook.Role.Glyph); } }
        /// <summary>Debris chunk colour (dark brown streaks thrown out of a blast).</summary>
        public static readonly Color Debris = new Color(0.32f, 0.24f, 0.18f, 1f);

        /// <summary>Crowd dim of a cast by `caster` (ClericLightFx.DimFor: 1 own, 0.7 party, 0.35 anyone else).</summary>
        public static float Dim(HeroController caster) { return ClericLightFx.DimFor(caster, null); }
    }
}
