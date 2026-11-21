using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Dynamic ground blobs (hero, pet, monster, boss, loot, NPC, turret). Each entity keeps its old "Shadow" SpriteRenderer and keeps driving it
    /// (position, width, enabled, fade alpha), so nothing in the entity code changes; registered here with <see cref="Attach"/>, that sprite is
    /// hidden (forceRenderingOff) while FakeSun is on and becomes one quad of a shared mesh rebuilt every frame from FakeSun's rules.
    /// GroundShadowRenderer draws the mesh into the max-opacity mask (one draw call for all blobs, no per-blob material).
    /// </summary>
    public static class GroundShadowBlobs
    {
        sealed class Entry
        {
            public SpriteRenderer Sr;
            public ShadowKind Kind;
            public Transform Lifted;   // part raised off the ground (hero lean, monster body); its local y = height
            public float BaseAlpha;    // the old sprite's full-strength alpha: its current alpha / this = fade
        }

        static readonly List<Entry> Entries = new List<Entry>();
        static readonly List<Vector3> Verts = new List<Vector3>();
        static readonly List<Vector2> Uvs = new List<Vector2>();
        static readonly List<Color> Cols = new List<Color>();
        static readonly List<int> Tris = new List<int>();
        static Mesh mesh;

        /// <summary>Registers an entity's old shadow sprite. `lifted`: the transform raised when the unit leaves the ground (null = never).</summary>
        public static void Attach(SpriteRenderer shadowSr, ShadowKind kind, Transform lifted = null, float baseAlpha = 0.3f)
        {
            if (shadowSr == null) return;
            Entries.Add(new Entry { Sr = shadowSr, Kind = kind, Lifted = lifted, BaseAlpha = Mathf.Max(0.01f, baseAlpha) });
        }

        /// <summary>The shared blob mesh (world space, z = 0), filled by <see cref="Rebuild"/>.</summary>
        public static Mesh Mesh
        {
            get
            {
                if (mesh == null) { mesh = new Mesh { name = "GroundShadowBlobs" }; mesh.MarkDynamic(); }
                return mesh;
            }
        }

        /// <summary>Rebuilds the mesh from every live entity inside `view` (world rect). Old sprites are hidden while `on`, shown again when off.</summary>
        public static int Rebuild(bool on, Rect view)
        {
            Verts.Clear(); Uvs.Clear(); Cols.Clear(); Tris.Clear();
            // Character-screen previews film a HeroRenderer far outside the map with their own camera: those keep their old sprite shadow.
            Rect world = on && MapRegistry.Current != null ? new Rect(-64f, -64f, GameConfig.MapW + 128f, GameConfig.MapH + 128f) : new Rect();
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                Entry e = Entries[i];
                if ((object)e.Sr == null || e.Sr == null) { Entries.RemoveAt(i); continue; } // entity destroyed
                Vector3 p = e.Sr.transform.position;
                bool hide = on && world.Contains(new Vector2(p.x, p.y));
                if (e.Sr.forceRenderingOff != hide) e.Sr.forceRenderingOff = hide;
                if (!hide || !e.Sr.enabled || !e.Sr.gameObject.activeInHierarchy) continue;
                AddEntry(e, view);
            }
            Mesh m = Mesh;
            m.Clear();
            m.SetVertices(Verts);
            m.SetUVs(0, Uvs);
            m.SetColors(Cols);
            m.SetTriangles(Tris, 0, false);
            m.bounds = new Bounds(new Vector3(view.center.x, view.center.y, 0f), new Vector3(view.width + 64f, view.height + 64f, 10f));
            return Verts.Count / 4;
        }

        static void AddEntry(Entry e, Rect view)
        {
            Transform t = e.Sr.transform;
            Vector3 p = t.position;
            float width = Mathf.Abs(t.lossyScale.x);
            if (!view.Overlaps(new Rect(p.x - width, p.y - width, width * 2f, width * 2f))) return; // off screen
            BlobRule rule = FakeSun.Blob(e.Kind);
            float fade = Mathf.Clamp01(e.Sr.color.a / e.BaseAlpha);
            if (fade <= 0.01f) return;
            float height = Mathf.Max(0f, (e.Lifted != null ? e.Lifted.localPosition.y : 0f) - t.localPosition.y); // pets hover by pushing their shadow down
            float k = Mathf.Clamp01(height / FakeSun.LiftFull);
            width *= rule.WidthMul * (1f + FakeSun.LiftGrow * k);
            float alpha = rule.Alpha * fade * (1f - FakeSun.LiftFade * k);
            Vector2 c = new Vector2(p.x, p.y) + FakeSun.Along(width * rule.OffsetShare);
            AddQuad(c, width * 0.5f, width * 0.5f * FakeSun.BlobDepth, alpha);
            if (e.Kind == ShadowKind.Boss) // a small darker contact patch under the belly (spec 2.4)
            {
                float cw = width * FakeSun.BossContactShare;
                AddQuad(new Vector2(p.x, p.y) + FakeSun.Along(cw * 0.1f), cw * 0.5f, cw * 0.5f * FakeSun.BlobDepth, FakeSun.BossContactAlpha * fade * (1f - FakeSun.LiftFade * k));
            }
        }

        /// <summary>One ellipse quad: uv -1..1 (the mask shader turns it into a feathered disc), colour a = alpha, r = feather.</summary>
        static void AddQuad(Vector2 c, float rx, float ry, float alpha)
        {
            int b = Verts.Count;
            Verts.Add(new Vector3(c.x - rx, c.y - ry, 0f)); Verts.Add(new Vector3(c.x - rx, c.y + ry, 0f));
            Verts.Add(new Vector3(c.x + rx, c.y + ry, 0f)); Verts.Add(new Vector3(c.x + rx, c.y - ry, 0f));
            Uvs.Add(new Vector2(-1f, -1f)); Uvs.Add(new Vector2(-1f, 1f)); Uvs.Add(new Vector2(1f, 1f)); Uvs.Add(new Vector2(1f, -1f));
            var col = new Color(FakeSun.Feather, 0f, 0f, alpha);
            Cols.Add(col); Cols.Add(col); Cols.Add(col); Cols.Add(col);
            Tris.Add(b); Tris.Add(b + 1); Tris.Add(b + 2); Tris.Add(b); Tris.Add(b + 2); Tris.Add(b + 3);
        }
    }
}
