using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Element palettes + extra masks of the RO3 Soft Fantasy V1 light FX for the non-holy classes (Wizard first, 2026-11-21, user: "làm lại hết skill
    /// của warrior, wizard, mechanic đổi qua kiểu mới"; rules at the top of docs/sprite-rules.md). Generic on top of the Cleric toolkit, nothing renamed:
    /// palettes are ClericLightLook.Pal (so ClericLightFx, its trails, LOD and particles take them as they are), shapes of the Cleric masks come
    /// straight from ClericLightLook.M, the element masks (Tools/light_skill_textures_wizard_elements.py) are drawn by a copy of the same role's
    /// material with only the mask swapped, so every role keeps one look (solid body, darker rim of its own hue, low additive core).
    /// Colours (rule 8: at most 2 main colours + 1 core per skill; Wizard class colour = blue / ice, no violet - that is the Assassin's):
    ///  - Ice: pale cyan body with a clearly darker teal-blue rim (rule 6: readable on snow), white-cyan core;
    ///  - Fire: warm orange body, red-brown rim, gold-cream core (additive kept low: Sổ lỗi 2026-11-19 portal, warm colours burn pale roads);
    ///  - Arcane: soft periwinkle blue leaning to lavender (the icons draw the arcane star blue-white), indigo rim;
    ///  - Gold: star / target accent (Arcane Lance target ring, Mana Shield sparkles, Astral stars);
    ///  - Mana: azure for Mana Shield (deeper than the Cleric's pale Protection blue so the two rings never read as one buff);
    ///  - Astral: starlight blue for the Wizard ultimate (+ Gold as its one accent).
    /// </summary>
    public static class ElementLightLook
    {
        public static readonly ClericLightLook.Pal Ice = new ClericLightLook.Pal("Ice", 0x9EE3F5, 0xE2F8FF, 0xF6FDFF, 0x3E8FB0, new Color(0.03f, 0.12f, 0.17f));
        public static readonly ClericLightLook.Pal Fire = new ClericLightLook.Pal("Fire", 0xFF9C4E, 0xFFD27E, 0xFFF4D8, 0xB2431F, new Color(0.2f, 0.07f, 0.01f));
        public static readonly ClericLightLook.Pal Arcane = new ClericLightLook.Pal("Arcane", 0xAFC0F4, 0xE5EBFF, 0xFAFBFF, 0x5363AE, new Color(0.07f, 0.08f, 0.17f));
        public static readonly ClericLightLook.Pal Gold = new ClericLightLook.Pal("ElemGold", 0xF3D16B, 0xFFF0B8, 0xFFFBE8, 0x9C7424, new Color(0.15f, 0.11f, 0.03f));
        public static readonly ClericLightLook.Pal Mana = new ClericLightLook.Pal("Mana", 0x7DBDF2, 0xD8EEFF, 0xF4FAFF, 0x2F5F9C, new Color(0.03f, 0.09f, 0.18f));
        public static readonly ClericLightLook.Pal Astral = new ClericLightLook.Pal("Astral", 0xA8CBF6, 0xEAF2FF, 0xFFFFFF, 0x4A6FA8, new Color(0.06f, 0.1f, 0.18f));

        /// <summary>Smoke / soot particle colour of fire skills (alpha-blended, never additive).</summary>
        public static readonly Color Smoke = new Color(0.36f, 0.31f, 0.3f, 0.45f);

        // Element masks (Resources/Sprites/fx/light-skill, Tools/light_skill_textures_wizard_elements.py)
        public const string IceShard = "ice-shard", Snowflake = "snowflake", FrostRing = "frost-ring", FireHead = "fire-head", FireBurst = "fire-burst",
            HexCells = "hex-cells", SwirlFire = "swirl-fire", SwirlIce = "swirl-ice", Cloud = "storm-cloud";
        static readonly string[] Masks = { IceShard, Snowflake, FrostRing, FireHead, FireBurst, HexCells, SwirlFire, SwirlIce, Cloud };

        static readonly Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static bool built, failed;

        /// <summary>True when this process draws the element light FX (renders FX, Cleric toolkit + element masks present).</summary>
        public static bool Active { get { return NetMode.RendersFx && Ready; } }

        public static bool Ready { get { Build(); return !failed; } }

        /// <summary>Loads shaders / masks / particle materials ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Load() { if (NetMode.RendersFx) Build(); }

        static void Build()
        {
            if (built) return;
            built = true;
            ClericLightLook.Load(); // particle materials (ClericLightFx.Emit) + the base role materials
            if (!ClericLightLook.Ready) { failed = true; Debug.LogWarning("[ZenoniaProto] element light FX: Cleric light toolkit missing, the old art draws"); return; }
            foreach (string n in Masks)
            {
                Texture2D t = Resources.Load<Texture2D>("Sprites/fx/light-skill/" + n);
                if (t == null) { failed = true; Debug.LogWarning("[ZenoniaProto] element light FX: mask " + n + " missing"); continue; }
                t.wrapMode = TextureWrapMode.Clamp;
                Tex[n] = t;
            }
        }

        /// <summary>
        /// Material drawing `mask` in palette `p` as `role`: a Cleric mask (ClericLightLook constants) goes straight to ClericLightLook.M; an element
        /// mask gets a copy of the same role's material (built on the Cleric star mask) with its own texture. Cached.
        /// </summary>
        public static Material M(string mask, ClericLightLook.Pal p, ClericLightLook.Role role)
        {
            Build();
            Texture2D tex;
            if (!Tex.TryGetValue(mask, out tex)) return ClericLightLook.M(mask, p, role);
            string key = mask + "|" + p.Name + "|" + role;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            Material baseM = ClericLightLook.M(ClericLightLook.Star, p, role);
            m = new Material(baseM) { name = "Elem_" + key };
            if (role != ClericLightLook.Role.Fill) m.SetTexture("_MainTex", tex);
            Cache[key] = m;
            return m;
        }

        public static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }
    }
}
