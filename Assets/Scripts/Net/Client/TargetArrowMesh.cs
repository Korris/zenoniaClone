using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Small gold arrow over a locked Wanted boss's head (ChatGPT RO3 advice, 2026-11-20: when the foot ring is buried under 40 players and skill
    /// VFX, mark the boss from above). Procedural: a downward triangle mesh, gold with a soft brown rim, constant screen size, drawn over the FX
    /// layer, bobbing a few pixels with the ring's 1.5 s pulse (no spin).
    /// </summary>
    public sealed class TargetArrowMesh
    {
        static readonly Color Rim = new Color(0.30f, 0.20f, 0.08f, 1f);
        readonly GameObject go;
        readonly MeshRenderer mr;
        readonly Mesh mesh;
        readonly Color[] cols = new Color[6];

        TargetArrowMesh(Transform parent)
        {
            go = new GameObject("TargetArrow");
            go.transform.SetParent(parent, false);
            mesh = new Mesh { name = "TargetArrow" };
            // Outer triangle (rim) then inner triangle (gold): unit size, tip at (0, 0), 1 wide, 0.9 tall.
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0.9f, 0f), new Vector3(0.5f, 0.9f, 0f), new Vector3(0f, 0f, 0f),
                new Vector3(-0.36f, 0.81f, 0f), new Vector3(0.36f, 0.81f, 0f), new Vector3(0f, 0.17f, 0f),
            };
            mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            mesh.colors = cols;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = FeelConfig.FxOrder + 40; // over skill VFX and the crowd
            mr.enabled = false;
        }

        public static TargetArrowMesh Create(Transform parent) { return new TargetArrowMesh(parent); }

        public bool Visible { set { mr.enabled = value; } }

        /// <summary>Tip at `tip` (world), `size` world units wide, tinted `gold` (its alpha pulses).</summary>
        public void Draw(Vector3 tip, float size, Color gold)
        {
            go.transform.position = tip;
            go.transform.localScale = new Vector3(size, size, 1f);
            Color rim = new Color(Rim.r, Rim.g, Rim.b, 0.85f * gold.a);
            cols[0] = cols[1] = cols[2] = rim;
            cols[3] = cols[4] = cols[5] = gold;
            mesh.colors = cols;
        }
    }
}
