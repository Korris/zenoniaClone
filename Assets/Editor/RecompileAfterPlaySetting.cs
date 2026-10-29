using UnityEditor;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// The game is built entirely at runtime, so a script recompile in the middle of Play mode leaves
    /// objects with null references (endless NullReferenceExceptions). Ask the Editor to recompile only
    /// after Play mode ends ("Script Changes While Playing: Recompile After Finished Playing").
    /// </summary>
    [InitializeOnLoad]
    static class RecompileAfterPlaySetting
    {
        const string Key = "ScriptCompilationDuringPlay";
        const int RecompileAfterFinishedPlaying = 1;

        static RecompileAfterPlaySetting()
        {
            if (EditorPrefs.GetInt(Key, 0) != RecompileAfterFinishedPlaying)
                EditorPrefs.SetInt(Key, RecompileAfterFinishedPlaying);
        }
    }
}
