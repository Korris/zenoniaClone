using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// One light for everything on a RO3 2.5D map (View25D.Ro3Look; ChatGPT RO3 comparison section 3: "warm sun on lit areas, cool shade in
    /// shadow, the same on ground, trees, rocks and characters"). All values come from the map's View25DProfile and FakeSun:
    ///  - ground chunks, flat decor, standing props get the shared Sprite25DLit materials (sun tint, fog; props: sun-side shading + base AO;
    ///    trees: + trunk shade + wind sway). Three shared materials, so props keep batching.
    ///  - heroes, monsters, pets keep their own shaders (recolour, white flash) and take a per-unit tint through the `_Color` property block:
    ///    a lighter sun tint, and the cool shade when they stand in a canopy / building shadow (PropShadowSilhouettes.Field under the feet, eased).
    ///  - globals for the ground-shadow composite (dapple, fog fade) and the lit shader, refreshed per frame; zeroed when the map is not RO3.
    /// `--view25d-look off` skips all of it (A/B against the plain 2.5D view).
    /// </summary>
    public static class View25DLighting
    {
        static readonly int SunId = Shader.PropertyToID("_Sun25D"), CrownId = Shader.PropertyToID("_CrownShade25D"), FogId = Shader.PropertyToID("_Fog25D"),
            FogColorId = Shader.PropertyToID("_Fog25DColor"), WindId = Shader.PropertyToID("_Wind25D"), DappleId = Shader.PropertyToID("_Dapple25D"),
            ColorId = Shader.PropertyToID("_Color");
        static Material ground, prop, tree;
        static bool tried;
        static MaterialPropertyBlock block;

        sealed class UnitLight { public readonly List<SpriteRenderer> Parts = new List<SpriteRenderer>(8); public float Shade; public Color Applied; public int RefreshIn; }
        static readonly Dictionary<Transform, UnitLight> Units = new Dictionary<Transform, UnitLight>();
        static readonly List<Transform> Gone = new List<Transform>();

        static bool Ready()
        {
            if (!tried)
            {
                tried = true;
                Shader s = Resources.Load<Shader>("Shaders/Sprite25DLit");
                if (s != null && s.isSupported)
                {
                    ground = new Material(s) { name = "Sprite25DLit_Ground", hideFlags = HideFlags.DontSave };
                    prop = new Material(s) { name = "Sprite25DLit_Prop", hideFlags = HideFlags.DontSave };
                    tree = new Material(s) { name = "Sprite25DLit_Tree", hideFlags = HideFlags.DontSave };
                    ground.SetFloat("_Mode", 0f); prop.SetFloat("_Mode", 1f); tree.SetFloat("_Mode", 2f);
                }
                else Debug.LogWarning("[ZenoniaProto] Sprite25DLit shader missing: RO3 2.5D maps draw unlit");
            }
            return ground != null;
        }

        /// <summary>Ground chunk or flat decor of the map being built (WorldBuilder, MapScenery).</summary>
        public static void ApplyGround(SpriteRenderer sr)
        {
            if (sr != null && View25D.Ro3Look && Ready()) sr.sharedMaterial = ground;
        }

        /// <summary>Own material for a mesh drawn like the ground (View25DGroundApron's tiled border grass): the lit ground shader on the RO3 pass,
        /// else plain Sprites/Default. Both take the mesh's vertex colour (alpha = the seam feather).</summary>
        public static Material GroundMaterialFor(Texture2D tex)
        {
            Shader s = View25D.Ro3Look && Ready() ? ground.shader : Shader.Find("Sprites/Default");
            if (s == null) return null;
            var m = new Material(s) { name = "GroundApron_" + tex.name, hideFlags = HideFlags.DontSave, mainTexture = tex };
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 0f);
            return m;
        }

        /// <summary>Standing prop (View25D.StandProp): trees sway and shade their trunk, the rest get sun-side shading + base AO.</summary>
        public static void ApplyProp(SpriteRenderer sr)
        {
            if (sr == null || !View25D.Ro3Look || !Ready()) return;
            PropDef d;
            bool isTree = VillagePropDefs.TryGet(sr.name, out d) && StaticPropShadows.Classify(sr.name, d) == StaticPropShadows.Shape.Tree;
            sr.sharedMaterial = isTree ? tree : prop;
            if (isTree) Trees.Add(sr);
        }

        static readonly List<SpriteRenderer> Trees = new List<SpriteRenderer>();
        static readonly List<SpriteRenderer> Candidates = new List<SpriteRenderer>();

        /// <summary>A random standing tree whose foot is on screen (falling leaves): x, y = foot, z = sprite height, w = sprite width.</summary>
        public static bool RandomTreeInView(Camera cam, out Vector4 tree)
        {
            tree = Vector4.zero;
            Candidates.Clear();
            Rect view = View25DCamera.VisibleGround(cam);
            for (int i = Trees.Count - 1; i >= 0; i--)
            {
                SpriteRenderer sr = Trees[i];
                if (sr == null) { Trees.RemoveAt(i); continue; } // map rebuilt
                Vector3 p = sr.transform.position;
                if (view.Contains(new Vector2(p.x, p.y))) Candidates.Add(sr);
            }
            if (Candidates.Count == 0) return false;
            SpriteRenderer t = Candidates[Random.Range(0, Candidates.Count)];
            Vector3 f = t.transform.position;
            Vector2 size = t.sprite != null ? (Vector2)t.sprite.bounds.size : new Vector2(60f, 90f);
            tree = new Vector4(f.x, f.y, size.y, size.x);
            return true;
        }

        /// <summary>Per frame from View25DWorld while a 2.5D map is on screen (after the camera moved): shader globals of the RO3 look.</summary>
        public static void TickGlobals(Camera cam)
        {
            if (!View25D.Ro3Look || cam == null) { ClearGlobals(); return; }
            View25DProfile p = View25D.Profile;
            Shader.SetGlobalColor(SunId, SunTint(p.SunStrength));
            Color cs = p.CrownShadeColor;
            Shader.SetGlobalVector(CrownId, new Vector4(cs.r, cs.g, cs.b, 0.25f));
            float focus = cam.orthographic ? 0f : View25DCamera.Distance(cam);
            Shader.SetGlobalVector(FogId, new Vector4(focus + p.FogStart, 1f / Mathf.Max(1f, p.FogEnd - p.FogStart), p.FogMax, p.FogDesaturate));
            Shader.SetGlobalColor(FogColorId, p.FogColor);
            Shader.SetGlobalVector(WindId, new Vector4(FeelConfig.FxOn ? p.WindSway : 0f, 1.25f, 0f, 0f));
            // flecks ~10 units across, drifting at ~0.6 units / s; inside a fleck the ground gets the sun tint + 10 %
            Shader.SetGlobalVector(DappleId, new Vector4(p.Dapple, 1f / 11f, 0.05f, 0.10f));
        }

        /// <summary>Back to neutral (2D map, RO3 look off): the composite shader then behaves exactly as on 2D maps.</summary>
        public static void ClearGlobals()
        {
            Shader.SetGlobalColor(SunId, Color.white);
            Shader.SetGlobalVector(FogId, Vector4.zero);
            Shader.SetGlobalVector(DappleId, Vector4.zero);
            Shader.SetGlobalVector(WindId, Vector4.zero);
        }

        static Color SunTint(float strength)
        {
            Color sun = View25D.Profile.SunColor;
            Color t = Color.Lerp(Color.white, sun, strength) * 1.02f; // a touch brighter: the warm multiply must not dim the scene
            t.a = 1f;
            return t;
        }

        /// <summary>Per unit per frame (View25DWorld.Stand): sun tint + canopy shade through the `_Color` property block of its figure sprites.</summary>
        public static void TickUnit(Transform unit)
        {
            if (unit == null || !View25D.Ro3Look) return;
            UnitLight u;
            if (!Units.TryGetValue(unit, out u)) { u = new UnitLight(); Units[unit] = u; }
            if (--u.RefreshIn <= 0) Gather(unit, u);
            View25DProfile p = View25D.Profile;
            float target = Mathf.Clamp01(PropShadowSilhouettes.Field(unit.position) / Mathf.Max(0.01f, p.UnitShadeFull));
            u.Shade = Mathf.MoveTowards(u.Shade, target, Time.deltaTime * 4f); // ~0.25 s in / out of a shadow
            Color c = SunTint(p.UnitSunStrength) * Color.Lerp(Color.white, p.ShadeColor, u.Shade * p.UnitShadeStrength);
            c.a = 1f;
            bool changed = Mathf.Abs(c.r - u.Applied.r) + Mathf.Abs(c.g - u.Applied.g) + Mathf.Abs(c.b - u.Applied.b) > 0.004f;
            if (!changed && u.RefreshIn != 30) return; // re-applied right after a gather: new parts (gear swap) get it too
            u.Applied = c;
            Set(u, c);
        }

        /// <summary>Drops units that left (died, despawned, another map) and restores the tint of those still alive when the look turns off.</summary>
        public static void EndFrame(HashSet<Transform> seen)
        {
            Gone.Clear();
            foreach (KeyValuePair<Transform, UnitLight> kv in Units) if (kv.Key == null || !seen.Contains(kv.Key)) Gone.Add(kv.Key);
            foreach (Transform t in Gone) { if (t != null) Set(Units[t], Color.white); Units.Remove(t); }
        }

        /// <summary>2.5D off / another map: every unit back to its own colours.</summary>
        public static void Reset()
        {
            foreach (KeyValuePair<Transform, UnitLight> kv in Units) if (kv.Key != null) Set(kv.Value, Color.white);
            Units.Clear();
            ClearGlobals();
        }

        /// <summary>Figure sprites as the drop shadow gathers them: under "Lean" (heroes), "Body" (monsters) or the unit's own renderer (pets);
        /// glow, additive, aura, shine, rim and trail layers are FX and keep their colour.</summary>
        static void Gather(Transform root, UnitLight u)
        {
            u.RefreshIn = 30;
            u.Parts.Clear();
            Transform figure = root.Find("Lean") ?? root.Find("Body");
            if (figure != null) figure.GetComponentsInChildren(true, u.Parts);
            else { var own = root.GetComponent<SpriteRenderer>(); if (own != null) u.Parts.Add(own); }
            for (int i = u.Parts.Count - 1; i >= 0; i--)
            {
                Material m = u.Parts[i].sharedMaterial;
                string sh = m != null && m.shader != null ? m.shader.name : "";
                if (m == null || !m.HasProperty(ColorId) || sh.Contains("Additive") || sh.Contains("Aura") || sh.Contains("Shine") || sh.Contains("Light")
                    || sh.Contains("Rim") || sh.Contains("Trail")) u.Parts.RemoveAt(i);
            }
        }

        static void Set(UnitLight u, Color c)
        {
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < u.Parts.Count; i++)
            {
                SpriteRenderer r = u.Parts[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(ColorId, c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
