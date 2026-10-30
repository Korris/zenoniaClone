using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>Hero animation selection (mirrors the web player-renderer heroFrame priorities).</summary>
    public partial class HeroController
    {
        static int ByProgress(int n, float p) { return Mathf.Min(n - 1, Mathf.FloorToInt(Mathf.Clamp01(p) * n)); }

        void Render()
        {
            if (view == null) return; // headless server hero: nothing to draw
            string anim;
            int frame;
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
            else if (CastPose(out anim, out frame)) { }
            else if (dashLeft > 0f)
            {
                anim = "dash";
                frame = Mathf.FloorToInt(animTimer / 55f) % Mathf.Max(1, view.FrameCount(anim, facing));
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
                frame = Mathf.FloorToInt(animTimer / (560f / n)) % n; // one full step cycle = 560 ms regardless of frame count
            }
            else
            {
                anim = "idle";
                frame = Mathf.FloorToInt(idleTimer / 200f) % Mathf.Max(1, view.FrameCount(anim, facing));
            }
            view.Show(anim, facing, frame, GameConfig.OrderBase(y), 1f);
        }
    }
}
