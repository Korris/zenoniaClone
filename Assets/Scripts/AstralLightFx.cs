using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Shader-driven picture of the wizard ultimate Astral Cataclysm, in the Ragnarok Online 3 style "light pillar / meteor shower" (replaces the painted purple choreography
    /// when Enabled). Only the art changes: damage stays with the Rain effect in SkillRun (shared with the server), and this runs only where NetMode.RendersFx.
    ///  - a scrolling, noise-warped ice-blue light pillar stands on the rain centre from the cast until the last drop, then dissolves (LightBeamScroll);
    ///  - one meteor per rain drop (LightMeteor: glowing head + white -> ice-blue TrailRenderer) lands exactly on that drop's damage step (LightSkillTiming.LandMs);
    ///  - each landing explodes (LightImpact: ground light 0 -> peak in 0.15 s -> 0, ring, ray burst, shaft).
    /// Drop positions use the same SkillScatter.InDisc + seed as SkillEffects.Rain, so the explosions sit on the damage circles.
    /// Toggle: Enabled (default on); command line `--light-fx off` brings back the old choreography for comparison.
    /// </summary>
    public sealed class AstralLightFx : MonoBehaviour
    {
        public const string SkillId = "wizard.astral-cataclysm";
        const float SkyRise = 120f, SkySide = 80f, PillarWidthTiles = 1.2f, PillarHeight = 110f;

        /// <summary>New light FX on (default) or the old painted choreography (`--light-fx off`, or set false at runtime).</summary>
        public static bool Enabled = ReadFlag();

        SkillEffect rain;
        Vector2[] spots;
        LightMeteor[] meteors;
        LightImpact[] impacts;
        bool[] meteorSpawned, impactSpawned;
        MeshRenderer pillar;
        float ageMs, pillarFullW;
        int endMs, lastLandMs;

        public static bool Handles(string skillId) { return skillId == SkillId && Enabled && NetMode.RendersFx && LightSkillAssets.Ready; }

        public static void Preload() { if (NetMode.RendersFx && Enabled) LightSkillAssets.Load(); }

        static bool ReadFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--light-fx");
            return !(i >= 0 && i + 1 < a.Length && a[i + 1].Equals("off", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Starts the picture of a resolved cast (call where SkillChoreoCastHook.Play would run).</summary>
        public static void Cast(SkillContext ctx)
        {
            if (!ctx.View || !NetMode.RendersFx) return;
            SkillEffect e = null;
            foreach (SkillEffect x in ctx.Def.Effects) if (x.Kind == EffectKind.Rain) { e = x; break; }
            if (e == null) return;
            var go = new GameObject("AstralLightFx");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            go.AddComponent<AstralLightFx>().Begin(ctx, e);
        }

        void Begin(SkillContext ctx, SkillEffect e)
        {
            rain = e;
            float u = ctx.Units;
            int n = Mathf.Max(1, e.Count);
            Vector2 mid = SkillAim.ClampToWalls(ctx.CastFrom, ctx.CastFrom + ctx.Dir * e.Range * u); // same centre and scatter as SkillEffects.Rain
            spots = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float dx, dy;
                SkillScatter.InDisc(ctx.Seed, i, e.Radius * u, out dx, out dy);
                spots[i] = mid + new Vector2(dx, dy);
            }
            meteors = new LightMeteor[n];
            impacts = new LightImpact[n];
            meteorSpawned = new bool[n];
            impactSpawned = new bool[n];
            lastLandMs = LightSkillTiming.LandMs(e, n - 1);
            endMs = LightSkillTiming.EndMs(e);
            pillarFullW = PillarWidthTiles * u;
            pillar = LightSkillAssets.Quad(transform, "LightPillar", LightSkillAssets.Beam, true, FeelConfig.FxOrder - 20);
            pillar.transform.position = new Vector3(mid.x, mid.y - 2f, 0f);
            Tick(0f, ctx.Units);
        }

        void Update() { ageMs += GameClock.Dt * 1000f; Tick(ageMs, GameConfig.TileUnits); }

        void Tick(float ms, float u)
        {
            TickPillar(ms);
            for (int i = 0; i < spots.Length; i++)
            {
                if (!meteorSpawned[i] && ms >= LightSkillTiming.MeteorStartMs(rain, i))
                {
                    meteorSpawned[i] = true;
                    Vector2 sky = spots[i] + new Vector2(SkySide * (i % 2 == 0 ? 1f : 0.8f), SkyRise);
                    meteors[i] = new LightMeteor(transform, sky, spots[i], LightSkillTiming.MeteorProgress(rain, i, ms));
                }
                if (meteors[i] != null)
                {
                    meteors[i].Tick(LightSkillTiming.MeteorProgress(rain, i, ms), ms / 1000f);
                    if (meteors[i].Done(ms / 1000f)) { meteors[i].Destroy(); meteors[i] = null; }
                }
                float since = ms - LightSkillTiming.LandMs(rain, i);
                if (!impactSpawned[i] && since >= 0f) { impactSpawned[i] = true; impacts[i] = new LightImpact(transform, spots[i], rain.ImpactRadius * u); }
                if (impacts[i] != null && !impacts[i].Tick(since)) { impacts[i].Destroy(); impacts[i] = null; }
            }
            if (ms >= endMs + 300f) Destroy(gameObject); // the last trail has faded too
        }

        /// <summary>Pillar: widens in 0.15 s from the cast, holds while meteors fall, dissolves over PillarTailMs after the last drop.</summary>
        void TickPillar(float ms)
        {
            if (pillar == null) return;
            float open = Mathf.Clamp01(ms / LightSkillTiming.GroundPeakMs), w = pillarFullW * (0.25f + 0.75f * (1f - (1f - open) * (1f - open)));
            float tail = Mathf.Clamp01((ms - lastLandMs) / LightSkillTiming.PillarTailMs);
            pillar.transform.localScale = new Vector3(w * (1f - 0.4f * tail), PillarHeight, 1f);
            LightSkillAssets.Set(pillar, 1f - tail * tail, tail);
            if (tail >= 1f) { Destroy(pillar.gameObject); pillar = null; }
        }

        void OnDestroy()
        {
            if (meteors != null) foreach (LightMeteor m in meteors) if (m != null) m.Destroy();
        }
    }
}
