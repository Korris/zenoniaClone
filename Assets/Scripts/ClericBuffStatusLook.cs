using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Materials of the Cleric buff look: Spirit Blessing (+ATK) = amber gold, Divine Aegis (+DEF) = cyan-jade (concept the user picked 21/11:
    /// docs/reference/cleric-buff-status-concept/tay-buff-concept-1..4.png). Heal keeps ClericLightLook.Buff*.
    /// 2026-11-21 08:00, bold Midgard Senki colour (user: "nên làm hiệu ứng skill màu đậm như này mới thấy rõ, hiện tại vẫn nhạt lắm"; spec
    /// docs/reference/cleric-buff-status-concept/midgard-fx-map-rings-shadows-chatgpt.md section 1): solid colour body (alpha blend, ~65 %) over a
    /// narrow glow (~35 %): LightBlob _Solid turns the soft strokes into solid, thicker bands, a darker rim colour on their edges, the highlight
    /// tone at their centre, the additive part narrowed to the stroke cores (_GlowPow) and kept low (warm colours on a pale road burn white,
    /// Sổ lỗi 2026-11-19 portal); white only in tiny cores / sparks. The maintained look is two rings round the body (BodyRing, ClericBuffBodyRing).
    /// Masks: Resources/Sprites/fx/light-skill buff-compass / buff-dome / buff-dome-rim / buff-emblem-* / buff-arrow
    /// (Tools/build-light-skill-textures.py + light_skill_textures_cleric_buff.py), plus the shared portal-veil / burst-ring / meteor-core.
    /// </summary>
    public static class ClericBuffStatusLook
    {
        // Midgard palette (ChatGPT 21/11 section 1.4): rim, body, highlight, core
        public static readonly Color AtkRim = Hex(0xA96519), AtkBody = Hex(0xE9A32D), AtkHi = Hex(0xFFD56A), AtkCore = Hex(0xFFF1BA);
        public static readonly Color DefRim = Hex(0x126E91), DefBody = Hex(0x16B8D2), DefHi = Hex(0x75EAF0), DefCore = Hex(0xD6FCFF);

        /// <summary>One buff's colour set. Main = body colour, Light = highlight / particles, Deep = dark rim.</summary>
        public sealed class Set
        {
            public Color Main, Light, Deep;
            public Material Halo, Compass, Ring, Column, Emblem, EmblemOutline, Arrow, Flash, Head, Trail, Dome, DomeRim, BodyRing;
        }

        public static Set Atk, Def;
        /// <summary>Neutral particle materials: the particle colour alone decides the hue (gold sparks, cyan motes).</summary>
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
            Shader particle = Resources.Load<Shader>("Shaders/LightParticle"), ringShader = Resources.Load<Shader>("Shaders/BuffBodyRing");
            Texture2D domeRim = Resources.Load<Texture2D>(F + "buff-dome-rim"),
                swordLine = Resources.Load<Texture2D>(F + "buff-emblem-sword-outline"), shieldLine = Resources.Load<Texture2D>(F + "buff-emblem-shield-outline");
            Texture2D compass = Resources.Load<Texture2D>(F + "buff-compass"), dome = Resources.Load<Texture2D>(F + "buff-dome"),
                sword = Resources.Load<Texture2D>(F + "buff-emblem-sword"), shield = Resources.Load<Texture2D>(F + "buff-emblem-shield"),
                arrow = Resources.Load<Texture2D>(F + "buff-arrow"), veil = Resources.Load<Texture2D>(F + "portal-veil"), band = Resources.Load<Texture2D>(F + "burst-ring"),
                core = Resources.Load<Texture2D>(F + "meteor-core"), cross = Resources.Load<Texture2D>(F + "holy-cross"), mote = Resources.Load<Texture2D>(F + "portal-mote");
            if (!LightSkillAssets.Ready || particle == null || !particle.isSupported || ringShader == null || !ringShader.isSupported || compass == null || dome == null
                || sword == null || shield == null || arrow == null || veil == null || band == null || core == null || cross == null || mote == null || domeRim == null
                || swordLine == null || shieldLine == null)
            {
                Debug.LogWarning("[ZenoniaProto] Cleric buff status FX: shader / texture missing, Spirit Blessing / Divine Aegis draw no status");
                failed = true;
                return;
            }
            veil.wrapMode = TextureWrapMode.Repeat;
            foreach (Texture2D t in new[] { compass, dome, sword, shield, arrow, band, core, cross, mote, domeRim, swordLine, shieldLine }) t.wrapMode = TextureWrapMode.Clamp;

            // Additive glow: narrow (GlowPow) and low; gold keeps B low and stays under the bloom threshold on a pale road, cyan keeps R low.
            Atk = Make("Atk", AtkBody, AtkHi, AtkRim, AtkCore, new Color(0.34f, 0.22f, 0.04f), sword, swordLine, compass, dome, domeRim, arrow, veil, band, core, ringShader);
            Def = Make("Def", DefBody, DefHi, DefRim, DefCore, new Color(0.02f, 0.22f, 0.28f), shield, shieldLine, compass, dome, domeRim, arrow, veil, band, core, ringShader);

            Spark = Particle("ClericStatus_Spark", particle, cross, new Color(0.3f, 0.3f, 0.3f), 0.22f, 1f);
            Mote = Particle("ClericStatus_Mote", particle, mote, new Color(0.16f, 0.16f, 0.16f), 0.04f, 1f);
        }

        static Set Make(string n, Color body, Color hi, Color rim, Color coreTone, Color glow, Texture2D emblem, Texture2D emblemLine, Texture2D compass,
            Texture2D dome, Texture2D domeRim, Texture2D arrow, Texture2D veil, Texture2D band, Texture2D core, Shader ringShader)
        {
            var s = new Set { Main = body, Light = hi, Deep = rim };
            // Ground pool: body colour, smaller radius than before (falloff 2.4 -> 4.2, ~ -24 %), a soft base under the solid lines
            s.Halo = Blob("ClericStatus_" + n + "Halo", Texture2D.whiteTexture, 4.2f, glow * 0.35f, 0f, A(body, 0.32f));
            // Rune circle: solid body lines (+~45 % thicker through the gain), dark rim edges, highlight centre, a bright arc of the highlight running round
            s.Compass = Blob("ClericStatus_" + n + "Compass", compass, 0f, glow, 0f, A(body, 0.78f));
            Tone(s.Compass, 2.4f, A(rim, 1f), 0.5f, A(hi, 0.55f), 0.85f, 4f);
            ArcOn(s.Compass, A(hi, 0.75f), 0.3f);
            s.Ring = Blob("ClericStatus_" + n + "Ring", band, 0f, glow, 0f, A(body, 0.75f));
            Tone(s.Ring, 2f, A(rim, 1f), 0.5f, A(hi, 0.5f), 0.85f, 4f);
            ArcOn(s.Ring, A(hi, 0.75f), 0.3f);
            // Emblem: solid body disc, highlight glyph, clear dark outline behind (bloom of the emblem -40 %)
            s.Emblem = Blob("ClericStatus_" + n + "Emblem", emblem, 0f, glow * 0.3f, 0.04f, A(body, 1f));
            Tone(s.Emblem, 2.8f, A(rim, 0f), 0.4f, A(hi, 0.9f), 0.78f, 5f);
            s.EmblemOutline = Blob("ClericStatus_" + n + "EmblemLine", emblemLine, 0f, Color.black, 0f, A(rim, 0.95f));
            Tone(s.EmblemOutline, 1.6f, A(rim, 0f), 0.4f, A(hi, 0f), 1f, 1f);
            s.Arrow = Blob("ClericStatus_" + n + "Arrow", arrow, 0f, glow * 0.6f, 0.03f, A(body, 0.95f));
            Tone(s.Arrow, 2f, A(rim, 1f), 0.55f, A(hi, 0.8f), 0.85f, 4f);
            // Small flashes / stream heads: highlight colour, only a tiny white-hot centre
            s.Flash = Blob("ClericStatus_" + n + "Flash", core, 0f, glow * 0.7f, 0.06f, A(hi, 0.62f));
            Tone(s.Flash, 1.3f, A(rim, 0f), 0.4f, A(coreTone, 0.5f), 0.9f, 3f);
            s.Head = Blob("ClericStatus_" + n + "Head", core, 0f, glow, 0.22f, A(hi, 0.9f));
            Tone(s.Head, 1.5f, A(body, 0.6f), 0.5f, A(coreTone, 0.7f), 0.85f, 2.5f);
            // Dome (DEF receive): face behind the hero, faint in the middle, body colour at the silhouette; rim lines solid with dark edges
            s.Dome = Blob("ClericStatus_" + n + "Dome", dome, 0f, glow * 0.4f, 0f, A(body, 0.6f));
            Tone(s.Dome, 1.3f, A(rim, 0f), 0.4f, A(hi, 0.4f), 0.6f, 4f);
            ArcOn(s.Dome, A(hi, 0.6f), 0.4f);
            s.DomeRim = Blob("ClericStatus_" + n + "DomeRim", domeRim, 0f, glow, 0.02f, A(body, 0.85f));
            Tone(s.DomeRim, 2.4f, A(rim, 1f), 0.5f, A(hi, 0.55f), 0.85f, 4f);
            ArcOn(s.DomeRim, A(hi, 0.8f), 0.35f);

            s.Column = new Material(LightSkillAssets.Beam) { name = "ClericStatus_" + n + "Column" };
            s.Column.SetTexture("_MainTex", veil);
            s.Column.SetVector("_MainTiling", new Vector4(1f, 0.6f, 0f, 0f));
            s.Column.SetFloat("_ScrollSpeed", 1.4f);
            s.Column.SetFloat("_StreakMin", 0.55f);
            s.Column.SetFloat("_EdgeSoft", 0.7f);
            s.Column.SetFloat("_BottomFade", 0.1f);
            s.Column.SetFloat("_TopFade", 0.55f);
            s.Column.SetColor("_EmissionColor", glow * 0.7f);
            s.Column.SetFloat("_CoreWhite", 0.03f);
            s.Column.SetColor("_BodyColor", A(body, 0.62f));
            s.Column.SetColor("_EdgeColor", A(hi * 0.5f, 1f));

            // Swirl ribbon (receive burst): solid body, glow only on the centre line
            s.Trail = new Material(LightSkillAssets.Trail) { name = "ClericStatus_" + n + "Trail" };
            s.Trail.SetColor("_EmissionColor", new Color(0.22f, 0.22f, 0.22f, 1f));
            s.Trail.SetFloat("_CoreWhite", 0.1f);
            s.Trail.SetColor("_BodyColor", A(body, 0.9f));

            // Maintained ring round the body (ClericBuffBodyRing): body 0.65-0.75, dark rim 0.8, two highlight segments, thin core
            s.BodyRing = new Material(ringShader) { name = "ClericStatus_" + n + "BodyRing" };
            s.BodyRing.SetColor("_BodyColor", A(body, n == "Atk" ? 0.72f : 0.66f));
            s.BodyRing.SetColor("_RimColor", A(rim, 0.82f));
            s.BodyRing.SetColor("_HiColor", A(hi, 0.9f));
            s.BodyRing.SetColor("_CoreColor", n == "Atk" ? new Color(0.62f, 0.5f, 0.22f, 1f) : new Color(0.16f, 0.5f, 0.6f, 1f));
            s.BodyRing.SetFloat("_Segs", 2f);
            s.BodyRing.SetFloat("_SegLen", 0.3f);
            return s;
        }

        /// <summary>LightBlob bold-colour knobs (see the shader; Heal uses them too): body alpha gain, rim colour below `rimEnd`, highlight above `hiStart`, glow power.</summary>
        public static void Tone(Material m, float solid, Color rim, float rimEnd, Color hi, float hiStart, float glowPow)
        {
            m.SetFloat("_Solid", solid);
            m.SetColor("_RimColor", rim);
            m.SetFloat("_RimEnd", rimEnd);
            m.SetColor("_HiColor", hi);
            m.SetFloat("_HiStart", hiStart);
            m.SetFloat("_GlowPow", glowPow);
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
