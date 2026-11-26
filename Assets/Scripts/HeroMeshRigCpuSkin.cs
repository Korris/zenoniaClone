using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// CPU linear-blend-skinning fallback of the TEST mesh rig (HeroMeshRigView), used only when SpriteSkin refuses the runtime sprites
    /// (state not Ready / package missing). Same data and same matrices as SpriteSkin (bone matrix in rig-root space x bind pose, weighted
    /// sum of up to 4 bones per vertex), drawn by a MeshFilter + MeshRenderer (Sprites/Default, vertex colour = tint) per slot, so both
    /// paths draw the same picture.
    /// </summary>
    public sealed class HeroMeshRigCpuSkin
    {
        readonly HeroMeshRigSet set;
        readonly MeshFilter[] filters;
        readonly MeshRenderer[] renderers;
        readonly Mesh[] meshes;          // one Unity mesh per json mesh (swaps switch the filter's mesh)
        readonly Vector3[][] work;
        readonly Color[][] colours;
        readonly Matrix4x4[] skin;

        public HeroMeshRigCpuSkin(HeroMeshRigSet rigSet, Transform root)
        {
            set = rigSet;
            int ns = set.Slots.Length, nm = set.Meshes.Length;
            filters = new MeshFilter[ns]; renderers = new MeshRenderer[ns];
            Shader sh = Shader.Find("Sprites/Default");
            for (int s = 0; s < ns; s++)
            {
                var go = new GameObject("MeshRigCpu_" + set.Slots[s]);
                go.transform.SetParent(root, false);
                filters[s] = go.AddComponent<MeshFilter>();
                renderers[s] = go.AddComponent<MeshRenderer>();
                renderers[s].sharedMaterial = new Material(sh) { mainTexture = null };
            }
            meshes = new Mesh[nm]; work = new Vector3[nm][]; colours = new Color[nm][];
            for (int i = 0; i < nm; i++)
            {
                var m = new Mesh { name = "MeshRigCpu_" + set.Meshes[i].name };
                m.MarkDynamic();
                m.vertices = set.LocalVerts[i];
                m.uv = set.Uvs[i];
                m.triangles = set.Meshes[i].t;
                work[i] = new Vector3[set.LocalVerts[i].Length];
                colours[i] = new Color[work[i].Length];
                meshes[i] = m;
            }
            skin = new Matrix4x4[set.Bones.Length];
        }

        public void Hide() { foreach (MeshRenderer r in renderers) r.enabled = false; }

        /// <summary>Skins json mesh `mi` into slot `s` with the bone matrices (rig-root space) of this frame.</summary>
        public void Draw(int s, int mi, Transform[] bones, int order, float z, Color tint)
        {
            for (int b = 0; b < bones.Length; b++)
                skin[b] = Matrix4x4.TRS(bones[b].localPosition, bones[b].localRotation, bones[b].localScale) * set.BindPoses[b];
            Vector3[] src = set.LocalVerts[mi], dst = work[mi];
            BoneWeight[] w = set.Weights[mi];
            for (int k = 0; k < src.Length; k++)
            {
                BoneWeight bw = w[k];
                Vector3 v = src[k];
                Vector3 o = skin[bw.boneIndex0].MultiplyPoint3x4(v) * bw.weight0;
                if (bw.weight1 > 0f) o += skin[bw.boneIndex1].MultiplyPoint3x4(v) * bw.weight1;
                if (bw.weight2 > 0f) o += skin[bw.boneIndex2].MultiplyPoint3x4(v) * bw.weight2;
                if (bw.weight3 > 0f) o += skin[bw.boneIndex3].MultiplyPoint3x4(v) * bw.weight3;
                o.z = 0f;
                dst[k] = o;
            }
            Color[] c = colours[mi];
            if (c.Length > 0 && c[0] != tint) for (int k = 0; k < c.Length; k++) c[k] = tint;
            Mesh m = meshes[mi];
            m.vertices = dst;
            m.colors = c;
            m.RecalculateBounds();
            filters[s].sharedMesh = m;
            MeshRenderer r = renderers[s];
            r.enabled = true;
            r.sharedMaterial.mainTexture = set.MeshSprites[mi].texture;
            r.sortingOrder = order;
            r.transform.localPosition = new Vector3(0f, 0f, z);
        }
    }
}
