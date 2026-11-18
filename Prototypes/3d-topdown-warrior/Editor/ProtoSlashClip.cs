using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds the Warrior's basic attack without any imported clip: one short diagonal slash (right shoulder -> lower left, 0.48 s, hit at 0.22 s)
/// authored as key poses on the spine and arm bones only, played on an "Upper" animator layer (avatar mask) over the looping idle so the
/// legs never crouch. Poses follow the ChatGPT Ragnarok preset: back leans at most ~10°, shoulders turn ~25°, sword arc ~75°, shield held
/// in front of the chest. Key 0 and the last key are the idle pose, so blending in (0.06 s) and out (0.09 s) is seamless.
/// </summary>
public static partial class ProtoBuilder
{
    static readonly string[] UpperBones = { "Spine", "Spine1", "Spine2", "RightShoulder", "RightArm", "RightForeArm", "RightHand", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" };

    /// <summary>Adds the "Upper" layer (Empty default state + Slash state that returns to Empty) with a mask of the upper-body bones.</summary>
    static void AddSlashLayer(AnimatorController ctrl, GameObject prefab, string folder)
    {
        var idle = ctrl.layers[0].stateMachine.states.Select(s => s.state.motion as AnimationClip).First(c => c != null && Has(c.name, "idle") && !Has(c.name, "relax"));
        var clip = SlashClip(prefab, idle, folder + "/Slash.anim");
        var mask = UpperMask(prefab, folder + "/UpperMask.mask");
        var sm = new AnimatorStateMachine { name = "Upper", hideFlags = HideFlags.HideInHierarchy };
        AssetDatabase.AddObjectToAsset(sm, ctrl);
        var empty = sm.AddState("Empty"); sm.defaultState = empty;
        var slash = sm.AddState("Slash"); slash.motion = clip;
        var back = slash.AddTransition(empty);
        back.hasExitTime = true; back.exitTime = 1f - 0.09f / clip.length; back.hasFixedDuration = true; back.duration = 0.09f;
        ctrl.AddLayer(new AnimatorControllerLayer { name = "Upper", stateMachine = sm, avatarMask = mask, defaultWeight = 1f, blendingMode = AnimatorLayerBlendingMode.Override });
    }

    static AvatarMask UpperMask(GameObject prefab, string path)
    {
        var go = Object.Instantiate(prefab);
        var mask = new AvatarMask();
        mask.AddTransformPath(go.transform, true);
        for (int i = 0; i < mask.transformCount; i++)
        {
            string leaf = mask.GetTransformPath(i).Split('/').Last();
            mask.SetTransformActive(i, UpperBones.Any(b => leaf.EndsWith(b)));
        }
        Object.DestroyImmediate(go);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mask, path);
        return mask;
    }

    static AnimationClip SlashClip(GameObject prefab, AnimationClip idle, string path)
    {
        var go = Object.Instantiate(prefab);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        idle.SampleAnimation(go, 0f);
        var all = go.GetComponentsInChildren<Transform>();
        var bones = UpperBones.Select(b => all.First(t => t.name.EndsWith(b))).ToArray();
        var rest = bones.Select(b => b.localRotation).ToArray();
        Transform B(string n) => bones[System.Array.IndexOf(UpperBones, n)];
        var keys = new List<(float t, Quaternion[] rot)>();

        void Key(float t, float yaw, float lean, Vector3? rArm, Vector3? rFore, Vector3? lArm, Vector3? lFore)
        {
            for (int i = 0; i < bones.Length; i++) bones[i].localRotation = rest[i];
            foreach (var s in new[] { "Spine", "Spine1", "Spine2" }) B(s).rotation = Quaternion.AngleAxis(yaw / 3f, Vector3.up) * Quaternion.AngleAxis(lean / 3f, Vector3.right) * B(s).rotation;
            if (rArm.HasValue) Aim(B("RightArm"), B("RightForeArm"), rArm.Value);
            if (rFore.HasValue) Aim(B("RightForeArm"), B("RightHand"), rFore.Value);
            if (lArm.HasValue) Aim(B("LeftArm"), B("LeftForeArm"), lArm.Value);
            if (lFore.HasValue) Aim(B("LeftForeArm"), B("LeftHand"), lFore.Value);
            keys.Add((t, bones.Select(b => b.localRotation).ToArray()));
        }
        // character space: +Z forward, +X right. Directions are world directions of upper arm (shoulder->elbow) and forearm (elbow->wrist).
        Vector3 shieldArm = new Vector3(-0.35f, -0.75f, 0.45f), shieldFore = new Vector3(0.45f, 0.25f, 0.85f);
        Key(0.00f, 0f, 0f, null, null, null, null);                                                                                       // idle
        Key(0.12f, 22f, -3f, new Vector3(0.6f, 0.45f, -0.5f), new Vector3(0.1f, 0.85f, 0.4f), shieldArm, shieldFore);                     // wind-up: sword over the right shoulder
        Key(0.22f, -10f, 8f, new Vector3(-0.25f, -0.4f, 0.85f), new Vector3(-0.5f, -0.55f, 0.65f), shieldArm, shieldFore);              // hit: blade crosses forward-down-left
        Key(0.30f, -16f, 10f, new Vector3(-0.55f, -0.65f, 0.5f), new Vector3(-0.7f, -0.6f, 0.35f), shieldArm, shieldFore);               // follow-through
        Key(0.48f, 0f, 0f, null, null, null, null);                                                                                       // back to idle

        var clip = new AnimationClip { frameRate = 30f };
        for (int i = 0; i < bones.Length; i++)
        {
            string bonePath = AnimationUtility.CalculateTransformPath(bones[i], go.transform);
            string[] comp = { "x", "y", "z", "w" };
            for (int c = 0; c < 4; c++)
            {
                var curve = new AnimationCurve(keys.Select(k => new Keyframe(k.t, k.rot[i][c])).ToArray());
                for (int k = 0; k < curve.length; k++) AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
                for (int k = 0; k < curve.length; k++) AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalRotation." + comp[c]), curve);
            }
        }
        clip.EnsureQuaternionContinuity();
        Object.DestroyImmediate(go);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        Debug.Log("[proto] slash clip " + clip.length.ToString("0.00") + " s, bones " + bones.Length);
        return clip;
    }

    /// <summary>Rotates `bone` (world space) so the segment bone -> child points along `dir`.</summary>
    static void Aim(Transform bone, Transform child, Vector3 dir)
    {
        Vector3 cur = (child.position - bone.position).normalized;
        bone.rotation = Quaternion.FromToRotation(cur, dir.normalized) * bone.rotation;
    }
}
