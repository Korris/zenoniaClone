using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Baked sun shadow of a 3D map at graphics quality Medium (22/11 perf pass; user: "có cách nào cải thiện FPS không, lag quá"). The sun never
    /// moves (Map3DLighting) and the props never move, so their shadow is rendered ONCE: an orthographic camera along the sun renders every prop
    /// that casts at High (Map3DBatcher.BakeCasters: trees with the casting leaf cards, rocks, ruins, wood, cutout sprites, terrain hills) with
    /// Shaders/Map3DShadowBake into a 4096 float depth map covering the whole terrain (~30 texels per metre, like the realtime near cascade), and the
    /// Map3D shaders read it (Map3DCommon M3DBakedSun, 4 PCF taps). Per frame that is 4 texture taps per pixel instead of shadow maps, a depth
    /// pre-pass and the screen-space collect: Medium runs at about the speed of no shadows at all. Lost against High: leaves swaying in the
    /// shadow and the hero / monsters never cast (they do not cast at High either: blob shadows). Released with the map.
    /// `--map3d-test nobake`: Medium without it (no sun shadows) for A/B runs.
    /// </summary>
    public sealed class Map3DBakedShadow : MonoBehaviour
    {
        const int Size = 4096;
        const int BakeLayer = 31;           // unused layer: only the casters sit on it during the bake
        static readonly int TexId = Shader.PropertyToID("_M3DBakedShadow"), VpId = Shader.PropertyToID("_M3DBakedVP"), ParamId = Shader.PropertyToID("_M3DBaked");
        RenderTexture rt;

        /// <summary>After the props are merged (Map3D.Finish): bakes now when Medium is on, else when the player picks Medium.</summary>
        public static void Attach(Transform mapRoot)
        {
            var go = new GameObject("Map3DBakedShadow");
            go.transform.SetParent(mapRoot, false);
            go.AddComponent<Map3DBakedShadow>();
        }

        void OnEnable() { GraphicsQuality.Changed += Apply; Apply(); }

        void OnDestroy()
        {
            GraphicsQuality.Changed -= Apply;
            Shader.SetGlobalVector(ParamId, Vector4.zero);
            if (rt != null) { rt.Release(); Destroy(rt); }
        }

        void Apply()
        {
            bool on = GraphicsQuality.Current == GraphicsQuality.Level.Medium && !Map3D.Test("nobake");
            if (on && rt == null) Bake();
            Light sun = RenderSettings.sun;
            float floor = 1f - (sun != null ? sun.shadowStrength : 0.46f); // same darkness as the realtime shadow
            Shader.SetGlobalVector(ParamId, on && rt != null ? new Vector4(1f, 1.1f / Size, 0.07f * Map3D.M, floor) : Vector4.zero);
        }

        void Bake()
        {
            Light sun = RenderSettings.sun;
            Shader shader = Resources.Load<Shader>("Shaders/Map3DShadowBake");
            if (sun == null || shader == null || !shader.isSupported || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat))
            {
                Debug.LogWarning("[ZenoniaProto] Map3D baked shadow unavailable (sun / shader / RFloat)");
                return;
            }
            float t0 = Time.realtimeSinceStartup;
            Quaternion rot = sun.transform.rotation;
            Quaternion inv = Quaternion.Inverse(rot);
            // world box of everything that casts or receives: the terrain with its apron, from the stream bed up to the tallest crowns (up = -z)
            float a = Map3DHeightField.Apron * Map3D.M;
            Vector3 lo = new Vector3(-a, -a, -9f * Map3D.M), hi = new Vector3(GameConfig.MapW + a, GameConfig.MapH + a, 1f * Map3D.M);
            Vector3 min = Vector3.one * float.MaxValue, max = -min;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = inv * new Vector3((i & 1) == 0 ? lo.x : hi.x, (i & 2) == 0 ? lo.y : hi.y, (i & 4) == 0 ? lo.z : hi.z);
                min = Vector3.Min(min, c); max = Vector3.Max(max, c);
            }
            var camGo = new GameObject("Map3DShadowBakeCamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.transform.SetPositionAndRotation(rot * new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, min.z - 4f), rot);
            cam.orthographic = true;
            cam.orthographicSize = (max.y - min.y) * 0.5f;
            cam.aspect = (max.x - min.x) / (max.y - min.y);
            cam.nearClipPlane = 1f;
            cam.farClipPlane = max.z - min.z + 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;          // depth 1 = nothing between the sun and the ground
            cam.cullingMask = 1 << BakeLayer;
            cam.allowHDR = false; cam.allowMSAA = false;
            // world -> (u, v, depth 0..1 from the camera plane): the bake shader writes this depth, the receivers compare against it (Map3DCommon)
            Transform ct = cam.transform;
            float hy = cam.orthographicSize, hx = hy * cam.aspect, far = cam.farClipPlane;
            Vector3 o = ct.position, r = ct.right, u = ct.up, f = ct.forward;
            var m = new Matrix4x4();
            m.SetRow(0, new Vector4(r.x / (2f * hx), r.y / (2f * hx), r.z / (2f * hx), 0.5f - Vector3.Dot(o, r) / (2f * hx)));
            m.SetRow(1, new Vector4(u.x / (2f * hy), u.y / (2f * hy), u.z / (2f * hy), 0.5f - Vector3.Dot(o, u) / (2f * hy)));
            m.SetRow(2, new Vector4(f.x / far, f.y / far, f.z / far, -Vector3.Dot(o, f) / far));
            m.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
            Shader.SetGlobalMatrix(VpId, m);
            var target = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.RFloat);
            cam.targetTexture = target;
            int n = Map3DBatcher.BakeCasters(BakeLayer, true);
            try { cam.RenderWithShader(shader, "RenderType"); }
            finally { Map3DBatcher.BakeCasters(BakeLayer, false); }
            cam.targetTexture = null;
            Destroy(camGo);
            rt = new RenderTexture(Size, Size, 0, RenderTextureFormat.RFloat) { name = "Map3DBakedShadow", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, useMipMap = false, hideFlags = HideFlags.DontSave };
            rt.Create();
            Graphics.CopyTexture(target, rt);             // keep the depth only: the bake's 24-bit z-buffer goes back to the pool
            RenderTexture.ReleaseTemporary(target);
            Shader.SetGlobalTexture(TexId, rt);
            Debug.Log("[ZenoniaProto] Map3D baked sun shadow: " + n + " casters into " + Size + "x" + Size + " in " + Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms");
        }
    }
}
