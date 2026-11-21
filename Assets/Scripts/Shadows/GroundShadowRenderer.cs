using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>
    /// Draws every ground shadow of FakeSun in two steps, on the gameplay camera:
    /// 1. Before the camera culls (after every Update / LateUpdate moved units and the camera), a command buffer clears a low-res R8 mask over the
    ///    visible world rect (half screen resolution) and draws, with BlendOp Max, the map's baked prop-shadow cache (StaticPropShadows) and the
    ///    shared blob mesh (GroundShadowBlobs). Overlaps keep the darkest shadow, they never stack.
    /// 2. One quad over the map at sortingOrder GroundOrder + 8 (above the painting and flat decor at + 4, below ground FX at -31000 and every
    ///    y-sorted unit, ring and telegraph) reads the mask by world position and multiplies the ground toward FakeSun.Tint, capped at FakeSun.AlphaCap.
    /// Cost: one mesh rebuild of ~4 vertices per visible blob on the CPU, 2-3 draws into a half-res R8 target and one full-screen multiply quad;
    /// no particles, no lights, one shared material per step. Works on the 2.5D perspective camera too (mask over the visible ground trapezoid's bounds).
    /// Off (`--shadows off`, the --view25d Hydra prototype, missing shader): the old per-unit sprites draw again.
    /// </summary>
    public class GroundShadowRenderer : MonoBehaviour
    {
        const float Margin = 24f;           // world units of mask beyond the screen edge (blobs half off screen)
        const float MaskResolution = 0.5f;  // mask texels per screen pixel
        static readonly int MaskId = Shader.PropertyToID("_GroundShadowMask"), RectId = Shader.PropertyToID("_GroundShadowRect"), TintId = Shader.PropertyToID("_GroundShadowTint");

        Camera cam;
        Material maskMat, staticMat, compositeMat;
        RenderTexture rt;
        CommandBuffer cb;
        Mesh staticQuad, compositeQuad;
        GameObject composite;
        Texture2D propTex;
        string bakedMap;
        int bakedVersion;
        bool triedShaders;

        /// <summary>Adds the renderer to the gameplay camera (views only; a server has no camera).</summary>
        public static void Attach(Camera camera)
        {
            if (camera == null || !NetMode.RendersFx || camera.GetComponent<GroundShadowRenderer>() != null) return;
            camera.gameObject.AddComponent<GroundShadowRenderer>();
        }

        void OnEnable() { cam = GetComponent<Camera>(); Camera.onPreCull += PreCull; }

        void OnDisable()
        {
            Camera.onPreCull -= PreCull;
            GroundShadowBlobs.Rebuild(false, new Rect());
            if (composite != null) composite.SetActive(false);
        }

        void OnDestroy()
        {
            if (rt != null) { rt.Release(); Destroy(rt); }
            if (propTex != null) Destroy(propTex);
            if (composite != null) Destroy(composite);
            if (staticQuad != null) Destroy(staticQuad);
            if (compositeQuad != null) Destroy(compositeQuad);
            if (cb != null) cb.Release();
        }

        bool Ready()
        {
            if (!triedShaders)
            {
                triedShaders = true;
                Shader m = Resources.Load<Shader>("Shaders/GroundShadowMask"), c = Resources.Load<Shader>("Shaders/GroundShadowComposite");
                if (m != null && m.isSupported && c != null && c.isSupported)
                {
                    maskMat = new Material(m) { name = "GroundShadowMask", hideFlags = HideFlags.DontSave };
                    staticMat = new Material(m) { name = "GroundShadowMaskStatic", hideFlags = HideFlags.DontSave };
                    compositeMat = new Material(c) { name = "GroundShadowComposite", hideFlags = HideFlags.DontSave };
                    cb = new CommandBuffer { name = "GroundShadowMask" };
                }
                else Debug.LogWarning("[ZenoniaProto] Ground shadow shaders missing or unsupported: old unit shadows only");
            }
            return maskMat != null && cam != null;
        }

        void PreCull(Camera c)
        {
            if (c != cam) return;
            if (!FakeSun.Enabled || !Ready())
            {
                GroundShadowBlobs.Rebuild(false, new Rect()); // shows the old sprites again (also for units attached while the switch was on)
                if (composite != null && composite.activeSelf) composite.SetActive(false);
                return;
            }
            EnsureMap();
            float halfH = c.orthographicSize, halfW = halfH * c.aspect;
            Rect view;
            if (c.orthographic)
            {
                Vector3 cp = c.transform.position;
                view = new Rect(cp.x - halfW - Margin, cp.y - halfH - Margin, (halfW + Margin) * 2f, (halfH + Margin) * 2f);
            }
            else
            {
                // 2.5D map (View25D): the visible ground is a trapezoid; mask its bounding rect. orthographicSize stays the zoom knob there.
                Rect g = View25DCamera.VisibleGround(c);
                view = new Rect(g.xMin - Margin, g.yMin - Margin, g.width + Margin * 2f, g.height + Margin * 2f);
            }
            int blobs = GroundShadowBlobs.Rebuild(true, view);
            float texelsPerUnit = c.pixelHeight * MaskResolution / (halfH * 2f); // as at the focus; the far edge is coarser, which soft shadows hide
            EnsureTarget(Mathf.CeilToInt(view.width * texelsPerUnit), Mathf.CeilToInt(view.height * texelsPerUnit));

            cb.Clear();
            cb.SetRenderTarget(rt);
            cb.ClearRenderTarget(false, true, Color.clear);
            cb.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.Ortho(view.xMin, view.xMax, view.yMin, view.yMax, -100f, 100f));
            if (propTex != null) cb.DrawMesh(staticQuad, Matrix4x4.identity, staticMat, 0, 1);
            if (blobs > 0) cb.DrawMesh(GroundShadowBlobs.Mesh, Matrix4x4.identity, maskMat, 0, 0);
            Graphics.ExecuteCommandBuffer(cb);

            Shader.SetGlobalTexture(MaskId, rt);
            Shader.SetGlobalVector(RectId, new Vector4(view.xMin, view.yMin, 1f / view.width, 1f / view.height));
            Color t = FakeSun.Tint;
            Shader.SetGlobalVector(TintId, new Vector4(t.r, t.g, t.b, FakeSun.AlphaCap));
            if (!composite.activeSelf) composite.SetActive(true);
        }

        /// <summary>Bakes the prop-shadow cache and sizes the map quads when the map (or FakeSun) changed.</summary>
        void EnsureMap()
        {
            IGameMap map = MapRegistry.Current;
            if (map == null || (map.Id == bakedMap && bakedVersion == FakeSun.Version && composite != null)) return;
            bakedMap = map.Id;
            bakedVersion = FakeSun.Version;
            if (propTex != null) Destroy(propTex);
            float t0 = Time.realtimeSinceStartup;
            propTex = StaticPropShadows.Bake(map);
            Debug.Log("[ZenoniaProto] Prop shadows " + map.Id + ": baked in " + Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms");
            staticMat.mainTexture = propTex;
            float w = GameConfig.MapW, h = GameConfig.MapH;
            staticQuad = Quad(staticQuad, w, h);
            compositeQuad = Quad(compositeQuad, w, h);
            if (composite == null)
            {
                composite = new GameObject("GroundShadows") { hideFlags = HideFlags.DontSave };
                composite.AddComponent<MeshFilter>();
                var mr = composite.AddComponent<MeshRenderer>();
                mr.sharedMaterial = compositeMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.sortingOrder = GameConfig.GroundOrder + 8;
            }
            composite.GetComponent<MeshFilter>().sharedMesh = compositeQuad;
        }

        /// <summary>World-space quad (0,0)-(w,h), uv 0..1.</summary>
        static Mesh Quad(Mesh m, float w, float h)
        {
            if (m == null) m = new Mesh { name = "GroundShadowQuad", hideFlags = HideFlags.DontSave };
            m.Clear();
            m.vertices = new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, h, 0f), new Vector3(w, h, 0f), new Vector3(w, 0f, 0f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds();
            return m;
        }

        void EnsureTarget(int w, int h)
        {
            w = Mathf.Clamp(w, 32, 2048); h = Mathf.Clamp(h, 32, 2048);
            if (rt != null && Mathf.Abs(rt.width - w) <= 8 && Mathf.Abs(rt.height - h) <= 8) return;
            if (rt != null) { rt.Release(); Destroy(rt); }
            RenderTextureFormat f = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            rt = new RenderTexture(w, h, 0, f) { name = "GroundShadowMask", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            rt.Create();
        }
    }
}
