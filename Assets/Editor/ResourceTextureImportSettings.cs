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
            if (importer != null) Apply(importer, assetPath);
        }

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
            return a.overridden && a.maxTextureSize == AndroidMaxSize && a.format == AndroidFormat;
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
        /// <summary>Android target only: the imported texture is not ASTC yet (a cached artifact from before the override existed).</summary>
        static bool StaleForAndroid(string path)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) return false;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return tex != null && tex.format != TextureFormat.ASTC_6x6;
        }

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
                bool settings = ResourceTextureImportSettings.NeedsChange(importer, path);
                if (!settings && !StaleForAndroid(path)) continue;
                ResourceTextureImportSettings.Apply(importer, path);
                if (settings) importer.SaveAndReimport();
                else AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // meta is right but the imported artifact is not
                fixedCount++;
            }
            if (fixedCount > 0) Debug.Log("[ZenoniaProto] Re-imported " + fixedCount + " textures with prototype import settings.");
        }
    }
}
