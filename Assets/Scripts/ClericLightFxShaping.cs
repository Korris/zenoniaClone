using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shaping options of a ClericLightFx layer (2026-11-21, richer Spirit Blessing / Divine Aegis receive burst, RO3 rule 4 "strong" skill):
    ///  - Reveal: a ground ring / sigil draws itself in clockwise (LightBlob _Reveal) over its first `ms`, a bright head on the sweep front;
    ///  - Pop: the size goes W0 -> W1 over `ms` with a small overshoot (ease-out back) and then holds W1: an emblem popping in;
    ///  - Drop: the layer starts `dy` units higher and falls onto its place over `ms`, dipping ~10 % past it and settling (a dome dropping);
    ///  - Phases: brightness in RO3's four phases on top of the envelope: lead-in at `lead` (25-45 %) until `burstMs`, a 70 ms rise to 100 %,
    ///    held until `peakEndMs`, then 120 ms down to `hold` (45-70 %) until the envelope fades it out.
    /// All times are ms from the layer's own start.
    /// </summary>
    public sealed partial class ClericLightFx
    {
        public ClericLightFx Reveal(float ms) { if (layers.Count > 0) Last.RevealMs = Mathf.Max(1f, ms); return this; }

        /// <summary>Fixed rotation (degrees, around the view axis) of the last added layer instead of its random start angle: a streak or petal laid
        /// along a direction (boss skills, CaveBossLightFx, 21/11/2026). Spin, if any, still adds on top.</summary>
        public ClericLightFx Turn(float deg) { if (layers.Count > 0) { Last.Rot = deg; Last.Turned = true; } return this; }

        /// <summary>The last added layer burns away (shader _Dissolve 0 -> 1) over its last `ms` instead of the default second-half dissolve:
        /// a summoned object that stays whole through the hits and crumbles at the end (Earth Break painted sword, 21/11/2026).</summary>
        public ClericLightFx DissolveOut(float ms) { if (layers.Count > 0) Last.DissolveMs = Mathf.Max(1f, ms); return this; }

        public ClericLightFx Pop(float ms) { if (layers.Count > 0) Last.PopMs = Mathf.Max(1f, ms); return this; }

        static readonly System.Collections.Generic.Dictionary<int, Mesh> AimMeshes = new System.Collections.Generic.Dictionary<int, Mesh>();

        /// <summary>Lays the last added (centred, unsquashed: Ground(..., squash 1)) quad on the ground TURNED toward `deg` (0 = right, 90 = up the
        /// screen) and then squashed by `squash`: the quad's +x axis points along the aim on the ground plane, so a fan drawn along +x (RoSoftSector)
        /// faces the strike (a plain localRotation would squash first and tilt the fan). `mirror` flips the quad's v: the fan opens the other way
        /// round. Meshes are baked once per whole degree (RO trailer style Warrior sectors, 21/11/2026).</summary>
        public ClericLightFx Aim(float deg, float squash, bool mirror = false)
        {
            if (layers.Count == 0) return this;
            int d = Mathf.RoundToInt(Mathf.Repeat(deg, 360f)), key = d * 4000 + Mathf.RoundToInt(squash * 1000f) * 2 + (mirror ? 1 : 0);
            Mesh m;
            if (!AimMeshes.TryGetValue(key, out m) || m == null)
            {
                float a = d * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
                var v = new Vector3[4];
                Vector2[] corners = { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f) };
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = corners[i];
                    v[i] = new Vector3(p.x * c - p.y * s, (p.x * s + p.y * c) * squash, 0f);
                }
                m = new Mesh { name = "LightQuadAim" };
                m.vertices = v;
                m.uv = mirror ? new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) }
                              : new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
                m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
                m.RecalculateBounds();
                AimMeshes[key] = m;
            }
            MeshFilter f = Last.R.GetComponent<MeshFilter>();
            if (f != null) f.sharedMesh = m;
            return this;
        }

        public ClericLightFx Drop(float dy, float ms)
        {
            if (layers.Count == 0) return this;
            Layer l = Last;
            l.Offset += new Vector2(0f, dy); l.Move = new Vector2(0f, -dy); l.DropMs = Mathf.Max(1f, ms);
            return this;
        }

        public ClericLightFx Phases(float burstMs, float peakEndMs, float lead, float hold)
        {
            if (layers.Count == 0) return this;
            Layer l = Last;
            l.PhBurst = Mathf.Max(0f, burstMs); l.PhPeakEnd = Mathf.Max(burstMs + 70f, peakEndMs); l.PhLead = lead; l.PhHold = hold;
            return this;
        }

        static float PhaseGain(Layer l, float local)
        {
            if (l.PhBurst < 0f) return 1f;
            if (local < l.PhBurst) return l.PhLead;
            if (local < l.PhBurst + 70f) return Mathf.Lerp(l.PhLead, 1f, (local - l.PhBurst) / 70f);
            if (local < l.PhPeakEnd) return 1f;
            return Mathf.Lerp(1f, l.PhHold, Mathf.SmoothStep(0f, 1f, (local - l.PhPeakEnd) / 120f));
        }

        /// <summary>Ease-out back on 0..1 (peaks ~1.1 near 0.6, ends at 1); 1 past the end.</summary>
        static float PopCurve(float u)
        {
            if (u >= 1f) return 1f;
            float g = Mathf.Clamp01(u) - 1f;
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * g * g * g + c1 * g * g;
        }
    }
}
