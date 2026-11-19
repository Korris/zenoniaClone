using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Materials of the Mire Hydra Queen skill light FX (MireHydraLightFx, 2026-11-19), built once from the Ice Storm / portal shaders
    /// (LightSkillAssets: LightBeamScroll, LightBlob; LightParticle) plus WantedWarningDisc for the ground warning. Boss colours: toxic purple +
    /// moss green. The arena (Sunken Reliquary, RO Soft Fantasy) is BRIGHT stone, so every material carries its shape with a saturated
    /// alpha-blended body and keeps the additive HDR part low (Sổ lỗi 2026-11-19: warm / bright additive on a pale road burns to a white sheet).
    /// Textures are the grey masks of Resources/Sprites/fx/light-skill (procedural stand-ins, Tools/build-light-skill-textures.py).
    /// </summary>
    public static class MireHydraFxLook
    {
        public static readonly Color Purple = new Color(0.56f, 0.2f, 0.84f), Moss = new Color(0.44f, 0.76f, 0.14f);
        static readonly Color PurpleGlow = new Color(0.3f, 0.06f, 0.5f), MossGlow = new Color(0.16f, 0.42f, 0.04f);

        public static Material Warning, StreamOuter, StreamCore, Column, ColumnCore, Burst, GroundGlow, Ring, Drop, Bubble;
        static bool failed, built;

        /// <summary>False when a shader or texture is missing: the boss falls back to the painted sprite FX (WantedSkillVfx + warning sprites).</summary>
        public static bool Ready { get { Build(); return !failed; } }

        static void Build()
        {
            if (built) return;
            built = true;
            Shader warn = Resources.Load<Shader>("Shaders/WantedWarningDisc"), particle = Resources.Load<Shader>("Shaders/LightParticle");
            Texture2D noise = Resources.Load<Texture2D>("Sprites/fx/light-skill/noise-clouds"), veil = Resources.Load<Texture2D>("Sprites/fx/light-skill/portal-veil"),
                bubble = Resources.Load<Texture2D>("Sprites/fx/light-skill/venom-bubble"), core = Resources.Load<Texture2D>("Sprites/fx/light-skill/meteor-core");
            if (!LightSkillAssets.Ready || warn == null || !warn.isSupported || particle == null || !particle.isSupported || noise == null || veil == null || bubble == null || core == null)
            {
                Debug.LogWarning("[ZenoniaProto] Mire Hydra light FX: shader / texture missing, the boss uses the painted FX");
                failed = true;
                return;
            }
            noise.wrapMode = TextureWrapMode.Repeat; veil.wrapMode = TextureWrapMode.Repeat;

            Warning = new Material(warn) { name = "Mire_Warning" };
            Warning.SetTexture("_NoiseTex", noise);
            // Rim a toxic red-magenta: still reads as the "red circle" every boss hint names (WantedSkillTable.Hint), fill in the boss's purple + moss.
            Warning.SetColor("_RimColor", new Color(0.95f, 0.22f, 0.5f, 0.95f));
            Warning.SetColor("_RimGlow", new Color(0.3f, 0.05f, 0.15f, 1f));
            Warning.SetColor("_FillColor", new Color(Purple.r, Purple.g, Purple.b, 0.45f));
            Warning.SetColor("_FleckColor", Moss);

            // Venom breath streams lying on the ground: a wide purple sheet with a narrower moss-green core, rays scrolling away from the mouths.
            StreamOuter = new Material(LightSkillAssets.Beam) { name = "Mire_StreamOuter" };
            StreamOuter.SetTexture("_MainTex", veil);
            StreamOuter.SetVector("_MainTiling", new Vector4(1f, 0.7f, 0f, 0f));
            StreamOuter.SetFloat("_ScrollSpeed", 1.4f);
            StreamOuter.SetFloat("_StreakMin", 0.4f);
            StreamOuter.SetFloat("_Distort", 0.18f);
            StreamOuter.SetFloat("_EdgeSoft", 0.85f);
            StreamOuter.SetFloat("_BottomFade", 0.12f);
            StreamOuter.SetFloat("_TopFade", 0.35f);
            StreamOuter.SetColor("_EmissionColor", PurpleGlow * 0.8f);
            StreamOuter.SetFloat("_CoreWhite", 0.05f);
            StreamOuter.SetColor("_BodyColor", new Color(Purple.r, Purple.g, Purple.b, 0.55f)); // a hero standing in the breath must still read
            StreamOuter.SetColor("_EdgeColor", PurpleGlow);
            StreamCore = new Material(StreamOuter) { name = "Mire_StreamCore" };
            StreamCore.SetFloat("_ScrollSpeed", 2.0f);
            StreamCore.SetColor("_EmissionColor", MossGlow * 1.2f);
            StreamCore.SetFloat("_CoreWhite", 0.12f);
            StreamCore.SetColor("_BodyColor", new Color(Moss.r, Moss.g, Moss.b, 0.7f));

            // Poison geyser (Poison Bloom, Toxic Reign splash): an upright column, same two layers.
            // First capture (19/11): the geyser read as a faint haze on the pale stone -> denser body, higher streak floor.
            Column = new Material(StreamOuter) { name = "Mire_Column" };
            Column.SetFloat("_ScrollSpeed", 1.1f);
            Column.SetFloat("_StreakMin", 0.65f);
            Column.SetFloat("_EdgeSoft", 0.6f);
            Column.SetFloat("_TopFade", 0.5f);
            Column.SetFloat("_BottomFade", 0.06f);
            Column.SetColor("_BodyColor", new Color(Purple.r, Purple.g, Purple.b, 0.72f));
            ColumnCore = new Material(StreamCore) { name = "Mire_ColumnCore" };
            ColumnCore.SetFloat("_StreakMin", 0.65f);
            ColumnCore.SetFloat("_TopFade", 0.6f);
            ColumnCore.SetColor("_BodyColor", new Color(Moss.r, Moss.g, Moss.b, 0.6f)); // 2nd capture: 0.9 hid the hero standing in it

            // Tail Crush: radial ray burst (polar beam) running out with the shockwave.
            Burst = new Material(LightSkillAssets.Burst) { name = "Mire_Burst" };
            Burst.SetColor("_EmissionColor", PurpleGlow);
            Burst.SetFloat("_CoreWhite", 0.05f);
            Burst.SetColor("_BodyColor", new Color(Purple.r, Purple.g, Purple.b, 0.6f));
            Burst.SetColor("_EdgeColor", MossGlow * 2f);
            Burst.SetFloat("_RingWidth", 0.22f);

            GroundGlow = new Material(LightSkillAssets.Glow) { name = "Mire_GroundGlow" };
            GroundGlow.SetTexture("_MainTex", Texture2D.whiteTexture);
            GroundGlow.SetFloat("_Falloff", 2.4f);
            GroundGlow.SetColor("_EmissionColor", PurpleGlow * 0.6f);
            GroundGlow.SetFloat("_CoreWhite", 0f);
            GroundGlow.SetColor("_BodyColor", new Color(Purple.r * 0.9f, Purple.g, Purple.b, 0.42f));

            Ring = new Material(LightSkillAssets.Ring) { name = "Mire_Ring" };
            Ring.SetColor("_EmissionColor", MossGlow * 1.4f);
            Ring.SetFloat("_CoreWhite", 0.1f);
            Ring.SetColor("_BodyColor", new Color(Moss.r, Moss.g, Moss.b, 0.85f));

            Drop = new Material(LightSkillAssets.Core) { name = "Mire_Drop" };
            Drop.SetTexture("_MainTex", core);
            Drop.SetColor("_EmissionColor", MossGlow * 2f);
            Drop.SetFloat("_CoreWhite", 0.35f);
            Drop.SetColor("_BodyColor", new Color(Moss.r * 0.8f, Moss.g * 0.9f, Moss.b, 0.95f));

            Bubble = new Material(particle) { name = "Mire_Bubble" };
            Bubble.SetTexture("_MainTex", bubble);
            Bubble.SetColor("_EmissionColor", new Color(0.25f, 0.25f, 0.25f));  // vertex colour (purple / moss per particle) tints it
            Bubble.SetFloat("_CoreWhite", 0.15f);
            Bubble.SetColor("_BodyColor", new Color(1f, 1f, 1f, 0.9f));
        }
    }
}
