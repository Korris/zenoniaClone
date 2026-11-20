using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// RO3-style light picture of the Cleric's basic attack (2026-11-20), replacing the painted cleric-v2 set: "dew drops of holy light, fast and clean,
    /// never stealing the skills' spotlight" (ChatGPT spec, ClericLightLook). The bolt itself (speed, range, hit box, burst radius) is ComboConfigs'.
    ///  - Rig (hits 1-3): white-green glowing head + jade LightTrail ribbon, ~20 units of tail; the finisher orb is bigger with a wider ribbon;
    ///  - Muzzle: a small flash + 2-3 sparkles at the staff crystal;
    ///  - Impact: white flash, small ring on the ground, 4 motes; Blast (hit 4, ExplodeRadius 26): flash, ring stopping exactly at the burst radius,
    ///    upright holy cross, glow pool, 14 motes flying out, afterglow. Another player's bolts draw dimmer (ClericLightFx.Dim) with a thinner ribbon.
    /// </summary>
    public static class ClericAttackLightFx
    {
        /// <summary>A LightTrail ribbon (jade, white core) under `parent`: `width` units, `seconds` long, alpha scaled by `dim`.</summary>
        public static TrailRenderer MakeTrail(Transform parent, float width, float seconds, float dim)
        {
            var t = new GameObject("Trail");
            t.transform.SetParent(parent, false);
            TrailRenderer tr = t.AddComponent<TrailRenderer>();
            tr.sharedMaterial = ClericLightLook.Trail;
            tr.time = seconds;
            tr.minVertexDistance = 0.5f;
            tr.numCapVertices = 4;
            tr.alignment = LineAlignment.View;
            tr.textureMode = LineTextureMode.Stretch;
            tr.sortingOrder = FeelConfig.FxOrder - 14;
            tr.widthMultiplier = width;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0.6f), new Keyframe(1f, 0f));
            SetTrailColour(tr, dim);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tr;
        }

        static void SetTrailColour(TrailRenderer tr, float dim)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(ClericLightLook.Pale, 0.25f), new GradientColorKey(ClericLightLook.Jade, 1f) },
                new[] { new GradientAlphaKey(dim, 0f), new GradientAlphaKey(0.7f * dim, 0.35f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
        }

        /// <summary>The flying light of one combo bolt (lives on the pooled ComboProjectile, reused bolt after bolt).</summary>
        public sealed class Rig
        {
            readonly Transform root;
            readonly MeshRenderer head;
            readonly TrailRenderer trail;

            public Rig(Transform parent)
            {
                root = new GameObject("ClericBoltLight").transform;
                root.SetParent(parent, false);
                head = LightSkillAssets.Quad(root, "Head", ClericLightLook.BoltCore, false, FeelConfig.FxOrder - 12);
                trail = MakeTrail(root, 2.5f, 0.09f, 1f);
                root.gameObject.SetActive(false);
            }

            public void Begin(Vector2 pos, bool big, bool own)
            {
                root.position = new Vector3(pos.x, pos.y, 0f);                       // before activating: the ribbon must not streak in from the last bolt's end
                float s = big ? 16f : 11f;
                head.transform.localScale = new Vector3(s, s, 1f);
                LightSkillAssets.Set(head, own ? 1f : 0.5f);
                trail.widthMultiplier = (big ? 4.5f : 2.6f) * (own ? 1f : 0.55f);
                trail.time = big ? 0.12f : 0.09f;
                SetTrailColour(trail, own ? 1f : 0.4f);
                root.gameObject.SetActive(true);
                trail.Clear();
                trail.emitting = own;                                                  // raid rule: another player's bolt is only its glowing head (no ribbon)
            }

            public void Tick(Vector2 pos) { root.position = new Vector3(pos.x, pos.y, 0f); head.enabled = FeelConfig.FxOn; }

            public void End() { trail.emitting = false; root.gameObject.SetActive(false); }
        }

        static bool Own(HeroController shooter) { HeroController me = LocalPlayer.Hero; return me == null || shooter == null || shooter == me; }

        /// <summary>Cast flash at the staff crystal as the shot leaves.</summary>
        public static void Muzzle(Vector2 tip, bool big, HeroController shooter)
        {
            if (!Own(shooter)) return;                                                  // raid rule: no cast flash for other players' shots
            Color white = ClericLightLook.Spark;
            ClericLightFx.Create("Muzzle", tip, null, 140f, Own(shooter))
                .Upright(ClericLightLook.Flash, ClericLightFx.Band.Over, false, 0f, big ? 160f : 110f, 3f, big ? 13f : 8f, 3f, big ? 13f : 8f, 0.7f, Vector2.zero)
                .At(0f, f => f.Ring(true, f.Anchor, big ? 4 : 2, 22f, 1.8f, 2.6f, white, 0.22f));
        }

        /// <summary>The bolt hit at `pos` (ground point under the bolt); blast = the finisher burst of `radius` units.</summary>
        public static void Impact(Vector2 pos, bool blast, float radius, HeroController shooter)
        {
            Vector2 lift = new Vector2(0f, 6f);
            Color white = ClericLightLook.Spark, jade = ClericLightLook.Leaf, pale = ClericLightLook.Spark;
            if (!blast && !Own(shooter))
            {
                ClericLightFx.Create("HitFar", pos, null, 130f, false)                      // raid rule: one small flash for other players' hits (1 quad, no particles)
                    .Upright(ClericLightLook.Flash, ClericLightFx.Band.Over, false, 0f, 130f, 5f, 12f, 5f, 12f, 0.9f, lift);
                return;
            }
            if (!blast)
            {
                ClericLightFx.Create("Hit", pos, null, 220f, Own(shooter))
                    .Ground(ClericLightLook.Ring, 0f, 220f, 6f, 20f, 0.8f, 0.9f, 0f, 9)
                    .Upright(ClericLightLook.Flash, ClericLightFx.Band.Over, false, 0f, 130f, 5f, 13f, 5f, 13f, 0.9f, lift)
                    .Upright(ClericLightLook.Cross, ClericLightFx.Band.Over, false, 0f, 140f, 6f, 11f, 8f, 15f, 0.8f, lift, 1)
                    .At(0f, f => f.Ring(false, f.Anchor + lift, 4, 30f, 1.6f, 2.4f, new Color(pale.r, pale.g, pale.b, 0.9f), 0.2f));
                return;
            }
            float d = 2f * radius;
            ClericLightFx.Create("Blast", pos, null, 500f, Own(shooter))
                .Ground(ClericLightLook.GroundGlow, 0f, 500f, d * 0.6f, d * 1.05f, 0.75f, 1f)
                .Ground(ClericLightLook.Ring, 30f, 290f, 8f, d * 1.28f, 0.75f, 1f, 0f, 9)              // band at 78 % of the quad: stops on the burst radius
                .Ground(ClericLightLook.GroundGlow, 250f, 500f, d, d, 0.28f, 1f)
                .Upright(ClericLightLook.Flash, ClericLightFx.Band.Over, false, 0f, 110f, 5f, 18f, 5f, 18f, 0.8f, lift)
                .Upright(ClericLightLook.Cross, ClericLightFx.Band.Over, false, 0f, 200f, 12f, 20f, 16f, 28f, 0.65f, lift, 1)
                .At(20f, f => f.Ring(false, f.Anchor + lift * 0.5f, 14, radius * 2.4f, 2f, 3.2f, new Color(jade.r, jade.g, jade.b, 0.9f), 0.36f))
                .At(40f, f => f.Ring(true, f.Anchor + lift, 6, radius * 1.6f, 2.2f, 3.2f, white, 0.3f));
        }
    }
}
