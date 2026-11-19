using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Map exit marker drawn with shaders, a particle system and bloom (the RO3-style light of IceStormLightFx, user 19/11/2026):
    ///  - a soft pool of light and a wider faint halo on the ground (LightBlob, under heroes and monsters);
    ///  - a light column standing on the pool (LightBeamScroll: broad bands scrolling slowly up, noise warp) with a narrower bright core that blooms;
    ///  - motes rising and swaying through it (ParticleSystem built in code, at most MaxMotes alive, prewarmed so nothing pops in on map load).
    /// Everything moves continuously and the whole light breathes slowly (MapPortalExits.Breath, 3.6 s): nothing blinks and no spot jumps.
    /// One icy blue on every map (user 19/11/2026: "to ra, màu xanh"; MapPortalLightLook). Placement is MapPortalExits: portal tiles and triggers are untouched.
    /// `--portal-fx off` (or Enabled = false before the map is built) brings back the old haze / beam (MapPortalHaze). World units: 16 per tile.
    /// </summary>
    public static class MapPortalLightFx
    {
        public const int MaxMotes = 18;          // per exit; a map has at most 5 exits -> under 100 particles on screen, fine on phones
        const float ColumnH = 92f;               // nearly 6 tiles, the hero is about 20 units tall (user 19/11: "make the pillar bigger")

        /// <summary>New light on (default) or the old haze / beam (`--portal-fx off`).</summary>
        public static bool Enabled = ReadFlag();

        public static bool Active { get { return Enabled && MapPortalLightLook.Ready; } }

        static bool ReadFlag()
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--portal-fx");
            return !(i >= 0 && i + 1 < a.Length && a[i + 1].Equals("off", StringComparison.OrdinalIgnoreCase));
        }

        public static void Build(Transform parent, IGameMap map)
        {
            MapPortalLightLook.Set look = MapPortalLightLook.Shared;
            int n = 0;
            foreach (PortalExit e in MapPortalExits.Of(map))
            {
                var feet = new Vector3(e.AnchorTx * GameConfig.TileUnits, GameConfig.MapH - e.AnchorTy * GameConfig.TileUnits, 0f);
                BuildOne(parent, feet, e, look, n++);
            }
        }

        static void BuildOne(Transform parent, Vector3 feet, PortalExit e, MapPortalLightLook.Set look, int index)
        {
            var go = new GameObject("PortalLight_" + e.Target);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            float span = Mathf.Clamp(e.Tiles, 1, 3);
            float poolW = 60f + 14f * span, colW = 40f + 8f * span; // twice the first version (user 19/11)
            int ground = UltimateFx.GroundFxOrder + 1, order = GameConfig.OrderBase(feet.y); // ground light under everyone; column y-sorted like a prop

            MeshRenderer halo = Quad(go.transform, "Halo", look.Halo, false, ground, new Vector3(0f, 0f, 0f), new Vector3(poolW * 1.5f, poolW * 0.62f, 1f));
            MeshRenderer pool = Quad(go.transform, "Pool", look.Pool, false, ground + 1, new Vector3(0f, 0f, 0f), new Vector3(poolW, poolW * 0.42f, 1f));
            MeshRenderer column = Quad(go.transform, "Column", look.Column, true, order, new Vector3(0f, -3f, 0f), new Vector3(colW, ColumnH, 1f));
            MeshRenderer core = Quad(go.transform, "Core", look.Core, true, order + 1, new Vector3(0f, -3f, 0f), new Vector3(colW * 0.38f, ColumnH * 0.9f, 1f));
            BuildMotes(go.transform, look.Mote, colW * 0.85f, order + 2, e.Tiles, e.Target.GetHashCode() ^ index);

            var b = go.AddComponent<Breathe>();
            b.Parts = new Renderer[] { halo, pool, column, core };
            b.Phase = index * 0.37f; // neighbouring exits do not breathe in step
        }

        static MeshRenderer Quad(Transform parent, string name, Material m, bool bottomPivot, int order, Vector3 pos, Vector3 scale)
        {
            MeshRenderer r = LightSkillAssets.Quad(parent, name, m, bottomPivot, order);
            r.transform.localPosition = pos;
            r.transform.localScale = scale;
            return r;
        }

        /// <summary>Motes rise from the pool through the column, swaying side to side; colour-over-lifetime fades each one in and out smoothly.</summary>
        static void BuildMotes(Transform parent, Material mat, float width, int order, int tiles, int seed)
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // duration / seed can only change while stopped

            var main = ps.main;
            main.duration = 4f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 3.8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(3.2f, 4.8f); // smaller ones got lost on snow
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.maxParticles = MaxMotes;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)seed;

            var emission = ps.emission;
            emission.rateOverTime = 4f + Mathf.Clamp(tiles, 1, 3);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width, 4f, 0f);

            // All three velocity axes must use the same curve mode: random between two curves.
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            var sway = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(0.5f, 0f), new Keyframe(0.75f, -1f), new Keyframe(1f, 0f));
            var swayBack = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, -1f), new Keyframe(0.5f, 0f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f));
            vel.x = new ParticleSystem.MinMaxCurve(4f, sway, swayBack);
            vel.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 8f), AnimationCurve.Constant(0f, 1f, 11f));
            vel.z = new ParticleSystem.MinMaxCurve(0f, AnimationCurve.Constant(0f, 1f, 0f), AnimationCurve.Constant(0f, 1f, 0f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.85f, 0.65f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.55f)));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = order;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
        }

        /// <summary>The ground light and the column swing slowly between 80 % and 100 % brightness.</summary>
        sealed class Breathe : MonoBehaviour
        {
            public Renderer[] Parts;
            public float Phase;
            void Update()
            {
                float k = MapPortalExits.Breath(Time.time, Phase);
                for (int i = 0; i < Parts.Length; i++) LightSkillAssets.Set(Parts[i], k);
            }
        }
    }
}
