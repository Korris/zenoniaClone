using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Fix Enemy Animator: tạo Idle + Walk Blend Trees giống Player.
/// Chạy: menu Tools > Fix Enemy Animator
/// Áp dụng cho cả Monster.controller và Monster 1.controller
/// </summary>
public static class FixEnemyAnimator
{
    [MenuItem("Tools/Fix Enemy Animator")]
    static void Fix()
    {
        string[] controllerPaths = {
            "Assets/Script/Enemy/Monster.controller",
            "Assets/Script/Enemy/Monster 1.controller"
        };

        foreach (var path in controllerPaths)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                Debug.LogWarning($"Controller not found: {path}, skipping");
                continue;
            }

            FixController(controller, path);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("=== DONE! Enemy Animators rebuilt ===");
    }

    static void FixController(AnimatorController controller, string path)
    {
        Debug.Log($"--- Fixing: {path} ---");

        // Đảm bảo parameters tồn tại
        EnsureParameter(controller, "moveX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "moveY", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "isMoving", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Xóa tất cả states cũ
        var toRemove = new List<AnimatorState>();
        foreach (var s in sm.states) toRemove.Add(s.state);
        foreach (var s in toRemove) sm.RemoveState(s);
        Debug.Log($"Removed {toRemove.Count} old states");

        // Tìm Enemy clips (trong Assets/Animation/Enemy/)
        var idleDown  = FindEnemyClip("IdleDown");
        var idleUp    = FindEnemyClip("IdleUp");
        var idleLeft  = FindEnemyClip("IdleLeft");
        var idleRight = FindEnemyClip("IdleRight");
        var walkDown  = FindEnemyClip("WalkDown");
        var walkUp    = FindEnemyClip("WalkUp");
        var walkLeft  = FindEnemyClip("WalkLeft");
        var walkRight = FindEnemyClip("WalkRight");

        // === IDLE Blend Tree ===
        var idleState = sm.AddState("Idle", new Vector3(300, 0, 0));
        sm.defaultState = idleState;
        var idleBT = CreateBlendTree(controller, "Enemy Idle Blend",
            idleDown, idleUp, idleLeft, idleRight);
        idleState.motion = idleBT;
        Debug.Log($"Idle BT: down={idleDown != null} up={idleUp != null} left={idleLeft != null} right={idleRight != null}");

        // === WALK Blend Tree ===
        var walkState = sm.AddState("Walk", new Vector3(300, 100, 0));
        var walkBT = CreateBlendTree(controller, "Enemy Walk Blend",
            walkDown, walkUp, walkLeft, walkRight);
        walkState.motion = walkBT;
        Debug.Log($"Walk BT: down={walkDown != null} up={walkUp != null} left={walkLeft != null} right={walkRight != null}");

        // === Transitions ===
        var t1 = idleState.AddTransition(walkState);
        t1.hasExitTime = false; t1.duration = 0;
        t1.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        var t2 = walkState.AddTransition(idleState);
        t2.hasExitTime = false; t2.duration = 0;
        t2.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        EditorUtility.SetDirty(controller);
        Debug.Log($"Done: {path}");
    }

    static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
        Debug.Log($"Added parameter: {name}");
    }

    static BlendTree CreateBlendTree(AnimatorController controller, string name,
        AnimationClip down, AnimationClip up, AnimationClip left, AnimationClip right)
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

    static AnimationClip FindEnemyClip(string name)
    {
        // Tìm trong Assets/Animation/Enemy/ trước
        string[] guids = AssetDatabase.FindAssets($"{name} t:AnimationClip", new[] { "Assets/Animation/Enemy" });
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip != null && clip.name == name)
                return clip;
        }
        return null;
    }
}
