using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Materials of every Cleric light FX, "RO3 Soft Fantasy V1" (2026-11-21, user: "làm giống bên RO3 luôn đi"; rules at the top of
    /// docs/sprite-rules.md, per-skill spec docs/reference/ro3-skill-style/ro3-acolyte-priest-skill-style-chatgpt.md section 4). RO3's pastel colours
    /// replace the bold Midgard set (3248c37f) and the gold / blue concept colours. One palette per function (rule 8: at most 2 main colours + 1 core):
    /// Heal green, Holy ivory-gold (Spirit Blessing, Divine Light, basic attack), Protection pale blue + gold (Divine Aegis), Sanctuary green-ivory,
    /// Guardian ivory + sage, Purify white-teal (+ lilac for the bad status), Ascension gold + sage.
    /// Pastel bodies vanish on snow (Sổ lỗi 2026-11-20 / 2026-11-19): every stroke keeps a slightly darker rim of its own hue (rule 6) and the
    /// additive part stays low and narrow (GlowPow), only flash cores pass the bloom threshold (rule 5).
    /// Materials are made on first use from (mask, palette, role) and cached. Masks: Resources/Sprites/fx/light-skill
    /// (Tools/build-light-skill-textures.py; holy-* RO3 masks in Tools/light_skill_textures_cleric_ro3.py). `--cleric-fx legacy` keeps the old art.
    /// </summary>
    public static class ClericLightLook
    {
        /// <summary>One function's colours: Main = body, Light = highlight / particles, Core = flash centre, Rim = darker edge, Glow = additive (linear HDR, low).</summary>
        public sealed class Pal
        {
            public string Name; public Color Main, Light, Core, Rim, Glow;
            public Pal(string name, int main, int light, int core, int rim, Color glow) { Name = name; Main = Hex(main); Light = Hex(light); Core = Hex(core); Rim = Hex(rim); Glow = glow; }
        }

        public static readonly Pal Heal = new Pal("Heal", 0xB8EBAA, 0xF5FFDA, 0xFFF5C8, 0x5E9A52, new Color(0.07f, 0.16f, 0.05f));
        public static readonly Pal Holy = new Pal("Holy", 0xF2D078, 0xFFF1BC, 0xFFFBE5, 0xA27B2E, new Color(0.18f, 0.13f, 0.03f));
        public static readonly Pal Guard = new Pal("Aegis", 0x9DDAEC, 0xD9F3F6, 0xFFF0B8, 0x4A8DA3, new Color(0.04f, 0.13f, 0.17f));
        public static readonly Pal Sanct = new Pal("Sanct", 0xE5F2C3, 0xF4DE9A, 0xFFF8DF, 0x86A65A, new Color(0.1f, 0.13f, 0.05f));
        public static readonly Pal Spirit = new Pal("Spirit", 0xFFF2C7, 0xFFFBEA, 0xFFFFF4, 0xAE9350, new Color(0.15f, 0.13f, 0.06f));
        public static readonly Pal Sage = new Pal("Sage", 0xD7EAD6, 0xF2FAF1, 0xFFFFF6, 0x7D9A7B, new Color(0.08f, 0.12f, 0.08f));
        public static readonly Pal Purify = new Pal("Purify", 0xB6E8D8, 0xE3FAED, 0xF6FFFB, 0x569C8B, new Color(0.06f, 0.14f, 0.12f));
        public static readonly Pal Taint = new Pal("Taint", 0xA58CBD, 0xC9B6DA, 0xE6DCF0, 0x5E4C78, new Color(0.04f, 0.02f, 0.06f));
        public static readonly Pal Ascend = new Pal("Ascend", 0xF3D47D, 0xFFF9E1, 0xFFFDF3, 0xA3832F, new Color(0.17f, 0.13f, 0.04f));

        /// <summary>Heal number colour (rule 11).</summary>
        public static readonly Color HealNumber = Hex(0x8CDD74);

        /// <summary>How a material draws its mask: Line = rune strokes / rings (solid body, dark rim, light centre), Fill = soft ground pool (10-20 %),
        /// Glyph = emblem / star / cross / wings (solid, light core), Flash = soft core flash (the only part that blooms), Arc = Line with a bright arc running round.</summary>
        public enum Role { Line, Fill, Glyph, Flash, Arc }

        // Mask names (Resources/Sprites/fx/light-skill)
        public const string Band = "burst-ring", RuneRing = "holy-rune-ring", Rune = "holy-rune", Compass = "buff-compass", Hexagon = "holy-hexagon", Grid = "holy-grid",
            Star = "holy-star4", Diamond = "holy-diamond", Latin = "holy-cross-latin", Wings = "holy-wings", Streak = "holy-streak", Core = "meteor-core",
            Mote = "portal-mote", Shield = "buff-emblem-shield", Sparkle = "holy-cross";
        static readonly string[] Masks = { Band, RuneRing, Rune, Compass, Hexagon, Grid, Star, Diamond, Latin, Wings, Streak, Core, Mote, Shield, Sparkle };

        /// <summary>Half width (rad) of a ring's bright arc: 2 x 0.94 rad = 30 % of the circle.</summary>
        public const float ArcHalfWidth = 0.94f;

        /// <summary>Neutral particle materials: the particle colour alone decides the hue. Spark = 4-point sparkle, Dot = soft mote.</summary>
        public static Material SparkMat, DotMat;

        static readonly Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static Texture2D veil;
        static bool failed, built;

        /// <summary>Light FX on (default) or the painted art (`--cleric-fx legacy`).</summary>
        public static bool Enabled = ReadFlag();

        /// <summary>True when this process draws the Cleric light FX (flag on, renders FX, shaders and textures present).</summary>
        public static bool Active { get { return Enabled && NetMode.RendersFx && Ready; } }

        /// <summary>True when the light FX can draw at all, whatever `--cleric-fx` says: the skills whose painted art was removed (Sanctuary, Guardian
        /// Spirit, Purification, Divine Ascension) use it even in legacy mode.</summary>
        public static bool Drawable { get { return NetMode.RendersFx && Ready; } }

        public static bool Ready { get { Build(); return !failed; } }

        /// <summary>Loads the shaders / masks ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Load() { if (NetMode.RendersFx) Build(); }

        static bool ReadFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--cleric-fx");
            return !(i >= 0 && i + 1 < a.Length && a[i + 1].Equals("legacy", StringComparison.OrdinalIgnoreCase));
        }

        public static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f); }
        public static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

        static void Build()
        {
            if (built) return;
            built = true;
            Shader particle = Resources.Load<Shader>("Shaders/LightParticle");
            veil = Resources.Load<Texture2D>("Sprites/fx/light-skill/portal-veil");
            bool ok = LightSkillAssets.Ready && particle != null && particle.isSupported && veil != null;
            foreach (string n in Masks)
            {
                Texture2D t = Resources.Load<Texture2D>("Sprites/fx/light-skill/" + n);
                if (t == null) { ok = false; Debug.LogWarning("[ZenoniaProto] Cleric light FX: mask " + n + " missing"); continue; }
                t.wrapMode = TextureWrapMode.Clamp;
                Tex[n] = t;
            }
            if (!ok)
            {
                Debug.LogWarning("[ZenoniaProto] Cleric light FX: shader / texture missing, the cleric uses the painted FX");
                failed = true;
                return;
            }
            veil.wrapMode = TextureWrapMode.Repeat;
            SparkMat = Particle("Cleric_Spark", particle, Tex[Sparkle], new Color(0.22f, 0.22f, 0.2f), 0.2f);
            DotMat = Particle("Cleric_Dot", particle, Tex[Mote], new Color(0.1f, 0.1f, 0.1f), 0.04f);
        }

        /// <summary>The material drawing `mask` in palette `p` as `role` (made once, cached). Fill ignores the mask (soft radial pool).</summary>
        public static Material M(string mask, Pal p, Role role)
        {
            Build();
            string key = mask + "|" + p.Name + "|" + role;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            Texture2D tex = role == Role.Fill ? Texture2D.whiteTexture : Tex.ContainsKey(mask) ? Tex[mask] : Texture2D.whiteTexture;
            switch (role)
            {
                case Role.Fill:  // ground pool: RO3 "nền phép" 10-20 %, glow barely there
                    m = Blob(key, tex, 3.2f, p.Glow * 0.35f, 0f, A(p.Main, 0.2f));
                    break;
                case Role.Glyph:
                    m = Blob(key, tex, 0f, p.Glow * 1.1f, 0.1f, A(p.Main, 0.92f));
                    Tone(m, 2f, A(p.Rim, 0.7f), 0.42f, A(p.Light, 0.85f), 0.72f, 4f);
                    break;
                case Role.Flash: // the bloom core of a skill: short, small, light tone with a near-white centre
                    m = Blob(key, tex, 0f, p.Glow * 1.6f, 0.32f, A(p.Light, 0.6f));
                    Tone(m, 1.3f, A(p.Main, 0.55f), 0.45f, A(p.Core, 0.7f), 0.85f, 3f);
                    break;
                default:         // Line / Arc: rune strokes 65-85 % with a darker rim, light centre, glow only on the stroke cores
                    m = Blob(key, tex, 0f, p.Glow, 0f, A(p.Main, 0.82f));
                    Tone(m, 1.6f, A(p.Rim, 0.62f), 0.45f, A(p.Light, 0.6f), 0.82f, 3.5f);
                    if (role == Role.Arc) { m.SetVector("_Arc", new Vector4(0f, ArcHalfWidth, 0.3f, 1f)); m.SetColor("_ArcTint", A(p.Light, 0.7f)); }
                    break;
            }
            Cache[key] = m;
            return m;
        }

        /// <summary>Short soft light column (portal-veil streaks scrolling up) in palette `p`: Heal / Ascension; translucent, drawn behind the hero.</summary>
        public static Material Column(Pal p)
        {
            Build();
            string key = "column|" + p.Name;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(LightSkillAssets.Beam) { name = key };
            m.SetTexture("_MainTex", veil);
            m.SetVector("_MainTiling", new Vector4(1f, 0.6f, 0f, 0f));
            m.SetFloat("_ScrollSpeed", 1.2f);
            m.SetFloat("_StreakMin", 0.5f);
            m.SetFloat("_EdgeSoft", 0.75f);
            m.SetFloat("_BottomFade", 0.1f);
            m.SetFloat("_TopFade", 0.6f);
            m.SetColor("_EmissionColor", p.Glow * 0.9f);
            m.SetFloat("_CoreWhite", 0.05f);
            m.SetColor("_BodyColor", A(p.Main, 0.5f));
            m.SetColor("_EdgeColor", A(p.Light * 0.4f, 1f));
            Cache[key] = m;
            return m;
        }

        /// <summary>LightTrail ribbon material in palette `p` (the TrailRenderer gradient sets the per-vertex colour; body = main colour).</summary>
        public static Material Trail(Pal p)
        {
            Build();
            string key = "trail|" + p.Name;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(LightSkillAssets.Trail) { name = key };
            m.SetColor("_EmissionColor", new Color(0.28f, 0.28f, 0.26f, 1f));
            m.SetFloat("_CoreWhite", 0.12f);
            m.SetColor("_BodyColor", A(p.Main, 0.8f));
            Cache[key] = m;
            return m;
        }

        /// <summary>LightBlob bold-colour knobs (see the shader): body alpha gain, rim colour below `rimEnd`, highlight above `hiStart`, glow power.</summary>
        public static void Tone(Material m, float solid, Color rim, float rimEnd, Color hi, float hiStart, float glowPow)
        {
            m.SetFloat("_Solid", solid);
            m.SetColor("_RimColor", rim);
            m.SetFloat("_RimEnd", rimEnd);
            m.SetColor("_HiColor", hi);
            m.SetFloat("_HiStart", hiStart);
            m.SetFloat("_GlowPow", glowPow);
        }

        static Material Blob(string name, Texture2D tex, float falloff, Color emission, float coreWhite, Color body)
        {
            var m = new Material(LightSkillAssets.Glow) { name = "Cleric_" + name };
            m.SetTexture("_MainTex", tex);
            m.SetFloat("_Falloff", falloff);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_CoreWhite", coreWhite);
            m.SetColor("_BodyColor", body);
            return m;
        }

        /// <summary>LightParticle: the per-particle colour tints both passes, so one material serves every hue.</summary>
        static Material Particle(string name, Shader s, Texture2D tex, Color emission, float coreWhite)
        {
            var m = new Material(s) { name = name };
            m.SetTexture("_MainTex", tex);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_CoreWhite", coreWhite);
            m.SetColor("_BodyColor", new Color(1f, 1f, 1f, 1f));
            return m;
        }
    }
}
