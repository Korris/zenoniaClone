using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Renders the gameplay camera at RenderScale.Current of the screen and stretches it back (22/11 perf pass 2). Built-in pipeline trick:
    /// the camera's targetTexture is set to a small texture only for its render (at the very end of LateUpdate: execution order 30000, after every
    /// other script; setting it in OnPreCull is too late, Unity has picked the target by then) and put back to the screen right after (the blit
    /// camera's pre-cull, before IMGUI and end-of-frame captures), so every script that turns world points into screen points (click to move, IMGUI names, damage numbers, minimap) keeps
    /// seeing the full-size camera. The camera's image effects (bloom, 2.5D atmosphere) run at the small size too. A second camera, after it and
    /// drawing nothing, copies the texture to the screen in its image pass (RenderScaleBlit) with Shaders/RenderScaleUpscale (bilinear + a light sharpen that grows as the scale
    /// drops); the overlay canvas and IMGUI come later at the native resolution. Scale 1 = the camera draws straight to the screen as before.
    /// Also runs dynamic resolution (RenderScaleDynamic) from the real frame time.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(30000)]
    public sealed class RenderScaleCamera : MonoBehaviour
    {
        Camera cam, blitCam;
        RenderTexture rt;
        Material mat;
        bool swapped;
        readonly RenderScaleDynamic dyn = new RenderScaleDynamic();

        public static void Attach(Camera camera)
        {
            if (camera == null || camera.GetComponent<RenderScaleCamera>() != null) return;
            camera.gameObject.AddComponent<RenderScaleCamera>();
        }

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            Shader s = Resources.Load<Shader>("Shaders/RenderScaleUpscale");
            if (s == null || !s.isSupported) { Debug.LogWarning("[ZenoniaProto] RenderScaleUpscale shader missing: render scale off"); enabled = false; return; }
            if (mat == null) mat = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
            if (blitCam == null)
            {
                var go = new GameObject("RenderScaleBlit");
                go.transform.SetParent(transform, false);
                blitCam = go.AddComponent<Camera>();
                blitCam.clearFlags = CameraClearFlags.Nothing;
                blitCam.cullingMask = 0;
                blitCam.orthographic = true;
                blitCam.allowHDR = false; blitCam.allowMSAA = false; blitCam.useOcclusionCulling = false;
                blitCam.enabled = false;
                go.AddComponent<RenderScaleBlit>().Owner = this;
            }
            Camera.onPreCull += AnyPreCull;
            dyn.Scale = RenderScale.Ceiling;
        }

        void OnDisable()
        {
            Camera.onPreCull -= AnyPreCull;
            Restore();
            if (blitCam != null) blitCam.enabled = false;
            RenderScale.Current = 1f;
        }

        void OnDestroy()
        {
            if (rt != null) { rt.Release(); Destroy(rt); }
            if (mat != null) Destroy(mat);
            if (blitCam != null) Destroy(blitCam.gameObject);
        }

        void LateUpdate()
        {
            Restore(); // safety: never leave the camera on the small texture outside its own render
            dyn.Tick(Time.unscaledDeltaTime, RenderScale.Ceiling, RenderScale.Dynamic, RenderScale.TargetFps);
            float s = Mathf.Round(dyn.Scale * 100f) / 100f;
            RenderScale.Current = s;
            bool on = s < 0.995f;
            blitCam.depth = cam.depth + 1f;
            if (blitCam.enabled != on) blitCam.enabled = on;
            if (on) Swap();
        }

        void Swap()
        {
            int w = Mathf.Max(64, Mathf.RoundToInt(Screen.width * RenderScale.Current)), h = Mathf.Max(64, Mathf.RoundToInt(Screen.height * RenderScale.Current));
            int aa = Mathf.Max(1, QualitySettings.antiAliasing);
            if (rt == null || rt.width != w || rt.height != h || rt.antiAliasing != aa)
            {
                if (rt != null) { rt.Release(); Destroy(rt); }
                rt = new RenderTexture(w, h, 24, RenderTextureFormat.Default) { name = "RenderScale", antiAliasing = aa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                rt.Create();
            }
            mat.SetFloat("_Sharpen", Mathf.Clamp01((1f - RenderScale.Current) * 1.4f) * 0.45f); // 85 % -> 0.09, 70 % -> 0.19, 60 % -> 0.25
            cam.targetTexture = rt;
            swapped = true;
        }

        /// <summary>The blit camera's image pass: the small frame, stretched and sharpened, onto the screen.</summary>
        internal void Upscale(RenderTexture dst)
        {
            if (rt == null) { Graphics.Blit(Texture2D.blackTexture, dst); return; }
            Graphics.Blit(rt, dst, mat);
        }

        /// <summary>The blit camera is about to render: the gameplay camera is done (image effects included), so it goes back to the screen.</summary>
        void AnyPreCull(Camera c) { if (c == blitCam) Restore(); }

        void Restore()
        {
            if (!swapped) return;
            swapped = false;
            if (cam != null) cam.targetTexture = null;
        }
    }
}
