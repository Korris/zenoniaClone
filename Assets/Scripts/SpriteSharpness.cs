using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Sampling of the character sheets in the world (hero layers, monsters, pets, NPCs, class body / weapon / head sheets), from ChatGPT
    /// "Visual Sharpness V1.0" (24/11/2026, docs/reference/ro3-compare/ro3-sharpness-analysis-chatgpt.txt, preset A + test D / E / F).
    /// The sheets are cut in 192 px cells and drawn ~65-80 px tall (2.4-3x smaller; 4x at the widest zoom), so plain trilinear mipmapping
    /// blends toward a mip that is already 4x smaller and the face / hair / gold trim go soft. Modes (all keep the import's uncompressed RGBA):
    /// - Bias (default since 25/11/2026, bias -1.0): trilinear with a negative mip bias (test F at -0.6 read "hơi gắt và dễ rung khi chuyển động" at 1080p);
    /// - Bilinear (default 24/11, ChatGPT picked test D 9/10 at 1080p): mip 0 only, bilinear: aliases (jagged sword / armour edges) in a 720p window or zoomed out;
    /// - Point: mip 0 only, point (test E, shows how much softness is the filter);
    /// - Trilinear: the old look (test A..C).
    /// Test builds: `--sprite-filter bias|bilinear|point|trilinear` and `--sprite-bias -0.6` pick the mode for this run (A/B shots).
    /// Ground tiles, props, UI and FX textures are not touched.
    /// </summary>
    public static class SpriteSharpness
    {
        public enum Mode { Bias, Bilinear, Point, Trilinear }

        /// <summary>Mip bias of the default mode: -0.6 keeps the eyes / trim of a 192 px cell readable at 65-70 px without the sparkle of mip 0.</summary>
        public const float DefaultBias = -1.0f;

        static readonly string[] UnitRoots = { "Sprites/hero-layered/", "Sprites/monsters/", "Sprites/pets/", "Sprites/village/npcs/" };
        static readonly string[] ClassParts = { "/body/", "/weapons/", "/heads/", "/companion/" };

        static bool parsed;
        // Bias since 25/11 (user: "răng cưa" in a 1280x720 window): mip 0 bilinear (ChatGPT "CHỐT D", judged at 1080p) aliases once a 192 px cell is drawn
        // 4-5x smaller (small window / zoomed out); trilinear -1.0 stays as sharp as mip 0 at 1080p and smooths the edges as the hero gets smaller (A/B shots D:/temp/sharp-test, crops compare*.png).
        static Mode mode = Mode.Bias;
        static float bias = DefaultBias;

        public static Mode Current { get { Parse(); return mode; } }

        /// <summary>True for a character sheet path (relative to Resources, no extension).</summary>
        public static bool IsUnitSheet(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return false;
            foreach (string r in UnitRoots) if (resourcePath.StartsWith(r, StringComparison.Ordinal)) return true;
            if (!resourcePath.StartsWith("Sprites/classes/", StringComparison.Ordinal)) return false;
            foreach (string p in ClassParts) if (resourcePath.IndexOf(p, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>Sets filter / mip bias of a freshly loaded texture (SpriteSheet.TryLoad).</summary>
        public static void Apply(string resourcePath, Texture2D tex)
        {
            if (tex == null || tex.mipmapCount <= 1 || !IsUnitSheet(resourcePath)) return;
            Parse();
            switch (mode)
            {
                case Mode.Bias: tex.filterMode = FilterMode.Trilinear; tex.mipMapBias = bias; break;
                case Mode.Bilinear: tex.filterMode = FilterMode.Bilinear; tex.mipMapBias = -16f; break; // mip 0 only
                case Mode.Point: tex.filterMode = FilterMode.Point; tex.mipMapBias = -16f; break;
                default: tex.filterMode = FilterMode.Trilinear; tex.mipMapBias = 0f; break;
            }
            tex.anisoLevel = 1; // billboards face the camera: anisotropic filtering buys nothing (preset A)
        }

        static void Parse()
        {
            if (parsed) return;
            parsed = true;
            if (ReleaseBuild.IsRelease) return;
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, "--sprite-filter");
            if (i >= 0 && i + 1 < a.Length)
            {
                string v = a[i + 1].ToLowerInvariant();
                mode = v.StartsWith("bil") ? Mode.Bilinear : v.StartsWith("p") ? Mode.Point : v.StartsWith("t") ? Mode.Trilinear : Mode.Bias;
            }
            i = Array.IndexOf(a, "--sprite-bias");
            float f;
            if (i >= 0 && i + 1 < a.Length && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))
                bias = Mathf.Clamp(f, -3f, 1f);
        }
    }
}
