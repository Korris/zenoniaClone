using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Evaluates a HeroRigSet anim at a continuous phase and runs the forward kinematics (TEST rig, HeroRigSet).
    /// Interpolation per segment (the starting key's `e`): 0 Catmull-Rom through the neighbour keys (smooth velocity, loops wrap),
    /// 1 linear, 2 ease-in, 3 ease-out, 4 hold, 5 in-out, 6 back-out (overshoot then settle; the mesh rig). An anim's bone lag (fraction of the cycle) samples that bone that much later: cape / head
    /// follow-through without extra keys. Output per bone: position (cell px, y down) and angle (screen degrees clockwise), own scale.
    /// </summary>
    public sealed class HeroRigPose
    {
        public readonly Vector2[] Pos;
        public readonly float[] Angle;
        public readonly Vector2[] Scale;
        public bool WeaponBehind;
        public float WristAngle;
        readonly HeroRigSet set;
        readonly float[] a, x, y, sx, sy;
        readonly float[] ch = new float[5];

        public HeroRigPose(HeroRigSet rig)
        {
            set = rig;
            int n = rig.Bones.Length;
            Pos = new Vector2[n]; Angle = new float[n]; Scale = new Vector2[n];
            a = new float[n]; x = new float[n]; y = new float[n]; sx = new float[n]; sy = new float[n];
        }

        /// <summary>Poses every bone for `anim` at phase (0..1; loops wrap, one-shots clamp).</summary>
        public void Evaluate(HeroRigSet.Anim anim, float phase)
        {
            HeroRigSet.Bone[] bones = set.Bones;
            for (int i = 0; i < bones.Length; i++)
            {
                float p = phase - Lag(anim, bones[i].name);
                p = anim.loop ? p - Mathf.Floor(p) : Mathf.Clamp01(p);
                Sample(anim, bones[i].name, p, i);
            }
            float q = anim.loop ? phase - Mathf.Floor(phase) : Mathf.Clamp01(phase);
            int k0 = Segment(anim, q);
            WeaponBehind = anim.keys[k0].wb;
            WristAngle = SampleWrist(anim, q);
            // forward kinematics, parents first (HeroRigSet checks the order)
            for (int i = 0; i < bones.Length; i++)
            {
                int pi = set.BoneParent[i];
                Vector2 off = new Vector2(x[i], y[i]);
                if (pi < 0) { Pos[i] = new Vector2(bones[i].x, bones[i].y) + off; Angle[i] = a[i]; }
                else
                {
                    Vector2 local = new Vector2(bones[i].x - bones[pi].x, bones[i].y - bones[pi].y) + off;
                    Pos[i] = Pos[pi] + Rotate(local, Angle[pi]);
                    Angle[i] = Angle[pi] + a[i];
                }
                Scale[i] = new Vector2(sx[i], sy[i]);
            }
        }

        /// <summary>A rest-pose cell point carried by bone b to its posed place (cell px).</summary>
        public Vector2 Carry(int b, float restX, float restY)
        {
            HeroRigSet.Bone bone = set.Bones[b];
            return Pos[b] + Rotate(new Vector2(restX - bone.x, restY - bone.y), Angle[b]);
        }

        /// <summary>Clockwise on screen in the y-down cell.</summary>
        public static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        internal static float Lag(HeroRigSet.Anim anim, string bone)
        {
            if (anim.lags != null) foreach (HeroRigSet.BoneLag l in anim.lags) if (l.n == bone) return l.v;
            return 0f;
        }

        internal static int Segment(HeroRigSet.Anim anim, float p)
        {
            HeroRigSet.Key[] k = anim.keys;
            int i = 0;
            while (i + 1 < k.Length && k[i + 1].t <= p) i++;
            return i;
        }

        internal static float SampleWrist(HeroRigSet.Anim anim, float p)
        {
            HeroRigSet.Key[] k = anim.keys;
            int i = Segment(anim, p);
            int j = i + 1 < k.Length ? i + 1 : (anim.loop ? 0 : i);
            float u = Fraction(anim, i, j, p);
            return Ease(k[i].e, u, Neighbour(anim, i - 1).wa, k[i].wa, k[j].wa, Neighbour(anim, j + 1).wa);
        }

        void Sample(HeroRigSet.Anim anim, string bone, float p, int bi)
        {
            SampleBone(anim, bone, p, ch);
            a[bi] = ch[0]; x[bi] = ch[1]; y[bi] = ch[2]; sx[bi] = ch[3]; sy[bi] = ch[4];
        }

        /// <summary>Channels of `bone` at phase p (loop-wrapped / clamped by the caller) into c: angle, x, y, sx, sy.</summary>
        internal static void SampleBone(HeroRigSet.Anim anim, string bone, float p, float[] c)
        {
            HeroRigSet.Key[] k = anim.keys;
            int i = Segment(anim, p);
            int j = i + 1 < k.Length ? i + 1 : (anim.loop ? 0 : i);
            float u = Fraction(anim, i, j, p);
            HeroRigSet.BoneKey m = Find(Neighbour(anim, i - 1), bone), b0 = Find(k[i], bone), b1 = Find(k[j], bone), n = Find(Neighbour(anim, j + 1), bone);
            int e = k[i].e;
            for (int ci = 0; ci < 5; ci++) c[ci] = Ease(e, u, V(m, ci), V(b0, ci), V(b1, ci), V(n, ci));
        }

        static float Fraction(HeroRigSet.Anim anim, int i, int j, float p)
        {
            if (i == j) return 0f;
            float t0 = anim.keys[i].t, t1 = anim.keys[j].t;
            if (j < i) t1 += 1f; // wrap segment of a loop
            if (p < t0) p += 1f;
            return t1 > t0 ? Mathf.Clamp01((p - t0) / (t1 - t0)) : 0f;
        }

        static HeroRigSet.Key Neighbour(HeroRigSet.Anim anim, int i)
        {
            int n = anim.keys.Length;
            if (anim.loop) return anim.keys[((i % n) + n) % n];
            return anim.keys[Mathf.Clamp(i, 0, n - 1)];
        }

        static HeroRigSet.BoneKey Find(HeroRigSet.Key k, string bone)
        {
            if (k.b != null) foreach (HeroRigSet.BoneKey b in k.b) if (b.n == bone) return b;
            return null;
        }

        /// <summary>Channel c of a bone key (0 angle, 1 x, 2 y, 3 sx, 4 sy); a bone missing from a key is at rest.</summary>
        static float V(HeroRigSet.BoneKey b, int c)
        {
            if (b == null) return c >= 3 ? 1f : 0f;
            return c == 0 ? b.a : c == 1 ? b.x : c == 2 ? b.y : c == 3 ? b.sx : b.sy;
        }

        internal static float Ease(int e, float u, float pm, float p0, float p1, float pn)
        {
            switch (e)
            {
                case 1: return Mathf.Lerp(p0, p1, u);
                case 2: return Mathf.Lerp(p0, p1, u * u);
                case 3: return Mathf.Lerp(p0, p1, 1f - (1f - u) * (1f - u));
                case 4: return p0;
                case 5: return Mathf.Lerp(p0, p1, u * u * (3f - 2f * u));
                case 6: { float v = u - 1f; return Mathf.LerpUnclamped(p0, p1, 1f + 2.70158f * v * v * v + 1.70158f * v * v); } // back-out: overshoot, settle
                default: // Catmull-Rom
                    float u2 = u * u, u3 = u2 * u;
                    return 0.5f * (2f * p0 + (-pm + p1) * u + (2f * pm - 5f * p0 + 4f * p1 - pn) * u2 + (-pm + 3f * p0 - 3f * p1 + pn) * u3);
            }
        }
    }
}
