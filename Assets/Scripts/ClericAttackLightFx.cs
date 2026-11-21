using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Light picture of the Cleric's basic attack, RO3 Soft Fantasy V1 (2026-11-21): a small Holy Light (spec section 2.6) in the Holy palette
    /// (ivory #FFF9DC / gold #F6D076), "fast, sharp, clean", smaller than the skills. The bolt itself (speed, range, hit box, burst radius) is ComboConfigs'.
    ///  - Rig (hits 1-3): ivory diamond head 0.33 H turned along its flight + a short gold LightTrail (~0.6 H at 220 u/s); the finisher is bigger;
    ///  - Muzzle: a small flash + 2-3 sparkles at the staff crystal;
    ///  - Impact: 4-point star + small ring + 4 gold sparks; Blast (hit 4, ExplodeRadius 26): flash, ring stopping on the burst radius, star,
    ///    soft pool, 8 sparks. Crowd: another player's bolt is its head only, its hit one small star (rule 12).
    /// </summary>
    public static class ClericAttackLightFx
    {
        static ClericLightLook.Pal P { get { return ClericLightLook.Holy; } }

        /// <summary>A LightTrail ribbon under `parent` in palette `p`: `width` units, `seconds` long, alpha scaled by `dim`.</summary>
        public static TrailRenderer MakeTrail(Transform parent, float width, float seconds, float dim, ClericLightLook.Pal p)
        {
            var t = new GameObject("Trail");
            t.transform.SetParent(parent, false);
            TrailRenderer tr = t.AddComponent<TrailRenderer>();
            tr.sharedMaterial = ClericLightLook.Trail(p);
            tr.time = seconds;
            tr.minVertexDistance = 0.5f;
            tr.numCapVertices = 4;
            tr.alignment = LineAlignment.View;
            tr.textureMode = LineTextureMode.Stretch;
            tr.sortingOrder = FeelConfig.FxOrder - 14;
            tr.widthMultiplier = width;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0.6f), new Keyframe(1f, 0f));
            SetTrailColour(tr, dim, p);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tr;
        }

        /// <summary>Head = light tone, tail = main colour fading out (rule 9: the head is the brightest part).</summary>
        public static void SetTrailColour(TrailRenderer tr, float dim, ClericLightLook.Pal p)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(p.Core, 0f), new GradientColorKey(p.Light, 0.25f), new GradientColorKey(p.Main, 1f) },
                new[] { new GradientAlphaKey(dim, 0f), new GradientAlphaKey(0.7f * dim, 0.35f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
        }

        /// <summary>The flying light of one combo bolt (lives on the pooled ComboProjectile, reused bolt after bolt).</summary>
        public sealed class Rig
        {
            readonly Transform root;
            readonly MeshRenderer head;
            readonly TrailRenderer trail;
            Vector2 last;

            public Rig(Transform parent)
            {
                root = new GameObject("ClericBoltLight").transform;
                root.SetParent(parent, false);
                head = LightSkillAssets.Quad(root, "Head", ClericLightLook.M(ClericLightLook.Diamond, P, ClericLightLook.Role.Glyph), false, FeelConfig.FxOrder - 12);
                trail = MakeTrail(root, 2f, 0.055f, 1f, P);
                root.gameObject.SetActive(false);
            }

            public void Begin(Vector2 pos, bool big, bool own)
            {
                root.position = new Vector3(pos.x, pos.y, 0f);                       // before activating: the ribbon must not streak in from the last bolt's end
                last = pos;
                float s = big ? 10f : 6.8f;                                            // 0.33 H (finisher 0.5 H)
                head.transform.localScale = new Vector3(s * 0.62f, s, 1f);
                LightSkillAssets.Set(head, own ? 1f : 0.55f);
                trail.widthMultiplier = (big ? 3.2f : 2f) * (own ? 1f : 0.55f);       // 0.1-0.16 H
                trail.time = big ? 0.07f : 0.055f;
                SetTrailColour(trail, own ? 1f : 0.4f, P);
                root.gameObject.SetActive(true);
                trail.Clear();
                trail.emitting = own;                                                  // raid rule: another player's bolt is only its glowing head (no ribbon)
            }

            public void Tick(Vector2 pos)
            {
                root.position = new Vector3(pos.x, pos.y, 0f);
                Vector2 d = pos - last;
                if (d.sqrMagnitude > 0.01f) head.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f); // diamond points along the flight
                last = pos;
                head.enabled = FeelConfig.FxOn;
            }

            public void End() { trail.emitting = false; root.gameObject.SetActive(false); }
        }

        static bool Own(HeroController shooter) { HeroController me = LocalPlayer.Hero; return me == null || shooter == null || shooter == me; }

        /// <summary>Cast flash at the staff crystal as the shot leaves.</summary>
        public static void Muzzle(Vector2 tip, bool big, HeroController shooter)
        {
            if (!Own(shooter)) return;                                                  // raid rule: no cast flash for other players' shots
            Color gold = P.Light;
            ClericLightFx.Create("Muzzle", tip, null, 140f, true)
                .Upright(ClericLightLook.M(ClericLightLook.Core, P, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, big ? 150f : 110f, 3f, big ? 9f : 6f, 3f, big ? 9f : 6f, 0.7f, Vector2.zero)
                .At(0f, f => f.Ring(true, f.Anchor, big ? 3 : 2, 22f, 1.6f, 2.2f, gold, 0.2f));
        }

        /// <summary>The bolt hit at `pos` (ground point under the bolt); blast = the finisher burst of `radius` units.</summary>
        public static void Impact(Vector2 pos, bool blast, float radius, HeroController shooter)
        {
            Vector2 lift = new Vector2(0f, 6f);
            Color gold = P.Main, ivory = P.Light;
            Material star = ClericLightLook.M(ClericLightLook.Star, P, ClericLightLook.Role.Glyph), ring = ClericLightLook.M(ClericLightLook.Band, P, ClericLightLook.Role.Line),
                flash = ClericLightLook.M(ClericLightLook.Core, P, ClericLightLook.Role.Flash);
            if (!blast && !Own(shooter))
            {
                ClericLightFx.Create("HitFar", pos, null, 140f, false)                      // raid rule: one small star for other players' hits (1 quad, no particles)
                    .Upright(star, ClericLightFx.Band.Over, false, 0f, 140f, 6f, 10f, 6f, 10f, 0.8f, lift);
                return;
            }
            if (!blast)
            {
                ClericLightFx.Create("Hit", pos, null, 250f, Own(shooter))
                    .Ground(ring, 0f, 230f, 6f, 22f, 0.8f, ClericLightFx.Squash, 0f, 9)               // 0.9 H ring (band at 78 % of the quad)
                    .Upright(flash, ClericLightFx.Band.Over, false, 0f, 110f, 4f, 9f, 4f, 9f, 0.75f, lift)
                    .Upright(star, ClericLightFx.Band.Over, false, 0f, 240f, 9f, 16f, 9f, 16f, 0.95f, lift, 1)
                    .At(0f, f => f.Ring(true, f.Anchor + lift, 4, 30f, 1.6f, 2.3f, ivory, 0.22f));
                return;
            }
            float d = 2f * radius;
            ClericLightFx.Create("Blast", pos, null, 520f, Own(shooter))
                .Ground(ClericLightLook.M(ClericLightLook.Band, P, ClericLightLook.Role.Fill), 0f, 500f, d * 0.6f, d * 1.05f, 1f).Kind(LayerKind.Glow)
                .Ground(ring, 30f, 300f, 8f, d / 0.78f, 0.8f, ClericLightFx.Squash, 0f, 9)            // band at 78 % of the quad: stops on the burst radius
                .Upright(flash, ClericLightFx.Band.Over, false, 0f, 130f, 5f, 15f, 5f, 15f, 0.8f, lift)
                .Upright(star, ClericLightFx.Band.Over, false, 0f, 300f, 14f, 24f, 14f, 24f, 0.95f, lift, 1)
                .At(20f, f => f.Ring(true, f.Anchor + lift, 8, radius * 2f, 1.8f, 2.6f, gold, 0.32f));
        }
    }
}
