using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Fix tất cả transitions trong Player Animator Controller.
/// Chạy: menu Tools > Fix Player Animator Transitions
/// </summary>
public static class FixPlayerAnimatorTransitions
{
    [MenuItem("Tools/Fix Player Animator Transitions")]
    static void Fix()
    {
        const string controllerPath = "Assets/Animation/Player.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"AnimatorController not found at {controllerPath}");
            return;
        }

        var sm = controller.layers[0].stateMachine;

        // Tìm tất cả states
        AnimatorState idle = null, walk = null, attack = null;
        foreach (var s in sm.states)
        {
            switch (s.state.name)
            {
                case "Idle":   idle   = s.state; break;
                case "Walk":   walk   = s.state; break;
                case "Attack": attack = s.state; break;
            }
        }

        if (idle == null || walk == null || attack == null)
        {
            Debug.LogError($"Missing states! Idle={idle != null} Walk={walk != null} Attack={attack != null}");
            return;
        }

        // Xóa tất cả transitions cũ để tạo lại sạch
        idle.transitions = new AnimatorStateTransition[0];
        walk.transitions = new AnimatorStateTransition[0];
        attack.transitions = new AnimatorStateTransition[0];

        // --- Idle → Walk: khi isMoving = true ---
        var idleToWalk = idle.AddTransition(walk);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0f;
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        // --- Walk → Idle: khi isMoving = false ---
        var walkToIdle = walk.AddTransition(idle);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0f;
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        // --- Idle → Attack: khi isAttacking = true ---
        var idleToAttack = idle.AddTransition(attack);
        idleToAttack.hasExitTime = false;
        idleToAttack.duration = 0f;
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "isAttacking");

        // --- Walk → Attack: khi isAttacking = true ---
        var walkToAttack = walk.AddTransition(attack);
        walkToAttack.hasExitTime = false;
        walkToAttack.duration = 0f;
        walkToAttack.AddCondition(AnimatorConditionMode.If, 0, "isAttacking");

        // --- Attack → Idle: khi isAttacking = false (chờ animation xong) ---
        var attackToIdle = attack.AddTransition(idle);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.05f;
        attackToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isAttacking");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("Done! Transitions fixed: Idle↔Walk, Idle→Attack, Walk→Attack, Attack→Idle");
    }
}
