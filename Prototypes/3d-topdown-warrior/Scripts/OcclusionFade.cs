using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fades out props (pines, rocks, crystals, runes) that stand between the camera and the hero, Ragnarok style, so the player is never hidden.
/// No colliders needed: a prop blocks when its bounds come close to the camera -> hero segment. Each prop has an opaque and a "Fade" material
/// (made at build time); while faded the renderer uses the Fade one with its alpha lerped through a MaterialPropertyBlock.
/// </summary>
public class OcclusionFade : MonoBehaviour
{
    public Transform Hero;
    public float FadedAlpha = 0.22f, Speed = 8f;

    class Item { public Renderer R; public Material Opaque, Fade; public float Alpha = 1f; }
    readonly List<Item> items = new List<Item>();
    MaterialPropertyBlock block;

    /// <summary>Registers a prop renderer with its two materials (called by the builder through the FadeMaterials component).</summary>
    void Start()
    {
        block = new MaterialPropertyBlock();
        foreach (var fm in FindObjectsByType<FadeMaterials>(FindObjectsSortMode.None))
            foreach (var r in fm.GetComponentsInChildren<Renderer>()) items.Add(new Item { R = r, Opaque = fm.Opaque, Fade = fm.Fade });
    }

    void LateUpdate()
    {
        if (Hero == null) return;
        Vector3 a = transform.position, b = Hero.position + Vector3.up * 0.5f;
        Vector3 ab = b - a; float len = ab.magnitude; Vector3 dir = ab / len;
        foreach (var it in items)
        {
            Bounds bb = it.R.bounds;
            Vector3 c = bb.center;
            float t = Mathf.Clamp(Vector3.Dot(c - a, dir), 0f, len - 0.3f);           // ignore things behind the hero
            float dist = Vector3.Distance(a + dir * t, c);
            bool blocks = t < len - 0.3f && dist < bb.extents.magnitude * 0.55f + 0.25f;
            float goal = blocks ? FadedAlpha : 1f;
            if (Mathf.Approximately(it.Alpha, goal)) continue;
            it.Alpha = Mathf.MoveTowards(it.Alpha, goal, Speed * Time.unscaledDeltaTime);
            bool faded = it.Alpha < 0.999f;
            if (faded && it.R.sharedMaterial != it.Fade) it.R.sharedMaterial = it.Fade;
            if (!faded && it.R.sharedMaterial != it.Opaque) { it.R.sharedMaterial = it.Opaque; it.R.SetPropertyBlock(null); continue; }
            it.R.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(1f, 1f, 1f, it.Alpha));
            it.R.SetPropertyBlock(block);
        }
    }
}
