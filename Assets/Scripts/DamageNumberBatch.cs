using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Batched drawing of the floating numbers (22/11 RO spec, performance): instead of one IMGUI DrawTexture call per glyph / spark
    /// (each its own draw call, ~360 calls with 90 live numbers), every quad of a frame is collected into a bucket per (layer, texture)
    /// and each bucket goes out as ONE GL quad batch in the Repaint pass. Layers keep the old paint order where it matters (behind FX,
    /// digits, front sparks; crits and totals above the rest). The glyph outline is baked into the sprite-font atlases, so an outline
    /// costs nothing extra (no 8-copy outline like RoTextOutline). Lists are reused frame to frame: no per-hit allocation.
    /// Falls back to plain IMGUI calls when the shader is missing (Resources/Shaders/DamageNumberQuad.shader).
    /// </summary>
    public static class DamageNumberBatch
    {
        struct Quad { public Vector2 A, B, C, D; public Rect Uv; public Color Col; } // corners clockwise from top-left, GUI px

        class Bucket { public Texture2D Tex; public int Layer, Order; public readonly List<Quad> Quads = new List<Quad>(128); }

        static readonly List<Bucket> Buckets = new List<Bucket>(16);
        static readonly List<Bucket> Live = new List<Bucket>(16);
        static Material mat;
        static bool matTried, open;

        static Material Mat
        {
            get
            {
                if (matTried) return mat;
                matTried = true;
                Shader s = Resources.Load<Shader>("Shaders/DamageNumberQuad");
                if (s != null && s.isSupported) mat = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
                else Debug.LogWarning("[ZenoniaProto] damage numbers: no DamageNumberQuad shader, IMGUI fallback");
                return mat;
            }
        }

        /// <summary>Starts a frame of quads (call in the Repaint event before any Add).</summary>
        public static void Begin()
        {
            for (int i = 0; i < Live.Count; i++) Live[i].Quads.Clear();
            Live.Clear();
            open = Mat != null;
        }

        /// <summary>Adds a textured quad (GUI px, y down) rotated deg around its centre; drawn at once through IMGUI when batching is off.</summary>
        public static void Add(Texture2D tex, Rect r, Rect uv, Color col, int layer, float deg = 0f)
        {
            if (tex == null || col.a <= 0.004f) return;
            if (!open)
            {
                Color old = GUI.color;
                GUI.color = col;
                if (deg == 0f) GUI.DrawTextureWithTexCoords(r, tex, uv);
                else
                {
                    Matrix4x4 saved = GUI.matrix;
                    GUIUtility.RotateAroundPivot(deg, r.center);
                    GUI.DrawTextureWithTexCoords(r, tex, uv);
                    GUI.matrix = saved;
                }
                GUI.color = old;
                return;
            }
            var q = new Quad { Uv = uv, Col = col };
            if (deg == 0f)
            {
                q.A = new Vector2(r.xMin, r.yMin); q.B = new Vector2(r.xMax, r.yMin);
                q.C = new Vector2(r.xMax, r.yMax); q.D = new Vector2(r.xMin, r.yMax);
            }
            else
            {
                float rad = deg * Mathf.Deg2Rad, cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
                Vector2 c = r.center, hx = new Vector2(cs, sn) * (r.width * 0.5f), hy = new Vector2(-sn, cs) * (r.height * 0.5f);
                q.A = c - hx - hy; q.B = c + hx - hy; q.C = c + hx + hy; q.D = c - hx + hy;
            }
            BucketFor(tex, layer).Quads.Add(q);
        }

        static Bucket BucketFor(Texture2D tex, int layer)
        {
            for (int i = 0; i < Live.Count; i++) if (Live[i].Tex == tex && Live[i].Layer == layer) return Live[i];
            Bucket b = null;
            for (int i = 0; i < Buckets.Count; i++) if (!Live.Contains(Buckets[i])) { b = Buckets[i]; break; }
            if (b == null) { b = new Bucket(); Buckets.Add(b); }
            b.Tex = tex; b.Layer = layer; b.Order = Live.Count;
            Live.Add(b);
            return b;
        }

        static int ByLayer(Bucket x, Bucket y) { return x.Layer != y.Layer ? x.Layer.CompareTo(y.Layer) : x.Order.CompareTo(y.Order); }

        /// <summary>Draws every bucket: one GL batch (one draw call) per (layer, texture), lower layers first.</summary>
        public static void Flush()
        {
            if (!open || Live.Count == 0) return;
            open = false;
            Live.Sort(ByLayer);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f); // GUI space: origin top-left, y down
            for (int i = 0; i < Live.Count; i++)
            {
                Bucket b = Live[i];
                if (b.Quads.Count == 0) continue;
                mat.mainTexture = b.Tex;
                mat.SetPass(0);
                GL.Begin(GL.QUADS);
                for (int k = 0; k < b.Quads.Count; k++)
                {
                    Quad q = b.Quads[k];
                    GL.Color(q.Col);
                    GL.TexCoord2(q.Uv.xMin, q.Uv.yMax); GL.Vertex3(q.A.x, q.A.y, 0f);
                    GL.TexCoord2(q.Uv.xMax, q.Uv.yMax); GL.Vertex3(q.B.x, q.B.y, 0f);
                    GL.TexCoord2(q.Uv.xMax, q.Uv.yMin); GL.Vertex3(q.C.x, q.C.y, 0f);
                    GL.TexCoord2(q.Uv.xMin, q.Uv.yMin); GL.Vertex3(q.D.x, q.D.y, 0f);
                }
                GL.End();
            }
            GL.PopMatrix();
        }
    }
}
