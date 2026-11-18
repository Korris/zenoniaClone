using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Putting the Tripo sword and shield into the Warrior's hand bones, plus the sword-tip trail used by the basic attack.</summary>
public static partial class ProtoBuilder
{
    /// <summary>A material from the Tripo export folder of `fbx` (its *BaseColor.png and *Normal_Bake.png).</summary>
    static Material FolderMat(string fbx)
    {
        string dir = Path.GetDirectoryName(fbx).Replace('\\', '/');
        string bc = Directory.GetFiles(dir, "*BaseColor.png").FirstOrDefault(), nm = Directory.GetFiles(dir, "*Normal*.png").FirstOrDefault();
        return Mat(dir + "/Mat.mat", bc?.Replace('\\', '/'), nm?.Replace('\\', '/'), Color.white);
    }

    /// <summary>
    /// Puts a static Tripo item in a hand bone. Sword: the long axis is found from the mesh, the crossguard is the widest slab, the grip sits between it and
    /// the nearer end, and the blade is turned to point forward-down from the hand in the bind pose; a "Tip" child with a TrailRenderer (off until a swing)
    /// marks the blade end. Shield: thinnest axis = face normal, turned out to the left-front.
    /// </summary>
    static void Hold(GameObject hero, string boneSuffix, string fbx, float size, bool sword)
    {
        var bone = hero.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.EndsWith(boneSuffix));
        if (bone == null) { Debug.LogError("[proto] no bone " + boneSuffix); return; }
        var item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        item.name = sword ? "Sword" : "Shield";
        var mat = FolderMat(fbx);
        foreach (var r in item.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
        var mf = item.GetComponentInChildren<MeshFilter>();
        var verts = mf.sharedMesh.vertices.Select(v => mf.transform.localToWorldMatrix.MultiplyPoint3x4(v) - item.transform.position).ToArray();
        Vector3 min = verts.Aggregate(Vector3.positiveInfinity, Vector3.Min), max = verts.Aggregate(Vector3.negativeInfinity, Vector3.Max), ext = max - min;
        int longAx = ext.x >= ext.y && ext.x >= ext.z ? 0 : ext.y >= ext.z ? 1 : 2, thinAx = ext.x <= ext.y && ext.x <= ext.z ? 0 : ext.y <= ext.z ? 1 : 2;
        Vector3 L = Vector3.zero; L[longAx] = 1f; Vector3 T = Vector3.zero; T[thinAx] = 1f;
        float scale = size / ext[longAx];
        item.transform.localScale *= scale;
        Vector3 grip, bladeDir, tip = (min + max) * 0.5f;
        if (sword)
        {
            int n = 24; var width = new float[n];
            foreach (var v in verts) { int i = Mathf.Clamp((int)((v[longAx] - min[longAx]) / ext[longAx] * n), 0, n - 1); Vector3 q = v; q[longAx] = 0; width[i] = Mathf.Max(width[i], q.magnitude); }
            int guard = 0; for (int i = 1; i < n; i++) if (width[i] > width[guard]) guard = i;
            bool hiltLow = guard < n / 2;
            float guardPos = min[longAx] + (guard + 0.5f) / n * ext[longAx], endPos = hiltLow ? min[longAx] : max[longAx];
            grip = (min + max) * 0.5f; grip[longAx] = (guardPos + endPos) * 0.5f;
            bladeDir = hiltLow ? L : -L;
            tip[longAx] = hiltLow ? max[longAx] : min[longAx];
        }
        else { grip = (min + max) * 0.5f; bladeDir = T; }
        Vector3 fwd = hero.transform.forward;
        Quaternion rot = sword ? Quaternion.FromToRotation(bladeDir, (fwd * 0.85f + Vector3.down * 0.5f).normalized)
                               : Quaternion.FromToRotation(bladeDir, (fwd * 0.6f - hero.transform.right * 0.8f).normalized);
        item.transform.rotation = rot;
        item.transform.position = bone.position - rot * (grip * scale) + (sword ? Vector3.zero : -hero.transform.right * 0.05f);
        item.transform.SetParent(bone, true);
        if (sword) SwordTrail(item, item.transform.position + rot * (tip * scale));
    }

    /// <summary>A short additive trail from the blade tip; HeroController3D turns `emitting` on only while the blade is moving fast.</summary>
    static void SwordTrail(GameObject sword, Vector3 tipWorld)
    {
        var tipGo = new GameObject("Tip");
        tipGo.transform.position = tipWorld; tipGo.transform.SetParent(sword.transform, true);
        var trailMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        AssetDatabase.DeleteAsset("Assets/Env/Trail.mat"); AssetDatabase.CreateAsset(trailMat, "Assets/Env/Trail.mat");
        var tr = tipGo.AddComponent<TrailRenderer>();
        tr.time = 0.14f; tr.startWidth = 0.11f; tr.endWidth = 0f; tr.minVertexDistance = 0.005f; tr.numCapVertices = 3; tr.numCornerVertices = 3;
        tr.startColor = new Color(1f, 0.96f, 0.75f, 0.95f); tr.endColor = new Color(1f, 0.8f, 0.4f, 0f);
        tr.sharedMaterial = trailMat; tr.emitting = false;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tr.receiveShadows = false;
    }
}
