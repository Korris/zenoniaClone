using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Colours and materials of the Warrior's shader FX. First pass "RO3 Soft Fantasy V1" (d1cb6ec1) used pastel coral / cream and the Cleric's
    /// low-glow recipes: on snow the rings were pale hairlines and the flames washed out (user 21/11: "chơi warrior vẫn thấy chán, không có hiệu
    /// ứng gì mạnh mẽ"). GRAND pass (21/11, docs/reference/warrior-fx-grand/): ATTACK FX are bold and saturated, not pastel:
    ///  - Blade (sword slashes): crimson-red body, orange-gold highlight, white-hot core, near-black red rim;
    ///  - Flame (fire): deep orange-red body, gold highlight, white-yellow core, dark red rim;
    ///  - Earth (Earth Break sword / seal): blazing gold-orange, brown-red rim;
    ///  - Rage (Berserker Awakening): red-orange, gold highlight.
    /// Every material is the Warrior's own (never the shared Cleric one): solid opaque body (_Solid high), a DARK rim band (what keeps the shape on
    /// snow), a hot highlight toward the centre and a stronger additive part (what makes it glow on grass and in the dark cave). Masks:
    /// war-sigil / war-crack / war-flame / war-greatsword / war-shock / war-scorch / war-burst (Tools/light_skill_textures_warrior.py).
    /// </summary>
    public static class WarriorLightLook
    {
        public static readonly ClericLightLook.Pal Blade = new ClericLightLook.Pal("WarBlade", 0xE8361E, 0xFFB03A, 0xFFF6D6, 0x4A0A04, new Color(0.75f, 0.2f, 0.05f));
        public static readonly ClericLightLook.Pal Flame = new ClericLightLook.Pal("WarFlame", 0xE8361E, 0xFF8B35, 0xFFE08A, 0x4E0C04, new Color(0.8f, 0.24f, 0.04f)); // round 2: red body 45-55 %, orange 25-35 %, yellow / white small
        public static readonly ClericLightLook.Pal Earth = new ClericLightLook.Pal("WarEarth", 0xFF9A12, 0xFFE070, 0xFFFBEA, 0x5C2304, new Color(0.85f, 0.5f, 0.1f));
        public static readonly ClericLightLook.Pal Rage = new ClericLightLook.Pal("WarRage", 0xF2401A, 0xFFB43C, 0xFFF2CC, 0x520C04, new Color(0.8f, 0.24f, 0.05f));

        /// <summary>Warrior masks (Resources/Sprites/fx/light-skill). Sigil, crack, shock, scorch lie on the ground; flame and great sword stand on their foot.</summary>
        public const string Sigil = "war-sigil", Crack = "war-crack", FlameTongue = "war-flame", GreatSword = "war-greatsword",
            Shock = "war-shock", Scorch = "war-scorch", Burst = "war-burst";
        static readonly string[] Own = { Sigil, Crack, FlameTongue, GreatSword, Shock, Scorch, Burst };

        /// <summary>Hero height in world units: every size below is a fraction of it (H in the RO3 rules).</summary>
        public const float H = HeroLayeredSet.BodyUnits;

        /// <summary>Main band of a ring mask as a fraction of the quad radius: war-sigil 0.955, burst-ring 0.78, war-shock front 0.89.</summary>
        public const float SigilBand = 0.955f, BandRing = 0.78f, ShockBand = 0.89f;

        static readonly Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static Material bodyRing;
        static bool built, failed;

        /// <summary>True when the Warrior FX can draw here (renders FX, Cleric toolkit shaders + the Warrior masks present).</summary>
        public static bool Ready { get { return NetMode.RendersFx && Build(); } }

        /// <summary>Loads masks / shaders ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Load() { if (!NetMode.RendersFx) return; Build(); WarriorPaintedSword.Preload(); }

        static bool Build()
        {
            if (built) return !failed;
            built = true;
            if (!ClericLightLook.Ready) { failed = true; Debug.LogWarning("[ZenoniaProto] Warrior light FX: light shaders missing, no skill FX"); return false; }
            foreach (string n in Own)
            {
                Texture2D t = Resources.Load<Texture2D>("Sprites/fx/light-skill/" + n);
                if (t == null) { failed = true; Debug.LogWarning("[ZenoniaProto] Warrior light FX: mask " + n + " missing"); continue; }
                t.wrapMode = TextureWrapMode.Clamp;
                Tex[n] = t;
            }
            return !failed;
        }

        /// <summary>Bold material drawing `mask` in palette `p` as `role` (Warrior recipe, cached):
        /// Line = rings / strokes: opaque Main body, dark rim, Light centre, real glow; Glyph = flame / sword / star: opaque, dark rim, white-hot
        /// centre; Flash = the white-hot impact core: Core body with an orange rim halo, strong bloom; Fill = warm ground light pool (~35 %).</summary>
        public static Material M(string mask, ClericLightLook.Pal p, ClericLightLook.Role role)
        {
            string key = mask + "|" + p.Name + "|" + role;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            Texture2D tex;
            if (!Tex.TryGetValue(mask, out tex)) tex = SkillLightMaterials.Mask(mask);
            m = new Material(LightSkillAssets.Glow) { name = "WarriorGrand_" + key };
            m.SetTexture("_MainTex", role == ClericLightLook.Role.Fill ? Texture2D.whiteTexture : tex != null ? tex : Texture2D.whiteTexture);
            m.SetFloat("_Falloff", 0f);
            switch (role)
            {
                case ClericLightLook.Role.Fill:
                    m.SetFloat("_Falloff", 3f);
                    m.SetColor("_EmissionColor", p.Glow * 0.4f); m.SetFloat("_CoreWhite", 0f); m.SetColor("_BodyColor", ClericLightLook.A(p.Main, 0.26f));
                    break;
                case ClericLightLook.Role.Flash:
                    m.SetColor("_EmissionColor", p.Glow * 2f); m.SetFloat("_CoreWhite", 0.9f); m.SetColor("_BodyColor", ClericLightLook.A(p.Light, 0.9f));
                    ClericLightLook.Tone(m, 1.15f, ClericLightLook.A(p.Main, 0.55f), 0.45f, ClericLightLook.A(p.Core, 1f), 0.78f, 2f); // white only in the centre (round 1: white discs)
                    break;
                case ClericLightLook.Role.Glyph:
                    m.SetColor("_EmissionColor", p.Glow * 1.5f); m.SetFloat("_CoreWhite", 0.35f); m.SetColor("_BodyColor", ClericLightLook.A(p.Main, 1f));
                    ClericLightLook.Tone(m, 2.4f, ClericLightLook.A(p.Rim, 0.95f), 0.5f, ClericLightLook.A(p.Light, 0.95f), 0.8f, 2.4f); // gold highlight, white only from the additive core
                    break;
                default: // Line / Arc
                    m.SetColor("_EmissionColor", p.Glow * 1.3f); m.SetFloat("_CoreWhite", 0.25f); m.SetColor("_BodyColor", ClericLightLook.A(p.Main, 1f));
                    ClericLightLook.Tone(m, 2.6f, ClericLightLook.A(p.Rim, 0.92f), 0.5f, ClericLightLook.A(p.Light, 0.95f), 0.72f, 2.4f);
                    if (role == ClericLightLook.Role.Arc) { m.SetVector("_Arc", new Vector4(0f, ClericLightLook.ArcHalfWidth, 0.45f, 1f)); m.SetColor("_ArcTint", ClericLightLook.A(p.Core, 0.8f)); }
                    break;
            }
            Cache[key] = m;
            return m;
        }

        /// <summary>LightTrail ribbon material of one slash layer: `body` = alpha colour (what reads on snow), `glow` = additive multiplier of the
        /// vertex colour, `core` = white-hot centre line. Slash layers: dark rim underlay, opaque main body, hot core (WarriorGrandFx.Slash).</summary>
        public static Material TrailLayer(string name, Color body, Color glow, float core)
        {
            string key = "trail|" + name;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(LightSkillAssets.Trail) { name = "WarriorGrand_" + key };
            m.SetColor("_EmissionColor", glow);
            m.SetFloat("_CoreWhite", core);
            m.SetColor("_BodyColor", body);
            Cache[key] = m;
            return m;
        }

        /// <summary>Kept for callers of the first pass: the main body layer of a slash.</summary>
        public static Material TrailBright(ClericLightLook.Pal p) { return TrailLayer(p.Name + "|main", ClericLightLook.A(p.Main, 1f), new Color(1.1f, 0.6f, 0.3f, 1f), 0.7f); }

        /// <summary>Dark scorch decal on the ground (alpha only, no glow): a burnt patch that lingers after a fire / earth impact.</summary>
        public static Material ScorchMat
        {
            get
            {
                Material m;
                if (Cache.TryGetValue("scorch", out m) && m != null) return m;
                m = new Material(LightSkillAssets.Glow) { name = "WarriorGrand_scorch" };
                Texture2D tex;
                m.SetTexture("_MainTex", Tex.TryGetValue(Scorch, out tex) ? tex : Texture2D.whiteTexture);
                m.SetFloat("_Falloff", 0f); m.SetColor("_EmissionColor", Color.black); m.SetFloat("_CoreWhite", 0f);
                m.SetColor("_BodyColor", ClericLightLook.A(ClericLightLook.Hex(0x3E2A22), 0.85f));
                ClericLightLook.Tone(m, 2.2f, ClericLightLook.A(ClericLightLook.Hex(0x5A3018), 0.6f), 0.35f, ClericLightLook.A(ClericLightLook.Hex(0x160804), 0.8f), 0.7f, 1f);
                Cache["scorch"] = m;
                return m;
            }
        }

        public static Material Line(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Line); }
        public static Material Glyph(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Glyph); }
        public static Material Flash(ClericLightLook.Pal p) { return M(ClericLightLook.Core, p, ClericLightLook.Role.Flash); }
        public static Material Spark(ClericLightLook.Pal p) { return M(Burst, p, ClericLightLook.Role.Glyph); }
        public static Material Fill(ClericLightLook.Pal p) { return M(ClericLightLook.Band, p, ClericLightLook.Role.Fill); }
        public static Material Wave(ClericLightLook.Pal p) { return M(Shock, p, ClericLightLook.Role.Line); }

        /// <summary>Maintained Berserker ring round the body (rule 10 exception: an ATK buff shows a clearly visible ring round the hero): the Cleric
        /// BuffBodyRing shader in the Rage colours, red-orange so it never reads as the Cleric's gold ATK ring. Null when the shader is missing.</summary>
        public static Material BodyRing
        {
            get
            {
                if (bodyRing != null) return bodyRing;
                Shader sh = Resources.Load<Shader>("Shaders/BuffBodyRing");
                if (sh == null || !sh.isSupported) return null;
                bodyRing = new Material(sh) { name = "Warrior_BerserkerBodyRing" };
                bodyRing.SetColor("_BodyColor", ClericLightLook.A(ClericLightLook.Hex(0xEE4A25), 0.85f));
                bodyRing.SetColor("_RimColor", ClericLightLook.A(ClericLightLook.Hex(0x5A1206), 0.9f));
                bodyRing.SetColor("_HiColor", ClericLightLook.A(ClericLightLook.Hex(0xFFD070), 0.95f));
                bodyRing.SetColor("_CoreColor", new Color(0.7f, 0.25f, 0.06f, 1f));
                bodyRing.SetFloat("_Segs", 3f);
                bodyRing.SetFloat("_SegLen", 0.22f);
                return bodyRing;
            }
        }

        /// <summary>Ellipse parameter angle (deg) of a screen direction `deg` on a ring squashed by `squash` (so an arc centred on it faces that way).</summary>
        public static float ParamDeg(float deg, float squash)
        {
            float r = deg * Mathf.Deg2Rad;
            return Mathf.Atan2(Mathf.Sin(r) / Mathf.Max(0.05f, squash), Mathf.Cos(r)) * Mathf.Rad2Deg;
        }

        /// <summary>Shake / hit-stop / punch / flash for the caster's own screen only (sprite-rules VFX 12).</summary>
        public static void Punch(bool local, float hitStopMs, float shake, float punch = 0f, float flashA = 0f)
        {
            if (!local) return;
            if (hitStopMs > 0f) GameClock.HitStop(hitStopMs);
            if (shake > 0f) CameraFollow.SkillShake(shake);
            if (punch > 0f) CameraFollow.SkillPunch(punch);
            if (flashA > 0f) ScreenFlash.Trigger(new Color(1f, 0.5f, 0.2f, flashA), 0.16f);
        }
    }
}
