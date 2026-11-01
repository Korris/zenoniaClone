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
    ///   Linux server: Unity -batchmode -quit -projectPath P -executeMethod ZenoniaProto.EditorTools.PlayerBuildScript.BuildLinuxServer -buildOut C:\out\ZenoniaServer.x86_64
    /// or the menu Zenonia / Build Android APK (writes Build/Zenonia.apk). Android needs the "Android Build Support" module (with OpenJDK + SDK/NDK)
    /// installed for this Unity version; the APK is signed with the debug key, landscape only, IL2CPP ARM64.
    /// A public release of Windows or Android: add -zenoniaRelease -apiUrl https://... (see ReleaseBuild): the define is for this build only, the address is baked, the product is called Pocket Valor.
    /// </summary>
    public static class PlayerBuildScript
    {
        const string TempScene = "Assets/_BuildBoot.unity";
        const string AndroidId = "com.korris.zenoniaclone";
        const string ReleaseDefine = "ZENONIA_RELEASE";
        const string ReleaseUrlAsset = "Assets/Resources/Data/release-api.txt";
        // The player's save folder and PlayerPrefs are named after these two, on every platform but Android: they must never change once a release is out, or players lose their login and offline saves.
        const string ReleaseCompany = "Pocket Valor", ReleaseProduct = "Pocket Valor", ReleaseExe = "PocketValor.exe";

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

        /// <summary>The zone server for a Linux host: Dedicated Server subtarget, x86_64. Needs the Linux Mono and Linux Dedicated Server modules; says so and fails when they are missing.</summary>
        public static void BuildLinuxServer()
        {
            string missing = MissingLinuxServerModule();
            if (missing != null)
            {
                Debug.LogError("BUILD RESULT: Failed. " + missing);
                EditorApplication.Exit(1);
                return;
            }
            int code = Build(BuildTarget.StandaloneLinux64, OutputArg(Path.Combine("Build", "ZenoniaServer.x86_64")), (int)StandaloneBuildSubtarget.Server);
            EditorApplication.Exit(code);
        }

        static string MissingLinuxServerModule()
        {
            const string hint = "Install them with Unity Hub: Installs > " + "this Unity version > Add modules > \"Linux Build Support (Mono)\" and \"Linux Dedicated Server Build Support\".";
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64)) return "The Linux build modules are not installed. " + hint;
            string variations = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "LinuxStandaloneSupport", "Variations");
            if (!Directory.Exists(variations) || Directory.GetDirectories(variations, "*server*").Length == 0) return "The Linux Dedicated Server module is not installed. " + hint;
            return null;
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

        static bool HasArg(string name) { return Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0; }

        static string ArgAfter(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        /// <summary>Null when this address can be baked. A public client only ever talks https: passwords travel in the request body.</summary>
        static string ReleaseUrlProblem(string url)
        {
            Uri uri;
            if (string.IsNullOrWhiteSpace(url)) return "a release build needs -apiUrl https://...";
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(uri.Host)) return "a release build needs an https:// API address, got '" + url + "'";
            return null;
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
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC; // compress every texture to ASTC
            // The project had an EMPTY Android texture-compression-format list (m_BuildTargetDefaultTextureCompressionFormat: []), which made
            // Unity 6 ship every texture as raw RGBA32 (198 MB APK) no matter the per-texture ASTC override.
            PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ASTC };
        }

        /// <returns>0 on success, 1 on failure.</returns>
        static int Build(BuildTarget target, string output, int subtarget = 0)
        {
            bool release = HasArg("-zenoniaRelease");
            string apiUrl = ArgAfter("-apiUrl");
            if (release)
            {
                string problem = ReleaseUrlProblem(apiUrl);
                if (problem == null && HasArg("-development")) problem = "a release build cannot also be a development build";
                if (problem == null && subtarget != 0) problem = "the release option is for the game client; the zone server gets its API address from --api-url";
                if (problem != null) { Debug.LogError("BUILD RESULT: Failed. " + problem); return 1; }
                if (target == BuildTarget.StandaloneWindows64) output = Path.Combine(Path.GetDirectoryName(output) ?? "", ReleaseExe);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            if (target == BuildTarget.Android) { ConfigureAndroid(); ResourceTextureAudit.Run(); } // ASTC overrides before the APK packs textures
            string companyBefore = PlayerSettings.companyName, productBefore = PlayerSettings.productName;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, TempScene);
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { TempScene },
                    locationPathName = output,
                    target = target,
                    subtarget = subtarget,
                    // -development: a test build, which also reads the bundled Resources/Data/dev-start.json (test stones, level, SP, gear)
                    options = HasArg("-development") ? BuildOptions.Development : BuildOptions.None,
                };
                if (release)
                {
                    options.extraScriptingDefines = new[] { ReleaseDefine }; // this build only, nothing is written to the project settings
                    File.WriteAllText(ReleaseUrlAsset, apiUrl.Trim().TrimEnd('/'));
                    AssetDatabase.ImportAsset(ReleaseUrlAsset, ImportAssetOptions.ForceSynchronousImport);
                    PlayerSettings.companyName = ReleaseCompany;
                    PlayerSettings.productName = ReleaseProduct;
                    Debug.Log("RELEASE BUILD: " + ReleaseProduct + ", API " + apiUrl.Trim());
                }
                var report = BuildPipeline.BuildPlayer(options);
                Debug.Log("BUILD RESULT: " + report.summary.result + " errors " + report.summary.totalErrors + " -> " + output);
                return report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1;
            }
            finally
            {
                AssetDatabase.DeleteAsset(TempScene);
                if (release)
                {
                    AssetDatabase.DeleteAsset(ReleaseUrlAsset);
                    PlayerSettings.companyName = companyBefore;
                    PlayerSettings.productName = productBefore;
                }
            }
        }
    }
}
