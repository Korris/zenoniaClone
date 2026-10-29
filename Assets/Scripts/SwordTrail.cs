using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Pooled sword-swing ribbon: one shared crescent mesh (unit radius, tapered tail) scaled to the combo reach and rotated to the
    /// facing. The TrailRibbon shader reveals it head-first (white core -> orange -> red edge) then fades it, driven by _Head / _Fade.
    /// </summary>
    public class SwordTrail : MonoBehaviour
    {
        const int Segments = 28;
        const int PoolMax = 6;
        static readonly List<SwordTrail> Pool = new List<SwordTrail>();
        static Transform root;
        static readonly Mesh[] Meshes = new Mesh[2]; // [0] normal, [1] finisher (wider)
        static MaterialPropertyBlock block;
        static int headId, fadeId, coreId, midId, edgeId;

        MeshRenderer mr;
        MeshFilter mf;
        float age, sweep;
        ClassPalette palette = ClassPalette.Neutral;

        public static void Init(Transform parent)
        {
            root = parent;
            Pool.Clear();
        }

        /// <summary>Spawns a swing ribbon centred on (x, y). angleDeg: 0 = right, 90 = up. reverse = sweep the other way (alternating swings).</summary>
        public static void Spawn(float x, float y, float angleDeg, float reach, bool finisher, bool reverse, ClassPalette palette = null)
        {
            if (!FeelConfig.FxOn || root == null) return;
            SwordTrail t = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { t = Pool[i]; break; }
            if (t == null)
            {
                if (Pool.Count >= PoolMax) return;
                t = Create();
            }
            float radius = reach + FeelConfig.TrailReachPad;
            t.sweep = finisher ? FeelConfig.TrailSweepFinisherSec : FeelConfig.TrailSweepSec;
            t.age = 0f;
            t.palette = palette ?? ClassPalette.Neutral;
            t.transform.position = new Vector3(x, y, 0f);
            t.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            // Uniform scale by radius; negative Y mirrors the arc so the swing runs the other way.
            t.transform.localScale = new Vector3(radius, reverse ? -radius : radius, 1f);
            t.mr.sortingOrder = FeelConfig.FxOrder - 1;
            t.mf.sharedMesh = MeshFor(finisher);
            t.gameObject.SetActive(true);
            t.Apply();
        }

        static SwordTrail Create()
        {
            var go = new GameObject("SwordTrail");
            go.transform.SetParent(root, false);
            var t = go.AddComponent<SwordTrail>();
            t.mf = go.AddComponent<MeshFilter>();
            t.mr = go.AddComponent<MeshRenderer>();
            t.mr.sharedMaterial = FeelMaterials.Trail;
            t.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            t.mr.receiveShadows = false;
            if (block == null) block = new MaterialPropertyBlock();
            headId = Shader.PropertyToID("_Head");
            fadeId = Shader.PropertyToID("_Fade");
            coreId = Shader.PropertyToID("_Core"); midId = Shader.PropertyToID("_Mid"); edgeId = Shader.PropertyToID("_Edge");
            Pool.Add(t);
            return t;
        }

        static Mesh MeshFor(bool finisher)
        {
            int k = finisher ? 1 : 0;
            if (Meshes[k] == null) Meshes[k] = BuildMesh(finisher ? FeelConfig.TrailWidthFinisher : FeelConfig.TrailWidthNormal);
            return Meshes[k];
        }

        /// <summary>Unit-radius arc from +span to -span; the inner edge recedes with u^0.6 so the tail is thin and the head thick.</summary>
        static Mesh BuildMesh(float width)
        {
            var m = new Mesh { name = "SwordTrailArc" };
            var verts = new Vector3[(Segments + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[Segments * 6];
            float span = FeelConfig.TrailSpanDeg * Mathf.Deg2Rad;
            for (int i = 0; i <= Segments; i++)
            {
                float u = i / (float)Segments;
                float a = Mathf.Lerp(span, -span, u);
                float inner = 1f - Mathf.Pow(u, 0.6f) * width;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                verts[i * 2] = dir;
                verts[i * 2 + 1] = dir * inner;
                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
            }
            for (int i = 0; i < Segments; i++)
            {
                int v = i * 2, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.bounds = new Bounds(Vector3.zero, new Vector3(4f, 4f, 1f));
            return m;
        }

        void Update()
        {
            age += Time.deltaTime; // real time: the ribbon keeps flowing through hit-stop, which sells the impact
            if (age >= sweep + FeelConfig.TrailFadeSec) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            float head = Mathf.Clamp01(age / sweep);
            float fade = 1f - Mathf.Clamp01((age - sweep) / FeelConfig.TrailFadeSec);
            mr.GetPropertyBlock(block);
            block.SetFloat(headId, head);
            block.SetFloat(fadeId, fade);
            block.SetColor(coreId, palette.Core);
            block.SetColor(midId, palette.Mid);
            block.SetColor(edgeId, palette.Edge);
            mr.SetPropertyBlock(block);
        }
    }
}
