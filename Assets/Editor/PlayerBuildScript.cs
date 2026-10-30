using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Player builds (the project ships no scene; Bootstrap builds everything at runtime). A throw-away empty scene is created for the build and deleted afterwards.
    ///   Windows: Unity -batchmode -quit -projectPath P -executeMethod ZenoniaProto.EditorTools.PlayerBuildScript.BuildWindows -buildOut C:\out\Zenonia.exe
    ///   Android: Unity -batchmode -quit -projectPath P -executeMethod ZenoniaProto.EditorTools.PlayerBuildScript.BuildAndroid -buildOut C:\out\Zenonia.apk
    /// or the menu Zenonia / Build Android APK (writes Build/Zenonia.apk). Android needs the "Android Build Support" module (with OpenJDK + SDK/NDK)
    /// installed for this Unity version; the APK is signed with the debug key, landscape only, IL2CPP ARM64.
    /// </summary>
    public static class PlayerBuildScript
    {
        const string TempScene = "Assets/_BuildBoot.unity";
        const string AndroidId = "com.korris.zenoniaclone";

        public static void BuildWindows()
        {
            int code = Build(BuildTarget.StandaloneWindows64, OutputArg(Path.Combine("Build", "Zenonia.exe")));
            EditorApplication.Exit(code);
        }

        public static void BuildAndroid()
        {
            int code = Build(BuildTarget.Android, OutputArg(Path.Combine("Build", "Zenonia.apk")));
            EditorApplication.Exit(code);
        }

        [MenuItem("Zenonia/Build Android APK")]
        static void BuildAndroidMenu()
        {
            string output = Path.Combine(Directory.GetCurrentDirectory(), "Build", "Zenonia.apk");
            if (Build(BuildTarget.Android, output) == 0) EditorUtility.RevealInFinder(output);
        }

        static string OutputArg(string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-buildOut");
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Directory.GetCurrentDirectory(), fallback);
        }

        /// <summary>Landscape game, touch controls, phone-friendly defaults. Only touches the Android player settings.</summary>
        static void ConfigureAndroid()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidId);
            if (string.IsNullOrEmpty(PlayerSettings.productName) || PlayerSettings.productName == "zenoniaUnity") PlayerSettings.productName = "Zenonia Clone";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false; // .apk you can install directly, not .aab
        }

        /// <returns>0 on success, 1 on failure.</returns>
        static int Build(BuildTarget target, string output)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            if (target == BuildTarget.Android) { ConfigureAndroid(); ResourceTextureAudit.Run(); } // ASTC overrides before the APK packs textures
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, TempScene);
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { TempScene },
                    locationPathName = output,
                    target = target,
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(options);
                Debug.Log("BUILD RESULT: " + report.summary.result + " errors " + report.summary.totalErrors + " -> " + output);
                return report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1;
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempScene);
            }
        }
    }
}
