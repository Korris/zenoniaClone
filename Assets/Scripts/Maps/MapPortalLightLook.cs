using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Materials of the portal light (MapPortalLightFx), one set per colour region, built from the light skill shaders (LightSkillAssets: LightBeamScroll,
    /// LightBlob) plus LightParticle for the motes. Every material has an alpha-blended saturated body (reads on snow and pale roads) under an HDR additive
    /// part kept low so only the thin core of the column passes the BloomLite threshold (docs/sprite-rules.md Sổ lỗi 2026-11-19, light shader on snow).
    /// </summary>
    public static class MapPortalLightLook
    {
        public sealed class Set { public Material Column, Core, Pool, Halo, Mote; }

        struct Palette
        {
            public Color Emission, Body; public float CoreWhite;
            public Palette(Color e, Color b, float w) { Emission = e; Body = b; CoreWhite = w; }
        }

        static readonly Dictionary<PortalLightRegion, Set> Sets = new Dictionary<PortalLightRegion, Set>();
        static Shader particle;
        static Texture2D veil, mote;
        static bool failed;

        /// <summary>False when a shader or texture is missing: MapScenery falls back to the old haze / beam.</summary>
        public static bool Ready
        {
            get
            {
                if (failed) return false;
                if (particle != null) return true;
                if (!LightSkillAssets.Ready) { failed = true; return false; }
                particle = Resources.Load<Shader>("Shaders/LightParticle");
                veil = Resources.Load<Texture2D>("Sprites/fx/light-skill/portal-veil");
                mote = Resources.Load<Texture2D>("Sprites/fx/light-skill/portal-mote");
                if (particle == null || !particle.isSupported || veil == null || mote == null)
                {
                    Debug.LogWarning("[ZenoniaProto] portal light shader / textures missing, map exits use the old haze");
                    failed = true; particle = null; return false;
                }
                veil.wrapMode = TextureWrapMode.Repeat;
                mote.wrapMode = TextureWrapMode.Clamp;
                return true;
            }
        }

        static Palette Of(PortalLightRegion r)
        {
            switch (r)
            {
                case PortalLightRegion.Snow:   return new Palette(new Color(0.12f, 0.5f, 1.3f), new Color(0.1f, 0.42f, 0.95f, 0.5f), 0.45f);  // ice blue, deep blue body for snow
                case PortalLightRegion.Forest: return new Palette(new Color(0.3f, 1.15f, 0.55f), new Color(0.15f, 0.7f, 0.35f, 0.45f), 0.4f); // leaf green
                case PortalLightRegion.Desert: return new Palette(new Color(0.08f, 0.6f, 0.7f), new Color(0.03f, 0.52f, 0.62f, 0.55f), 0.3f);   // oasis turquoise, reads on sand; low add (pale ground)
                case PortalLightRegion.Marsh:  return new Palette(new Color(0.85f, 0.4f, 1.25f), new Color(0.55f, 0.25f, 0.9f, 0.45f), 0.4f);  // will-o'-wisp violet
                // warm lantern gold (towns, meadows). The add stays low: gold added onto pale dirt roads clipped to a white glare over the whole road (first capture, 19/11/2026);
                // the saturated amber body carries the shape instead.
                default:                       return new Palette(new Color(0.55f, 0.33f, 0.08f), new Color(0.95f, 0.52f, 0.1f, 0.55f), 0.25f);
            }
        }

        public static Set For(PortalLightRegion region)
        {
            Set s;
            if (Sets.TryGetValue(region, out s)) return s;
            Palette p = Of(region);
            s = new Set();

            // Column: broad soft bands drifting slowly up, gently warped by the noise; no dissolve holes (they read as spots jumping around).
            s.Column = new Material(LightSkillAssets.Beam) { name = "Portal_Column_" + region };
            s.Column.SetTexture("_MainTex", veil);
            s.Column.SetVector("_MainTiling", new Vector4(1f, 0.5f, 0f, 0f));
            s.Column.SetFloat("_ScrollSpeed", 0.22f);
            s.Column.SetFloat("_StreakMin", 0.45f);
            s.Column.SetFloat("_NoiseScroll", 0.1f);
            s.Column.SetFloat("_Distort", 0.07f);
            s.Column.SetFloat("_EdgeSoft", 0.95f);
            s.Column.SetFloat("_BottomFade", 0.08f);
            s.Column.SetFloat("_TopFade", 0.75f);
            s.Column.SetFloat("_Dissolve", 0f);
            s.Column.SetColor("_EmissionColor", p.Emission * 0.55f);
            s.Column.SetFloat("_CoreWhite", 0.15f);
            s.Column.SetColor("_BodyColor", new Color(p.Body.r, p.Body.g, p.Body.b, p.Body.a * 0.8f));

            // Core: narrow brighter column in the middle, the only part that blooms.
            s.Core = new Material(s.Column) { name = "Portal_Core_" + region };
            s.Core.SetFloat("_ScrollSpeed", 0.3f);
            s.Core.SetFloat("_EdgeSoft", 1f);
            s.Core.SetFloat("_TopFade", 0.85f);
            s.Core.SetColor("_EmissionColor", p.Emission);
            s.Core.SetFloat("_CoreWhite", p.CoreWhite);
            s.Core.SetColor("_BodyColor", new Color(Mathf.Lerp(p.Body.r, 1f, 0.35f), Mathf.Lerp(p.Body.g, 1f, 0.35f), Mathf.Lerp(p.Body.b, 1f, 0.35f), p.Body.a));

            // Ground: a soft pool of light under the column and a wider faint halo (no hard rim: a rimmed disc read as a "flying saucer", Sổ lỗi 2026-11-13).
            s.Pool = new Material(LightSkillAssets.Glow) { name = "Portal_Pool_" + region };
            s.Pool.SetTexture("_MainTex", Texture2D.whiteTexture);
            s.Pool.SetFloat("_Falloff", 2.6f);
            s.Pool.SetColor("_EmissionColor", p.Emission * 0.5f);
            s.Pool.SetFloat("_CoreWhite", 0.2f);
            s.Pool.SetColor("_BodyColor", new Color(p.Body.r, p.Body.g, p.Body.b, p.Body.a * 0.9f));
            s.Halo = new Material(s.Pool) { name = "Portal_Halo_" + region };
            s.Halo.SetFloat("_Falloff", 3.4f);
            s.Halo.SetColor("_EmissionColor", p.Emission * 0.22f);
            s.Halo.SetFloat("_CoreWhite", 0f);
            s.Halo.SetColor("_BodyColor", new Color(p.Body.r, p.Body.g, p.Body.b, p.Body.a * 0.45f));

            s.Mote = new Material(particle) { name = "Portal_Mote_" + region };
            s.Mote.SetTexture("_MainTex", mote);
            s.Mote.SetColor("_EmissionColor", p.Emission * 0.9f);
            s.Mote.SetFloat("_CoreWhite", 0.6f);
            s.Mote.SetColor("_BodyColor", new Color(p.Body.r, p.Body.g, p.Body.b, Mathf.Min(1f, p.Body.a * 1.5f)));
            Sets[region] = s;
            return s;
        }
    }
}
