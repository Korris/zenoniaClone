using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Fix toàn bộ Player Animator: tạo lại Blend Trees cho Idle/Walk/Attack + transitions.
/// Chạy: menu Tools > Fix ALL Player Animator
/// </summary>
public static class FixAllPlayerAnimator
{
    [MenuItem("Tools/Fix ALL Player Animator")]
    static void FixAll()
    {
        const string controllerPath = "Assets/Animation/Player.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"Controller not found at {controllerPath}");
            return;
        }

        // Tìm tất cả clips cần thiết
        var clips = new Dictionary<string, AnimationClip>();
        string[] needed = {
            "IdleDown", "IdleUp", "IdleLeft", "IdleRight",
            "WalkDown", "WalkLeft",
            "AttackDown", "AttackUp", "AttackLeft", "AttackRight"
        };

        foreach (string name in needed)
        {
            var clip = FindClip(name);
            if (clip != null)
            {
                clips[name] = clip;
                Debug.Log($"Found clip: {name}");
            }
            else
            {
                Debug.LogWarning($"Clip NOT found: {name} (sẽ skip)");
            }
        }

        var sm = controller.layers[0].stateMachine;

        // === Xóa tất cả states cũ ===
        var statesToRemove = new List<AnimatorState>();
        foreach (var s in sm.states)
            statesToRemove.Add(s.state);
        foreach (var s in statesToRemove)
            sm.RemoveState(s);

        Debug.Log($"Removed {statesToRemove.Count} old states");

        // === Tạo IDLE Blend Tree ===
        AnimatorState idleState = sm.AddState("Idle", new Vector3(300, 0, 0));
        sm.defaultState = idleState;
        var idleBT = CreateDirectionalBlendTree(controller, "Idle Blend",
            clips.GetValueOrDefault("IdleDown"),
            clips.GetValueOrDefault("IdleUp"),
            clips.GetValueOrDefault("IdleLeft"),
            clips.GetValueOrDefault("IdleRight"));
        idleState.motion = idleBT;
        Debug.Log("Created Idle Blend Tree");

        // === Tạo WALK Blend Tree ===
        AnimatorState walkState = sm.AddState("Walk", new Vector3(300, 100, 0));
        var walkDown = clips.GetValueOrDefault("WalkDown");
        var walkLeft = clips.GetValueOrDefault("WalkLeft");
        // WalkUp và WalkRight có thể không có → dùng lại WalkDown/WalkLeft
        var walkUp = FindClip("WalkUp") ?? walkDown;
        var walkRight = FindClip("WalkRight") ?? walkLeft;
        var walkBT = CreateDirectionalBlendTree(controller, "Walk Blend",
            walkDown, walkUp, walkLeft, walkRight);
        walkState.motion = walkBT;
        Debug.Log("Created Walk Blend Tree");

        // === Tạo ATTACK Blend Tree ===
        AnimatorState attackState = sm.AddState("Attack", new Vector3(550, 50, 0));
        var atkBT = CreateDirectionalBlendTree(controller, "Attack Blend",
            clips.GetValueOrDefault("AttackDown"),
            clips.GetValueOrDefault("AttackUp"),
            clips.GetValueOrDefault("AttackLeft"),
            clips.GetValueOrDefault("AttackRight"));
        attackState.motion = atkBT;
        Debug.Log("Created Attack Blend Tree");

        // === Tạo Transitions ===
        // Idle → Walk (isMoving = true)
        var t1 = idleState.AddTransition(walkState);
        t1.hasExitTime = false; t1.duration = 0;
        t1.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        // Walk → Idle (isMoving = false)
        var t2 = walkState.AddTransition(idleState);
        t2.hasExitTime = false; t2.duration = 0;
        t2.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        // Idle → Attack (isAttacking = true)
        var t3 = idleState.AddTransition(attackState);
        t3.hasExitTime = false; t3.duration = 0;
        t3.AddCondition(AnimatorConditionMode.If, 0, "isAttacking");

        // Walk → Attack (isAttacking = true)
        var t4 = walkState.AddTransition(attackState);
        t4.hasExitTime = false; t4.duration = 0;
        t4.AddCondition(AnimatorConditionMode.If, 0, "isAttacking");

        // Attack → Idle (isAttacking = false, chờ animation xong)
        var t5 = attackState.AddTransition(idleState);
        t5.hasExitTime = true; t5.exitTime = 0.9f; t5.duration = 0.05f;
        t5.AddCondition(AnimatorConditionMode.IfNot, 0, "isAttacking");

        Debug.Log("Created 5 transitions: Idle↔Walk, Idle→Attack, Walk→Attack, Attack→Idle");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("=== DONE! All Player Animator states rebuilt ===");
    }

    static BlendTree CreateDirectionalBlendTree(AnimatorController controller,
        string name, AnimationClip down, AnimationClip up, AnimationClip left, AnimationClip right)
    {
        var bt = new BlendTree
        {
            name = name,
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "moveX",
            blendParameterY = "moveY"
        };

        AssetDatabase.AddObjectToAsset(bt, controller);

        if (down  != null) bt.AddChild(down,  new Vector2( 0, -1));
        if (up    != null) bt.AddChild(up,    new Vector2( 0,  1));
        if (left  != null) bt.AddChild(left,  new Vector2(-1,  0));
        if (right != null) bt.AddChild(right, new Vector2( 1,  0));

        return bt;
    }

    static AnimationClip FindClip(string name)
    {
        // Tìm trong Assets/Animation
        string[] guids = AssetDatabase.FindAssets($"{name} t:AnimationClip", new[] { "Assets/Animation" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && clip.name == name)
                return clip;
        }

        // Fallback: tìm toàn bộ Assets
        guids = AssetDatabase.FindAssets($"{name} t:AnimationClip");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && clip.name == name)
                return clip;
        }

        return null;
    }
}
