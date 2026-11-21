using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Colours and materials of the Warrior's shader FX, "RO trailer style" (user 21/11: "làm lại warrior vì hiện tại cái style này không liên quan
    /// với ragnarok tui muốn"; guide docs/reference/ro-trailer-skill-style/prompt-templates.md, before / after docs/reference/warrior-fx-ro-trailer/).
    /// "Knight of Light and Steel": BIG, TRANSLUCENT, PALE, clear shapes. RO Translucent Light rules: Outer same-family pale (never a dark rim),
    /// Body pastel at alpha 0.18-0.40 (Alpha Blend), Bright pale gold 0.45-0.75 and Core ivory 0.75-0.95 additive on thin lines only.
    /// Pal fields: Main = Body, Light = Bright, Core = Core, Rim = Outer (pale, same family), Glow = additive HDR multiplier (kept under the
    /// BloomLite threshold except the core lines / impact). The old red GRAND pass (dark burgundy rims, opaque bodies, fire) is gone.
    ///  - Blade (Knight gold, the Warrior palette) for slashes, sectors, impacts; Steel for wind / dash / outer air; Flame = honey fire (only
    ///    Flame Cleave, Inferno Crash, fire &lt;= 20-30 % of the area); Earth = Earth Break "Golden Knight Judgment"; Rage = Berserker ivory gold;
    ///    Coral = the few short Berserker streaks. Red / deep red-orange stays reserved for boss warnings.
    /// </summary>
    public static class WarriorLightLook
    {
        public static readonly ClericLightLook.Pal Blade = new ClericLightLook.Pal("WarKnight", 0xD5C378, 0xF5EA7C, 0xFEFDF8, 0xB2A873, new Color(0.42f, 0.38f, 0.17f));
        public static readonly ClericLightLook.Pal Steel = new ClericLightLook.Pal("WarSteel", 0xA5BCCD, 0xDEE9EF, 0xFFFFFF, 0x718B9D, new Color(0.26f, 0.32f, 0.38f));
        public static readonly ClericLightLook.Pal Flame = new ClericLightLook.Pal("WarHoney", 0xE0A35A, 0xF6D586, 0xFFF9DE, 0xB67C50, new Color(0.48f, 0.32f, 0.12f));
        public static readonly ClericLightLook.Pal Earth = new ClericLightLook.Pal("WarJudgment", 0xC6B37D, 0xF3E1A8, 0xFFFFFF, 0x887C5A, new Color(0.42f, 0.37f, 0.2f));
        public static readonly ClericLightLook.Pal Rage = new ClericLightLook.Pal("WarAura", 0xD9C784, 0xF7EDB0, 0xFFFDF4, 0xB2A873, new Color(0.42f, 0.38f, 0.2f));
        public static readonly ClericLightLook.Pal Coral = new ClericLightLook.Pal("WarCoral", 0xF37860, 0xFFC1A0, 0xFFF8ED, 0xB96064, new Color(0.45f, 0.22f, 0.15f));

        /// <summary>Warrior masks (Resources/Sprites/fx/light-skill). Sigil, crack, shock lie on the ground; flame and great sword stand on their foot.</summary>
        public const string Sigil = "war-sigil", Crack = "war-crack", FlameTongue = "war-flame", GreatSword = "war-greatsword",
            Shock = "war-shock", Scorch = "war-scorch", Burst = "war-burst";
        static readonly string[] Own = { Sigil, Crack, FlameTongue, GreatSword, Shock, Scorch, Burst };

        /// <summary>Hero height in world units: every size below is a fraction of it (H in the RO rules).</summary>
        public const float H = HeroLayeredSet.BodyUnits;

        /// <summary>Main band of a ring mask as a fraction of the quad radius: war-sigil 0.955, burst-ring 0.78, war-shock front 0.89.</summary>
        public const float SigilBand = 0.955f, BandRing = 0.78f, ShockBand = 0.89f;

        /// <summary>Rim band radius of an RoSoftSector quad (fraction of the quad radius): the fan's bright edge sits on the real hit range.</summary>
        public const float SectorRim = 0.9f;

        static readonly Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static Shader sectorShader;
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
            sectorShader = Resources.Load<Shader>("Shaders/RoSoftSector");
            if (sectorShader != null && !sectorShader.isSupported) sectorShader = null;
            if (sectorShader == null) Debug.LogWarning("[ZenoniaProto] Warrior light FX: RoSoftSector shader missing, sectors drawn as ring arcs");
            return !failed;
        }

        static Color Scale(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

        /// <summary>Translucent material drawing `mask` in palette `p` as `role` (RO recipe, cached):
        /// Line = rings / strokes: pale body ~0.35, same-family pale edge (no dark rim), Bright on the stroke centre, glow narrowed to the core;
        /// Glyph = star / spark / flame: Bright body ~0.7, ivory centre; Flash = the short ivory impact core (only part allowed to bloom);
        /// Fill = faint ground pool (~0.12); Arc = Line with a bright arc running round.</summary>
        public static Material M(string mask, ClericLightLook.Pal p, ClericLightLook.Role role)
        {
            string key = mask + "|" + p.Name + "|" + role;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            Texture2D tex;
            if (!Tex.TryGetValue(mask, out tex)) tex = SkillLightMaterials.Mask(mask);
            m = new Material(LightSkillAssets.Glow) { name = "WarriorRo_" + key };
            m.SetTexture("_MainTex", role == ClericLightLook.Role.Fill ? Texture2D.whiteTexture : tex != null ? tex : Texture2D.whiteTexture);
            m.SetFloat("_Falloff", 0f);
            switch (role)
            {
                case ClericLightLook.Role.Fill:
                    m.SetFloat("_Falloff", 3f);
                    m.SetColor("_EmissionColor", p.Glow * 0.25f); m.SetFloat("_CoreWhite", 0f); m.SetColor("_BodyColor", ClericLightLook.A(p.Main, 0.14f));
                    break;
                case ClericLightLook.Role.Flash:
                    m.SetColor("_EmissionColor", p.Glow * 1.2f); m.SetFloat("_CoreWhite", 0.3f); m.SetColor("_BodyColor", ClericLightLook.A(p.Light, 0.42f));
                    ClericLightLook.Tone(m, 1.1f, ClericLightLook.A(p.Main, 0.4f), 0.45f, ClericLightLook.A(p.Core, 0.85f), 0.8f, 2.6f); // ivory only in the very centre
                    break;
                case ClericLightLook.Role.Glyph:
                    m.SetColor("_EmissionColor", p.Glow * 0.8f); m.SetFloat("_CoreWhite", 0.1f); m.SetColor("_BodyColor", ClericLightLook.A(p.Light, 0.6f));
                    ClericLightLook.Tone(m, 1.3f, ClericLightLook.A(p.Main, 0.7f), 0.5f, ClericLightLook.A(p.Core, 0.6f), 0.88f, 3f); // r1: Core body read as white blobs on snow
                    break;
                default: // Line / Arc
                    m.SetColor("_EmissionColor", p.Glow * 0.9f); m.SetFloat("_CoreWhite", 0.2f); m.SetColor("_BodyColor", ClericLightLook.A(p.Main, 0.62f));
                    ClericLightLook.Tone(m, 1.3f, ClericLightLook.A(p.Rim, 0.5f), 0.45f, ClericLightLook.A(p.Light, 0.9f), 0.78f, 3.2f); // r1: thin strokes at 0.42 vanished on snow
                    if (role == ClericLightLook.Role.Arc) { m.SetVector("_Arc", new Vector4(0f, ClericLightLook.ArcHalfWidth, 0.35f, 1f)); m.SetColor("_ArcTint", ClericLightLook.A(p.Core, 0.6f)); }
                    break;
            }
            Cache[key] = m;
            return m;
        }

        /// <summary>Ground fan (RoSoftSector) of `angleDeg` (>= 360 = full ring) in palette `p`: body alpha `bodyA` (0.18-0.40), bright rim
        /// `rimA`, inner transparent hole `inner` (fraction of the radius). Null when the shader is missing.</summary>
        public static Material Sector(ClericLightLook.Pal p, float angleDeg, float bodyA, float rimA, float inner = 0.12f, float rimW = 0.05f)
        {
            if (sectorShader == null) return null;
            string key = "sector|" + p.Name + "|" + Mathf.RoundToInt(angleDeg) + "|" + bodyA.ToString("0.00") + "|" + rimA.ToString("0.00") + "|" + inner.ToString("0.00") + "|" + rimW.ToString("0.000");
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(sectorShader) { name = "WarriorRo_" + key };
            m.SetFloat("_Half", angleDeg >= 359f ? Mathf.PI : angleDeg * 0.5f * Mathf.Deg2Rad);
            m.SetFloat("_Inner", inner);
            m.SetFloat("_RimR", SectorRim);
            m.SetFloat("_RimW", rimW);
            m.SetColor("_OuterColor", ClericLightLook.A(p.Rim, 1f));
            m.SetColor("_BodyColor", ClericLightLook.A(p.Main, bodyA));
            m.SetColor("_BrightColor", ClericLightLook.A(p.Light, rimA));
            m.SetColor("_EmissionColor", Scale(p.Glow, 1.1f));
            m.SetFloat("_CoreWhite", 0.55f);
            m.SetFloat("_Streaks", 0.5f);
            Cache[key] = m;
            return m;
        }

        /// <summary>LightTrail ribbon material of one slash layer: `body` = alpha colour (what reads on snow), `glow` = additive multiplier of the
        /// vertex colour, `core` = ivory centre line. Slash layers: soft outer veil, translucent body, thin bright edge (WarriorGrandFx.Slash).</summary>
        public static Material TrailLayer(string name, Color body, Color glow, float core)
        {
            string key = "trail|" + name;
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(LightSkillAssets.Trail) { name = "WarriorRo_" + key };
            m.SetColor("_EmissionColor", glow);
            m.SetFloat("_CoreWhite", core);
            m.SetColor("_BodyColor", body);
            Cache[key] = m;
            return m;
        }

        /// <summary>The translucent body layer of a slash (body alpha ~0.34, low additive).</summary>
        public static Material TrailBright(ClericLightLook.Pal p) { return TrailLayer(p.Name + "|main", ClericLightLook.A(p.Main, 0.55f), Scale(p.Glow, 0.9f), 0.15f); } // r1: 0.34 was invisible on snow

        public static Material Line(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Line); }
        public static Material Glyph(string mask, ClericLightLook.Pal p) { return M(mask, p, ClericLightLook.Role.Glyph); }
        public static Material Flash(ClericLightLook.Pal p) { return M(ClericLightLook.Core, p, ClericLightLook.Role.Flash); }
        public static Material Spark(ClericLightLook.Pal p) { return M(Burst, p, ClericLightLook.Role.Glyph); }
        public static Material Fill(ClericLightLook.Pal p) { return M(ClericLightLook.Band, p, ClericLightLook.Role.Fill); }
        public static Material Wave(ClericLightLook.Pal p) { return M(Shock, p, ClericLightLook.Role.Line); }

        /// <summary>Maintained Berserker ring round the body (rule 10 exception: an ATK buff shows a clearly visible ring round the hero): the Cleric
        /// BuffBodyRing shader in ivory gold with a soft coral accent on the bright segments (RO: Berserker = gold aura + a little coral), no dark
        /// rim. Null when the shader is missing.</summary>
        public static Material BodyRing
        {
            get
            {
                if (bodyRing != null) return bodyRing;
                Shader sh = Resources.Load<Shader>("Shaders/BuffBodyRing");
                if (sh == null || !sh.isSupported) return null;
                bodyRing = new Material(sh) { name = "Warrior_BerserkerBodyRing" };
                bodyRing.SetColor("_BodyColor", ClericLightLook.A(ClericLightLook.Hex(0xD9C784), 0.55f));
                bodyRing.SetColor("_RimColor", ClericLightLook.A(ClericLightLook.Hex(0xB2A873), 0.35f));
                bodyRing.SetColor("_HiColor", ClericLightLook.A(ClericLightLook.Hex(0xFFC1A0), 0.8f));
                bodyRing.SetColor("_CoreColor", new Color(0.45f, 0.38f, 0.2f, 1f));
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

        /// <summary>Shake / hit-stop / punch / a soft ivory flash for the caster's own screen only (sprite-rules VFX 12).</summary>
        public static void Punch(bool local, float hitStopMs, float shake, float punch = 0f, float flashA = 0f)
        {
            if (!local) return;
            if (hitStopMs > 0f) GameClock.HitStop(hitStopMs);
            if (shake > 0f) CameraFollow.SkillShake(shake);
            if (punch > 0f) CameraFollow.SkillPunch(punch);
            if (flashA > 0f) ScreenFlash.Trigger(IvoryFlash(flashA), 0.14f);
        }

        /// <summary>Screen flash colour of a heavy Warrior hit: warm ivory, never orange-red (red = boss warnings).</summary>
        public static Color IvoryFlash(float a) { return new Color(1f, 0.96f, 0.82f, a); }
    }
}
