using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Colours and materials of the Warrior's shader FX, "RO3 Soft Fantasy V1" (2026-11-21, user: "làm lại hết skill của warrior ... đổi qua kiểu
    /// mới"; rules at the top of docs/sprite-rules.md). Built on the Cleric light toolkit (ClericLightLook roles, ClericLightFx timeline, LightBlob):
    /// nothing in it is renamed, the Warrior only adds palettes and three masks. One palette per function (rule 8, at most 2 main colours + 1 core),
    /// warm like the redrawn Warrior icons (docs/reference/skill-icons-soft-fantasy/warrior-after-soft-fantasy-icons.png: coral disc, cream blades,
    /// orange-gold flames):
    ///  - Blade (physical slashes: basic attack, Power Slash, Whirlwind, Charging Strike, Blade Cyclone): soft coral body, cream head, dark red rim;
    ///  - Flame (fire: Flame Cleave, Inferno Crash, Earth Break burns): soft orange, gold highlight;
    ///  - Earth (Earth Break sword and seal): amber gold, brown rim;
    ///  - Rage (Berserker Awakening, an ATK buff = gold in rule 8, pushed to orange-gold for the class): gold-orange.
    /// Warm additive light on a pale road / snow burns to white (Sổ lỗi 2026-11-19 portal, 2026-11-20 Cleric), so the shape is carried by the
    /// saturated alpha body with a darker rim and the glow stays low. Masks war-sigil / war-crack / war-flame / war-greatsword: Tools/light_skill_textures_warrior.py.
    /// </summary>
    public static class WarriorLightLook
    {
        public static readonly ClericLightLook.Pal Blade = new ClericLightLook.Pal("WarBlade", 0xF2876A, 0xFFE2C6, 0xFFF7EA, 0xA8432F, new Color(0.2f, 0.09f, 0.05f));
        public static readonly ClericLightLook.Pal Flame = new ClericLightLook.Pal("WarFlame", 0xF7A148, 0xFFD98C, 0xFFF3D0, 0xAE5320, new Color(0.22f, 0.12f, 0.03f));
        public static readonly ClericLightLook.Pal Earth = new ClericLightLook.Pal("WarEarth", 0xE9B865, 0xFFE7AE, 0xFFF8E4, 0x8E6127, new Color(0.16f, 0.12f, 0.04f));
        public static readonly ClericLightLook.Pal Rage = new ClericLightLook.Pal("WarRage", 0xF6B44E, 0xFFE6A6, 0xFFFAE8, 0xA0581C, new Color(0.2f, 0.13f, 0.03f));

        /// <summary>Warrior masks (Resources/Sprites/fx/light-skill). Sigil and crack lie on the ground; the flame and the great sword (tip down) stand on their foot (Upright standing).</summary>
        public const string Sigil = "war-sigil", Crack = "war-crack", FlameTongue = "war-flame", GreatSword = "war-greatsword";
        static readonly string[] Own = { Sigil, Crack, FlameTongue, GreatSword };

        /// <summary>Hero height in world units: every size below is a fraction of it (H in the RO3 rules).</summary>
        public const float H = HeroLayeredSet.BodyUnits;

        /// <summary>A ring mask's main band sits at this fraction of the quad radius (war-sigil, holy-rune-ring); burst-ring's band at 0.78.</summary>
        public const float SigilBand = 0.955f, BandRing = 0.78f;

        static readonly Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static Material bodyRing;
        static bool built, failed;

        /// <summary>True when the Warrior FX can draw here (renders FX, Cleric toolkit shaders + the Warrior masks present).</summary>
        public static bool Ready { get { return NetMode.RendersFx && Build(); } }

        /// <summary>Loads masks / shaders ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Load() { if (NetMode.RendersFx) Build(); }

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

        /// <summary>Material drawing `mask` in palette `p` as `role`: Cleric masks go straight to ClericLightLook.M; a Warrior mask takes the same
        /// role material (same tone, rim, glow) with its own texture. Cached.</summary>
        public static Material M(string mask, ClericLightLook.Pal p, ClericLightLook.Role role)
        {
            Texture2D tex;
            if (role == ClericLightLook.Role.Fill || !Tex.TryGetValue(mask, out tex)) return ClericLightLook.M(mask, p, role);
            string key = mask + "|" + p.Name + "|" + role;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(ClericLightLook.M(ClericLightLook.Star, p, role)) { name = "Warrior_" + key };
            m.SetTexture("_MainTex", tex);
            Cache[key] = m;
            return m;
        }

        /// <summary>Brighter LightTrail for the sword slashes (user 21/11: attack FX must feel big and powerful): denser body, more additive light
        /// and a hotter centre line than the Cleric trail; the glow stays warm-low in R/G so a pale road tints instead of clipping.</summary>
        public static Material TrailBright(ClericLightLook.Pal p)
        {
            string key = "trail+|" + p.Name;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(ClericLightLook.Trail(p)) { name = "Warrior_" + key };
            m.SetColor("_EmissionColor", new Color(0.62f, 0.5f, 0.4f, 1f));
            m.SetFloat("_CoreWhite", 0.42f);
            m.SetColor("_BodyColor", ClericLightLook.A(p.Main, 0.92f));
            Cache[key] = m;
            return m;
        }

        public static Material Line(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Line); }
        public static Material Glyph(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Glyph); }
        public static Material Flash(ClericLightLook.Pal p) { return M(ClericLightLook.Core, p, ClericLightLook.Role.Flash); }
        public static Material Spark(ClericLightLook.Pal p) { return M(ClericLightLook.Sparkle, p, ClericLightLook.Role.Flash); }
        public static Material Fill(ClericLightLook.Pal p) { return M(ClericLightLook.Band, p, ClericLightLook.Role.Fill); }
        public static Material Wave(ClericLightLook.Pal p) { return M(ClericLightLook.Band, p, ClericLightLook.Role.Line); }

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
                bodyRing.SetColor("_BodyColor", ClericLightLook.A(ClericLightLook.Hex(0xEE7A45), 0.78f));
                bodyRing.SetColor("_RimColor", ClericLightLook.A(ClericLightLook.Hex(0x8E3517), 0.85f));
                bodyRing.SetColor("_HiColor", ClericLightLook.A(ClericLightLook.Hex(0xFFE09A), 0.95f));
                bodyRing.SetColor("_CoreColor", new Color(0.5f, 0.26f, 0.08f, 1f)); // low, red-heavy: a pale road does not burn it white
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
            if (flashA > 0f) ScreenFlash.Trigger(new Color(1f, 0.55f, 0.25f, flashA), 0.16f);
        }
    }
}
