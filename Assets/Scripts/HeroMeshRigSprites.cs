using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace ZenoniaProto
{
    /// <summary>
    /// Builds the skinned Sprites of a HeroMeshRigSet at load time (no editor / Skinning Editor step): Sprite.Create on the part texture
    /// (pivot = feet), then UnityEngine.U2D.SpriteDataAccessExtensions writes the json mesh into it: vertex positions (sprite space =
    /// HeroMeshRigSet.Local), uvs, triangle indices, BlendWeight (BoneWeight, 4 per vertex), bind poses and SpriteBones. That is exactly the
    /// data the 2D Animation Skinning Editor would bake into a sprite, so SpriteSkin deforms it; HeroMeshRigCpuSkin uses the same arrays.
    /// Bind pose of bone b = inverse(TRS(rest joint, rest axis, 1)) in sprite space, so a bone transform at its rest TRS leaves the art as painted.
    /// </summary>
    public sealed partial class HeroMeshRigSet
    {
        public Matrix4x4[] BindPoses;
        public Vector3[][] LocalVerts;     // per mesh, sprite space (also the CPU skin's rest positions)
        public Vector2[][] Uvs;
        public BoneWeight[][] Weights;

        /// <summary>Rest TRS of bone b in sprite / rig-root space (position, z rotation in Unity degrees = -screen clockwise).</summary>
        public Matrix4x4 RestMatrix(int b)
        {
            return Matrix4x4.TRS(Local(new Vector2(Bones[b].x, Bones[b].y)), Quaternion.Euler(0f, 0f, -RestAxis[b]), Vector3.one);
        }

        bool BuildSprites(string sheets, string path)
        {
            int nb = Bones.Length;
            BindPoses = new Matrix4x4[nb];
            var spriteBones = new SpriteBone[nb];
            for (int b = 0; b < nb; b++)
            {
                BindPoses[b] = RestMatrix(b).inverse;
                Vector2 j = Local(new Vector2(Bones[b].x, Bones[b].y)), e = Local(new Vector2(Bones[b].ex, Bones[b].ey));
                spriteBones[b] = new SpriteBone { name = Bones[b].name, parentId = BoneParent[b], position = j, rotation = Quaternion.Euler(0f, 0f, -RestAxis[b]), length = Vector2.Distance(j, e) };
            }
            int nm = Meshes.Length;
            MeshSprites = new Sprite[nm];
            LocalVerts = new Vector3[nm][]; Uvs = new Vector2[nm][]; Weights = new BoneWeight[nm][];
            for (int i = 0; i < nm; i++)
            {
                Part m = Meshes[i];
                Texture2D tex = SpriteSheet.TryLoad(sheets + m.sprite);
                if (tex == null) { Debug.LogWarning("[ZenoniaProto] mesh rig " + path + ": missing " + sheets + m.sprite); return false; }
                int n = m.v.Length / 2;
                if (n == 0 || n > 65535 || m.bi.Length != n * 4 || m.bw.Length != n * 4) { Debug.LogWarning("[ZenoniaProto] mesh rig " + path + ": bad mesh " + m.name); return false; }
                bool ownUv = m.u != null && m.u.Length == m.v.Length;
                var lv = new Vector3[n]; var uv = new Vector2[n]; var bw = new BoneWeight[n];
                for (int k = 0; k < n; k++)
                {
                    lv[k] = Local(new Vector2(m.v[2 * k], m.v[2 * k + 1]));
                    float ux = ownUv ? m.u[2 * k] : m.v[2 * k], uy = ownUv ? m.u[2 * k + 1] : m.v[2 * k + 1];
                    uv[k] = new Vector2(ux / tex.width, 1f - uy / tex.height);
                    bw[k] = new BoneWeight
                    {
                        boneIndex0 = m.bi[4 * k], boneIndex1 = m.bi[4 * k + 1], boneIndex2 = m.bi[4 * k + 2], boneIndex3 = m.bi[4 * k + 3],
                        weight0 = m.bw[4 * k], weight1 = m.bw[4 * k + 1], weight2 = m.bw[4 * k + 2], weight3 = m.bw[4 * k + 3],
                    };
                }
                LocalVerts[i] = lv; Uvs[i] = uv; Weights[i] = bw;
                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(CellW * 0.5f / tex.width, 1f - FeetY / (float)tex.height), Ppu, 0, SpriteMeshType.FullRect);
                s.name = "MeshRig_" + m.name;
                var pos = new NativeArray<Vector3>(lv, Allocator.Temp);
                var uvs = new NativeArray<Vector2>(uv, Allocator.Temp);
                var wts = new NativeArray<BoneWeight>(bw, Allocator.Temp);
                var idx = new NativeArray<ushort>(m.t.Length, Allocator.Temp);
                for (int k = 0; k < m.t.Length; k++) idx[k] = (ushort)m.t[k];
                var binds = new NativeArray<Matrix4x4>(BindPoses, Allocator.Temp);
                s.SetVertexCount(n);
                s.SetVertexAttribute(VertexAttribute.Position, pos);
                s.SetVertexAttribute(VertexAttribute.TexCoord0, uvs);
                s.SetVertexAttribute(VertexAttribute.BlendWeight, wts);
                s.SetIndices(idx);
                s.SetBindPoses(binds);
                s.SetBones(spriteBones);
                pos.Dispose(); uvs.Dispose(); wts.Dispose(); idx.Dispose(); binds.Dispose();
                MeshSprites[i] = s;
            }
            return true;
        }
    }
}
