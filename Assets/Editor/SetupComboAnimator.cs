using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Setup Attack thành 3 sub-states (combo) dùng chung animation nhưng speed khác nhau.
/// Chạy: Tools > Setup Combo Animator
/// </summary>
public static class SetupComboAnimator
{
    [MenuItem("Tools/Setup Combo Animator")]
    static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/Player.controller");
        if (controller == null) { Debug.LogError("Player.controller not found!"); return; }

        // Ensure parameters
        EnsureParam(controller, "isAttacking", AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "comboStep", AnimatorControllerParameterType.Int);
        EnsureParam(controller, "moveX", AnimatorControllerParameterType.Float);
        EnsureParam(controller, "moveY", AnimatorControllerParameterType.Float);
        EnsureParam(controller, "isMoving", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Tìm Idle, Walk states (giữ nguyên)
        AnimatorState idle = null, walk = null;
        var toRemove = new List<AnimatorState>();
        foreach (var s in sm.states)
        {
            if (s.state.name == "Idle") idle = s.state;
            else if (s.state.name == "Walk") walk = s.state;
            else toRemove.Add(s.state); // xóa Attack states cũ
        }
        foreach (var s in toRemove) sm.RemoveState(s);

        // Tìm attack clips
        var atkDown = FindClip("AttackDown"); var atkUp = FindClip("AttackUp");
        var atkLeft = FindClip("AttackLeft"); var atkRight = FindClip("AttackRight");

        // Tạo 3 Attack states với speed khác nhau
        float[] speeds = { 1.0f, 1.3f, 0.8f }; // đòn 1 thường, đòn 2 nhanh, đòn 3 chậm+mạnh
        var attackStates = new AnimatorState[3];

        for (int i = 0; i < 3; i++)
        {
            var state = sm.AddState($"Attack{i + 1}", new Vector3(550, i * 70, 0));
            var bt = new BlendTree
            {
                name = $"Attack{i + 1} Blend",
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = "moveX",
                blendParameterY = "moveY"
            };
            AssetDatabase.AddObjectToAsset(bt, controller);
            if (atkDown != null) bt.AddChild(atkDown, new Vector2(0, -1));
            if (atkUp != null) bt.AddChild(atkUp, new Vector2(0, 1));
            if (atkLeft != null) bt.AddChild(atkLeft, new Vector2(-1, 0));
            if (atkRight != null) bt.AddChild(atkRight, new Vector2(1, 0));

            state.motion = bt;
            state.speed = speeds[i];
            attackStates[i] = state;
        }

        // Xóa transitions cũ từ Idle và Walk
        if (idle != null) idle.transitions = new AnimatorStateTransition[0];
        if (walk != null) walk.transitions = new AnimatorStateTransition[0];

        // Idle ↔ Walk
        var t = idle.AddTransition(walk);
        t.hasExitTime = false; t.duration = 0;
        t.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        t = walk.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        // Idle/Walk → Attack1/2/3 (dựa vào comboStep)
        for (int i = 0; i < 3; i++)
        {
            // Idle → Attack[i]
            t = idle.AddTransition(attackStates[i]);
            t.hasExitTime = false; t.duration = 0;
            t.AddCondition(AnimatorConditionMode.If, 0, "isAttacking");
            t.AddCondition(AnimatorConditionMode.Equals, i, "comboStep");

            // Walk → Attack[i]
            t = walk.AddTransition(attackStates[i]);
            t.hasExitTime = false; t.duration = 0;
            t.AddCondition(AnimatorConditionMode.If, 0, "isAttacking");
            t.AddCondition(AnimatorConditionMode.Equals, i, "comboStep");
        }

        // Attack1 → Attack2, Attack2 → Attack3 (combo chain)
        t = attackStates[0].AddTransition(attackStates[1]);
        t.hasExitTime = false; t.duration = 0;
        t.AddCondition(AnimatorConditionMode.Equals, 1, "comboStep");
        t.AddCondition(AnimatorConditionMode.If, 0, "isAttacking");

        t = attackStates[1].AddTransition(attackStates[2]);
        t.hasExitTime = false; t.duration = 0;
        t.AddCondition(AnimatorConditionMode.Equals, 2, "comboStep");
        t.AddCondition(AnimatorConditionMode.If, 0, "isAttacking");

        // All Attack → Idle (khi isAttacking = false)
        for (int i = 0; i < 3; i++)
        {
            t = attackStates[i].AddTransition(idle);
            t.hasExitTime = true; t.exitTime = 0.85f; t.duration = 0.05f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "isAttacking");
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Done! Combo: Attack1(1x) → Attack2(1.3x) → Attack3(0.8x)");
    }

    static void EnsureParam(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in c.parameters) if (p.name == name) return;
        c.AddParameter(name, type);
    }

    static AnimationClip FindClip(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{name} t:AnimationClip", new[] { "Assets/Animation" }))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (clip != null && clip.name == name) return clip;
        }
        return null;
    }
}
