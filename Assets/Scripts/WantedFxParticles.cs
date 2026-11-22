using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// A code-built ParticleSystem for the shader boss FX (MireHydraLightFx): no emission module, every particle is placed by Emit() with its own
    /// position / velocity / size / colour / life, so the caller decides exactly where the bubbles go (inside a warning circle, along a breath cone,
    /// out of a geyser) and when (tied to the server strike instant). Colour-over-lifetime fades each particle in and out smoothly (no popping, no
    /// blinking: Sổ lỗi 2026-11-13 portal markers), size-over-lifetime swells it a little then shrinks it. World simulation space, so particles stay
    /// where they were emitted. Same layout rules as MapPortalLightFx.BuildMotes.
    /// </summary>
    public static class WantedFxParticles
    {
        public static ParticleSystem Create(Transform parent, string name, Material mat, int order, int maxParticles, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startSpeed = 0f;
            main.startColor = Color.white;
            main.gravityModifier = gravity;   // world gravity is (0, -9.81) units/s^2: tiny next to 16-unit tiles, so callers pass large values
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;

            var emission = ps.emission;
            emission.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.7f)));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = order;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>One particle at world `pos` moving at `vel` (units / s).</summary>
        public static void Emit(ParticleSystem ps, Vector2 pos, Vector2 vel, float size, Color color, float life)
        {
            Emit(ps, new Vector3(pos.x, pos.y, 0f), new Vector3(vel.x, vel.y, 0f), size, color, life);
        }

        /// <summary>Same in world 3D (2.5D / 3D maps: ClericLightFx.ParticlePos / ParticleVel put the point on the standing picture).</summary>
        public static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, Color color, float life)
        {
            if (ps == null) return;
            var p = new ParticleSystem.EmitParams
            {
                position = pos, velocity = vel,
                startSize = size, startColor = color, startLifetime = life, applyShapeToPosition = false,
            };
            ps.Emit(p, 1);
        }

        /// <summary>A purple or moss bubble colour (purple twice as often: the boss's main colour).</summary>
        public static Color VenomColor()
        {
            return Random.value < 0.66f ? MireHydraFxLook.Purple : MireHydraFxLook.Moss;
        }
    }
}
