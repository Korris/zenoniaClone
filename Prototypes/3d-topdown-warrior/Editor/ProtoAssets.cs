using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Asset helpers shared by the prototype builders: rigged FBX import, animator controllers, materials, spawning, fitting.</summary>
public static partial class ProtoBuilder
{
    static GameObject Rigged(string fbx, string ctrlPath, string defaultKey, out AnimatorController ctrl)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        mi.animationType = ModelImporterAnimationType.Generic; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        var clips = mi.defaultClipAnimations;
        foreach (var c in clips) c.loopTime = Has(c.name, "idle", "walk", "run", "relax", "fly", "jump");
        mi.clipAnimations = clips; mi.SaveAndReimport();
        var anims = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
        Debug.Log("[proto] " + fbx + " clips: " + string.Join(", ", anims.Select(c => c.name + " " + c.length.ToString("0.00"))));
        AssetDatabase.DeleteAsset(ctrlPath);
        ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var c in anims) { var st = sm.AddState(c.name); st.motion = c; if (Has(c.name, defaultKey) && !Has(c.name, "relax")) sm.defaultState = st; }
        return AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
    }

    static string Clip(AnimatorController ctrl, params string[] keys)
    {
        var names = ctrl.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
        foreach (var k in keys) { var hit = names.Where(n => Has(n, k) && !Has(n, "relax")).OrderBy(n => n.Length).FirstOrDefault(); if (hit != null) return hit; }
        Debug.LogWarning("[proto] no clip for " + string.Join("/", keys) + " in " + string.Join(",", names));
        return names.FirstOrDefault() ?? "";
    }

    static Material Mat(string path, string tex, string normal, Color tint)
    {
        AssetDatabase.DeleteAsset(path);
        var m = new Material(Shader.Find("Standard")) { color = tint, mainTexture = tex == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(tex) };
        if (normal != null) { m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal)); m.EnableKeyword("_NORMALMAP"); }
        m.SetFloat("_Metallic", 0.1f); m.SetFloat("_Glossiness", 0.35f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    /// <summary>Plain-colour Standard material (matte) saved under Assets/Env.</summary>
    static Material Flat(string name, Color c, float gloss = 0.2f)
    {
        var m = Mat("Assets/Env/" + name + ".mat", null, null, c);
        m.SetFloat("_Glossiness", gloss); m.SetFloat("_Metallic", 0f);
        return m;
    }

    /// <summary>A Standard-shader "Fade" copy of `opaque` for props that must turn translucent when they block the camera.</summary>
    static Material FadeTwin(Material opaque, string path)
    {
        var m = new Material(opaque);
        m.SetFloat("_Mode", 2f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON"); m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static GameObject Spawn(GameObject model, Material mat, AnimatorController ctrl, string name, Vector3 at, float height)
    {
        var root = new GameObject(name); root.transform.position = at;
        var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
        body.transform.SetParent(root.transform, false);
        foreach (var r in body.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
        var anim = body.GetComponent<Animator>() ?? body.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        FitHeight(body, at, height);
        return root;
    }

    /// <summary>Scales a model to `height` metres and stands it on `at`.</summary>
    static void FitHeight(GameObject go, Vector3 at, float height)
    {
        Bounds b = Bounds(go);
        go.transform.localScale *= height / Mathf.Max(0.001f, b.size.y);
        b = Bounds(go);
        go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
    }

    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>A primitive without its collider, parented, positioned, scaled and coloured; props get FadeMaterials so OcclusionFade can see through them.</summary>
    static GameObject Part(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, Material fade = null)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false); go.transform.localPosition = localPos; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (fade != null) { var fm = go.AddComponent<FadeMaterials>(); fm.Opaque = mat; fm.Fade = fade; }
        return go;
    }

    static bool Has(string name, params string[] keys) { string n = name.ToLowerInvariant(); return keys.Any(k => n.Contains(k)); }
}
