using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// `--fps-log` (test builds): turns vsync and the frame cap off and logs the average frame rate, the average and worst frame time every 5 s,
    /// e.g. "[ZenoniaProto] FPS 143.2 avg 6.98 ms worst 14.1 ms view25d=True heroes=40". Used to compare the 2.5D prototype with the 2D view.
    /// </summary>
    public class FpsLog : MonoBehaviour
    {
        const float Window = 5f;
        int frames;
        float time, worst;

        public static void AttachIfRequested(GameObject root)
        {
            if (ReleaseBuild.IsRelease || Array.IndexOf(Environment.GetCommandLineArgs(), "--fps-log") < 0) return;
            root.AddComponent<FpsLog>();
        }

        void Update()
        {
            if (QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0; // re-applied: capture roles may set their own cap
            if (Application.targetFrameRate != -1) Application.targetFrameRate = -1;
            float dt = Time.unscaledDeltaTime;
            frames++;
            time += dt;
            worst = Mathf.Max(worst, dt);
            if (time < Window) return;
            Zone z = Zone.Main;
            Debug.Log("[ZenoniaProto] FPS " + (frames / time).ToString("F1") + " avg " + (time * 1000f / frames).ToString("F2") + " ms worst " + (worst * 1000f).ToString("F1")
                + " ms view25d=" + View25D.Active + " heroes=" + (z != null ? z.Heroes.All.Count : 0) + " monsters=" + (z != null ? z.Monsters.Count : 0));
            frames = 0; time = 0f; worst = 0f;
        }
    }
}
