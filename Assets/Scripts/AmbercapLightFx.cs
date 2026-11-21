using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Skill FX of the Ambercap Warden (Rootburrow's dungeon boss, 21/11/2026), "RO3 Soft Fantasy V1" (docs/sprite-rules.md lines 9-22, ChatGPT spec
    /// docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md section E): shaders, meshes and particles only, no sprite
    /// frames. Built on the Cleric light toolkit: ClericLightFx (timeline of LightBlob quads, particles, reveal / pop / turn shaping), ClericLightLook.M
    /// (mask x palette x role materials, masks in Resources/Sprites/fx/light-skill) with the boss's own four palettes (at most 2 main colours + 1 core each).
    ///  - warning (the telegraph): WantedWarningDisc shader, the exact skill circle / cone, amber fill #E9A96E with a #FFE0A0 rim, filling to the rim at the
    ///    strike instant; the enrage circle is a 270 deg amber sector + a 90 deg teal (#5DE0C5) safe sector; the line is a LightBlob strip with two rim strips;
    ///  - the cast pieces (rune rings drawing in, glyphs lighting up) and the impact (shockwave ring, petal streaks, root spikes, flash core, sparks) are one
    ///    ClericLightFx timeline started when the telegraph arrives, its strike at the server's locked instant (AtMs - ServerNowMs at spawn).
    /// Every ground circle is round (squash 1): the 2.5D camera foreshortens the ground itself. Telegraph / FX of a boss always draw at full strength
    /// (RO3 rule 12: boss warnings come before player FX). ClearAll on a map change (WantedSkillTelegraphFx.ClearAll).
    /// </summary>
    public sealed class AmbercapLightFx : MonoBehaviour
    {
        public const string Boss = "ambercapWarden";
        static readonly ClericLightLook.Pal Amber = new ClericLightLook.Pal("WardenAmber", 0xF3BA70, 0xFFE0A0, 0xFFF0C8, 0xA8673E, new Color(0.17f, 0.10f, 0.03f));
        static readonly ClericLightLook.Pal Spore = new ClericLightLook.Pal("WardenSpore", 0xB7D995, 0xE6C781, 0xFFF2C9, 0x5F8549, new Color(0.08f, 0.13f, 0.04f));
        static readonly ClericLightLook.Pal Root = new ClericLightLook.Pal("WardenRoot", 0xC99A70, 0x77DAD2, 0xFFF0CF, 0x6E4B32, new Color(0.05f, 0.13f, 0.12f));
        static readonly ClericLightLook.Pal Bloom = new ClericLightLook.Pal("WardenBloom", 0x8CDDD6, 0xF4BC7C, 0xFFF5D5, 0x3E8C86, new Color(0.06f, 0.15f, 0.14f));
        static readonly Color Fill = new Color(0.914f, 0.663f, 0.431f, 0.24f), Rim = new Color(1f, 0.878f, 0.627f, 0.8f), SafeTeal = new Color(0.365f, 0.878f, 0.773f, 0.3f);
        static readonly List<AmbercapLightFx> Live = new List<AmbercapLightFx>(8);
        static Material warnMat, stripMat;

        WantedSkillDef skill;
        Vector2 origin;
        float dirDeg;
        uint atMs;
        readonly List<MeshRenderer> warns = new List<MeshRenderer>(3);
        MaterialPropertyBlock block;

        public static bool ActiveFor(string bossId) { return bossId == Boss && NetMode.RendersFx && FeelConfig.FxOn && ClericLightLook.Drawable && Ready(); }

        static bool Ready()
        {
            if (warnMat != null) return true;
            Shader warn = Resources.Load<Shader>("Shaders/WantedWarningDisc");
            Texture2D noise = Resources.Load<Texture2D>("Sprites/fx/light-skill/noise-clouds");
            if (warn == null || !warn.isSupported || noise == null || !LightSkillAssets.Ready) return false;
            noise.wrapMode = TextureWrapMode.Repeat;
            warnMat = new Material(warn) { name = "Warden_Warning", hideFlags = HideFlags.DontSave };
            warnMat.SetTexture("_NoiseTex", noise);
            warnMat.SetColor("_FleckColor", new Color(1f, 0.82f, 0.55f, 1f));
            warnMat.SetFloat("_Base", 0.14f);
            stripMat = new Material(LightSkillAssets.Glow) { name = "Warden_LineStrip", hideFlags = HideFlags.DontSave };
            stripMat.SetFloat("_Falloff", 0f);
            stripMat.SetFloat("_CoreWhite", 0f);
            stripMat.SetColor("_EmissionColor", new Color(0.05f, 0.03f, 0.01f, 1f));
            return true;
        }

        public static void Spawn(WantedSkillDef skill, Vector2 origin, float dirDeg, uint atMs)
        {
            if (Zone.Main == null) return;
            var go = new GameObject("AmbercapLightFx_" + skill.Id);
            go.transform.SetParent(Zone.Main.Root, false);
            go.transform.position = new Vector3(origin.x, origin.y, 0f);
            var fx = go.AddComponent<AmbercapLightFx>();
            fx.skill = skill; fx.origin = origin; fx.dirDeg = dirDeg; fx.atMs = atMs; fx.block = new MaterialPropertyBlock();
            fx.BuildWarning();
            float strike = Mathf.Max(200f, (float)(atMs - WantedSkillTelegraphFx.ClockMs)); // local ms to the server's strike instant
            AmbercapLightCasts.Play(skill, origin, dirDeg, strike);
            Live.Add(fx);
        }

        public static void ClearAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--) if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }

        void BuildWarning()
        {
            float u = WantedSkillArea.TileUnits;
            switch (skill.Shape)
            {
                case WantedAreaShape.Line:
                    float len = skill.LengthTiles * u, wid = skill.WidthTiles * u;
                    Strip(len, wid, 0f, Fill);
                    Strip(len, 1.2f, wid * 0.5f, Rim); Strip(len, 1.2f, -wid * 0.5f, Rim);
                    break;
                case WantedAreaShape.Cone:
                    Disc(skill.RadiusTiles * u, skill.AngleDeg * 0.5f, dirDeg, Fill, Rim);
                    break;
                default:
                    float r = skill.RadiusTiles * u;
                    if (skill.SafeArcCount > 0) // the enrage bloom: danger everywhere but the lane, the lane in teal
                    {
                        Disc(r, 180f - skill.SafeArcDeg * 0.5f, dirDeg + 180f, Fill, Rim);
                        Disc(r, skill.SafeArcDeg * 0.5f, dirDeg, SafeTeal, new Color(0.62f, 1f, 0.9f, 0.9f));
                    }
                    else Disc(r, 180f, 0f, Fill, Rim);
                    break;
            }
        }

        void Disc(float radius, float halfAngle, float rotDeg, Color fill, Color rim)
        {
            MeshRenderer m = LightSkillAssets.Quad(transform, "Warning", warnMat, false, UltimateFx.GroundFxOrder + 1);
            m.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            m.transform.localRotation = Quaternion.Euler(0f, 0f, rotDeg);
            m.GetPropertyBlock(block);
            block.SetFloat("_HalfAngle", halfAngle);
            block.SetFloat("_RimWidth", Mathf.Clamp(1.2f / radius, 0.004f, 0.2f));
            block.SetColor("_FillColor", fill);
            block.SetColor("_RimColor", rim);
            block.SetColor("_RimGlow", new Color(rim.r, rim.g, rim.b, 1f) * 0.12f);
            m.SetPropertyBlock(block);
            warns.Add(m);
        }

        /// <summary>A strip along the line from the boss (local +X after the rotation), `side` units off its axis.</summary>
        void Strip(float len, float wid, float side, Color c)
        {
            MeshRenderer m = LightSkillAssets.Quad(transform, "WarningStrip", stripMat, false, UltimateFx.GroundFxOrder + 1);
            float a = dirDeg * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), n = new Vector2(-d.y, d.x);
            Vector2 mid = d * len * 0.5f + n * side;
            m.transform.localPosition = new Vector3(mid.x, mid.y, 0f);
            m.transform.localRotation = Quaternion.Euler(0f, 0f, dirDeg);
            m.transform.localScale = new Vector3(len, wid, 1f);
            m.GetPropertyBlock(block);
            block.SetColor("_BodyColor", c);
            m.SetPropertyBlock(block);
            warns.Add(m);
        }

        void Update()
        {
            float t = (float)(WantedSkillTelegraphFx.ClockMs - atMs); // ms relative to the strike
            float fade = t <= 0f ? 1f : 1f - Mathf.Clamp01(t / 220f);
            float progress = Mathf.Clamp01(1f + t / Mathf.Max(1f, skill.TelegraphMs));
            for (int i = 0; i < warns.Count; i++)
            {
                MeshRenderer m = warns[i];
                if (m == null) continue;
                m.GetPropertyBlock(block);
                block.SetFloat("_Progress", progress);
                block.SetFloat("_Fade", fade * Mathf.Lerp(0.75f, 1f, progress));
                m.SetPropertyBlock(block);
                m.enabled = fade > 0f;
            }
            if (t > 600f) { Live.Remove(this); Destroy(gameObject); }
        }

        void OnDestroy() { Live.Remove(this); }

        internal static ClericLightLook.Pal PalOf(int skillId) { return skillId == 0 ? Amber : skillId == 1 ? Spore : skillId == 2 ? Root : Bloom; }
    }
}
