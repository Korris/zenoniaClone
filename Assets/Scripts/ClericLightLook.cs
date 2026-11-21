using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Materials of the Cleric light FX (2026-11-20, user: "sửa lại skill buff dam, attack, hồi máu của cleric dùng hiệu ứng như ragnarok 3"): Spirit Blessing,
    /// Heal, Divine Light and the basic attack, built once from the light shaders (LightSkillAssets: LightBlob, LightBeamScroll, LightTrail; LightParticle).
    /// Palette from the ChatGPT spec (Tan Pham, https://chatgpt.com/c/6b001902-3eec-83ec-8da9-29241e160c1c): jade #72C9A0, mint #A8E4C5, milk #E8FFF0,
    /// pale #B8F0D3, white, cream #F3E8BA only as a hint. Maps are bright (snow, pale roads): every material carries its shape with a saturated
    /// alpha body and keeps the additive part mostly green and low (Sổ lỗi 2026-11-19: bright additive on snow / a pale road burns to a white sheet);
    /// only small cores pass the bloom threshold. Textures: grey masks of Resources/Sprites/fx/light-skill (Tools/build-light-skill-textures.py,
    /// holy-cross and holy-rune added for these). `--cleric-fx legacy` keeps the old painted art.
    /// 2026-11-20 evening: the three buffs (Heal, Spirit Blessing, Divine Aegis) use the Buff* set (Midgard Senki layering); Divine Light and
    /// the basic attack keep the first jade set. 2026-11-21: Spirit Blessing / Divine Aegis moved to the gold / blue ClericBuffStatusLook
    /// (concept the user picked); Heal still uses Buff*.
    /// </summary>
    public static class ClericLightLook
    {
        public static readonly Color Jade = Hex(0x72C9A0), Mint = Hex(0xA8E4C5), Milk = Hex(0xE8FFF0), Pale = Hex(0xB8F0D3), Cream = Hex(0xF3E8BA);

        /// <summary>Particle colours that still read on white snow (first capture 20/11: white / milk / mint particles and flashes vanished on snow): leaf = deep jade, spark = light jade.</summary>
        public static readonly Color Leaf = Hex(0x3FAE7C), Spark = Hex(0x8FE0B4);

        /// <summary>
        /// Buff palette (Heal, Spirit Blessing, Divine Aegis; 2026-11-20 Midgard Senki rules, docs/reference/cleric-fx-ro3/midgard-senki-vs-cleric-fx-chatgpt.md):
        /// the first light pass was mint / white, the snow's own colour family, and burned the Cleric's face white. Now: saturated teal shape, mint halo,
        /// cream-gold warm accents (they read on cool snow; teal reads on yellow-green grass), a deep teal band near the ground instead of a dark outline.
        /// </summary>
        /// <summary>Heal: green identity, bold Midgard treatment (21/11 08:00): dark rim, solid body, highlight, small near-white core.</summary>
        public static readonly Color HealDeep = Hex(0x2E8050), HealMain = Hex(0x3FBF6A), HealLight = Hex(0x8FE8A8), HealCore = Hex(0xE8FFE8);

        public static readonly Color Teal = Hex(0x35B69F), Teal2 = Hex(0x48C9AE), DeepTeal = Hex(0x299C91), Halo = Hex(0xB3FFE5), Gold = Hex(0xFFE6A0);

        /// <summary>
        /// Buff materials: shape layers alpha 0.55-0.8 (saturated body, low additive), halo 0.12-0.25, cream sparks additive but small, flashes short and
        /// small. Rings carry a bright ARC (LightBlob _Arc, angle set per renderer by ClericLightFx.Arc): only ~30 % of the ring glows at once, and the
        /// arc also lightens the body, so it reads and moves with Bloom off. Additive parts stay under the bloom threshold except the spark cores.
        /// </summary>
        public static Material BuffHalo, BuffRing, BuffRune, BuffCross, BuffFlash, BuffColumn, BuffSpark, BuffMote, BuffTrail, BuffHead;

        /// <summary>Half width (rad) of a buff ring's bright arc: 2 x 0.94 rad = 30 % of the circle.</summary>
        public const float ArcHalfWidth = 0.94f;

        /// <summary>Flat on the ground: soft glow pool, ring, rune circle. Upright / over: cross star, white flash, healing column. Particles: mote, sparkle. Trail: bolt tail / blessing stream.</summary>
        public static Material GroundGlow, Ring, Rune, Cross, Flash, Column, Mote, Sparkle, Trail, BoltCore;
        static bool failed, built;

        /// <summary>Light FX on (default) or the painted art (`--cleric-fx legacy`).</summary>
        public static bool Enabled = ReadFlag();

        /// <summary>True when this process draws the Cleric light FX (flag on, renders FX, shaders and textures present).</summary>
        public static bool Active { get { return Enabled && NetMode.RendersFx && Ready; } }

        public static bool Ready { get { Build(); return !failed; } }

        /// <summary>Builds the materials ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Load() { if (NetMode.RendersFx) Build(); }

        static bool ReadFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--cleric-fx");
            return !(i >= 0 && i + 1 < a.Length && a[i + 1].Equals("legacy", StringComparison.OrdinalIgnoreCase));
        }

        static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f); }
        static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

        static void Build()
        {
            if (built) return;
            built = true;
            Shader particle = Resources.Load<Shader>("Shaders/LightParticle");
            const string F = "Sprites/fx/light-skill/";
            Texture2D veil = Resources.Load<Texture2D>(F + "portal-veil"), mote = Resources.Load<Texture2D>(F + "portal-mote"), core = Resources.Load<Texture2D>(F + "meteor-core"),
                cross = Resources.Load<Texture2D>(F + "holy-cross"), rune = Resources.Load<Texture2D>(F + "holy-rune"), band = Resources.Load<Texture2D>(F + "burst-ring");
            if (!LightSkillAssets.Ready || particle == null || !particle.isSupported || veil == null || mote == null || core == null || cross == null || rune == null || band == null)
            {
                Debug.LogWarning("[ZenoniaProto] Cleric light FX: shader / texture missing, the cleric uses the painted FX");
                failed = true;
                return;
            }
            veil.wrapMode = TextureWrapMode.Repeat;
            foreach (Texture2D t in new[] { mote, core, cross, rune, band }) t.wrapMode = TextureWrapMode.Clamp;
            Color jadeGlow = new Color(0.05f, 0.3f, 0.16f), mintGlow = new Color(0.12f, 0.42f, 0.26f);

            // Alpha bodies are SATURATED jade (not milk / mint): the first capture on snow lost every white-ish piece; the additive part adds the light on top.
            GroundGlow = Blob("Cleric_GroundGlow", Texture2D.whiteTexture, 2.6f, jadeGlow * 0.7f, 0f, A(Leaf, 0.5f));
            Ring = Blob("Cleric_Ring", band, 0f, mintGlow, 0.12f, A(Leaf, 0.9f));
            Rune = Blob("Cleric_Rune", rune, 0f, mintGlow * 1.1f, 0.15f, A(Leaf * 0.95f, 1f));
            Cross = Blob("Cleric_Cross", cross, 0f, new Color(0.25f, 0.6f, 0.4f), 0.4f, new Color(0.36f, 0.78f, 0.56f, 0.95f));
            Flash = Blob("Cleric_Flash", core, 0f, new Color(0.22f, 0.5f, 0.35f), 0.45f, new Color(0.42f, 0.82f, 0.6f, 0.85f));
            BoltCore = Blob("Cleric_BoltCore", core, 0f, new Color(0.3f, 0.75f, 0.5f), 0.6f, new Color(0.36f, 0.8f, 0.58f, 0.95f));

            // Healing column: a short soft shaft of rising light on the healed hero (portal veil streaks), translucent so the hero stays readable.
            Column = new Material(LightSkillAssets.Beam) { name = "Cleric_Column" };
            Column.SetTexture("_MainTex", veil);
            Column.SetVector("_MainTiling", new Vector4(1f, 0.6f, 0f, 0f));
            Column.SetFloat("_ScrollSpeed", 1.3f);
            Column.SetFloat("_StreakMin", 0.45f);
            Column.SetFloat("_EdgeSoft", 0.8f);
            Column.SetFloat("_BottomFade", 0.08f);
            Column.SetFloat("_TopFade", 0.55f);
            Column.SetColor("_EmissionColor", mintGlow * 1.3f);
            Column.SetFloat("_CoreWhite", 0.3f);
            Column.SetColor("_BodyColor", A(Leaf, 0.45f));
            Column.SetColor("_EdgeColor", mintGlow * 2f);

            Mote = Particle("Cleric_Mote", particle, mote, new Color(0.22f, 0.38f, 0.3f), 0.1f, 1f);
            Sparkle = Particle("Cleric_Sparkle", particle, cross, new Color(0.3f, 0.5f, 0.38f), 0.25f, 1f);

            Trail = new Material(LightSkillAssets.Trail) { name = "Cleric_Trail" };
            Trail.SetColor("_EmissionColor", new Color(0.35f, 0.85f, 0.55f, 1f));
            Trail.SetFloat("_CoreWhite", 0.45f);
            Trail.SetColor("_BodyColor", A(Leaf, 0.75f));

            BuildBuff(particle, veil, mote, core, cross, rune, band);
        }

        /// <summary>The buff set (see BuffHalo). Emission is linear HDR, kept low and narrowed to the stroke cores (snow ~0.9 plus a little stays coloured).</summary>
        static void BuildBuff(Shader particle, Texture2D veil, Texture2D mote, Texture2D core, Texture2D cross, Texture2D rune, Texture2D band)
        {
            // Heal-only since 21/11 (Blessing / Aegis use ClericBuffStatusLook). 21/11 08:00 bold Midgard colour (user: "hiện tại vẫn nhạt lắm"):
            // solid green body (~65 %) with a dark rim and a highlight centre over a narrow glow (~35 %), glow radius ~ -25 %, white only in tiny cores.
            Color glow = new Color(0.04f, 0.2f, 0.08f);

            BuffHalo = Blob("ClericBuff_Halo", Texture2D.whiteTexture, 4.2f, glow * 0.4f, 0f, A(HealMain, 0.3f));
            BuffRing = Blob("ClericBuff_Ring", band, 0f, glow * 1.3f, 0f, A(HealMain, 0.78f));
            ClericBuffStatusLook.Tone(BuffRing, 2f, A(HealDeep, 1f), 0.5f, A(HealLight, 0.5f), 0.85f, 4f);
            ArcOn(BuffRing, A(HealLight, 0.75f), 0.25f);
            BuffRune = Blob("ClericBuff_Rune", rune, 0f, glow, 0f, A(HealMain, 0.78f));
            ClericBuffStatusLook.Tone(BuffRune, 2.2f, A(HealDeep, 1f), 0.5f, A(HealLight, 0.5f), 0.85f, 4f);
            ArcOn(BuffRune, A(HealLight, 0.6f), 0.3f);
            BuffCross = Blob("ClericBuff_Cross", cross, 0f, glow * 1.2f, 0.04f, A(HealMain, 0.85f));
            ClericBuffStatusLook.Tone(BuffCross, 1.8f, A(HealDeep, 1f), 0.45f, A(HealLight, 0.85f), 0.75f, 4f);
            BuffFlash = Blob("ClericBuff_Flash", core, 0f, glow, 0.06f, A(HealLight, 0.62f)); // short, small: never a white sheet over the face
            ClericBuffStatusLook.Tone(BuffFlash, 1.3f, A(HealMain, 0.5f), 0.45f, A(HealCore, 0.5f), 0.9f, 3f);
            BuffHead = Blob("ClericBuff_Head", core, 0f, glow * 1.2f, 0.22f, A(HealLight, 0.9f));
            ClericBuffStatusLook.Tone(BuffHead, 1.5f, A(HealMain, 0.6f), 0.5f, A(HealCore, 0.7f), 0.85f, 2.5f);

            // Green shaft rising BEHIND the healed hero: the hero stays in front of it, so hair and face are never covered.
            BuffColumn = new Material(LightSkillAssets.Beam) { name = "ClericBuff_Column" };
            BuffColumn.SetTexture("_MainTex", veil);
            BuffColumn.SetVector("_MainTiling", new Vector4(1f, 0.6f, 0f, 0f));
            BuffColumn.SetFloat("_ScrollSpeed", 1.2f);
            BuffColumn.SetFloat("_StreakMin", 0.55f);
            BuffColumn.SetFloat("_EdgeSoft", 0.7f);
            BuffColumn.SetFloat("_BottomFade", 0.1f);
            BuffColumn.SetFloat("_TopFade", 0.6f);
            BuffColumn.SetColor("_EmissionColor", glow * 0.8f);
            BuffColumn.SetFloat("_CoreWhite", 0.03f);
            BuffColumn.SetColor("_BodyColor", A(HealMain, 0.62f));
            BuffColumn.SetColor("_EdgeColor", A(HealLight * 0.5f, 1f));

            BuffSpark = Particle("ClericBuff_Spark", particle, cross, new Color(0.3f, 0.36f, 0.26f), 0.25f, 1f); // the only white-hot bits, and tiny
            BuffMote = Particle("ClericBuff_Mote", particle, mote, new Color(0.04f, 0.2f, 0.1f), 0.03f, 1f);

            BuffTrail = new Material(LightSkillAssets.Trail) { name = "ClericBuff_Trail" };
            BuffTrail.SetColor("_EmissionColor", new Color(0.1f, 0.35f, 0.18f, 1f));
            BuffTrail.SetFloat("_CoreWhite", 0.1f);
            BuffTrail.SetColor("_BodyColor", A(HealMain, 0.88f));
        }

        static void ArcOn(Material m, Color tint, float glowFloor)
        {
            m.SetVector("_Arc", new Vector4(0f, ArcHalfWidth, glowFloor, 1f));
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

        /// <summary>LightParticle: the per-particle colour tints both passes, so one material serves every hue of the palette.</summary>
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
