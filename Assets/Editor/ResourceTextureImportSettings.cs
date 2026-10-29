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
                || i.npotScale != TextureImporterNPOTScale.None;
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
        }
    }

    [InitializeOnLoad]
    static class ResourceTextureAudit
    {
        static ResourceTextureAudit()
        {
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Zenonia/Fix Texture Import Settings")]
        static void Run()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Sprites" });
            int fixedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || !ResourceTextureImportSettings.NeedsChange(importer, path)) continue;
                ResourceTextureImportSettings.Apply(importer, path);
                importer.SaveAndReimport();
                fixedCount++;
            }
            if (fixedCount > 0) Debug.Log("[ZenoniaProto] Re-imported " + fixedCount + " textures with prototype import settings.");
        }
    }
}
