using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Forces sane import settings for every texture under Assets/Resources/Sprites, so no .meta files have to be
    /// hand-written: uncompressed RGBA, up to 4096px, clamped, no NPOT scaling, alpha-is-transparency (edge dilation),
    /// trilinear + mipmaps for character art (clean 2x downscale on screen), bilinear without mipmaps for ground tiles.
    /// Runs on import (AssetPostprocessor) and re-checks existing assets on editor load (covers "scripts compiled after textures imported").
    /// </summary>
    public class ResourceTextureImportSettings : AssetPostprocessor
    {
        const string Root = "Assets/Resources/Sprites/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = assetImporter as TextureImporter;
            if (importer == null) return;
            Apply(importer, assetPath);
            DropMipsForAndroidNpot(importer);
        }

        // Bump when the in-memory (non-.meta) import logic changes so Unity re-imports cached artifacts.
        public override uint GetVersion() { return 2; }

        /// <summary>
        /// Unity silently refuses to compress a non-power-of-two texture that has mipmaps and npotScale None: whatever the platform
        /// override says (ASTC, ETC2, DXT5 all tested) the build ships raw RGBA32 + mips (a 1331x1181 sheet = 8.4 MB instead of ~0.9 MB).
        /// 511 of our 591 sprites are NPOT, so for the Android import only (in memory, the .meta keeps the PC setting) mipmaps are dropped
        /// for NPOT textures; they then compress to ASTC. Standalone keeps mips + uncompressed.
        /// </summary>
        static void DropMipsForAndroidNpot(TextureImporter i)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android || !i.mipmapEnabled) return;
            int w = 0, h = 0;
            var args = new object[] { w, h };
            MethodInfo size = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.Instance | BindingFlags.NonPublic); // no public source-size API
            if (size != null) { size.Invoke(i, args); w = (int)args[0]; h = (int)args[1]; }
            if (!IsPowerOfTwo(w) || !IsPowerOfTwo(h)) i.mipmapEnabled = false; // unknown size counts as NPOT
        }

        static bool IsPowerOfTwo(int v) { return v > 0 && (v & (v - 1)) == 0; }

        static bool WantsMips(string path) { return !path.Contains("/tiles/"); }

        internal static bool NeedsChange(TextureImporter i, string path)
        {
            bool mips = WantsMips(path);
            return i.mipmapEnabled != mips
                || i.filterMode != (mips ? FilterMode.Trilinear : FilterMode.Bilinear)
                || i.wrapMode != TextureWrapMode.Clamp
                || i.maxTextureSize != 4096
                || i.textureCompression != TextureImporterCompression.Uncompressed
                || !i.alphaIsTransparency
                || i.npotScale != TextureImporterNPOTScale.None
                || !AndroidOk(i);
        }

        internal static void Apply(TextureImporter i, string path)
        {
            bool mips = WantsMips(path);
            i.textureType = TextureImporterType.Default;
            i.mipmapEnabled = mips;
            i.filterMode = mips ? FilterMode.Trilinear : FilterMode.Bilinear;
            i.wrapMode = TextureWrapMode.Clamp;
            i.maxTextureSize = 4096;
            i.textureCompression = TextureImporterCompression.Uncompressed;
            i.alphaIsTransparency = true;
            i.npotScale = TextureImporterNPOTScale.None;
            i.isReadable = false;
            TextureImporterPlatformSettings a = i.GetPlatformTextureSettings(Android);
            a.overridden = true;
            a.maxTextureSize = AndroidMaxSize;
            a.format = AndroidFormat;
            a.textureCompression = TextureImporterCompression.Compressed; // with Uncompressed here Unity 6 ignores the ASTC format
            a.compressionQuality = 50;
            i.SetPlatformTextureSettings(a);
        }

        // Android: ASTC 6x6 (~3.6 bits/px, every ARM64 phone supports it) and at most 2048 px, so the APK is a fraction of the
        // uncompressed PC size. Standalone keeps the uncompressed RGBA above.
        const string Android = "Android";

        const int AndroidMaxSize = 2048;
        const TextureImporterFormat AndroidFormat = TextureImporterFormat.ASTC_6x6;

        static bool AndroidOk(TextureImporter i)
        {
            TextureImporterPlatformSettings a = i.GetPlatformTextureSettings(Android);
            return a.overridden && a.maxTextureSize == AndroidMaxSize && a.format == AndroidFormat && a.textureCompression == TextureImporterCompression.Compressed;
        }
    }

    [InitializeOnLoad]
    static class ResourceTextureAudit
    {
        static ResourceTextureAudit()
        {
            EditorApplication.delayCall += Run;
        }

        /// <summary>Re-imports every Resources sprite whose settings drifted (also called right before an Android build).</summary>
        [MenuItem("Zenonia/Fix Texture Import Settings")]
        internal static void Run()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Sprites" });
            int fixedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (!ResourceTextureImportSettings.NeedsChange(importer, path)) continue;
                ResourceTextureImportSettings.Apply(importer, path);
                importer.SaveAndReimport();
                fixedCount++;
            }
            if (fixedCount > 0) Debug.Log("[ZenoniaProto] Re-imported " + fixedCount + " textures with prototype import settings.");
        }
    }
}
