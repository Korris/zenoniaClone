using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One thin energy ring circling a hero's body for a maintained Cleric buff (2026-11-21, user: "nên làm 2 cái vòng trên người thay vì chỉ dưới
    /// chân"; spec docs/reference/cleric-buff-status-concept/midgard-fx-map-rings-shadows-chatgpt.md section 3; removed in 4b40aa2a for RO3 head
    /// emblems, back 21/11 10:30 in the RO3 palette, user: "khi buff dam và defense trên người sẽ có 1 cái vòng bao xung quanh chứ không phải icon
    /// trên đầu"). A procedural ellipse ribbon of
    /// constant width (the stroke stays 3-4 px all round, it does not thin at the front / back like a squashed texture), split in two meshes:
    /// the far (upper) half sorts just behind the hero, the near (lower) half just over the hero's own layers. Shader BuffBodyRing: solid colour
    /// with a dark rim, two bright segments running round (phase), a thin additive core, dissolve. Meshes are shared by every ring of the same size.
    /// </summary>
    public sealed class ClericBuffBodyRing
    {
        const int Steps = 40; // per half
        readonly MeshRenderer back, front;
        static readonly Dictionary<Vector3, Mesh[]> Meshes = new Dictionary<Vector3, Mesh[]>(); // (rx, ry, width) -> far, near
        static MaterialPropertyBlock block;
        static int fadeId, phaseId, dissolveId, flashId;

        /// <summary>Ring of half axes `rx` x `ry` (world units) and ribbon width `width`, drawn with `mat` (BuffBodyRing).</summary>
        public ClericBuffBodyRing(Transform parent, string name, Material mat, float rx, float ry, float width)
        {
            var key = new Vector3(rx, ry, width);
            Mesh[] m;
            if (!Meshes.TryGetValue(key, out m) || m[0] == null || m[1] == null) Meshes[key] = m = new[] { HalfMesh(rx, ry, width, false), HalfMesh(rx, ry, width, true) };
            back = Half(parent, name + "Back", mat, m[0]);
            front = Half(parent, name + "Front", mat, m[1]);
        }

        static MeshRenderer Half(Transform parent, string name, Material mat, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = false;
            return r;
        }

        /// <summary>
        /// Places the ring centred at `centre` (world). `heroOrder` = GameConfig.OrderBase(feet.y); `fade` 0..1, `dissolve` 0..1 (0 = whole),
        /// `phaseTurns` = where the bright segments are (turns round the ring; it grows anticlockwise seen from above), `flash` 0..1 = the whole
        /// ring lit (DEF hit).
        /// </summary>
        public void Lay(Vector2 centre, int heroOrder, float fade, float dissolve, float phaseTurns, float flash = 0f)
        {
            bool on = FeelConfig.FxOn && fade > 0.01f && dissolve < 0.999f;
            back.enabled = front.enabled = on;
            if (!on) return;
            if (block == null)
            {
                block = new MaterialPropertyBlock();
                fadeId = Shader.PropertyToID("_Fade"); phaseId = Shader.PropertyToID("_Phase"); dissolveId = Shader.PropertyToID("_Dissolve"); flashId = Shader.PropertyToID("_Flash");
            }
            var p = new Vector3(centre.x, centre.y, 0f);
            back.transform.position = front.transform.position = p;
            back.sortingOrder = heroOrder - 3;  // behind the body, hair, gear behind (OrderBase + 1..7)
            front.sortingOrder = heroOrder + 8; // over the hero's own layers, under the crowd-wide FX band
            block.Clear();
            block.SetFloat(fadeId, Mathf.Clamp01(fade));
            block.SetFloat(phaseId, phaseTurns - Mathf.Floor(phaseTurns));
            block.SetFloat(dissolveId, Mathf.Clamp01(dissolve));
            block.SetFloat(flashId, Mathf.Clamp01(flash));
            back.SetPropertyBlock(block);
            front.SetPropertyBlock(block);
        }

        public void Hide() { back.enabled = front.enabled = false; }

        /// <summary>Removes both halves (the crowd tier changed and the ring is rebuilt thinner / wider). The shared meshes stay cached.</summary>
        public void Destroy()
        {
            if (back != null) Object.Destroy(back.gameObject);
            if (front != null) Object.Destroy(front.gameObject);
        }

        /// <summary>
        /// Half of the ellipse ribbon: far half = angles 0..pi (upper on screen), near half = pi..2 pi. uv.x = angle / 2 pi (continuous over both
        /// halves so the bright segments run across the seam), uv.y = 0 inner .. 1 outer edge. Offsets follow the ellipse normal: constant width.
        /// </summary>
        static Mesh HalfMesh(float rx, float ry, float width, bool near)
        {
            var v = new Vector3[(Steps + 1) * 2];
            var uv = new Vector2[v.Length];
            var tri = new int[Steps * 6];
            float a0 = near ? Mathf.PI : 0f, hw = width * 0.5f;
            for (int i = 0; i <= Steps; i++)
            {
                float a = a0 + Mathf.PI * i / Steps, c = Mathf.Cos(a), s = Mathf.Sin(a);
                Vector2 pt = new Vector2(c * rx, s * ry), n = new Vector2(c / rx, s / ry).normalized;
                v[i * 2] = pt - n * hw;
                v[i * 2 + 1] = pt + n * hw;
                float u = a / (Mathf.PI * 2f);
                uv[i * 2] = new Vector2(u, 0f);
                uv[i * 2 + 1] = new Vector2(u, 1f);
            }
            for (int i = 0; i < Steps; i++)
            {
                int k = i * 2, t = i * 6;
                tri[t] = k; tri[t + 1] = k + 2; tri[t + 2] = k + 1;
                tri[t + 3] = k + 1; tri[t + 4] = k + 2; tri[t + 5] = k + 3;
            }
            var m = new Mesh { name = near ? "BuffRingNear" : "BuffRingFar", vertices = v, uv = uv, triangles = tri };
            m.RecalculateBounds();
            return m;
        }
    }
}
