using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Feel pass (19/11): one "Locomotion" 1D blend tree (walk at WalkSpeed, run at RunSpeed, parameter "Speed" = ground speed) instead of three
/// states cross-faded on/off. Each clip's playback rate is set so its feet match the ground: the clip's natural speed is measured from the
/// planted foot (in-place clips: while a foot is in its lowest band it slides backwards at exactly the speed the body should move forwards).
/// The state's speed multiplier "MoveRate" (HeroController3D) slows the cadence while the hero accelerates from or brakes to a stop.
/// </summary>
public static partial class ProtoBuilder
{
    static void AddLocomotion(AnimatorController ctrl, GameObject prefab, float height, float walkSpeed, float runSpeed)
    {
        var sm = ctrl.layers[0].stateMachine;
        AnimationClip Find(string key) => sm.states.Select(s => s.state.motion as AnimationClip).Where(c => c != null && Has(c.name, key) && !Has(c.name, "relax")).OrderBy(c => c.name.Length).FirstOrDefault();
        var walk = Find("walk"); var run = Find("run");
        if (walk == null || run == null) { Debug.LogWarning("[proto] locomotion: no walk / run clip, keeping separate states"); return; }

        float walkNat = NaturalSpeed(prefab, walk, height), runNat = NaturalSpeed(prefab, run, height);
        float walkRate = Mathf.Clamp(walkSpeed / walkNat, 0.5f, 3.6f), runRate = Mathf.Clamp(runSpeed / runNat, 0.5f, 3.6f);   // past ~x3.6 the legs blur
        Debug.Log($"[proto] locomotion: walk natural {walkNat:0.00} m/s -> x{walkRate:0.00} at {walkSpeed}, run natural {runNat:0.00} m/s -> x{runRate:0.00} at {runSpeed}");

        ctrl.AddParameter(new AnimatorControllerParameter { name = HeroController3D.SpeedParam, type = AnimatorControllerParameterType.Float, defaultFloat = walkSpeed });
        ctrl.AddParameter(new AnimatorControllerParameter { name = HeroController3D.RateParam, type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        var state = ctrl.CreateBlendTreeInController(HeroController3D.LocoState, out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = HeroController3D.SpeedParam; tree.useAutomaticThresholds = false;
        tree.AddChild(walk, walkSpeed); tree.AddChild(run, runSpeed);
        var kids = tree.children; kids[0].timeScale = walkRate; kids[1].timeScale = runRate; tree.children = kids;
        state.speedParameterActive = true; state.speedParameter = HeroController3D.RateParam;
    }

    /// <summary>Ground speed (m/s at `height` metres tall) of an in-place loop: mean horizontal speed of whichever foot is in the lowest 12 %
    /// of its height range, sampled at 120 Hz. Falls back to 1.5 m/s if the foot bones are missing.</summary>
    static float NaturalSpeed(GameObject prefab, AnimationClip clip, float height)
    {
        var go = Object.Instantiate(prefab);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        try
        {
            var all = go.GetComponentsInChildren<Transform>();
            var feet = new[] { all.FirstOrDefault(t => t.name.EndsWith("LeftFoot")), all.FirstOrDefault(t => t.name.EndsWith("RightFoot")) };
            if (feet.Any(f => f == null)) return 1.5f;
            clip.SampleAnimation(go, 0f);
            float modelHeight = Bounds(go).size.y;
            int n = Mathf.Max(8, Mathf.RoundToInt(clip.length * 120f));
            float dt = clip.length / n;
            var pos = new Vector3[2, n + 1];
            for (int i = 0; i <= n; i++) { clip.SampleAnimation(go, i * dt); for (int f = 0; f < 2; f++) pos[f, i] = feet[f].position; }
            float dist = 0f, time = 0f;
            for (int f = 0; f < 2; f++)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                for (int i = 0; i <= n; i++) { lo = Mathf.Min(lo, pos[f, i].y); hi = Mathf.Max(hi, pos[f, i].y); }
                float band = lo + (hi - lo) * 0.12f;
                for (int i = 0; i < n; i++)
                    if (pos[f, i].y <= band && pos[f, i + 1].y <= band)
                    {
                        Vector3 d = pos[f, i + 1] - pos[f, i]; d.y = 0f;
                        dist += d.magnitude; time += dt;
                    }
            }
            if (time <= 0f || modelHeight <= 0f) return 1.5f;
            return Mathf.Max(0.3f, dist / time * height / modelHeight);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
