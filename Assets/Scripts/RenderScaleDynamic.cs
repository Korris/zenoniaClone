using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Dynamic resolution controller of RenderScale (22/11 perf pass 2). Fed the real frame time every frame; every 0.5 s it compares the frame rate
    /// of that window with the target (RenderScale.TargetFps: 55 desktop, 30 phones):
    /// - two slow windows in a row (1 s under the target) -> one step (5 %) down, two steps when under 70 % of the target, never below RenderScale.Floor;
    /// - headroom (8 % over the target) held for 3 s -> one step up, never above the ceiling, and only after the back-off since the last drop;
    /// - a drop that comes less than 4 s after a climb means that climb did not fit: the back-off doubles (3 s .. 60 s) so the scale does not
    ///   flicker between two steps at the vsync cap; 30 s without a drop resets it to 3 s.
    /// Frames longer than 0.25 s (map load, alt-tab) are skipped and restart the window.
    /// </summary>
    public sealed class RenderScaleDynamic
    {
        const float Window = 0.5f, UpHold = 3f, Headroom = 1.08f, MinBackoff = 3f, MaxBackoff = 60f;
        float acc, now, lastDrop = -999f, lastRaise = -999f, backoff = MinBackoff, upTime;
        int frames, slow;

        /// <summary>Scale in use (starts at the ceiling).</summary>
        public float Scale = 1f;

        /// <summary>One frame: dt = unscaled frame time; ceiling = RenderScale.Ceiling; on = dynamic resolution switch.</summary>
        public void Tick(float dt, float ceiling, bool on, float target)
        {
            now += Mathf.Min(dt, 0.25f);
            if (!on) { Scale = ceiling; Reset(); return; }
            if (Scale > ceiling) Scale = ceiling;
            if (dt > 0.25f) { Reset(); return; }
            acc += dt; frames++;
            if (acc < Window) return;
            float fps = frames / acc;
            acc = 0f; frames = 0;
            if (fps < target)
            {
                upTime = 0f;
                if (++slow < 2) return;
                slow = 0;
                if (Scale <= RenderScale.Floor + 1e-3f) return;
                Scale = Mathf.Max(RenderScale.Floor, Scale - RenderScale.Step * (fps < target * 0.7f ? 2f : 1f));
                if (now - lastRaise < 4f) backoff = Mathf.Min(MaxBackoff, backoff * 2f); // the last climb did not fit
                lastDrop = now;
                return;
            }
            slow = 0;
            if (now - lastDrop > 30f) backoff = MinBackoff;
            if (fps < target * Headroom) { upTime = 0f; return; }
            upTime += Window;
            if (upTime < UpHold || now - lastDrop < backoff || Scale >= ceiling - 1e-3f) return;
            Scale = Mathf.Min(ceiling, Scale + RenderScale.Step);
            lastRaise = now;
            upTime = 0f;
        }

        void Reset() { acc = 0f; frames = 0; slow = 0; upTime = 0f; }
    }
}
