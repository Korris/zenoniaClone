using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>On RenderScaleCamera's blit camera (draws nothing): its image pass writes the gameplay camera's reduced frame to the screen.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RenderScaleBlit : MonoBehaviour
    {
        internal RenderScaleCamera Owner;

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (Owner != null) Owner.Upscale(dst);
            else Graphics.Blit(src, dst);
        }
    }
}
