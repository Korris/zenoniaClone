using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Lightweight bloom for the Built-in render pipeline, attached to the camera at runtime (no editor setup).
    /// The camera renders in HDR; additive FX / glow layers exceed 1.0 and pass the threshold, normal sprites stay below it.
    /// Pipeline: prefilter+downsample (1/2) -> downsample (1/4) -> blur -> downsample (1/8) -> blur -> composite onto the frame.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BloomEffect : MonoBehaviour
    {
        const int PassPrefilter = 0, PassDown = 1, PassBlurH = 2, PassBlurV = 3, PassComposite = 4;
        Material mat;

        void OnEnable()
        {
            GetComponent<Camera>().allowHDR = true;
            if (mat != null) return;
            Shader shader = Resources.Load<Shader>("Shaders/BloomLite");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("ZenoniaProto: BloomLite shader missing or unsupported, bloom disabled.");
                enabled = false;
                return;
            }
            mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        void OnDestroy() { if (mat != null) Destroy(mat); }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            int w = src.width / 2, h = src.height / 2;
            if (mat == null || w < 8 || h < 8)
            {
                Graphics.Blit(src, dst);
                return;
            }
            mat.SetFloat("_Threshold", FeelConfig.BloomThreshold);
            mat.SetFloat("_Knee", FeelConfig.BloomKnee);
            mat.SetFloat("_Intensity", FeelConfig.BloomIntensity);

            RenderTexture half = Temp(w, h, src);
            Graphics.Blit(src, half, mat, PassPrefilter);

            RenderTexture quarter = Temp(w / 2, h / 2, src);
            Graphics.Blit(half, quarter, mat, PassDown);
            RenderTexture.ReleaseTemporary(half);
            RenderTexture scratch = Temp(quarter.width, quarter.height, src);
            Blur(quarter, scratch);

            RenderTexture eighth = Temp(Mathf.Max(1, w / 4), Mathf.Max(1, h / 4), src);
            Graphics.Blit(quarter, eighth, mat, PassDown);
            RenderTexture.ReleaseTemporary(scratch);
            scratch = Temp(eighth.width, eighth.height, src);
            Blur(eighth, scratch);
            RenderTexture.ReleaseTemporary(scratch);

            mat.SetTexture("_Bloom", quarter);
            mat.SetTexture("_Bloom2", eighth);
            Graphics.Blit(src, dst, mat, PassComposite);
            RenderTexture.ReleaseTemporary(quarter);
            RenderTexture.ReleaseTemporary(eighth);
        }

        /// <summary>Separable gaussian in place: tex -> scratch (horizontal) -> tex (vertical).</summary>
        void Blur(RenderTexture tex, RenderTexture scratch)
        {
            Graphics.Blit(tex, scratch, mat, PassBlurH);
            Graphics.Blit(scratch, tex, mat, PassBlurV);
        }

        static RenderTexture Temp(int w, int h, RenderTexture like)
        {
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, like.format);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            return rt;
        }
    }
}
