using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Where a ClericLightFx layer sits on the 2.5D / 3D maps (View25D, Map3D; 22/11/2026, user: "sửa Earth Break đi" - the skills bent when the camera
    /// turned). The builders still draw one 2D picture per skill (offsets in units from the anchor); here that picture is put into the world so it
    /// survives any camera yaw / pitch and looks the same as before at the default camera (pitch 58, yaw 0):
    ///  - Ground band: flat on the real ground plane. An unturned flat ellipse (the old pre-squash for the fixed camera, Squash 0.7, Shock 0.6...)
    ///    becomes the real circle of the same x radius, which the perspective camera foreshortens by itself; turned streaks keep their shape.
    ///  - Every other band: the picture stands on the camera-facing plane through the anchor (the plane heroes are drawn on): picture x = screen
    ///    right, picture y = up. Heights are scaled by <see cref="PictureRise"/> = cos 32 deg, what lying on the ground at pitch 58 showed of them, so the
    ///    default view is unchanged. Rise / Drop move along that up direction (a sword falls through the air, not along the ground).
    ///  - Floor() layers (light columns, flame tongues, strike tips, bolt points...): the offset is a REAL ground point (world units), the quad stands
    ///    there as a camera-facing billboard. Use it whenever the offset comes from a world direction or position.
    /// On Map3D the standing picture is pushed <see cref="Map3DPush"/> units toward the camera so its lower half never sinks under the terrain.
    /// Particles emitted through the host (Emit, Puff, Ring, MechanicLightBursts.Sparks) follow the same standing picture.
    /// The flat 2D view is untouched.
    /// </summary>
    public sealed partial class ClericLightFx
    {
        static int flatTest = -1;

        /// <summary>The skill FX are put into the 3D world (2.5D / 3D map on screen). `--fx-flat` (test builds) keeps the old flat layout on the
        /// ground for before / after comparisons.</summary>
        public static bool Deep
        {
            get
            {
                if (flatTest < 0) flatTest = !ReleaseBuild.IsRelease && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--fx-flat") >= 0 ? 1 : 0;
                return flatTest == 0 && View25D.Active;
            }
        }

        /// <summary>Share of a picture height shown by the old flat layout at the default camera (pitch 58: cos 32 deg).</summary>
        public const float PictureRise = 0.848f;

        /// <summary>Map3D: how far (units) the standing picture comes toward the camera, so the terrain never hides its lower half.</summary>
        const float Map3DPush = 20f;

        /// <summary>The offset of the last added (non-ground) layer is a real ground point: it stands there instead of on the picture plane.
        /// `height` = the part of the offset's y that is height, not ground (the flat offset is given as usual: ground point + (0, height)); on the
        /// 2.5D / 3D maps that part goes up instead. A Turn() angle of such a layer is a ground direction (turned by the camera yaw on screen).
        /// The flat 2D view is unchanged.</summary>
        public ClericLightFx Floor(float height = 0f)
        {
            if (layers.Count == 0 || !ClericLightFx.Deep) return this;
            Layer l = Last;
            l.Floor = true; l.Offset.y -= height; l.Up0 += height;
            return this;
        }

        /// <summary>Ground offset at angle `rad`, `r` units out on the ground ring round the anchor: the squashed ellipse on the flat view, the real
        /// circle on the 2.5D / 3D maps (pair it with Floor()).</summary>
        public static Vector2 GroundRing(float rad, float r)
        {
            return new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r * (ClericLightFx.Deep ? 1f : Squash));
        }

        /// <summary>World point of picture offset `off` (x = screen right, y = up) from the ground point `anchor` on a 2.5D / 3D map.</summary>
        public static Vector3 PicturePoint(Vector2 anchor, Vector2 off)
        {
            Quaternion q = View25D.Tilt;
            Vector3 p = new Vector3(anchor.x, anchor.y, 0f) + q * new Vector3(off.x, off.y * PictureRise, 0f);
            if (Map3D.Active) p += q * new Vector3(0f, 0f, -Map3DPush);
            return p;
        }

        /// <summary>Particle position for a point the builders give as anchor + picture offset (flat 2D view: the point itself).</summary>
        public Vector3 ParticlePos(Vector2 pos)
        {
            if (!ClericLightFx.Deep) return new Vector3(pos.x, pos.y, 0f);
            Vector2 a = Anchor;
            return PicturePoint(a, pos - a);
        }

        /// <summary>Particle velocity given in picture terms (x right, y up).</summary>
        public Vector3 ParticleVel(Vector2 v)
        {
            if (!ClericLightFx.Deep) return new Vector3(v.x, v.y, 0f);
            return View25D.Tilt * new Vector3(v.x, v.y * PictureRise, 0f);
        }

        void Place(Layer l, Vector2 at, float moved, bool deep, Quaternion tilt)
        {
            Vector2 plane = l.Offset + l.Move * moved;
            float up = l.Up0 + l.UpMove * moved;
            Transform t = l.R.transform;
            if (!deep) { t.position = new Vector3(at.x + plane.x, at.y + plane.y + up, 0f); return; }
            if (l.Band == Band.Ground || l.Floor)
                t.position = new Vector3(at.x + plane.x, at.y + plane.y, 0f) + tilt * new Vector3(0f, up * PictureRise, 0f);
            else
                t.position = PicturePoint(at, plane + new Vector2(0f, up));
        }

        /// <summary>Quad height on a 2.5D / 3D map: ground ellipses become circles, standing pictures keep the default-camera height.</summary>
        static float DeepHeight(Layer l, float w, float h)
        {
            if (l.Band == Band.Ground) return !l.Turned && h < w ? w : h;
            return h * PictureRise;
        }

        void Turn(Layer l, bool deep, Quaternion tilt)
        {
            bool spun = l.Spin != 0f || l.Turned;
            Quaternion local = spun ? Quaternion.Euler(0f, 0f, l.Rot + l.Spin * age / 1000f - (deep && l.Floor && l.Turned ? View25D.YawDeg : 0f)) : Quaternion.identity;
            if (!deep) { if (spun) l.R.transform.localRotation = local; return; }
            if (l.Band == Band.Ground) l.R.transform.localRotation = local;
            else l.R.transform.rotation = tilt * local; // camera-facing billboard (turns with the camera yaw / pitch like the heroes)
        }
    }
}
