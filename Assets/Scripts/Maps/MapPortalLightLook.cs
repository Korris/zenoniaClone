using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Materials of the portal light (MapPortalLightFx), one shared blue set, built from the light skill shaders (LightSkillAssets: LightBeamScroll,
    /// LightBlob) plus LightParticle for the motes. Every material has an alpha-blended saturated body (reads on snow and pale roads) under an HDR additive
    /// part kept low so only the thin core of the column passes the BloomLite threshold (docs/sprite-rules.md Sổ lỗi 2026-11-19, light shader on snow).
    /// </summary>
    public static class MapPortalLightLook
    {
        public sealed class Set { public Material Column, Core, Pool, Halo, Mote; }

        static Set shared;
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

        // One icy / sky blue on every map (user 19/11/2026). Additive part mostly blue (R low) so on white snow and pale dirt roads it tints instead of
        // clipping to white; the saturated deep-blue body carries the shape on bright ground (Sổ lỗi 2026-11-19, light shader on snow / gold on dirt).
        static readonly Color Emission = new Color(0.1f, 0.48f, 1.25f), Body = new Color(0.08f, 0.4f, 0.95f, 0.55f);
        const float CoreWhite = 0.4f;

        /// <summary>The materials every portal shares (built on first use; check Ready first).</summary>
        public static Set Shared
        {
            get
            {
                if (shared != null) return shared;
                var s = new Set();
                Color e = Emission, b = Body;
                // Column: broad soft bands drifting slowly up, gently warped by the noise; no dissolve holes (they read as spots jumping around).
                s.Column = new Material(LightSkillAssets.Beam) { name = "Portal_Column" };
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
                s.Column.SetColor("_EmissionColor", e * 0.55f);
                s.Column.SetFloat("_CoreWhite", 0.15f);
                s.Column.SetColor("_BodyColor", new Color(b.r, b.g, b.b, b.a * 0.8f));

                // Core: narrow brighter column in the middle, the only part that blooms.
                s.Core = new Material(s.Column) { name = "Portal_Core" };
                s.Core.SetFloat("_ScrollSpeed", 0.3f);
                s.Core.SetFloat("_EdgeSoft", 1f);
                s.Core.SetFloat("_TopFade", 0.85f);
                s.Core.SetColor("_EmissionColor", e);
                s.Core.SetFloat("_CoreWhite", CoreWhite);
                s.Core.SetColor("_BodyColor", new Color(Mathf.Lerp(b.r, 1f, 0.35f), Mathf.Lerp(b.g, 1f, 0.35f), Mathf.Lerp(b.b, 1f, 0.35f), b.a));

                // Ground: a soft pool of light under the column and a wider faint halo (no hard rim: a rimmed disc read as a "flying saucer", Sổ lỗi 2026-11-13).
                s.Pool = new Material(LightSkillAssets.Glow) { name = "Portal_Pool" };
                s.Pool.SetTexture("_MainTex", Texture2D.whiteTexture);
                s.Pool.SetFloat("_Falloff", 2.6f);
                s.Pool.SetColor("_EmissionColor", e * 0.5f);
                s.Pool.SetFloat("_CoreWhite", 0.2f);
                s.Pool.SetColor("_BodyColor", new Color(b.r, b.g, b.b, b.a * 0.9f));
                s.Halo = new Material(s.Pool) { name = "Portal_Halo" };
                s.Halo.SetFloat("_Falloff", 3.4f);
                s.Halo.SetColor("_EmissionColor", e * 0.22f);
                s.Halo.SetFloat("_CoreWhite", 0f);
                s.Halo.SetColor("_BodyColor", new Color(b.r, b.g, b.b, b.a * 0.45f));

                s.Mote = new Material(particle) { name = "Portal_Mote" };
                s.Mote.SetTexture("_MainTex", mote);
                s.Mote.SetColor("_EmissionColor", e * 0.9f);
                s.Mote.SetFloat("_CoreWhite", 0.6f);
                s.Mote.SetColor("_BodyColor", new Color(b.r, b.g, b.b, Mathf.Min(1f, b.a * 1.5f)));
                return shared = s;
            }
        }
    }
}
