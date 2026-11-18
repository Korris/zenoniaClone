using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Layered hero path (docs/character-appearance-layered-spec.md, step B): body + mask recoloured by HeroRecolor.shader, hair, headgear,
    /// weapon / offhand on per-frame grips, fist (HeroLayeredView), 8 view dirs like the RO Soft path. No class has real layered art yet:
    /// `--layered-test` draws every non-local hero with the synthetic set (Tools/build-hero-recolor-mask.py test-sheets); the local hero and
    /// Warrior's RO Soft sheets stay as they are. Appearance (CharacterAppearanceData) picks the palette colours; heroes other than the local one
    /// get a small crowd variation from their seed: walk phase and breath offset, figure scale 0.96-1.04.
    /// </summary>
    public partial class HeroRenderer
    {
        public const string LayeredTestSet = "Data/hero-layered/test/class";
        const float CrowdScaleMin = 0.96f, CrowdScaleRange = 0.08f;

        static bool? layeredTest;
        /// <summary>`--layered-test` on the command line (or set by a capture before heroes spawn).</summary>
        public static bool LayeredTestEnabled
        {
            get { if (layeredTest == null) layeredTest = Array.IndexOf(Environment.GetCommandLineArgs(), "--layered-test") >= 0; return layeredTest.Value; }
            set { layeredTest = value; }
        }

        HeroLayeredView layered;
        float crowdScale = 1f, crowdPhase;

        /// <summary>True when this hero draws a layered set.</summary>
        public bool IsLayered { get { return layered != null; } }

        /// <summary>The look in use, colours resolved (never null after SetAppearance).</summary>
        public CharacterAppearanceData Appearance { get; private set; }

        /// <summary>Draws this hero from a layered set from now on; false (nothing changes) when the set is missing.</summary>
        public bool UseLayered(string classJson)
        {
            HeroLayeredSet set = HeroLayeredSet.Load(classJson);
            if (set == null || lean == null) return false;
            if (layered == null) layered = new HeroLayeredView(set, lean);
            ApplyLookToLayers();
            return true;
        }

        /// <summary>
        /// The character's look: palette colours (missing ones picked from `seed`, CharacterAppearanceData.SeedFor), hairstyle, headgear.
        /// classicHair: also dye the paper doll's layered hair with a CHOSEN palette colour (other players; the local hero keeps its device dye).
        /// local: the player's own hero, never varied.
        /// </summary>
        public void SetAppearance(CharacterAppearanceData raw, string seed, bool local, bool classicHair)
        {
            if (raw == null) raw = CharacterAppearanceData.Unset(cls != null ? cls.Name : null);
            Appearance = raw.Resolved(seed);
            if (classicHair && raw.HasHairColor) HairColor = HeroPalettes.Hair(raw.hairColorIndex);
            crowdScale = local ? 1f : CrowdScaleMin + CrowdScaleRange * CharacterAppearanceData.Unit(seed, 1);
            crowdPhase = local ? 0f : CharacterAppearanceData.Unit(seed, 2);
            if (!local) breathPhase = crowdPhase * BreathPeriod;
            ApplyLookToLayers();
        }

        void ApplyLookToLayers()
        {
            if (layered == null || Appearance == null) return;
            layered.SetLook(HeroPalettes.Hair(Appearance.hairColorIndex), HeroPalettes.Outfit(Appearance.outfitColorIndex), Appearance.hairStyle, Appearance.equippedHeadgearId);
        }

        /// <summary>Figure scale with the crowd variation (layered heroes only: the other paths keep their exact size).</summary>
        float FigureK { get { return figureScale * (layered != null ? crowdScale : 1f); } }

        int LayeredFrameCount(string anim)
        {
            if (anim == null) return 0;
            if (anim == "death") return DeathFadeSteps;
            return layered.Set.Frames(layered.Set.Resolve(anim));
        }

        void ShowLayered(string anim, int dir, int frame, int orderBase, float alpha)
        {
            body.sprite = null;
            handOver.sprite = null;
            if (headLayer != null) headLayer.Hide();
            foreach (Part p in parts) { p.Sr.sprite = null; if (p.Fx != null) p.Fx.Hide(); }
            motion = anim == "idle" ? 0 : anim == "walk" ? 1 : 2;
            string painted = layered.Set.Resolve(anim);
            int n = layered.Set.Frames(painted);
            float a = alpha;
            if (anim == "death") { a *= 1f - Mathf.Clamp01((frame + 1f) / DeathFadeSteps); frame = 0; }
            else if (painted == "walk" && n > 1) frame = (frame + Mathf.FloorToInt(crowdPhase * n)) % n; // crowd: not every walker steps in time
            Color c = anim == "hurt" ? HurtTint : Color.white;
            shadow.sortingOrder = orderBase;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * a);
            float sy = painted == "idle" && anim != "death" ? 1f + BreathAmp * Mathf.Sin((Time.time + breathPhase) * (2f * Mathf.PI / BreathPeriod)) : 1f;
            layered.Show(painted, SoftRow(dir), frame, orderBase, new Color(c.r, c.g, c.b, a), sy);
        }
    }
}
