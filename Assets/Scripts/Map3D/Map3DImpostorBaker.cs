using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZenoniaProto
{
    /// <summary>One generated tree / bush mesh (Map3DTreeBuilder key) as the impostors see it: its materials, bounding sphere and atlas rectangle.</summary>
    public sealed class Map3DImpostorKey
    {
        public int Key;
        public Mesh Mesh;
        public Material Bark, Leaf;
        public Vector3 Center;      // bounding sphere in mesh space (game frame, up = -z)
        public float Radius;
        public int Px;              // frame size in the albedo atlas (pixels)
        public int X, Y;            // first frame's bottom-left pixel in the albedo atlas
        public Vector4 Rect;        // (u0, v0, frame width, frame height) in atlas uv
    }

    /// <summary>
    /// Bakes the views of every tree / bush mesh used on a 3D map into two atlases (22/11 perf pass 2, Map3DImpostors): Frames views round the
    /// tree (object-space azimuth j x 45 deg) at the camera's pitch, orthographic over the mesh's bounding sphere. Albedo atlas (RGBA32, mipmapped,
    /// frame 224 px trees / 112 px bushes): the unlit colour exactly as Map3DFoliage / Map3DPainted compute it (leaf atlas x tint x per-cluster
    /// variation x crown AO; bark x tint x AO), premultiplied on a black transparent background. Normal atlas (half size): object-space normal
    /// (rgb) and the depth of the visible surface along the view, relative to the sphere centre (a, 0.5 = the billboard plane), so the impostor
    /// shader lights each pixel with the scene sun, the baked shadow at the real surface position and the fog, like the mesh it replaces.
    /// Cheap (a few hundred small draws): re-run when the camera pitch moves (Map3DImpostors).
    /// </summary>
    public static class Map3DImpostorBaker
    {
        public const int Frames = 8, TreePx = 224, BushPx = 112, AtlasW = TreePx * Frames * 2;
        static readonly int ViewId = Shader.PropertyToID("_ImpBakeView"), CenterId = Shader.PropertyToID("_ImpBakeCenter");
        static readonly Dictionary<Material, Material> BakeMats = new Dictionary<Material, Material>();
        static Shader bakeShader;

        /// <summary>Lays the keys out (2 trees or 4 bushes per row of 8 frames) and returns the atlas height.</summary>
        public static int Layout(List<Map3DImpostorKey> keys)
        {
            keys.Sort((a, b) => b.Px.CompareTo(a.Px)); // trees first: rows of one height
            int x = 0, y = 0, rowH = 0;
            foreach (Map3DImpostorKey k in keys)
            {
                int w = k.Px * Frames;
                if (x + w > AtlasW || (rowH != 0 && k.Px != rowH)) { x = 0; y += rowH; }
                rowH = k.Px;
                k.X = x; k.Y = y;
                x += w;
            }
            int h = y + rowH;
            h = (h + 3) / 4 * 4; // even halves for the normal atlas
            foreach (Map3DImpostorKey k in keys) k.Rect = new Vector4(k.X / (float)AtlasW, k.Y / (float)h, k.Px / (float)AtlasW, k.Px / (float)h);
            return h;
        }

        /// <summary>Bounding sphere of the mesh (centre of the bounds, farthest vertex).</summary>
        public static void Measure(Map3DImpostorKey k)
        {
            Vector3 c = k.Mesh.bounds.center;
            float r2 = 0f;
            foreach (Vector3 v in k.Mesh.vertices) r2 = Mathf.Max(r2, (v - c).sqrMagnitude);
            k.Center = c;
            k.Radius = Mathf.Sqrt(r2) * 1.02f;
        }

        public static bool Ready()
        {
            if (bakeShader == null) bakeShader = Resources.Load<Shader>("Shaders/Map3DImpostorBake");
            return bakeShader != null && bakeShader.isSupported;
        }

        /// <summary>Renders every key's frames at this camera pitch (degrees below the horizon) into albedo and normal.</summary>
        public static void Bake(List<Map3DImpostorKey> keys, RenderTexture albedo, RenderTexture normal, float pitchDeg)
        {
            var cb = new CommandBuffer { name = "Map3DImpostorBake" };
            for (int pass = 0; pass < 2; pass++)
            {
                RenderTexture target = pass == 0 ? albedo : normal;
                float s = target.width / (float)AtlasW;
                cb.SetRenderTarget(target);
                cb.ClearRenderTarget(true, true, pass == 0 ? new Color(0f, 0f, 0f, 0f) : new Color(0.5f, 0.5f, 0.5f, 0.5f));
                foreach (Map3DImpostorKey k in keys)
                    for (int j = 0; j < Frames; j++)
                    {
                        float th = j * Mathf.PI * 2f / Frames, p = pitchDeg * Mathf.Deg2Rad;
                        Vector3 toCam = new Vector3(Mathf.Cos(p) * Mathf.Cos(th), Mathf.Cos(p) * Mathf.Sin(th), -Mathf.Sin(p)); // up = -z
                        Vector3 camPos = k.Center + toCam * (k.Radius * 2f);
                        Quaternion rot = Quaternion.LookRotation(-toCam, new Vector3(0f, 0f, -1f));
                        Matrix4x4 view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.TRS(camPos, rot, Vector3.one).inverse;
                        Matrix4x4 proj = Matrix4x4.Ortho(-k.Radius, k.Radius, -k.Radius, k.Radius, k.Radius * 0.5f, k.Radius * 3.5f);
                        cb.SetViewport(new Rect((k.X + j * k.Px) * s, k.Y * s, k.Px * s, k.Px * s));
                        cb.SetViewProjectionMatrices(view, proj);
                        cb.SetGlobalVector(ViewId, toCam);
                        cb.SetGlobalVector(CenterId, new Vector4(k.Center.x, k.Center.y, k.Center.z, k.Radius));
                        for (int sub = 0; sub < 3 && sub < k.Mesh.subMeshCount; sub++)
                        {
                            if (k.Mesh.GetIndexCount(sub) == 0) continue;
                            Material m = BakeMat(sub == 0 ? k.Bark : k.Leaf, sub != 0);
                            cb.DrawMesh(k.Mesh, Matrix4x4.identity, m, sub, pass);
                        }
                    }
            }
            Graphics.ExecuteCommandBuffer(cb);
            cb.Release();
            albedo.GenerateMips();
            normal.GenerateMips();
        }

        /// <summary>Bake material of a scene material (same textures / tints; _Leaf picks the foliage or the bark formula).</summary>
        static Material BakeMat(Material src, bool leaf)
        {
            Material m;
            if (BakeMats.TryGetValue(src, out m) && m != null) return m;
            m = new Material(bakeShader) { name = "ImpostorBake_" + src.name, hideFlags = HideFlags.DontSave };
            m.CopyPropertiesFromMaterial(src);
            m.SetFloat("_Leaf", leaf ? 1f : 0f);
            BakeMats[src] = m;
            return m;
        }

        /// <summary>`--impostor-dump`: writes the albedo atlas next to the capture shots (visual check of the bake).</summary>
        public static void Dump(RenderTexture albedo, string path)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = albedo;
            var t = new Texture2D(albedo.width, albedo.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, albedo.width, albedo.height), 0, 0);
            t.Apply();
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t);
            Debug.Log("[ZenoniaProto] Map3D impostor atlas dumped to " + path);
        }
    }
}
