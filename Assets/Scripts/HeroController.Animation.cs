using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>Hero animation selection (mirrors the web player-renderer heroFrame priorities).</summary>
    public partial class HeroController
    {
        static int ByProgress(int n, float p) { return Mathf.Min(n - 1, Mathf.FloorToInt(Mathf.Clamp01(p) * n)); }

        const string SpinAnim = "melee3";
        const int SpinFrame = 1;              // the sword is held straight out in this frame of every row
        const float SpinMsPerFacing = 70f;
        static readonly int[] SpinOrder = { 2, 1, 3, 0 }; // read backward as the time left counts down: down, right, up, left, one clockwise turn
        float spinLeftMs;

        /// <summary>Plays the spinning pose for this long: view only, the sim, facing and hit boxes are untouched.</summary>
        public void PlaySpinPose(float ms) { spinLeftMs = ms; }

        string skillPoseAnim;
        int skillPoseFrame, skillPoseFacing = -1;
        float skillPoseLift, skillPoseAlpha = 1f, skillPoseSideways;

        /// <summary>A hand-made skill timeline drives the figure this frame: pose frame and height above the ground. View only; the shadow stays on the ground.</summary>
        public void SetSkillPose(string anim, int frame, float lift, int drawFacing = -1) { skillPoseAnim = anim; skillPoseFrame = frame; skillPoseLift = lift; skillPoseFacing = drawFacing; }

        public void ClearSkillPose() { skillPoseAnim = null; skillPoseLift = 0f; skillPoseFacing = -1; skillPoseAlpha = 1f; skillPoseSideways = 0f; }

        /// <summary>How the current skill pose is drawn: figure opacity (0 = dissolved) and a view-only step along x. Cleared with the pose.</summary>
        public void SetSkillPoseLook(float alpha, float sideways) { skillPoseAlpha = alpha; skillPoseSideways = sideways; }

        void Render()
        {
            if (view == null) return; // headless server hero: nothing to draw
            string anim;
            int frame, drawFacing = facing;
            if (spinLeftMs > 0f) spinLeftMs -= GameClock.Dt * 1000f;
            // A client proxy has no combo state machine: pose / step / progress come from the server snapshot.
            bool attacking = proxy ? proxyPose == HeroPose.Attack : combo.IsAttacking;
            int comboStep = proxy ? proxyStep : combo.Step;
            bool finisherStep = proxy ? comboStep == combo.HitCount : combo.IsFinisher;
            float phase = proxy ? proxyProgress : combo.PhaseProgress;
            if (dead)
            {
                anim = "death";
                int n = Mathf.Max(1, view.FrameCount(anim, facing));
                frame = ByProgress(n, deathTimer / (120f * n));
            }
            else if (hurt > 0f)
            {
                anim = "hurt";
                frame = ByProgress(Mathf.Max(1, view.FrameCount(anim, facing)), 1f - hurt / HurtMs);
            }
            else if (skillPoseAnim != null && view.FrameCount(skillPoseAnim, facing) > 0)
            {
                anim = skillPoseAnim;
                frame = skillPoseFrame;
                if (skillPoseFacing >= 0) drawFacing = skillPoseFacing;
            }
            else if (CastPose(out anim, out frame)) { }
            else if (dashLeft > 0f)
            {
                anim = "dash";
                frame = Mathf.FloorToInt(animTimer / 55f) % Mathf.Max(1, view.FrameCount(anim, facing));
            }
            else if (spinLeftMs > 0f)
            {
                // Blade Cyclone: turns through the four facings with the sword held out, while the sim keeps the real facing.
                anim = SpinAnim;
                frame = SpinFrame;
                drawFacing = SpinOrder[Mathf.FloorToInt(spinLeftMs / SpinMsPerFacing) % SpinOrder.Length];
            }
            else if (attacking)
            {
                string[] steps = cls.ComboAnims;
                anim = steps != null && comboStep >= 1 && comboStep <= steps.Length ? steps[comboStep - 1]
                    : finisherStep ? "attack3" : (((comboStep - 1) % 2) + 1 == 1 ? "attack1" : "attack2");
                frame = ByProgress(Mathf.Max(1, view.FrameCount(anim, facing)), phase);
            }
            else if (moving)
            {
                anim = "walk";
                int n = Mathf.Max(1, view.FrameCount(anim, facing));
                frame = Mathf.FloorToInt(animTimer / (cls.WalkCycleMs / n)) % n; // one full step cycle per class, regardless of frame count
            }
            else
            {
                anim = "idle";
                frame = Mathf.FloorToInt(idleTimer / 200f) % Mathf.Max(1, view.FrameCount(anim, facing));
            }
            // Raised over whatever stands in front of the landing line while in the air.
            view.Show(anim, drawFacing, frame, GameConfig.OrderBase(y - skillPoseLift), skillPoseAnim != null && anim == skillPoseAnim ? skillPoseAlpha : 1f);
            view.Lean(anim == "walk", facing, GameClock.Dt);
            view.Lift(skillPoseLift, skillPoseSideways);
        }
    }
}
