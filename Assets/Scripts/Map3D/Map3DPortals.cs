using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Map exits on a 3D map (Map3D, 22/11/2026, user: "làm cổng dịch chuyển 3D luôn đi"): the flat portal light quads (MapPortalLightFx) skewed under
    /// the 360 deg camera, so here every exit is a real 3D warp gate (Map3DPortalMesh):
    ///  - a broken stone ring with glowing runes on two plinths (painted ruin stone, Map3DPortalStone: sun ramp, real / baked shadow, hero dither);
    ///  - a swirling vortex filling the ring, a soft light column rising through it, a ground glow with a turning rune circle (Map3DPortalGlow);
    ///  - motes spiralling up (ParticleSystem, the portal mote material of the 2.5D light) and a real blue point light at the vortex that lights the stones.
    /// Placement = MapPortalExits (the spot the old light stood on); side exits turn the gate so it faces the road out. Portal tiles, triggers, targets
    /// and every position are unchanged: picture only. Everything breathes slowly (MapPortalExits.Breath), nothing blinks.
    /// Cost per gate: 4 draws (stone + its point-light pass, vortex, column, pool) + 1 particle draw of at most MaxMotes; ~700 stone vertices.
    /// </summary>
    public static class Map3DPortals
    {
        public const int MaxMotes = 24;
        const float ColumnRadius = 0.62f, ColumnHeight = 4.6f, PoolRadius = 2.3f; // metres
        static readonly Color LightColor = new Color(0.36f, 0.66f, 1f);
        static readonly Color Body = new Color(0.08f, 0.38f, 0.95f, 1f), Emission = new Color(0.1f, 0.5f, 1.3f, 1f);

        static Material stone, vortex, column, pool;
        static bool failed;

        static bool Ready()
        {
            if (failed) return false;
            if (stone != null) return true;
            Shader s = Resources.Load<Shader>("Shaders/Map3DPortalStone"), g = Resources.Load<Shader>("Shaders/Map3DPortalGlow");
            if (s == null || g == null || !s.isSupported || !g.isSupported)
            {
                Debug.LogWarning("[ZenoniaProto] Map3D portal shaders missing, exits keep the flat portal light");
                failed = true;
                return false;
            }
            Texture2D pave = Resources.Load<Texture2D>("Map3D/ground-paving");
            stone = new Material(s) { name = "Map3DPortal_Stone", hideFlags = HideFlags.DontSave };
            stone.SetTexture("_MainTex", pave != null ? Map3DAssets.Tex("ground-paving") : Map3DAssets.Tex("rock"));
            stone.SetTexture("_MossTex", Map3DAssets.Tex("ground-grass"));
            stone.SetTexture("_RuneTex", Map3DPortalMesh.Runes);
            stone.SetFloat("_TriScale", 1f / 30f);
            stone.SetColor("_Tint", new Color(0.9f, 0.92f, 0.98f));     // a little cooler than the ruins: reads as an old gate
            stone.SetColor("_RuneColor", new Color(0.25f, 0.7f, 1.6f));

            vortex = Glow(g, "Vortex", 0f, new Color(Body.r, Body.g, Body.b, 0.82f), Emission);
            column = Glow(g, "Column", 1f, new Color(Body.r, Body.g, Body.b, 0.45f), Emission * 0.55f);
            pool = Glow(g, "Pool", 2f, new Color(Body.r, Body.g, Body.b, 0.55f), Emission * 0.6f);
            return true;
        }

        static Material Glow(Shader g, string name, float mode, Color body, Color emission)
        {
            var m = new Material(g) { name = "Map3DPortal_" + name, hideFlags = HideFlags.DontSave };
            m.SetTexture("_Noise", Map3DAssets.Noise);
            m.SetTexture("_RuneTex", Map3DPortalMesh.Runes);
            m.SetFloat("_Mode", mode);
            m.SetColor("_BodyColor", body);
            m.SetColor("_EmissionColor", emission);
            return m;
        }

        /// <summary>Gates for every exit of the map; false when the shaders are missing (MapScenery then draws the flat light).</summary>
        public static bool Build(Transform parent, IGameMap map)
        {
            if (!Ready()) return false;
            Map3DHeightField f = Map3DHeightField.For(map);
            int n = 0;
            foreach (PortalExit e in MapPortalExits.Of(map))
            {
                Vector3 feet = Map3D.World(e.AnchorTx, e.AnchorTy, f.HeightAt(e.AnchorTx, e.AnchorTy));
                feet.z += Map3DTerrain.Sink;
                float yaw = e.Edge == 0 || e.Edge == 1 ? 90f : 0f; // the ring faces the road out: side exits turn it a quarter
                BuildOne(parent, feet, yaw, e, n++);
            }
            return true;
        }

        static void BuildOne(Transform parent, Vector3 feet, float yaw, PortalExit e, int index)
        {
            var go = new GameObject("Portal3D_" + e.Target);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            go.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.forward);
            int order = GameConfig.OrderBase(feet.y); // light parts y-sorted like the 2.5D column

            MeshRenderer arch = Part(go.transform, "Arch", Map3DPortalMesh.Arch(), stone, 0, Vector3.zero);
            arch.receiveShadows = true;
            Map3DBatcher.Register(arch, true); // casts at High, baked at Medium (GraphicsQuality)

            MeshRenderer disc = Part(go.transform, "Vortex", Map3DPortalMesh.Vortex(), vortex, order, Vector3.zero);
            MeshRenderer col = Part(go.transform, "Column", Map3DPortalMesh.Column(ColumnRadius, ColumnHeight), column, order + 1, Vector3.zero);
            MeshRenderer gnd = Part(go.transform, "Pool", Map3DPortalMesh.Pool(PoolRadius), pool, UltimateFx.GroundFxOrder + 1, new Vector3(0f, 0f, -0.4f - Map3DTerrain.Sink));
            ParticleSystem motes = MapPortalLightLook.Ready ? Motes(go.transform, MapPortalLightLook.Shared.Mote, order + 2, e.Target.GetHashCode() ^ index) : null;

            var lightGo = new GameObject("PortalLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, -Map3DPortalMesh.RingCentre * Map3D.M);
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = LightColor;
            l.range = 6.5f * Map3D.M;
            l.intensity = 2.4f;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;

            var b = go.AddComponent<Breathe>();
            b.Parts = new Renderer[] { arch, disc, col, gnd };
            b.Light = l;
            b.BaseIntensity = l.intensity;
            b.Phase = index * 0.37f;
            if (motes != null) motes.Play();
        }

        static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material mat, int order, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = order;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return r;
        }

        /// <summary>Motes rise from the ground glow through the ring, circling the column (orbital velocity), fading in and out. Up = -z.</summary>
        static ParticleSystem Motes(Transform parent, Material mat, int order, int seed)
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.5f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 4f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.6f, 3.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(3.2f, 5.2f);
            main.startColor = Color.white;
            main.maxParticles = MaxMotes;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)seed;

            var emission = ps.emission;
            emission.rateOverTime = 7f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle; // local x-y plane = the ground
            shape.radius = 1.1f * Map3D.M;
            shape.radiusThickness = 0.6f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(-1.0f * Map3D.M, -1.5f * Map3D.M); // rise 1-1.5 m/s
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(1.1f, 1.6f);                // circle the column
            vel.radial = new ParticleSystem.MinMaxCurve(-0.15f * Map3D.M, -0.3f * Map3D.M); // drawn inward a little

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.85f, 0.65f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.5f)));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = order;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        /// <summary>Light parts, rune glow and the point light swing slowly between 80 % and 100 % (per gate phase).</summary>
        sealed class Breathe : MonoBehaviour
        {
            public Renderer[] Parts;
            public Light Light;
            public float BaseIntensity, Phase;
            MaterialPropertyBlock mpb;
            static readonly int BreathId = Shader.PropertyToID("_Breath");

            void Update()
            {
                float k = MapPortalExits.Breath(Time.time, Phase);
                if (mpb == null) mpb = new MaterialPropertyBlock();
                mpb.SetFloat(BreathId, k);
                for (int i = 0; i < Parts.Length; i++) if (Parts[i] != null) Parts[i].SetPropertyBlock(mpb);
                if (Light != null) Light.intensity = BaseIntensity * k;
            }
        }
    }
}
