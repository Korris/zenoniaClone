using UnityEditor;
using UnityEngine;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Import settings for Resources/Audio: music streams from disk as Vorbis (small RAM, ~1 MB per 45 s loop); short sound effects are
    /// decompressed on load so they start instantly. Runs on import, so dropped-in mp3 / wav files need no manual setup.
    /// </summary>
    public class AudioImportSettings : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            var importer = assetImporter as AudioImporter;
            if (importer == null || !assetPath.StartsWith("Assets/Resources/Audio/")) return;
            bool music = assetPath.StartsWith("Assets/Resources/Audio/music/");
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = music ? 0.6f : 0.8f;
            importer.defaultSampleSettings = s;
            importer.forceToMono = !music;
            importer.loadInBackground = music;
        }
    }
}
