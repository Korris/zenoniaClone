using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Evaluates a HeroMeshRigSet anim (TEST mesh rig, `--hero-rig-mesh`); offline mirror: Tools/warrior_rig_mesh_pose.py (keep in step).
    /// Per bone, channels a, x, y, sx, sy at phase - lag (HeroRigPose.SampleBone: same easings incl. 5 in-out, 6 back-out overshoot).
    /// FK: turn = parent turn + a; joint = parent affine(rest joint) + parent turn (x, y); affine of the bone
    ///   M(p) = joint + R(rest axis + turn) * S(sx, sy) * R(-rest axis) * (p - rest joint)
    /// so `sy` foreshortens a limb along its own axis; scale is not inherited (no shear on the children, Sổ lỗi 15/11 Fighter rig).
    /// Springs (cape, hair): turn and length chase the keyed target with a damped spring (2.6 Hz, damping 0.38: lag + overshoot +
    /// settle); the target leans against the joint's own velocity (cloth trails the body, shortens when it drops). dt-based, per hero.
    /// </summary>
    public sealed class HeroMeshRigPose
    {
        const float SpringHz = 2.6f, SpringDamp = 0.38f, TrailDeg = 0.05f, TrailLen = 0.0012f, TrailMax = 12f;
        public readonly Vector2[] Pos;
        public readonly float[] Turn;
        public readonly Vector2[] Scale;
        public bool WeaponBehind, LeftBehind, RightBehind;
        public float WristAngle;
        public string Swaps;
        readonly HeroMeshRigSet set;
        readonly float[] ch = new float[5];
        readonly float[] sTurn, sTurnV, sLen, sLenV;
        readonly Vector2[] sLast;
        bool springReady; // springs keep their state across anim changes (cloth keeps moving); first frame / long gap = snap

        public HeroMeshRigPose(HeroMeshRigSet rig)
        {
            set = rig;
            int n = rig.Bones.Length;
            Pos = new Vector2[n]; Turn = new float[n]; Scale = new Vector2[n];
            sTurn = new float[n]; sTurnV = new float[n]; sLen = new float[n]; sLenV = new float[n]; sLast = new Vector2[n];
        }

        /// <summary>Rest cell point p carried to its posed place by bone b (cell px).</summary>
        public Vector2 Affine(int b, Vector2 p)
        {
            HeroMeshRigSet.Bone bone = set.Bones[b];
            Vector2 q = HeroRigPose.Rotate(new Vector2(p.x - bone.x, p.y - bone.y), -set.RestAxis[b]);
            q = new Vector2(q.x * Scale[b].x, q.y * Scale[b].y);
            return Pos[b] + HeroRigPose.Rotate(q, set.RestAxis[b] + Turn[b]);
        }

        /// <summary>Poses every bone for `anim` at phase (loops wrap, one-shots clamp); dt drives the springs (0 = snap).</summary>
        public void Evaluate(HeroRigSet.Anim anim, float phase, float dt)
        {
            HeroMeshRigSet.Bone[] bones = set.Bones;
            for (int i = 0; i < bones.Length; i++)
            {
                float p = phase - HeroRigPose.Lag(anim, bones[i].name);
                p = anim.loop ? p - Mathf.Floor(p) : Mathf.Clamp01(p);
                HeroRigPose.SampleBone(anim, bones[i].name, p, ch);
                int pi = set.BoneParent[i];
                float turn;
                if (pi < 0) { Pos[i] = new Vector2(bones[i].x + ch[1], bones[i].y + ch[2]); turn = ch[0]; }
                else
                {
                    Pos[i] = Affine(pi, new Vector2(bones[i].x, bones[i].y)) + HeroRigPose.Rotate(new Vector2(ch[1], ch[2]), Turn[pi]);
                    turn = Turn[pi] + ch[0];
                }
                float len = ch[4];
                if (bones[i].spring) Spring(i, ref turn, ref len, dt);
                Turn[i] = turn;
                Scale[i] = new Vector2(ch[3], len);
            }
            springReady = true;
            float q = anim.loop ? phase - Mathf.Floor(phase) : Mathf.Clamp01(phase);
            HeroRigSet.Key k = anim.keys[HeroRigPose.Segment(anim, q)];
            WeaponBehind = k.wb; LeftBehind = k.lb; RightBehind = k.rb; Swaps = k.sw;
            WristAngle = HeroRigPose.SampleWrist(anim, q);
        }

        void Spring(int i, ref float turn, ref float len, float dt)
        {
            if (!springReady || dt <= 0f || dt > 0.25f)
            {
                sTurn[i] = turn; sTurnV[i] = 0f; sLen[i] = len; sLenV[i] = 0f; sLast[i] = Pos[i];
                return;
            }
            Vector2 v = (Pos[i] - sLast[i]) / dt;
            sLast[i] = Pos[i];
            float tt = turn + Mathf.Clamp(TrailDeg * v.x, -TrailMax, TrailMax);
            float lt = len * Mathf.Clamp(1f - TrailLen * v.y, 0.88f, 1.08f);
            float w = 2f * Mathf.PI * SpringHz;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt * 120f));
            float h = dt / steps;
            for (int s = 0; s < steps; s++)
            {
                sTurnV[i] += h * (w * w * (tt - sTurn[i]) - 2f * SpringDamp * w * sTurnV[i]);
                sTurn[i] += h * sTurnV[i];
                sLenV[i] += h * (w * w * (lt - sLen[i]) - 2f * SpringDamp * w * sLenV[i]);
                sLen[i] += h * sLenV[i];
            }
            turn = sTurn[i];
            len = sLen[i];
        }
    }
}
