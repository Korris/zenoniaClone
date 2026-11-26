using UnityEditor;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// 3D prop models of the 3D maps (Assets/Resources/Map3D/models/, e.g. ChatGPT's Willowbrook bench .obj, 25/11/2026): readable, because
    /// Map3DBatcher merges every prop mesh at runtime (CombineMeshes needs the vertex data); no imported materials (Map3DAssets gives each its own
    /// lit cutout material with the model's texture), no animation, metres as authored (scale 1).
    /// </summary>
    public class Map3DModelImportSettings : AssetPostprocessor
    {
        const string Root = "Assets/Resources/Map3D/models/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var m = assetImporter as ModelImporter;
            if (m == null) return;
            m.isReadable = true;
            m.globalScale = 1f;
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.importAnimation = false;
            m.importCameras = false;
            m.importLights = false;
        }
    }
}
