using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Class-neutral access to the RO3 Soft Fantasy V1 shader materials (2026-11-21, every class's skills move to Particle System + mesh trail +
    /// shader, docs/sprite-rules.md top block). Builds on the Cleric toolkit without renaming it: ClericLightLook keeps the role recipes
    /// (Line / Fill / Glyph / Flash / Arc, dark rim of the same hue, low additive part) and the palette type; this adds
    ///  - any mask of Resources/Sprites/fx/light-skill, loaded on demand (ClericLightLook only knows its own list);
    ///  - Smoke: an alpha-only soft puff (no additive part) that dissolves (explosions, exhaust);
    ///  - Sparks: a stretched-billboard particle system (sparks streaking along their velocity).
    /// Masks are grey on black (Tools/build-light-skill-textures.py).
    /// </summary>
    public static class SkillLightMaterials
    {
        const string Folder = "Sprites/fx/light-skill/";
        static readonly Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// <summary>True when the light shaders and the Cleric palette toolkit are usable in this process.</summary>
        public static bool Ready { get { return NetMode.RendersFx && ClericLightLook.Ready; } }

        /// <summary>The grey mask `name` (clamped), null when missing (logged once).</summary>
        public static Texture2D Mask(string name)
        {
            Texture2D t;
            if (Tex.TryGetValue(name, out t)) return t;
            t = Resources.Load<Texture2D>(Folder + name);
            if (t == null) Debug.LogWarning("[ZenoniaProto] light FX mask " + name + " missing");
            else t.wrapMode = TextureWrapMode.Clamp;
            Tex[name] = t;
            return t;
        }

        /// <summary>The material drawing `mask` in palette `p` as `role` (ClericLightLook recipe; masks outside its list are filled in here).</summary>
        public static Material M(string mask, ClericLightLook.Pal p, ClericLightLook.Role role)
        {
            Material m = ClericLightLook.M(mask, p, role);
            if (m != null && role != ClericLightLook.Role.Fill && m.GetTexture("_MainTex") == Texture2D.whiteTexture)
            {
                Texture2D t = Mask(mask);
                if (t != null) m.SetTexture("_MainTex", t); // the cache key is mask|palette|role: this material only ever draws this mask
            }
            return m;
        }

        /// <summary>Soft smoke puff in palette `p` (Main body, Rim darker edge, Light top), alpha blended only: reads on snow and on dark ground, never blooms.</summary>
        public static Material Smoke(string mask, ClericLightLook.Pal p, float alpha = 0.72f)
        {
            string key = "smoke|" + mask + "|" + p.Name + "|" + alpha.ToString("F2");
            Material m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(LightSkillAssets.Glow) { name = "Light_" + key };
            m.SetTexture("_MainTex", Mask(mask) ?? Texture2D.whiteTexture);
            m.SetFloat("_Falloff", 0f);
            m.SetColor("_EmissionColor", Color.black);
            m.SetFloat("_CoreWhite", 0f);
            m.SetColor("_BodyColor", ClericLightLook.A(p.Main, alpha));
            ClericLightLook.Tone(m, 1.25f, ClericLightLook.A(p.Rim, 0.55f), 0.35f, ClericLightLook.A(p.Light, 0.5f), 0.85f, 1f);
            Cache[key] = m;
            return m;
        }

        /// <summary>A particle system under `parent` whose particles stretch along their velocity (sparks): soft dot mask, colour per particle.</summary>
        public static ParticleSystem Sparks(Transform parent, int order, int max, float gravity = 0f, float stretch = 0.045f)
        {
            ParticleSystem ps = WantedFxParticles.Create(parent, "Sparks", ClericLightLook.DotMat, order, max, gravity);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = stretch;
            r.lengthScale = 1.2f;
            return ps;
        }
    }
}
