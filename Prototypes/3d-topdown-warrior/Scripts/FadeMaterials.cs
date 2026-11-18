using UnityEngine;

/// <summary>Build-time tag on a prop: which opaque material it shows and which Standard "Fade" twin OcclusionFade swaps in while it blocks the camera.</summary>
public class FadeMaterials : MonoBehaviour
{
    public Material Opaque, Fade;
}
