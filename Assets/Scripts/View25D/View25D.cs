using System;
using System.Globalization;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Fake 2.5D view (prototype 2026-11-20 on the Mire Hydra arena; first real 2.5D map 2026-11-21: Willowbrook Meadows). View-only: the simulation, collision,
    /// server and every position stay on the 2D x/y plane. The ground (painted map, telegraphs, flat decor, shadows) keeps lying on z = 0; a tilted
    /// perspective camera (View25DCamera) looks at it from the south, and units, tall props and name anchors stand up toward the camera as
    /// billboards rotated by <see cref="Tilt"/> around their feet (View25DWorld). Draw order is still the 2D y-sort (sortingOrder), so depth stays right.
    /// Which maps: every map listed in <see cref="Maps"/> is 2.5D for everyone (release builds too); `--view25d` (test builds) adds the prototype maps
    /// in <see cref="PrototypeMaps"/>; `--view25d off` turns 2.5D off everywhere (2D comparison). Converting another map = add its id to Maps
    /// (rules: docs/map-design-rules.md "Map 2.5D").
    /// Camera, sun, fog and ambient look per map: <see cref="View25DProfile"/> (RO3 Feel V2 on listed maps: tilt 32 = pitch 58 / FOV 27; Hydra prototype 38 / 22).
    /// Flags: --view25d [on|off] [--view25d-tilt deg] [--view25d-fov deg] (override the profile) [--view25d-atmos off]
    /// [--view25d-scalecomp 0..1 (default 0.6)] [--view25d-look off] (RO3 lighting pass off, for A/B)
    /// [--cam25d pitch,fov,heroPx[,feetY]] (camera A/B in RO3 terms, View25DCameraOverride; wins over --view25d-tilt / -fov).
    /// </summary>
    public static partial class View25D
    {
        static bool parsed, requested, forcedOff, atmosphere = true, lookOff;

        /// <summary>Maps drawn in 2.5D for every player (data: one id per converted map).</summary>
        static readonly string[] Maps = { WillowbrookMeadowsMap.MapId, RootburrowMap.MapId, RootburrowB2Map.MapId, RootburrowB3Map.MapId, // Rootburrow 1F-3F: dark dungeon profiles (21/11/2026)
            FrostHollowLayout.MapId, FrostfangDenMap.MapId,
            TauWillowbrookVillageMap.MapId, TauCloverMeadowsMap.MapId, TauAncientRootShrineMap.MapId }; // Taurus greybox (24/11/2026) // snow field + ice cave (21/11/2026, plans/261121-1930-frost-hollow-25d)
        /// <summary>Maps 2.5D only with `--view25d` (test builds): the Mire Hydra arena prototype, whose painted walls are not converted yet.</summary>
        static readonly string[] PrototypeMaps = { SunkenReliquaryMap.MapId };
        static float tiltArg = -1f, fovArg = -1f, scaleComp = 0.6f;

        static void Parse()
        {
            if (parsed) return;
            parsed = true;
            string[] a = Environment.GetCommandLineArgs();
            int f = Array.IndexOf(a, "--view25d");
            forcedOff = f >= 0 && f + 1 < a.Length && a[f + 1] == "off";
            requested = f >= 0 && !forcedOff && !ReleaseBuild.IsRelease;
            float t = ArgFloat(a, "--view25d-tilt", -1f), fv = ArgFloat(a, "--view25d-fov", -1f);
            if (t > 0f) tiltArg = Mathf.Clamp(t, 5f, 70f);
            if (fv > 0f) fovArg = Mathf.Clamp(fv, 10f, 60f);
            int lk = Array.IndexOf(a, "--view25d-look");
            lookOff = lk >= 0 && lk + 1 < a.Length && a[lk + 1] == "off";
            scaleComp = Mathf.Clamp01(ArgFloat(a, "--view25d-scalecomp", scaleComp));
            int i = Array.IndexOf(a, "--view25d-atmos");
            atmosphere = !(i >= 0 && i + 1 < a.Length && a[i + 1] == "off");
        }

        static float ArgFloat(string[] a, string key, float fallback)
        {
            int i = Array.IndexOf(a, key);
            float v;
            return i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        /// <summary>The prototype maps are asked for on the command line (test builds only).</summary>
        public static bool Requested { get { Parse(); return requested; } }

        /// <summary>2.5D can draw in this window at all: a client / offline view and not `--view25d off`.</summary>
        public static bool Available { get { Parse(); return !forcedOff && NetMode.RendersFx; } }

        /// <summary>The map with this id is drawn in 2.5D (listed in Maps, or a prototype map with `--view25d`).</summary>
        public static bool IsMap25D(string id)
        {
            if (!Available || string.IsNullOrEmpty(id)) return false;
            if (Array.IndexOf(Maps, id) >= 0) return true;
            return requested && Array.IndexOf(PrototypeMaps, id) >= 0;
        }

        /// <summary>2.5D view on right now: the map on screen is a 2.5D map and this window draws.</summary>
        public static bool Active { get { return MapRegistry.Current != null && IsMap25D(MapRegistry.Current.Id); } }

        /// <summary>Ground shadows come from FakeSun (soft blobs + baked prop shadows, the same system as the 2D maps) instead of the prototype's
        /// per-sprite silhouettes. True on every listed map; the Hydra prototype keeps its silhouettes. `--shadows off` falls back to silhouettes.</summary>
        public static bool UsesFakeSun { get { return Active && Array.IndexOf(Maps, MapRegistry.Current.Id) >= 0 && FakeSun.Allowed; } }

        public static bool Atmosphere { get { Parse(); return atmosphere; } }

        /// <summary>Look of the map on screen (camera, sun, fog, ambient); the RO3 preset when no map is loaded.</summary>
        public static View25DProfile Profile { get { return View25DProfile.For(MapRegistry.Current != null ? MapRegistry.Current.Id : ""); } }

        /// <summary>The RO3 lighting pass is on for the map on screen (2.5D active, its profile asks for it, not `--view25d-look off`).</summary>
        public static bool Ro3Look { get { Parse(); return Active && !lookOff && Profile.Ro3Look; } }

        /// <summary>Ground offset north of the feet that puts the screen centre on the hero's chest (LookAtHeight up the billboard):
        /// the view ray through a point h up a billboard tilted t meets the ground h / cos t north of the feet. 0 on a profile without it.</summary>
        public static float LookAtGroundOffset { get { float h = LookAtHeight; return h > 0f ? h / Mathf.Cos(TiltDeg * Mathf.Deg2Rad) : 0f; } }

        /// <summary>Profile look-at height, or the test override (`--cam25d`, View25DCameraOverride).</summary>
        public static float LookAtHeight
        {
            get
            {
                if (View25DCameraOverride.On) return View25DCameraOverride.LookAtHeight;
                View25DProfile p = Profile;
                return p.HeroPx > 0f ? View25DCameraOverride.LookAtFor(p.FeetY, ViewScale) : p.LookAtHeight;
            }
        }

        /// <summary>Profile camera distance multiplier, or the test override (`--cam25d` hero pixel height).</summary>
        public static float ViewScale
        {
            get
            {
                if (View25DCameraOverride.On) return View25DCameraOverride.ViewScale;
                View25DProfile p = Profile;
                return (p.HeroPx > 0f ? View25DCameraOverride.ViewScaleFor(p.HeroPx) : p.ViewScale) * View25DBossZoom.Factor; // Camera V3.1 boss zoom (3D maps)
            }
        }

        /// <summary>How much of the perspective size change units undo (0 = full perspective, 1 = every unit as tall as at the screen centre).
        /// 40-player readability: far heroes must not shrink much (ChatGPT: keep the size spread within ~10 %).</summary>
        public static float ScaleComp { get { Parse(); return scaleComp; } }

        /// <summary>Scale for a unit standing at `feet` so its on-screen size stays near the size at the camera focus (see ScaleComp).</summary>
        public static float UnitScale(Camera cam, Vector3 feet)
        {
            if (cam == null || cam.orthographic) return 1f;
            float refDist = View25DCamera.Distance(cam);
            float d = Vector3.Dot(feet - cam.transform.position, cam.transform.forward);
            return d <= 1f ? 1f : Mathf.Lerp(1f, d / refDist, ScaleComp);
        }
        /// <summary>Camera pitch away from straight down, degrees (0 = the old top-down view).</summary>
        public static float TiltDeg { get { Parse(); return View25DCameraControl.TiltFor(View25DCameraOverride.On ? View25DCameraOverride.TiltDeg : tiltArg > 0f ? tiltArg : Profile.TiltDeg); } }
        public static float FovDeg { get { Parse(); return View25DCameraOverride.On ? View25DCameraOverride.FovDeg : fovArg > 0f ? fovArg : Profile.FovDeg; } }

        /// <summary>Rotation that stands a sprite drawn on the x/y plane up toward the camera, pivoting on its feet (local +y becomes "up"), turned round
        /// the world vertical by the player's camera yaw (View25DCameraControl): a cylindrical billboard. The camera uses the same rotation.</summary>
        public static Quaternion Tilt { get { return Quaternion.AngleAxis(YawDeg, Vector3.forward) * Quaternion.Euler(-TiltDeg, 0f, 0f); } }

        /// <summary>World point `h` units above the ground point `feet` along a billboard (2D view: simply feet + h on y).</summary>
        public static Vector3 Above(float x, float y, float h)
        {
            if (!Active) return new Vector3(x, y + h, 0f);
            return new Vector3(x, y, 0f) + Tilt * new Vector3(0f, h, 0f);
        }

        /// <summary>Screen-space label anchor (damage numbers) given as a point already lifted on y: treated as ~20 units above a ground point,
        /// which is where most numbers start, so it lands near the standing figure instead of on the foreshortened ground behind it.</summary>
        public static Vector3 Label(float x, float y)
        {
            const float TypicalLift = 20f;
            return Active ? Above(x, y - TypicalLift, TypicalLift) : new Vector3(x, y, 0f);
        }

        /// <summary>Sun from the upper left: drop shadows fall down-right (south-east), short and soft (ChatGPT: a subtle directional extension,
        /// 20-30 % opacity, warm charcoal-brown, never long shadows across other players). Angle = rotation from north, length = squash.</summary>
        public const float SunShadowAngle = -140f, SunShadowLength = 0.42f, SunShadowAlpha = 0.24f;
        public static readonly Color ShadowTint = new Color(0.16f, 0.11f, 0.08f, 1f);
        /// <summary>Drop shadows lie just above the ground painting and flat decor, below telegraphs and every y-sorted unit.</summary>
        public const int ShadowOrder = GameConfig.GroundOrder + 8;

        static Material shadowMat;
        static bool shadowMatTried;

        /// <summary>Soft black silhouette material (Shaders/View25DShadow), or null when the shader is missing (no drop shadows then).</summary>
        public static Material ShadowMaterial
        {
            get
            {
                if (shadowMatTried) return shadowMat;
                shadowMatTried = true;
                Shader s = Resources.Load<Shader>("Shaders/View25DShadow");
                if (s != null && s.isSupported) shadowMat = new Material(s) { name = "View25D_Shadow", hideFlags = HideFlags.DontSave };
                else Debug.LogWarning("[ZenoniaProto] View25D: shadow shader missing, no drop shadows");
                return shadowMat;
            }
        }

        /// <summary>Stands a tall prop up (billboard at its foot line) and lays a soft sun shadow of it on the ground. Static, so done once at build.</summary>
        public static void StandProp(SpriteRenderer sr)
        {
            sr.transform.rotation = Tilt;
            RegisterStanding(sr.transform); // turned again when the player tilts / rotates the camera (View25D.CameraYaw.cs)
            View25DPropFade.Register(sr); // fades while it stands in front of the local hero
            View25DLighting.ApplyProp(sr); // RO3 pass: sun-side crown shading, base AO, wind sway, fog
            if (UsesFakeSun) return; // the baked FakeSun prop shadow lies under it already (StaticPropShadows)
            Material m = ShadowMaterial;
            if (m == null || sr.sprite == null) return;
            var go = new GameObject(sr.name + "_Shadow25D");
            go.transform.SetParent(sr.transform.parent, false);
            go.transform.position = sr.transform.position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, SunShadowAngle);
            go.transform.localScale = new Vector3(1f, SunShadowLength, 1f);
            var sh = go.AddComponent<SpriteRenderer>();
            sh.sprite = sr.sprite;
            sh.sharedMaterial = m;
            sh.color = new Color(ShadowTint.r, ShadowTint.g, ShadowTint.b, SunShadowAlpha * 1.3f);
            sh.sortingOrder = ShadowOrder;
        }
    }
}
