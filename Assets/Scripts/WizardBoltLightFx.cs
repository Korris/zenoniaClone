using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The three Wizard skill projectiles in RO3 Soft Fantasy V1 (2026-11-21), drawn on the real SkillBolt flight (SkillBoltArt), so the head is
    /// where the hit box is and every impact lands with its damage. Rule 9: heads 0.3-0.7 H, soft short mesh trails (0.1-0.2 H wide, 0.1-0.2 s),
    /// head brightest. Colours per element (ElementLightLook, rule 8):
    ///  - Arcane Bolt (periwinkle arcane): 4-point star head 0.45 H turning, short trail; hit: star + ring 0.9 H + one flash per damage number;
    ///  - Fireball (orange fire): fireball head 0.65 H pointing along the flight + ember specks; blast exactly on the 1.5-tile impact radius:
    ///    ground ring + warm pool, flame petals 1.7 H, flash, embers + a little soot, own screen punch;
    ///  - Arcane Lance (arcane + gold accent, the icon's target ring): a gold rune ring turns at the staff as the lance leaves, a long crystal
    ///    head 0.7 H x 0.2 H; every pierced monster: gold ring + arcane star + flashes.
    /// Crowd: another player's bolt keeps its head (dimmed) and no trail / particles beyond ClericLightFx's LOD.
    /// </summary>
    public sealed class WizardBoltLightFx : ISkillBoltArt
    {
        const float Lift = 4f;           // SkillBolt flies at feet + 6: the head sits at chest height
        readonly SkillContext ctx;
        readonly ClericLightLook.Pal pal;
        readonly Transform root;
        readonly MeshRenderer head, core;
        readonly TrailRenderer trail;
        readonly ClericLightFx host;     // muzzle flash + particle emitter of this flight
        readonly bool pierce, own;
        readonly float dim, impactRadius;
        float spin, nextEmber;

        public static ISkillBoltArt Begin(SkillContext ctx, SkillEffect e, Vector2 pos, Vector2 dir, Transform parent)
        {
            if (!ElementLightLook.Active) return null;
            return new WizardBoltLightFx(ctx, e, pos, dir);
        }

        WizardBoltLightFx(SkillContext ctx, SkillEffect e, Vector2 pos, Vector2 dir)
        {
            this.ctx = ctx;
            pierce = e.Pierce;
            impactRadius = e.ImpactRadius * ctx.Units;
            dim = ClericLightFx.DimFor(ctx.Body.Hero, null);
            own = dim >= 0.99f;
            string id = ctx.Def.Id;
            pal = id == WizardLightSkills.FireballId ? ElementLightLook.Fire : ElementLightLook.Arcane;
            Vector2 at = pos + new Vector2(0f, Lift);
            float flight = e.Range * ctx.Units / Mathf.Max(1f, e.Speed * ctx.Units) * 1000f;
            host = ClericLightFx.Create("WizardBoltHost", at, null, flight + 400f, dim);
            root = new GameObject("WizardBoltLight").transform;
            root.SetParent(host.transform, false); // a map change (ClericLightFx.ClearAll) takes the flying picture too
            root.position = new Vector3(at.x, at.y, 0f);
            float headA = own ? 1f : dim >= 0.6f ? 0.85f : 0.6f, trailW, trailS;
            if (id == WizardLightSkills.FireballId)
            {
                head = Quad("Head", ElementLightLook.M(ElementLightLook.FireHead, pal, ClericLightLook.Role.Glyph), 17f, 17f, -12);
                core = Quad("Core", ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), 12f, 12f, -11);
                trailW = 6f; trailS = 0.2f;
            }
            else if (id == WizardLightSkills.ArcaneLanceId)
            {
                head = Quad("Head", ElementLightLook.M(ClericLightLook.Diamond, pal, ClericLightLook.Role.Glyph), 6.5f, 20f, -12);
                core = Quad("Core", ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), 10f, 10f, -11);
                trailW = 4.4f; trailS = 0.14f;
            }
            else
            {
                head = Quad("Head", ElementLightLook.M(ClericLightLook.Star, pal, ClericLightLook.Role.Glyph), 14f, 14f, -12);
                core = Quad("Core", ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), 10f, 10f, -11);
                trailW = 4f; trailS = 0.16f;
            }
            LightSkillAssets.Set(head, headA);
            LightSkillAssets.Set(core, headA * 0.8f);
            if (dim >= 0.6f)
            {
                trail = ClericAttackLightFx.MakeTrail(root, trailW * (own ? 1f : 0.7f), trailS, own ? 1f : 0.6f, pal);
                trail.Clear();
            }
            Muzzle(id, at, dir);
            Tick(pos, dir);
        }

        MeshRenderer Quad(string name, Material m, float w, float h, int order)
        {
            MeshRenderer r = LightSkillAssets.Quad(root, name, m, false, FeelConfig.FxOrder + order);
            r.transform.localScale = new Vector3(w, h, 1f);
            return r;
        }

        void Muzzle(string id, Vector2 at, Vector2 dir)
        {
            Material flash = ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash);
            host.Upright(flash, ClericLightFx.Band.Over, false, 0f, 150f, 5f, 16f, 5f, 16f, 0.8f, Vector2.zero);
            if (id == WizardLightSkills.ArcaneLanceId)                  // the icon's gold target ring: the lance leaves through it
                host.Upright(ElementLightLook.M(ClericLightLook.RuneRing, ElementLightLook.Gold, ClericLightLook.Role.Line), ClericLightFx.Band.Over, false, 0f, 340f, 12f, 24f, 12f, 24f, 0.9f, Vector2.zero, 1).Env(40f, 160f);
            Color c = pal.Light;
            host.At(0f, f => f.Ring(true, at, 5, 30f, 1.8f, 2.6f, c, 0.25f));
        }

        public void Tick(Vector2 pos, Vector2 dir)
        {
            if (root == null) return;
            Vector2 at = pos + new Vector2(0f, Lift);
            root.position = new Vector3(at.x, at.y, 0f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            string id = ctx.Def.Id;
            if (id == WizardLightSkills.FireballId) head.transform.localRotation = Quaternion.Euler(0f, 0f, angle);       // mask points +x
            else if (id == WizardLightSkills.ArcaneLanceId) head.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f); // mask points up
            else { spin += GameClock.Dt * 540f; head.transform.localRotation = Quaternion.Euler(0f, 0f, spin); }
            if (id == WizardLightSkills.FireballId) core.transform.localPosition = new Vector3(dir.x * 2.5f, dir.y * 2.5f, 0f); // hot front of the ball
            head.enabled = core.enabled = FeelConfig.FxOn;
            if (trail != null) trail.emitting = FeelConfig.FxOn;
            if (id != WizardLightSkills.FireballId || host == null || host.AgeMs < nextEmber) return;
            nextEmber = host.AgeMs + 45f;                                  // embers shed behind the fireball (~22 / s, LOD in Emit / Count)
            if (host.Count(1) > 0)
                host.Emit(Random.value < 0.3f, at - dir * 4f + Random.insideUnitCircle * 2f, -dir * 12f + new Vector2(0f, 10f), Random.Range(1.4f, 2.2f), pal.Light, 0.35f);
        }

        public void Hit(Vector2 at)
        {
            if (!pierce) return; // a plain bolt's hit is drawn by End
            Impact(at + new Vector2(0f, Lift));
        }

        public void End(Vector2 pos, bool hit, bool blast)
        {
            if (root == null) return;
            Vector2 at = pos + new Vector2(0f, Lift);
            if (blast) FireBlast(pos);
            else if (hit) Impact(at);
            else Fizzle(at);
            head.enabled = core.enabled = false;
            if (trail != null) trail.emitting = false;
            Object.Destroy(root.gameObject, 0.3f); // the trail fades out behind the last point
        }

        /// <summary>Arcane Bolt / Arcane Lance hit: ring 0.9 H, star, one flash per damage number, sparks; the lance adds a small gold ring.</summary>
        void Impact(Vector2 at)
        {
            bool lance = ctx.Def.Id == WizardLightSkills.ArcaneLanceId;
            var fx = ClericLightFx.Create("WizardBoltHit", at, null, 420f, dim);
            Vector2 down = new Vector2(0f, -Lift - 4f);                         // the ground under the hit
            fx.Ground(ElementLightLook.M(ClericLightLook.Band, pal, ClericLightLook.Role.Fill), 0f, 380f, 16f, 34f, 1f, ClericLightFx.Squash, 0f, 4, down).Kind(LayerKind.Glow)
              .Ground(ElementLightLook.M(ClericLightLook.RuneRing, pal, ClericLightLook.Role.Line), 0f, 420f, 18f, 30f, 0.8f, ClericLightFx.Squash, 60f, 8, down).Env(20f, 200f).Kind(LayerKind.Detail)
              .Ground(ElementLightLook.M(ClericLightLook.Band, pal, ClericLightLook.Role.Line), 0f, 300f, 8f, 38f, 0.85f, ClericLightFx.Squash, 0f, 9, down)
              .Upright(ElementLightLook.M(ClericLightLook.Star, pal, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 320f, 12f, 28f, 12f, 28f, 0.95f, Vector2.zero, 1).Pop(120f);
            if (lance) fx.Upright(ElementLightLook.M(ClericLightLook.Band, ElementLightLook.Gold, ClericLightLook.Role.Line), ClericLightFx.Band.Over, false, 0f, 280f, 10f, 30f, 10f, 30f, 0.85f, Vector2.zero, 2);
            WizardLightSkills.Pulses(fx, ctx.Def.Id, 0f, ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), 9f, 20f, Vector2.zero, 0.8f);
            Color c = pal.Light;
            fx.At(0f, f => f.Ring(true, at, lance ? 6 : 9, 42f, 1.8f, 2.8f, c, 0.3f));
            WizardLightSkills.Punch(ctx, lance ? 0.1f : 0.08f, 0f);
        }

        void Fizzle(Vector2 at)
        {
            Color c = pal.Light;
            ClericLightFx.Create("WizardBoltFizzle", at, null, 220f, dim)
                .Upright(ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 160f, 6f, 3f, 6f, 3f, 0.5f, Vector2.zero)
                .At(0f, f => f.Puff(true, at, 2, 3f, 8f, 1.4f, 2f, c, 0.25f));
        }

        /// <summary>Fireball blast on the skill's impact radius (ground point `pos`): ground ring + pool, flame petals, flash, flashes per number, embers, soot.</summary>
        void FireBlast(Vector2 pos)
        {
            float r = Mathf.Max(8f, impactRadius), d = 2f * r;
            Vector2 lift = new Vector2(0f, 7f);
            var fx = ClericLightFx.Create("FireballBlast", pos, null, 650f, dim);
            fx.Ground(ElementLightLook.M(ClericLightLook.Band, pal, ClericLightLook.Role.Fill), 0f, 560f, d * 0.6f, d * 1.1f, 1f).Kind(LayerKind.Glow)
              .Ground(ElementLightLook.M(ClericLightLook.Band, pal, ClericLightLook.Role.Line), 20f, 460f, 8f, d / 0.78f, 0.9f, ClericLightFx.Squash, 0f, 9).Env(20f, 220f)
              .Ground(ElementLightLook.M(ClericLightLook.Band, ElementLightLook.Gold, ClericLightLook.Role.Line), 0f, 260f, 10f, d / 0.78f * 1.2f, 0.55f, ClericLightFx.Squash, 0f, 10).Kind(LayerKind.Detail) // shockwave just past the blast edge
              .Upright(ElementLightLook.M(ElementLightLook.FireBurst, pal, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 420f, 18f, 46f, 18f, 46f, 0.92f, lift, 1).Pop(160f)
              .Upright(ElementLightLook.M(ElementLightLook.FireBurst, ElementLightLook.Gold, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 40f, 300f, 10f, 26f, 10f, 26f, 0.8f, lift, 2).Kind(LayerKind.Detail)
              .Upright(ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 200f, 12f, 32f, 12f, 32f, 0.85f, lift, 3);
            WizardLightSkills.Pulses(fx, ctx.Def.Id, 40f, ElementLightLook.M(ClericLightLook.Core, ElementLightLook.Fire, ClericLightLook.Role.Flash), 10f, 22f, lift, 0.65f);
            Color ember = pal.Light, soot = ElementLightLook.Smoke;
            fx.At(10f, f => f.Ring(true, pos + lift, 16, r * 2.3f, 2f, 3.2f, ember, 0.45f))
              .At(30f, f => f.Ring(false, pos + lift, 10, r * 1.5f, 1.8f, 2.6f, ElementLightLook.A(ElementLightLook.Fire.Main, 0.9f), 0.5f))
              .At(60f, f => f.Puff(false, pos + lift, 8, r * 0.45f, 18f, 2.8f, 4f, soot, 0.75f));
            WizardLightSkills.Punch(ctx, 0.2f, 40f);
        }
    }
}
