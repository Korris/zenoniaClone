using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Screen pass of the 2.5D prototype (Shaders/View25DAtmosphere): distance haze toward the far (top) edge, a warm-to-cool light gradient and a
    /// vignette. Added to the camera after BloomEffect, so it runs after bloom; the UGUI HUD (screen-space overlay) is drawn later and stays clean.
    /// Passes the frame through untouched while View25D is off (another map).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class View25DAtmosphere : MonoBehaviour
    {
        Material mat;

        void OnEnable()
        {
            if (mat != null) return;
            Shader s = Resources.Load<Shader>("Shaders/View25DAtmosphere");
            if (s == null || !s.isSupported) { Debug.LogWarning("[ZenoniaProto] View25D: atmosphere shader missing"); enabled = false; return; }
            mat = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
        }

        void OnDestroy() { if (mat != null) Destroy(mat); }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null || !View25D.Active || !View25D.Atmosphere) { Graphics.Blit(src, dst); return; }
            mat.SetFloat("_Aspect", (float)src.width / Mathf.Max(1, src.height));
            Graphics.Blit(src, dst, mat);
        }
    }
}
