using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Light of a 3D map (Map3D; ChatGPT spec section 4 "Willowbrook RO3 Soft Fantasy" preset): one directional sun fixed in WORLD space (upper left of
    /// the default view = from the north-west, 48 deg elevation, #FFF0CF, soft shadows, strength 0.6: shadows keep falling the same way on the ground when
    /// the camera turns), trilight ambient for any stock shader, linear fog #D1DEC5 that starts just past the hero, shadow distance / cascades fitted to
    /// the far camera (~32 m away), and the globals of the Map3D shaders (sun / shade / sky colours, wind, the hero occlusion ellipse).
    /// Lives on a child of the map root: built with a 3D map, its OnDestroy puts the render / quality settings back for the next (2D / 2.5D) map.
    /// Shadows follow the graphics quality (GraphicsQuality, 22/11 perf pass: 54-64 FPS here against ~200 on the 2.5D map, 156 with shadows off):
    /// High = 2 soft cascades at 2048 to hero + 20 m (the reviewed look); Medium = no realtime shadows, the static props' shadow baked once
    /// (Map3DBakedShadow: realtime with 1 cascade and only the big props casting still ran at half the speed of no shadows); Low = no sun shadow,
    /// GroundShadowRenderer draws the baked prop silhouettes of the 2.5D map instead.
    /// </summary>
    public sealed class Map3DLighting : MonoBehaviour
    {
        public const float SunElevation = 48f;
        static readonly Vector2 SunGround = new Vector2(0.75f, -0.66f).normalized; // light travels toward the south-east (FakeSun direction)
        static readonly Color SunColor = new Color(1f, 0.94f, 0.81f);              // #FFF0CF
        static readonly Color FogColor = new Color(0.82f, 0.87f, 0.77f);           // #D1DEC5
        static Map3DLighting current;

        static readonly int SunId = Shader.PropertyToID("_M3DSun"), ShadeId = Shader.PropertyToID("_M3DShade"), SkyId = Shader.PropertyToID("_M3DSky"),
            HeroId = Shader.PropertyToID("_M3DHero"), HeroDepthId = Shader.PropertyToID("_M3DHeroDepth"), WindId = Shader.PropertyToID("_M3DWind");

        // settings before the 3D map took over
        bool savedFog; FogMode savedFogMode; Color savedFogColor; float savedFogStart, savedFogEnd;
        AmbientMode savedAmbMode; Color savedSky, savedEq, savedGround, savedAmb;
        float savedShadowDist, savedSplit; int savedCascades; ShadowResolution savedRes; ShadowQuality savedQuality;
        Light sun;
        float fogEndM = 260f;

        /// <summary>The 3D map on screen draws its own sun shadows (realtime at High, baked at Medium); false at Low, where GroundShadowRenderer
        /// draws the baked prop silhouettes of the 2.5D map instead.</summary>
        public static bool OwnShadows { get { return current != null && GraphicsQuality.Current != GraphicsQuality.Level.Low; } }

        public static void Build(Transform parent)
        {
            var go = new GameObject("Map3DLighting");
            go.transform.SetParent(parent, false);
            go.AddComponent<Map3DLighting>();
        }

        void OnEnable()
        {
            if (current == null) Save(); else CopySaved(current);
            current = this;
            sun = new GameObject("Map3DSun").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            float el = Map3DPalette.Taurus ? Map3DPalette.SunElevation : SunElevation;
            Vector3 dir = new Vector3(SunGround.x * Mathf.Cos(el * Mathf.Deg2Rad), SunGround.y * Mathf.Cos(el * Mathf.Deg2Rad), Mathf.Sin(el * Mathf.Deg2Rad)); // +z = down
            sun.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            bool tau = Map3DPalette.Taurus; // Taurus V2 "Golden Adventure Daylight" (Map3DPalette)
            fogEndM = tau ? Map3DPalette.FogEndMetres : 260f;
            sun.color = tau ? Map3DPalette.SunColor : SunColor;
            sun.intensity = tau ? Map3DPalette.SunIntensity : 0.9f;
            sun.shadowStrength = Map3DPalette.Taurus ? Map3DPalette.ShadowStrength : 0.46f; // round 1: 0.42-0.46, some shade read too dark
            sun.shadowBias = 0.06f;
            sun.shadowNormalBias = 0.5f;
            sun.shadowResolution = LightShadowResolution.FromQualitySettings;
            sun.renderMode = LightRenderMode.ForcePixel;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            float am = tau ? Map3DPalette.AmbientMul * (MapRegistry.Current != null && MapRegistry.Current.Id == TauWillowbrookVillageMap.MapId ? 0.94f : 1f) : 0.65f; // RO3 loop round 3: village ambient fill -6 %
            RenderSettings.ambientSkyColor = (tau ? Map3DPalette.AmbientSky : new Color(0.87f, 0.91f, 0.82f)) * am;      // #DDE8D2 (Taurus #DCE5D4)
            RenderSettings.ambientEquatorColor = (tau ? new Color(0.70f, 0.76f, 0.64f) : new Color(0.68f, 0.74f, 0.64f)) * am; // #AEBDA2
            RenderSettings.ambientGroundColor = new Color(0.51f, 0.57f, 0.45f) * am;                                     // #819274
            RenderSettings.fog = !tau; // Taurus V2.1: fog off (village / C01), the grove stays clear too
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = tau ? Map3DPalette.FogColor : FogColor;
            ApplyQuality();
            GraphicsQuality.Changed -= ApplyQuality;
            GraphicsQuality.Changed += ApplyQuality;
            Shader.SetGlobalColor(SunId, tau ? Map3DPalette.SunGlobal : new Color(1.18f, 1.12f, 0.97f)); // round 2: brighter, the scene read dull next to the 2.5D map
            Shader.SetGlobalColor(ShadeId, tau ? Map3DPalette.ShadeGlobal : new Color(0.64f, 0.70f, 0.72f));
            Shader.SetGlobalColor(SkyId, new Color(0.97f, 1.0f, 1.04f, 0.35f));
            Shader.SetGlobalVector(WindId, new Vector4(0.9f, 1.2f, 0.8f, 0.6f));
        }

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null || cam.orthographic) return;
            float d = View25DCamera.Distance(cam);
            RenderSettings.fogStartDistance = d + 4f * Map3D.M;
            RenderSettings.fogEndDistance = d + fogEndM * Map3D.M;   // ~9 % fog 25 m past the hero (first pass 150 m read hazy)
            QualitySettings.shadowDistance = d + 20f * Map3D.M;
            Shader.SetGlobalVector(WindId, new Vector4(FeelConfig.FxOn ? 0.9f : 0f, 1.2f, 0.8f, 0.6f));
            HeroController h = GameSession.Hero;
            if (h == null) { Shader.SetGlobalVector(HeroDepthId, Vector4.zero); return; }
            Vector3 p = h.transform.position;
            Vector3 chest = View25D.Above(p.x, p.y, 15f);
            Vector3 vp = cam.WorldToViewportPoint(chest);
            float px = View25D.Profile.HeroPx / 720f * DevStartConfig.CameraViewHeight() * 0.5f / Mathf.Max(1f, cam.orthographicSize * View25DBossZoom.Factor); // hero height in viewport heights at this zoom
            Shader.SetGlobalVector(HeroId, new Vector4(vp.x, vp.y, px * 0.80f, px * 1.15f));
            Shader.SetGlobalVector(HeroDepthId, new Vector4(vp.z, vp.z > 0f ? 1f : 0f, 0f, 0f));
        }

        /// <summary>Shadow settings of the graphics level: realtime sun shadows at High only (Medium bakes them once: Map3DBakedShadow).</summary>
        void ApplyQuality()
        {
            if (sun == null) return;
            bool off = GraphicsQuality.Current != GraphicsQuality.Level.High || Map3D.Test("noshadow");
            sun.shadows = off ? LightShadows.None : LightShadows.Soft;
            QualitySettings.shadows = off ? ShadowQuality.Disable : ShadowQuality.All;
            QualitySettings.shadowResolution = Map3D.Test("lowshadow") ? ShadowResolution.Medium : ShadowResolution.High; // round 2: 2048
            QualitySettings.shadowCascades = 2;          // round 2 (ChatGPT): 2 cascades at 2048, distance hero + 20 m
            QualitySettings.shadowCascade2Split = 0.72f; // near cascade ends just past the hero (camera ~34 m away, distance hero + 20 m)
        }

        void OnDestroy()
        {
            GraphicsQuality.Changed -= ApplyQuality;
            if (current != this) return; // a newer 3D map already took over
            current = null;
            Shader.SetGlobalVector(HeroDepthId, Vector4.zero);
            RenderSettings.fog = savedFog; RenderSettings.fogMode = savedFogMode; RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogStartDistance = savedFogStart; RenderSettings.fogEndDistance = savedFogEnd;
            RenderSettings.ambientMode = savedAmbMode; RenderSettings.ambientSkyColor = savedSky; RenderSettings.ambientEquatorColor = savedEq;
            RenderSettings.ambientGroundColor = savedGround; RenderSettings.ambientLight = savedAmb;
            QualitySettings.shadowDistance = savedShadowDist; QualitySettings.shadowCascades = savedCascades; QualitySettings.shadowCascade2Split = savedSplit;
            QualitySettings.shadowResolution = savedRes; QualitySettings.shadows = savedQuality;
        }

        void Save()
        {
            savedFog = RenderSettings.fog; savedFogMode = RenderSettings.fogMode; savedFogColor = RenderSettings.fogColor;
            savedFogStart = RenderSettings.fogStartDistance; savedFogEnd = RenderSettings.fogEndDistance;
            savedAmbMode = RenderSettings.ambientMode; savedSky = RenderSettings.ambientSkyColor; savedEq = RenderSettings.ambientEquatorColor;
            savedGround = RenderSettings.ambientGroundColor; savedAmb = RenderSettings.ambientLight;
            savedShadowDist = QualitySettings.shadowDistance; savedCascades = QualitySettings.shadowCascades; savedSplit = QualitySettings.shadowCascade2Split;
            savedRes = QualitySettings.shadowResolution; savedQuality = QualitySettings.shadows;
        }

        void CopySaved(Map3DLighting o)
        {
            savedFog = o.savedFog; savedFogMode = o.savedFogMode; savedFogColor = o.savedFogColor; savedFogStart = o.savedFogStart; savedFogEnd = o.savedFogEnd;
            savedAmbMode = o.savedAmbMode; savedSky = o.savedSky; savedEq = o.savedEq; savedGround = o.savedGround; savedAmb = o.savedAmb;
            savedShadowDist = o.savedShadowDist; savedCascades = o.savedCascades; savedSplit = o.savedSplit; savedRes = o.savedRes; savedQuality = o.savedQuality;
        }
    }
}
