using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Procedural lock ring (TargetMarker): an ellipse strip mesh, a gold core of constant SCREEN width with a 1 px soft brown edge on both sides,
    /// so it reads on snow and on pastel grass and has no stair-step pixels (no texture, no MSAA needed). Offsets are added to both radii, so
    /// the line is as thick at the top and bottom of the ellipse as at its sides. Vertices are rebuilt only when the size changes.
    /// </summary>
    public sealed class TargetRingMesh
    {
        const int Segments = 96;
        static readonly Color Edge = new Color(0.30f, 0.20f, 0.08f, 1f); // soft dark brown outline (RO Soft: brown ink, never black)
        const float EdgeAlpha = 0.4f;
        // Six loops from outside in: brown 0, brown edge, gold, gold, brown edge, brown 0.
        static readonly float[] LoopAlpha = { 0f, EdgeAlpha, 1f, 1f, EdgeAlpha, 0f };
        static readonly bool[] LoopGold = { false, false, true, true, false, false };

        readonly GameObject go;
        readonly MeshRenderer mr;
        readonly Mesh mesh;
        readonly Vector3[] verts = new Vector3[6 * (Segments + 1)];
        readonly Color[] cols = new Color[6 * (Segments + 1)];
        float lastRx = -1f, lastRy, lastLine, lastPx;

        TargetRingMesh(Transform parent)
        {
            go = new GameObject("TargetRing");
            go.transform.SetParent(parent, false);
            mesh = new Mesh { name = "TargetRing" };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var tris = new int[5 * Segments * 6];
            int t = 0;
            for (int l = 0; l < 5; l++)
                for (int i = 0; i < Segments; i++)
                {
                    int a = l * (Segments + 1) + i, b = a + 1, c = a + Segments + 1, d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.triangles = tris;
            mr.enabled = false;
        }

        public static TargetRingMesh Create(Transform parent) { return new TargetRingMesh(parent); }

        public bool Visible { set { mr.enabled = value; } }

        /// <summary>Ellipse at `centre` (rx x ry world units), gold line `line` units wide with `px`-unit feathered edges, tinted `tint`.</summary>
        public void Draw(Vector3 centre, float rx, float ry, float line, float px, Color tint, int sortingOrder)
        {
            go.transform.position = centre;
            mr.sortingOrder = sortingOrder;
            if (Mathf.Abs(rx - lastRx) > 0.01f || Mathf.Abs(ry - lastRy) > 0.01f || Mathf.Abs(line - lastLine) > 0.001f || Mathf.Abs(px - lastPx) > 0.001f)
            {
                lastRx = rx; lastRy = ry; lastLine = line; lastPx = px;
                float h = line * 0.5f;
                float[] off = { h + 2f * px, h + px, h, -h, -h - px, -h - 2f * px };
                for (int l = 0; l < 6; l++)
                    for (int i = 0; i <= Segments; i++)
                    {
                        float a = i * (2f * Mathf.PI / Segments);
                        verts[l * (Segments + 1) + i] = new Vector3(Mathf.Cos(a) * (rx + off[l]), Mathf.Sin(a) * (ry + off[l]), 0f);
                    }
                mesh.vertices = verts;
                mesh.RecalculateBounds();
            }
            for (int l = 0; l < 6; l++)
            {
                Color c = LoopGold[l] ? tint : Edge;
                c.a = LoopAlpha[l] * tint.a;
                for (int i = 0; i <= Segments; i++) cols[l * (Segments + 1) + i] = c;
            }
            mesh.colors = cols;
        }
    }
}
