using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// TEST bone rig (25/11, plans/261125-0045-warrior-skeletal-rig-prototype): with `--hero-rig` (cut-out) or `--hero-rig-mesh`
    /// (skinned mesh, Data/.../rig-mesh-down.json, HeroMeshRigView) a layered hero whose class has
    /// Data/hero-layered/ro-fantasy/&lt;class&gt;/rig-down.json (Warrior only) draws idle / walk / basic attack facing the camera (screen row 0)
    /// with the cut-out rig instead of the painted frames, so the two can be compared in the same build (flag off = frames). Every other
    /// direction, anim (skills, hurt, death, dash) and class keeps the frame art. Visual only: hit timing, lunge and hit-stop are unchanged;
    /// the rig reads a continuous phase (RigPhase, set by HeroController.Render) instead of the stepped frame index.
    /// </summary>
    public partial class HeroRenderer
    {
        static bool? rigEnabled;
        /// <summary>`--hero-rig` on the command line (or set by a capture before heroes spawn).</summary>
        public static bool RigEnabled
        {
            get { if (rigEnabled == null) rigEnabled = Array.IndexOf(Environment.GetCommandLineArgs(), "--hero-rig") >= 0; return rigEnabled.Value; }
            set { rigEnabled = value; }
        }

        static bool? rigMeshEnabled;
        /// <summary>`--hero-rig-mesh`: the skinned mesh rig (2D Animation SpriteSkin, Phase 2) instead of the cut-out one; wins over --hero-rig.</summary>
        public static bool RigMeshEnabled
        {
            get { if (rigMeshEnabled == null) rigMeshEnabled = Array.IndexOf(Environment.GetCommandLineArgs(), "--hero-rig-mesh") >= 0; return rigMeshEnabled.Value; }
            set { rigMeshEnabled = value; }
        }

        /// <summary>Continuous 0..1 phase of the walk cycle / attack swing for this frame (HeroController.Render); negative = unknown (frame / n used).</summary>
        public float RigPhase { get; set; } = -1f;

        bool rigTried;
        const float RigIdleMs = 1600f;

        /// <summary>Draws the rig when it applies (flag, rig data, facing camera, idle / walk / combo step); false = draw the frames.</summary>
        bool TryShowRig(string anim, int dir, int frame, int orderBase, float alpha)
        {
            if (!(RigEnabled || RigMeshEnabled) || layered == null || cls == null) return false;
            if (!rigTried)
            {
                rigTried = true;
                string dirPath = RoFantasyRoot + cls.Name.ToLowerInvariant();
                if (RigMeshEnabled)
                {
                    HeroMeshRigSet m = HeroMeshRigSet.Load(dirPath + "/rig-mesh-down");
                    if (m != null) layered.AttachRig(parent => new HeroMeshRigView(m, parent));
                    else Debug.LogWarning("[ZenoniaProto] --hero-rig-mesh: no mesh rig at " + dirPath + "/rig-mesh-down (frames drawn)");
                }
                else
                {
                    HeroRigSet s = HeroRigSet.Load(dirPath + "/rig-down");
                    if (s != null) layered.AttachRig(parent => new HeroRigView(s, parent));
                }
            }
            if (!layered.HasRig) return false;
            bool combo = cls.ComboAnims != null && Array.IndexOf(cls.ComboAnims, anim) >= 0;
            string rigAnim = anim == "idle" ? "idle" : anim == "walk" ? "walk" : combo ? "attack" : null;
            if (rigAnim == null || SoftRow(dir) != 0) { layered.HideRig(); return false; }
            float phase;
            if (rigAnim == "idle") phase = (Time.time + breathPhase) * 1000f / RigIdleMs;
            else if (RigPhase >= 0f) phase = rigAnim == "walk" ? RigPhase + crowdPhase : RigPhase;
            else
            {
                int n = Mathf.Max(1, layered.Set.Frames(LayeredAnim(anim)));
                phase = (frame + 0.5f) / n;
            }
            shadow.sortingOrder = orderBase;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);
            if (!layered.ShowRig(rigAnim, phase, orderBase, new Color(1f, 1f, 1f, alpha), LayeredWeaponOn, !offhandEmpty)) { layered.HideRig(); return false; }
            return true;
        }
    }
}
