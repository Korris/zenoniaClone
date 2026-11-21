using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Materials of the Cleric buff STATUS look (2026-11-21, concept the user picked: docs/reference/cleric-buff-status-concept/tay-buff-concept-1..4.png,
    /// ChatGPT https://chatgpt.com/c/6b005a53-8a78-83ec-8011-ed9ffd5160b2): Spirit Blessing (+ATK) = warm gold / cream, Divine Aegis (+DEF) = clear
    /// cyan-jade (21/11 in-game review; was sky / ice blue). Replaces the teal Buff* look of 20/11 for these two buffs (Heal keeps ClericLightLook.Buff*).
    /// Midgard rules kept: transparent layered light, the saturated alpha body carries every shape (reads with Bloom off and on a pale road),
    /// the additive part stays low (warm colours on a bright road burn white, Sổ lỗi 2026-11-19 portal), no dark outline.
    /// Masks: Resources/Sprites/fx/light-skill buff-compass / buff-dome / buff-emblem-sword / buff-emblem-shield / buff-arrow
    /// (Tools/build-light-skill-textures.py + light_skill_textures_cleric_buff.py), plus the shared portal-veil / burst-ring / meteor-core.
    /// </summary>
    public static class ClericBuffStatusLook
    {
        // 21/11 in-game review (docs/reference/cleric-buff-status-concept/ingame-review-chatgpt.md): gold more amber and contrasty on snow
        // (rim #E9A63F, core #FFF0B5, swirl #F4BC63); DEF moved from sky / ice blue (too close to the Frost Slime) to cyan-jade.
        public static readonly Color AtkCream = Hex(0xFFF0B5), AtkGold = Hex(0xF4BC63), AtkAmber = Hex(0xE9A63F), AtkOutline = Hex(0x9B612B);
        public static readonly Color DefIce = Hex(0xD8FFFF), DefSky = Hex(0x49C9D7), DefDeep = Hex(0x36B8C8), DefFace = Hex(0x80E4ED), DefOutline = Hex(0x267F9C), DefRim = Hex(0xF2FBFF);

        /// <summary>One buff's colour set. Main = shape colour, Light = highlights / particles, Deep = thin lines that must read on snow / a pale road.</summary>
        public sealed class Set
        {
            public Color Main, Light, Deep;
            public Material Halo, Compass, Ring, Column, Emblem, EmblemOutline, Arrow, Flash, Head, Trail, Dome, DomeRim, DomeSide;
        }

        public static Set Atk, Def;
        /// <summary>Neutral particle materials: the particle colour alone decides the hue (gold sparks, ice motes).</summary>
        public static Material Spark, Mote;
        static bool built, failed;

        public static bool Ready { get { Build(); return !failed; } }

        /// <summary>Builds the materials ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Load() { if (NetMode.RendersFx) Build(); }

        static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f); }
        static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

        static void Build()
        {
            if (built) return;
            built = true;
            const string F = "Sprites/fx/light-skill/";
            Shader particle = Resources.Load<Shader>("Shaders/LightParticle");
            Texture2D domeRim = Resources.Load<Texture2D>(F + "buff-dome-rim"), domeSide = Resources.Load<Texture2D>(F + "buff-dome-side"),
                swordLine = Resources.Load<Texture2D>(F + "buff-emblem-sword-outline"), shieldLine = Resources.Load<Texture2D>(F + "buff-emblem-shield-outline");
            Texture2D compass = Resources.Load<Texture2D>(F + "buff-compass"), dome = Resources.Load<Texture2D>(F + "buff-dome"),
                sword = Resources.Load<Texture2D>(F + "buff-emblem-sword"), shield = Resources.Load<Texture2D>(F + "buff-emblem-shield"),
                arrow = Resources.Load<Texture2D>(F + "buff-arrow"), veil = Resources.Load<Texture2D>(F + "portal-veil"), band = Resources.Load<Texture2D>(F + "burst-ring"),
                core = Resources.Load<Texture2D>(F + "meteor-core"), cross = Resources.Load<Texture2D>(F + "holy-cross"), mote = Resources.Load<Texture2D>(F + "portal-mote");
            if (!LightSkillAssets.Ready || particle == null || !particle.isSupported || compass == null || dome == null || sword == null || shield == null || arrow == null
                || veil == null || band == null || core == null || cross == null || mote == null || domeRim == null || domeSide == null || swordLine == null || shieldLine == null)
            {
                Debug.LogWarning("[ZenoniaProto] Cleric buff status FX: shader / texture missing, Spirit Blessing / Divine Aegis draw no status");
                failed = true;
                return;
            }
            veil.wrapMode = TextureWrapMode.Repeat;
            foreach (Texture2D t in new[] { compass, dome, sword, shield, arrow, band, core, cross, mote, domeRim, domeSide, swordLine, shieldLine }) t.wrapMode = TextureWrapMode.Clamp;

            // Additive glow: warm gold kept low (R and G both high burn a pale road white), blue keeps R low so snow tints instead of clipping.
            Atk = Make("Atk", AtkGold, AtkCream, AtkAmber, new Color(0.3f, 0.2f, 0.05f), sword, compass, dome, arrow, veil, band, core);
            Def = Make("Def", DefSky, DefIce, DefDeep, new Color(0.03f, 0.2f, 0.22f), shield, compass, dome, arrow, veil, band, core);
            // Thin dark outline behind each emblem (review: the emblems melted into snow / pale stone)
            Atk.EmblemOutline = Blob("ClericStatus_AtkEmblemLine", swordLine, 0f, Color.black, 0f, A(AtkOutline, 0.95f));
            Def.EmblemOutline = Blob("ClericStatus_DefEmblemLine", shieldLine, 0f, Color.black, 0f, A(DefOutline, 0.95f));
            // Dome: fresnel style, the rim (cyan #49C9D7) clearly brighter than the near-clear face (#80E4ED)
            Def.Dome.SetColor("_BodyColor", A(DefFace, 1f));
            Def.Dome.SetColor("_EmissionColor", new Color(0.02f, 0.08f, 0.09f));
            Def.DomeRim = Blob("ClericStatus_DefDomeRim", domeRim, 0f, new Color(0.03f, 0.18f, 0.2f), 0.08f, A(DefSky, 1f));
            ArcOn(Def.DomeRim, A(DefIce, 0.8f), 0.45f);
            Def.DomeSide = Blob("ClericStatus_DefDomeSide", domeSide, 0f, new Color(0.03f, 0.18f, 0.2f), 0.05f, A(DefSky, 1f));
            ArcOn(Def.DomeSide, A(DefIce, 0.8f), 0.45f);

            Spark = Particle("ClericStatus_Spark", particle, cross, new Color(0.4f, 0.4f, 0.4f), 0.3f, 1f);
            Mote = Particle("ClericStatus_Mote", particle, mote, new Color(0.22f, 0.22f, 0.22f), 0.05f, 0.9f);
        }

        static Set Make(string n, Color main, Color light, Color deep, Color glow, Texture2D emblem, Texture2D compass, Texture2D dome, Texture2D arrow, Texture2D veil, Texture2D band, Texture2D core)
        {
            var s = new Set { Main = main, Light = light, Deep = deep };
            s.Halo = Blob("ClericStatus_" + n + "Halo", Texture2D.whiteTexture, 2.4f, glow * 0.5f, 0f, A(light, 0.22f));
            // Rune circle: lines in the deep tone (they must read on snow and on a pale road), a bright arc of the light tone runs round it.
            s.Compass = Blob("ClericStatus_" + n + "Compass", compass, 0f, glow * 1.1f, 0.04f, A(deep, 0.95f)); // review: +contrast by a fuller, darker line, not more HDR
            ArcOn(s.Compass, A(light, 0.6f), 0.35f);
            s.Ring = Blob("ClericStatus_" + n + "Ring", band, 0f, glow, 0f, A(deep, 0.75f));
            ArcOn(s.Ring, A(light, 0.6f), 0.3f);
            // Emblem / arrows: the glyph (mask 1) gets a white-hot core, the badge disc (mask ~0.3) stays a soft tint of the main colour.
            s.Emblem = Blob("ClericStatus_" + n + "Emblem", emblem, 0f, glow * 0.5f, 0.2f, A(main, 1f)); // low bloom: a crisp glyph
            s.Arrow = Blob("ClericStatus_" + n + "Arrow", arrow, 0f, glow, 0.2f, A(light, 0.95f));
            s.Flash = Blob("ClericStatus_" + n + "Flash", core, 0f, glow * 0.8f, 0.08f, A(light, 0.55f));
            s.Head = Blob("ClericStatus_" + n + "Head", core, 0f, glow * 1.2f, 0.3f, A(light, 0.9f));
            // Dome: the mask is faint in the middle and bright at the silhouette, so the face behind its front half stays clear.
            s.Dome = Blob("ClericStatus_" + n + "Dome", dome, 0f, glow * 0.7f, 0.04f, A(main, 1f));
            ArcOn(s.Dome, A(DefRim, 0.7f), 0.45f);

            s.Column = new Material(LightSkillAssets.Beam) { name = "ClericStatus_" + n + "Column" };
            s.Column.SetTexture("_MainTex", veil);
            s.Column.SetVector("_MainTiling", new Vector4(1f, 0.6f, 0f, 0f));
            s.Column.SetFloat("_ScrollSpeed", 1.4f);
            s.Column.SetFloat("_StreakMin", 0.5f);
            s.Column.SetFloat("_EdgeSoft", 0.85f);
            s.Column.SetFloat("_BottomFade", 0.1f);
            s.Column.SetFloat("_TopFade", 0.55f);
            s.Column.SetColor("_EmissionColor", glow);
            s.Column.SetFloat("_CoreWhite", 0.06f);
            s.Column.SetColor("_BodyColor", A(main, 0.45f));
            s.Column.SetColor("_EdgeColor", A(light * 0.6f, 1f));

            s.Trail = new Material(LightSkillAssets.Trail) { name = "ClericStatus_" + n + "Trail" };
            s.Trail.SetColor("_EmissionColor", new Color(0.45f, 0.45f, 0.45f, 1f));
            s.Trail.SetFloat("_CoreWhite", 0.15f);
            s.Trail.SetColor("_BodyColor", A(main, 0.87f)); // review: swirl +0.12 alpha
            return s;
        }

        static void ArcOn(Material m, Color tint, float glowFloor)
        {
            m.SetVector("_Arc", new Vector4(0f, ClericLightLook.ArcHalfWidth, glowFloor, 1f));
            m.SetColor("_ArcTint", tint);
        }

        static Material Blob(string name, Texture2D tex, float falloff, Color emission, float coreWhite, Color body)
        {
            var m = new Material(LightSkillAssets.Glow) { name = name };
            m.SetTexture("_MainTex", tex);
            m.SetFloat("_Falloff", falloff);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_CoreWhite", coreWhite);
            m.SetColor("_BodyColor", body);
            return m;
        }

        static Material Particle(string name, Shader s, Texture2D tex, Color emission, float coreWhite, float bodyAlpha)
        {
            var m = new Material(s) { name = name };
            m.SetTexture("_MainTex", tex);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_CoreWhite", coreWhite);
            m.SetColor("_BodyColor", new Color(1f, 1f, 1f, bodyAlpha));
            return m;
        }
    }
}
