using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Chuyển Attack state thành 2D Blend Tree (moveX + moveY) để chém đúng hướng.
/// Chạy: menu Tools > Fix Player Attack Blend Tree
/// </summary>
public static class FixPlayerAttackBlendTree
{
    [MenuItem("Tools/Fix Player Attack Blend Tree")]
    static void Fix()
    {
        const string controllerPath = "Assets/Animation/Player.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"AnimatorController not found at {controllerPath}");
            return;
        }

        // Tìm animation clips
        var attackDown  = FindClip("AttackDown");
        var attackUp    = FindClip("AttackUp");
        var attackLeft  = FindClip("AttackLeft");
        var attackRight = FindClip("AttackRight");

        if (attackDown == null || attackUp == null || attackLeft == null || attackRight == null)
        {
            Debug.LogError($"Missing clips! Down={attackDown != null} Up={attackUp != null} Left={attackLeft != null} Right={attackRight != null}");
            return;
        }

        var sm = controller.layers[0].stateMachine;
        AnimatorState attackState = null;

        foreach (var s in sm.states)
        {
            if (s.state.name == "Attack")
            {
                attackState = s.state;
                break;
            }
        }

        // Nếu Attack state bị mất, tạo lại
        if (attackState == null)
        {
            attackState = sm.AddState("Attack");
            Debug.Log("Created new Attack state");
        }

        // Tạo BlendTree trực tiếp (không dùng CreateBlendTreeInController)
        var blendTree = new BlendTree
        {
            name = "Attack Blend Tree",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "moveX",
            blendParameterY = "moveY"
        };

        // Lưu BlendTree vào controller asset
        AssetDatabase.AddObjectToAsset(blendTree, controller);

        blendTree.AddChild(attackDown,  new Vector2(0, -1));
        blendTree.AddChild(attackUp,    new Vector2(0,  1));
        blendTree.AddChild(attackLeft,  new Vector2(-1, 0));
        blendTree.AddChild(attackRight, new Vector2( 1, 0));

        // Gán vào Attack state
        attackState.motion = blendTree;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("Done! Attack → 2D Blend Tree (AttackDown/Up/Left/Right)");
        Debug.Log("Hãy chạy lại: Tools > Fix Player Animator Transitions");
    }

    static AnimationClip FindClip(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"{name} t:AnimationClip", new[] { "Assets/Animation" });
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
