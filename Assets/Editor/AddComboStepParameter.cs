using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Thêm parameter comboStep (Int) vào Player Animator.
/// Chạy: Tools > Add ComboStep Parameter
/// </summary>
public static class AddComboStepParameter
{
    [MenuItem("Tools/Add ComboStep Parameter")]
    static void Add()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/Player.controller");
        if (controller == null) { Debug.LogError("Player.controller not found!"); return; }

        foreach (var p in controller.parameters)
            if (p.name == "comboStep") { Debug.Log("comboStep already exists"); return; }

        controller.AddParameter("comboStep", AnimatorControllerParameterType.Int);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Added comboStep (Int) parameter to Player Animator");
    }
}
