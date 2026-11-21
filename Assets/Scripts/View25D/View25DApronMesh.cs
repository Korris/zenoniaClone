using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Mesh of the 2.5D border ground (View25DGroundApron): a frame round the map on z = 0, `apron` units past every edge, opaque outside the
    /// map and fading to transparent `feather` units inside it (vertex alpha), uv = world position / `uvTile` so the tiled texture is seamless.</summary>
    public static class View25DApronMesh
    {
        public static Mesh Frame(float w, float h, float apron, float feather, float uvTile)
        {
            float[] xs = { -apron, 0f, feather, w - feather, w, w + apron };
            float[] ys = { -apron, 0f, feather, h - feather, h, h + apron };
            var verts = new List<Vector3>(36);
            var uvs = new List<Vector2>(36);
            var cols = new List<Color>(36);
            for (int j = 0; j < 6; j++)
                for (int i = 0; i < 6; i++)
                {
                    float x = xs[i], y = ys[j];
                    bool outside = i <= 1 || i >= 4 || j <= 1 || j >= 4; // on or past the map edge
                    verts.Add(new Vector3(x, y, 0f));
                    uvs.Add(new Vector2(x / uvTile, y / uvTile));
                    cols.Add(new Color(1f, 1f, 1f, outside ? 1f : 0f));
                }
            var tris = new List<int>(150);
            for (int j = 0; j < 5; j++)
                for (int i = 0; i < 5; i++)
                {
                    if (i == 2 && j == 2) continue; // the map's interior: nothing drawn
                    int a = j * 6 + i, b = a + 1, c = a + 6, d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            var m = new Mesh { name = "GroundApronFrame" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
