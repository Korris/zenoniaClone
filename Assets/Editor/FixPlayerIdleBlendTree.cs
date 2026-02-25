using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Editor script: fix Idle Blend Tree thành 2D Simple Directional (moveX + moveY)
/// Chạy: menu Tools > Fix Player Idle Blend Tree
/// </summary>
public static class FixPlayerIdleBlendTree
{
    [MenuItem("Tools/Fix Player Idle Blend Tree")]
    static void Fix()
    {
        const string controllerPath = "Assets/Animation/Player.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"AnimatorController not found at {controllerPath}");
            return;
        }

        var stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = null;

        foreach (var s in stateMachine.states)
        {
            if (s.state.name == "Idle")
            {
                idleState = s.state;
                break;
            }
        }

        if (idleState == null)
        {
            Debug.LogError("Idle state not found in Animator!");
            return;
        }

        var blendTree = idleState.motion as BlendTree;
        if (blendTree == null)
        {
            Debug.LogError("Idle state is not a Blend Tree!");
            return;
        }

        // Chuyển sang 2D Simple Directional với cả moveX và moveY
        blendTree.blendType = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter = "moveX";
        blendTree.blendParameterY = "moveY";

        // Set vị trí 2D cho từng child dựa theo tên
        var children = blendTree.children;
        for (int i = 0; i < children.Length; i++)
        {
            string name = children[i].motion != null ? children[i].motion.name : "";

            if (name.Contains("Down"))
                children[i].position = new Vector2(0, -1);
            else if (name.Contains("Up"))
                children[i].position = new Vector2(0, 1);
            else if (name.Contains("Left"))
                children[i].position = new Vector2(-1, 0);
            else if (name.Contains("Right"))
                children[i].position = new Vector2(1, 0);
            else
                Debug.LogWarning($"Unknown idle child: '{name}' at index {i}");
        }

        blendTree.children = children;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("Done! Idle blend tree → 2D Simple Directional (moveX + moveY)");
    }
}
