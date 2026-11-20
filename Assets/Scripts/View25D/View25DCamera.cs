using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Perspective placement for the 2.5D prototype (View25D), used by CameraFollow instead of its orthographic path. The camera keeps reading
    /// orthographicSize as its zoom knob (dev-start, wheel zoom, ultimate punch, captures): a billboard standing at the focus point shows exactly
    /// as tall as it did in the orthographic view, the ground around it is foreshortened by cos(tilt), and things farther north shrink with distance.
    /// </summary>
    public static class View25DCamera
    {
        const float TopOvershootV = 0.88f;
        /// <summary>Background beyond the map's north edge: the haze colour of View25DAtmosphere, so the far edge reads as distance fog.</summary>
        public static readonly Color FogColor = new Color(0.62f, 0.80f, 0.82f);

        /// <summary>Switches the camera to the tilted perspective view.</summary>
        public static void Configure(Camera cam)
        {
            cam.orthographic = false;
            cam.backgroundColor = FogColor;
            cam.fieldOfView = View25D.FovDeg;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 4000f;
            cam.transform.rotation = View25D.Tilt;
        }

        /// <summary>Back to the plain 2D camera (another map, or the flag off).</summary>
        public static void Restore(Camera cam)
        {
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor = new Color(0.06f, 0.09f, 0.13f); // CameraFollow's 2D background
        }

        static float Distance(Camera cam)
        {
            return cam.orthographicSize / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        /// <summary>Looks at the ground point `focus` along the tilted view direction.</summary>
        public static void Place(Camera cam, Vector2 focus)
        {
            Quaternion q = View25D.Tilt;
            cam.transform.rotation = q;
            cam.transform.position = new Vector3(focus.x, focus.y, 0f) - q * Vector3.forward * Distance(cam);
        }

        /// <summary>Ground point (z = 0) seen at viewport (u, v) when the camera looks at the origin; the footprint only moves with the focus.</summary>
        static Vector2 GroundHit(Camera cam, float u, float v)
        {
            Quaternion q = View25D.Tilt;
            float tv = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 dir = q * new Vector3(tv * cam.aspect * (2f * u - 1f), tv * (2f * v - 1f), 1f);
            Vector3 origin = -(q * Vector3.forward) * Distance(cam);
            if (dir.z <= 0.01f) dir.z = 0.01f; // above the horizon (only at absurd tilts): clamp far away instead of flipping
            float t = -origin.z / dir.z;
            return new Vector2(origin.x + dir.x * t, origin.y + dir.y * t);
        }

        /// <summary>Keeps the visible ground trapezoid inside the map (wider far edge sets the x limit); a map smaller than the view is centred.</summary>
        public static Vector2 Clamp(Camera cam, Vector2 p)
        {
            // The far edge may run past the north border by the top ~12 % of the screen: it fades into the haze-coloured background there
            // (View25DAtmosphere top band), so a hero near the north wall is not pushed up under the HUD as a hard clamp would do.
            Vector2 bottom = GroundHit(cam, 1f, 0f), top = GroundHit(cam, 1f, TopOvershootV);
            float halfW = Mathf.Max(top.x, bottom.x);
            float mapW = GameConfig.MapW, mapH = GameConfig.MapH;
            p.x = mapW <= halfW * 2f ? mapW * 0.5f : Mathf.Clamp(p.x, halfW, mapW - halfW);
            p.y = mapH <= top.y - bottom.y ? (mapH - top.y - bottom.y) * 0.5f : Mathf.Clamp(p.y, -bottom.y, mapH - top.y);
            return p;
        }
    }
}
