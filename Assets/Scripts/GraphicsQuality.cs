using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Graphics quality of the Settings window (user 22/11: "có cách nào cải thiện FPS không, lag quá"): Low / Medium / High, per device (PlayerPrefs),
    /// default Medium on desktop and Low on phones. What each level does:
    /// - every map: High keeps the project's MSAA (2x on desktop); Medium / Low turn MSAA off (sprites have no hard polygon edges, the 3D map's leaves
    ///   are alpha-tested and get nothing from it); Low also stops forcing anisotropic filtering on every texture.
    /// - 3D maps (Map3D): High = the look the prototype was reviewed with (2 soft cascades 2048, every prop and the terrain cast, all the grass);
    ///   Medium = the static props' sun shadow baked once per map instead of realtime cascades (Map3DBakedShadow), ~70 % of the grass;
    ///   Low = no realtime shadows (the baked prop silhouettes of the 2.5D map come back as blob shadows), ~40 % of the grass (Map3DLighting, Map3DBatcher).
    /// Test builds: `--gfx low|medium|high` picks the level for this run without saving it (FPS runs).
    /// </summary>
    public static class GraphicsQuality
    {
        public enum Level { Low = 0, Medium = 1, High = 2 }

        const string Key = "zenonia.gfx.quality";
        static bool loaded, savedProject;
        static Level level = Level.Medium;
        static int projectMsaa;
        static AnisotropicFiltering projectAniso;

        /// <summary>Raised after the level changed (Map3DLighting / Map3DBatcher re-apply on the map on screen).</summary>
        public static event Action Changed;

        public static Level Current { get { Load(); return level; } }

        public static string Name(Level l) { return l == Level.Low ? "Low" : l == Level.High ? "High" : "Medium"; }

        /// <summary>Settings window: sets, saves and applies the level.</summary>
        public static void Set(Level l)
        {
            Load();
            if (l == level) return;
            level = l;
            try { PlayerPrefs.SetInt(Key, (int)l); PlayerPrefs.Save(); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] could not save the graphics setting: " + e.Message); } // still applies for this run
            Apply();
            if (Changed != null) Changed();
        }

        /// <summary>Puts the quality-wide settings of the level in place (called at start and on every change).</summary>
        public static void Apply()
        {
            Load();
            if (!savedProject)
            {
                savedProject = true;
                projectMsaa = QualitySettings.antiAliasing;
                projectAniso = QualitySettings.anisotropicFiltering;
            }
            QualitySettings.antiAliasing = level == Level.High ? projectMsaa : 0;
            QualitySettings.anisotropicFiltering = level == Level.Low && projectAniso == AnisotropicFiltering.ForceEnable ? AnisotropicFiltering.Enable : projectAniso;
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            level = Application.isMobilePlatform ? Level.Low : Level.Medium;
            try { level = (Level)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)level), 0, 2); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] could not read the graphics setting: " + e.Message); }
            if (ReleaseBuild.IsRelease) return;
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--gfx");
            if (i < 0 || i + 1 >= a.Length) return;
            string v = a[i + 1].ToLowerInvariant();
            level = v.StartsWith("l") ? Level.Low : v.StartsWith("h") ? Level.High : Level.Medium; // this run only, not saved
        }
    }
}
