using System;
using System.Globalization;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Fake 2.5D view prototype (2026-11-20, opt-in `--view25d`, Sunken Reliquary / Mire Hydra arena only). View-only: the simulation, collision,
    /// server and every position stay on the 2D x/y plane. The ground (painted map, telegraphs, flat decor, shadows) keeps lying on z = 0; a tilted
    /// perspective camera (View25DCamera) looks at it from the south, and units, tall props and name anchors stand up toward the camera as
    /// billboards rotated by <see cref="Tilt"/> around their feet (View25DWorld). Draw order is still the 2D y-sort (sortingOrder), so depth stays right.
    /// Flags: --view25d [--view25d-tilt deg (default 38)] [--view25d-fov deg (default 22)] [--view25d-atmos off]
    /// [--view25d-scalecomp 0..1 (default 0.6)]. Defaults follow the ChatGPT art-direction chat linked in docs/pending-tasks.md.
    /// </summary>
    public static class View25D
    {
        static bool parsed, requested, atmosphere = true;
        static float tiltDeg = 38f, fovDeg = 22f, scaleComp = 0.6f;

        static void Parse()
        {
            if (parsed) return;
            parsed = true;
            string[] a = Environment.GetCommandLineArgs();
            requested = Array.IndexOf(a, "--view25d") >= 0 && !ReleaseBuild.IsRelease;
            tiltDeg = Mathf.Clamp(ArgFloat(a, "--view25d-tilt", tiltDeg), 5f, 70f);
            fovDeg = Mathf.Clamp(ArgFloat(a, "--view25d-fov", fovDeg), 10f, 60f);
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

        /// <summary>The prototype is on the command line (test builds only).</summary>
        public static bool Requested { get { Parse(); return requested; } }

        /// <summary>2.5D view on right now: requested, a client / offline window, and the map on screen is Sunken Reliquary (the Mire Hydra arena).</summary>
        public static bool Active { get { return Requested && NetMode.RendersFx && MapRegistry.Current != null && MapRegistry.Current.Id == SunkenReliquaryMap.MapId; } }

        public static bool Atmosphere { get { Parse(); return atmosphere; } }

        /// <summary>How much of the perspective size change units undo (0 = full perspective, 1 = every unit as tall as at the screen centre).
        /// 40-player readability: far heroes must not shrink much (ChatGPT: keep the size spread within ~10 %).</summary>
        public static float ScaleComp { get { Parse(); return scaleComp; } }

        /// <summary>Scale for a unit standing at `feet` so its on-screen size stays near the size at the camera focus (see ScaleComp).</summary>
        public static float UnitScale(Camera cam, Vector3 feet)
        {
            if (cam == null || cam.orthographic) return 1f;
            float refDist = cam.orthographicSize / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float d = Vector3.Dot(feet - cam.transform.position, cam.transform.forward);
            return d <= 1f ? 1f : Mathf.Lerp(1f, d / refDist, ScaleComp);
        }
        /// <summary>Camera pitch away from straight down, degrees (0 = the old top-down view).</summary>
        public static float TiltDeg { get { Parse(); return tiltDeg; } }
        public static float FovDeg { get { Parse(); return fovDeg; } }

        /// <summary>Rotation that stands a sprite drawn on the x/y plane up toward the camera, pivoting on its feet (local +y becomes "up").</summary>
        public static Quaternion Tilt { get { return Quaternion.Euler(-TiltDeg, 0f, 0f); } }

        /// <summary>World point `h` units above the ground point `feet` along a billboard (2D view: simply feet + h on y).</summary>
        public static Vector3 Above(float x, float y, float h)
        {
            if (!Active) return new Vector3(x, y + h, 0f);
            float r = TiltDeg * Mathf.Deg2Rad;
            return new Vector3(x, y + h * Mathf.Cos(r), -h * Mathf.Sin(r));
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
