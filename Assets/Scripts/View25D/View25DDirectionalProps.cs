using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Props drawn from several sides like the heroes' 8-way sheets (user 25/11: "nhân vật sprite mà làm 3D được, đồ vật thì không?"). On the 3D map
    /// a prop with side art stands up and shows the view drawn for the camera's side: &lt;type&gt;.png = front (seen from the south), _se, _e, _ne, _n
    /// (its back), _nw, _w, _sw = seen from those sides (8 views; 4 views = without the diagonals). Same px per metre as the front
    /// (Tools/cut-prop-4-views.py). Round 2 (user: "xoay ghế vậy nhìn kì quá"): the card no longer turns with the camera. Each view hangs on a card
    /// FIXED at the yaw it was drawn for, so between two views it foreshortens like a real object, and the art only changes at the half-way yaw.
    /// </summary>
    public static class View25DDirectionalProps
    {
        static readonly string[] Suffix8 = { "", "_se", "_e", "_ne", "_n", "_nw", "_w", "_sw" }; // camera yaw 0, 45, ... 315: camera south, south-east, ...
        struct Entry { public SpriteRenderer R; public Sprite[] Views; }
        static readonly List<Entry> Live = new List<Entry>(64);
        static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();
        static int applied = -1;
        static float appliedTilt = -1f;

        /// <summary>The views of a prop type (8, or 4 = front / east / back / west), or null without side art.</summary>
        public static Sprite[] Views(string type)
        {
            Sprite[] v;
            if (Cache.TryGetValue(type, out v)) return v;
            v = null;
            string root = "Sprites/village/props/" + type;
            if (Resources.Load<Texture2D>(root + "_n") != null)
            {
                bool eight = Resources.Load<Texture2D>(root + "_se") != null;
                v = new Sprite[eight ? 8 : 4];
                for (int i = 0; i < v.Length; i++) v[i] = MapScenery.PropSprite(type + Suffix8[eight ? i : i * 2]);
                if (System.Array.IndexOf(v, null) >= 0) v = null;
            }
            Cache[type] = v;
            return v;
        }

        /// <summary>Registers a stood-up prop renderer: taken off the camera-facing list, it shows the view of the camera's side on a fixed card.</summary>
        public static void Register(SpriteRenderer sr, Sprite[] views)
        {
            View25D.UnregisterStanding(sr.transform);
            Live.Add(new Entry { R = sr, Views = views });
            Apply(Live[Live.Count - 1], View25D.YawDeg);
        }

        static void Apply(Entry e, float yaw)
        {
            int n = e.Views.Length, k = ((Mathf.RoundToInt(yaw / (360f / n)) % n) + n) % n;
            e.R.sprite = e.Views[k];
            e.R.transform.rotation = Quaternion.AngleAxis(k * 360f / n, Vector3.forward) * Quaternion.Euler(-View25D.TiltDeg, 0f, 0f); // the view's own yaw, the camera's tilt
        }

        /// <summary>Per frame (View25D.TickStanding, when the camera turned): the view of the current side, on its card.</summary>
        public static void Tick()
        {
            if (Live.Count == 0) return;
            float yaw = View25D.YawDeg, tilt = View25D.TiltDeg;
            int k = Mathf.RoundToInt(yaw / 22.5f);
            if (k == applied && Mathf.Abs(tilt - appliedTilt) < 0.01f) return;
            applied = k; appliedTilt = tilt;
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (Live[i].R == null) { Live.RemoveAt(i); continue; } // map rebuilt
                Apply(Live[i], yaw);
            }
        }

        public static void Clear() { Live.Clear(); applied = -1; appliedTilt = -1f; }
    }
}
