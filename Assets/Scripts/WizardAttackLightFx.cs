using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Wizard basic attack in RO3 Soft Fantasy V1 (2026-11-21, replaces the painted wizard-v4 set; RO3 Wizard "Cold Bolt"): the class colour, ice
    /// (ElementLightLook.Ice). Bolt numbers stay ComboConfigs' (speed, range, hit box, burst radius 26). Per combo step (BoltDef sprite name):
    ///  - hit 1 MagicBolt: ice shard head 0.45 H pointing along the flight + short trail (0.12 H, 0.1 s);
    ///  - hit 2 FrostShard: snowflake head 0.35 H turning + short trail;
    ///  - hit 3 GlacierSpikes: a low shard skimming the ground that leaves small ice spikes standing behind it (they melt in ~0.3 s);
    ///  - hit 4 ArcaneOrb (finisher): ice orb (core + turning snowflake 0.6 H) + wider trail; burst = frost ring exactly on the radius drawing
    ///    itself in, snowflake 1.2 H popping, flash, pool, 8 snow sparks.
    /// Muzzle: small flash + 2 sparkles at the staff (own shots only). Hit: snowflake + ring 0.9 H + flash, one flash per damage number
    /// (SkillHitSplit.Basic). Crowd: another player's bolt = its head only (no trail, no spikes), its hit one small snowflake (rule 12).
    /// </summary>
    public static class WizardAttackLightFx
    {
        public enum Shot { Bolt, Frost, Spikes, Orb }

        static ClericLightLook.Pal P { get { return ElementLightLook.Ice; } }

        public static bool Active { get { return ElementLightLook.Active; } }

        public static Shot ShotFor(BoltDef bolt)
        {
            if (bolt.ExplodeRadius > 0f) return Shot.Orb;
            if (bolt.Sprite == ComboConfigs.FrostShardSprite) return Shot.Frost;
            if (bolt.Sprite == ComboConfigs.GlacierSpikesSprite) return Shot.Spikes;
            return Shot.Bolt;
        }

        static bool Own(HeroController shooter) { HeroController me = LocalPlayer.Hero; return me == null || shooter == null || shooter == me; }

        /// <summary>The flying picture of one combo bolt (lives on the pooled ComboProjectile, reused bolt after bolt).</summary>
        public sealed class Rig
        {
            const float SpikeEvery = 9f, Lift = 6f; // ComboProjectile.VisualLift: the bolt flies 6 over its ground point
            readonly Transform root;
            readonly MeshRenderer head, core;
            readonly TrailRenderer trail;
            Shot shot;
            bool own;
            Vector2 last, lastSpike;
            float spin;
            ClericLightFx spikes;

            public Rig(Transform parent)
            {
                root = new GameObject("WizardBoltLight").transform;
                root.SetParent(parent, false);
                head = LightSkillAssets.Quad(root, "Head", ElementLightLook.M(ElementLightLook.IceShard, P, ClericLightLook.Role.Glyph), false, FeelConfig.FxOrder - 12);
                core = LightSkillAssets.Quad(root, "Core", ElementLightLook.M(ClericLightLook.Core, P, ClericLightLook.Role.Flash), false, FeelConfig.FxOrder - 11);
                trail = ClericAttackLightFx.MakeTrail(root, 2.4f, 0.1f, 1f, P);
                root.gameObject.SetActive(false);
            }

            public void Begin(Vector2 at, Shot s, bool isOwn)
            {
                shot = s; own = isOwn; spin = Random.value * 360f;
                root.position = new Vector3(at.x, at.y, 0f);                      // before activating: the ribbon must not streak in from the last bolt's end
                last = lastSpike = at;
                bool round = s == Shot.Frost || s == Shot.Orb;
                head.sharedMaterial = ElementLightLook.M(round ? ElementLightLook.Snowflake : ElementLightLook.IceShard, P, ClericLightLook.Role.Glyph);
                float k = own ? 1f : 0.8f;
                head.transform.localScale = s == Shot.Orb ? new Vector3(15f, 15f, 1f) * k : s == Shot.Frost ? new Vector3(10f, 10f, 1f) * k : new Vector3(4.6f, 12f, 1f) * k;
                core.transform.localScale = Vector3.one * (s == Shot.Orb ? 13f : 7f) * k;
                LightSkillAssets.Set(head, own ? 1f : 0.6f);
                LightSkillAssets.Set(core, own ? 0.8f : 0.45f);
                trail.widthMultiplier = s == Shot.Orb ? 4.2f : s == Shot.Spikes ? 2.4f : 3.2f;
                trail.time = s == Shot.Orb ? 0.16f : 0.12f;
                ClericAttackLightFx.SetTrailColour(trail, 1f, P);
                root.gameObject.SetActive(true);
                trail.Clear();
                trail.emitting = own;                                               // raid rule: another player's bolt is only its head
                spikes = own && s == Shot.Spikes ? ClericLightFx.Create("WizardSpikes", at - new Vector2(0f, Lift), null, 900f, true) : null;
            }

            public void Tick(Vector2 at, float dt)
            {
                root.position = new Vector3(at.x, at.y, 0f);
                Vector2 d = at - last;
                if (shot == Shot.Frost || shot == Shot.Orb) { spin += dt * (shot == Shot.Orb ? 240f : 480f); head.transform.localRotation = Quaternion.Euler(0f, 0f, spin); }
                else if (d.sqrMagnitude > 0.01f) head.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f); // shard tip along the flight
                last = at;
                head.enabled = core.enabled = FeelConfig.FxOn;
                if (spikes == null || (at - lastSpike).sqrMagnitude < SpikeEvery * SpikeEvery) return;
                lastSpike = at;                                                     // a small spike stands up on the ground behind the skimming shard
                Vector2 off = at - new Vector2(0f, Lift) - spikes.Anchor;
                float t = spikes.AgeMs, h = Random.Range(8f, 12f);
                spikes.Upright(ElementLightLook.M(ElementLightLook.IceShard, P, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, true, t, t + 340f, 3.4f, 4f, h * 0.3f, h, 0.85f, off + new Vector2(Random.Range(-2f, 2f), -1f), -4)
                      .Floor().Pop(90f).Env(0f, 180f).Dissolve();
            }

            public void End() { trail.emitting = false; root.gameObject.SetActive(false); spikes = null; }
        }

        /// <summary>Cast flash at the staff as the shot leaves (own shots only).</summary>
        public static void Muzzle(Vector2 tip, bool big, HeroController shooter)
        {
            if (!Own(shooter)) return;
            Color snow = P.Light;
            ClericLightFx.Create("WizardMuzzle", tip, null, 150f, true)
                .Upright(ElementLightLook.M(ClericLightLook.Core, P, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, big ? 150f : 110f, 3f, big ? 10f : 6.5f, 3f, big ? 10f : 6.5f, 0.75f, Vector2.zero)
                .At(0f, f => f.Ring(true, f.Anchor, big ? 3 : 2, 22f, 1.5f, 2.2f, snow, 0.2f));
        }

        /// <summary>The bolt hit at `pos` (ground point under the bolt); blast = the finisher burst of `radius` units.</summary>
        public static void Impact(Vector2 pos, bool blast, float radius, HeroController shooter)
        {
            Vector2 lift = new Vector2(0f, 6f);
            Material flake = ElementLightLook.M(ElementLightLook.Snowflake, P, ClericLightLook.Role.Glyph), ring = ElementLightLook.M(ClericLightLook.Band, P, ClericLightLook.Role.Line),
                flash = ElementLightLook.M(ClericLightLook.Core, P, ClericLightLook.Role.Flash);
            bool own = Own(shooter);
            if (!blast && !own)
            {
                ClericLightFx.Create("WizardHitFar", pos, null, 150f, false)
                    .Upright(flake, ClericLightFx.Band.Over, false, 0f, 150f, 6f, 10f, 6f, 10f, 0.8f, lift);
                return;
            }
            Color snow = P.Light;
            HitSplit split = SkillHitSplit.Basic;
            if (!blast)
            {
                var hit = ClericLightFx.Create("WizardHit", pos, null, 280f, own);
                hit.Ground(ring, 0f, 250f, 6f, 28f, 0.85f, ClericLightFx.Squash, 0f, 9)
                   .Upright(flake, ClericLightFx.Band.Over, false, 0f, 260f, 10f, 20f, 10f, 20f, 0.95f, lift, 1).Pop(100f)
                   .At(0f, f => f.Ring(true, f.Anchor + lift, 6, 34f, 1.6f, 2.5f, snow, 0.25f));
                for (int i = 0; i < Mathf.Max(1, split.Hits); i++)
                    hit.Upright(flash, ClericLightFx.Band.Over, false, i * split.IntervalMs, i * split.IntervalMs + 110f, 5f, 12f, 5f, 12f, 0.8f, lift, 2);
                return;
            }
            float d = 2f * radius;
            var fx = ClericLightFx.Create("WizardBlast", pos, null, 560f, own);
            fx.Ground(ElementLightLook.M(ClericLightLook.Band, P, ClericLightLook.Role.Fill), 0f, 520f, d * 0.6f, d * 1.05f, 1f).Kind(LayerKind.Glow)
              .Ground(ElementLightLook.M(ElementLightLook.FrostRing, P, ClericLightLook.Role.Line), 0f, 520f, d / 0.955f * 0.85f, d / 0.955f, 0.85f, ClericLightFx.Squash, 30f, 9).Reveal(120f).Env(20f, 240f)
              .Upright(flake, ClericLightFx.Band.Over, false, 0f, 340f, 14f, 32f, 14f, 32f, 0.95f, lift, 1).Pop(140f)
              .Ground(ring, 0f, 220f, 10f, d / 0.78f * 1.15f, 0.5f, ClericLightFx.Squash, 0f, 10).Kind(LayerKind.Detail) // shockwave
              .At(20f, f => f.Ring(true, f.Anchor + lift, 12, radius * 2.2f, 2f, 3f, snow, 0.35f));
            for (int i = 0; i < Mathf.Max(1, split.FinisherHits); i++)
                fx.Upright(flash, ClericLightFx.Band.Over, false, i * split.IntervalMs, i * split.IntervalMs + 140f, 7f, 20f, 7f, 20f, 0.85f, lift, 2);
        }
    }
}
