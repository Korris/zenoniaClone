using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Batchmode standalone build for verification (the project ships no scene; Bootstrap builds everything at runtime):
    ///   Unity -batchmode -quit -projectPath P -executeMethod ZenoniaProto.EditorTools.PlayerBuildScript.BuildWindows -buildOut C:\out\Zenonia.exe
    /// A throw-away empty scene is created for the build and deleted afterwards.
    /// </summary>
    public static class PlayerBuildScript
    {
        const string TempScene = "Assets/_BuildBoot.unity";

        public static void BuildWindows()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-buildOut");
            string output = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Directory.GetCurrentDirectory(), "Build", "Zenonia.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, TempScene);
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { TempScene },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(options);
                Debug.Log("BUILD RESULT: " + report.summary.result + " errors " + report.summary.totalErrors + " -> " + output);
                EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempScene);
            }
        }
    }
}
