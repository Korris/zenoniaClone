using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Archer skill projectiles (Piercing Arrow, Multi Shot, Binding Shot; SkillTable.Archer.cs). The arrow is a solid object: the painted
    /// basic-attack arrow sprite (ArrowBoltRig), bigger for the strong single shots. Everything around it is light FX (RO3 Soft Fantasy V1,
    /// 2026-11-26, ArcherLightLook; no FX frames):
    ///  - every arrow: a soft mesh trail (rule 9, head brightest) in the skill's colour;
    ///  - Piercing Arrow: a wind spiral round the flight (motes on a helix) and a wind ring every 22 units the arrow flies through; each monster
    ///    it passes: pierce burst (leaf star, gold flash, wind gusts thrown forward, ground ring, sparks), small own screen punch;
    ///  - Multi Shot: thin trail, small leaf burst per hit;
    ///  - Binding Shot: vine-green trail; the hit: vine burst + the vine bind on the foe for the stun time (ArcherBindingLightFx).
    /// The bolt itself (flight, hits) stays in SkillBolt, so the picture can never run ahead of the damage (Hit / End come from the sim).
    /// Crowd: another player's arrow keeps the sprite, no trail / spiral / rings (ClericLightFx LOD).
    /// </summary>
    public sealed class ArcherSkillArrowArt : ISkillBoltArt
    {
        const float RingStep = 22f;
        readonly SkillContext ctx;
        readonly SkillEffect e;
        readonly ArrowBoltRig rig;
        readonly GameObject host;
        readonly ClassPalette pal;
        readonly ClericLightLook.Pal lp;
        readonly ClericLightFx emitter;   // particles of this flight (null when the light FX is off)
        readonly Transform trailRoot;
        readonly bool big, light;
        readonly float dim;
        float last, travelled, nextRing, spin;
        Vector2 lastPos;

        public ArcherSkillArrowArt(SkillContext ctx, SkillEffect e, Vector2 pos, Vector2 dir, Transform parent)
        {
            this.ctx = ctx; this.e = e;
            pal = ctx.Palette ?? ClassPalette.Leaf;
            big = e.Count <= 1; // Multi Shot's fan keeps the basic size
            int idx = ctx.Def.Index;
            lp = idx == ArcherLightSkills.BindingShot ? ArcherLightLook.Vine : idx == ArcherLightSkills.PiercingArrow ? ArcherLightLook.Wind : ArcherLightLook.Leaf;
            host = new GameObject("ArcherSkillArrow");
            host.transform.SetParent(parent, false);
            rig = new ArrowBoltRig(host.transform);
            rig.Begin(pos, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, idx == ArcherLightSkills.PiercingArrow ? 1.5f : big ? 1.35f : 1f, pal);
            last = Time.time; lastPos = pos; nextRing = 10f;
            light = ArcherLightLook.Active;
            dim = ArcherLightLook.Dim(ctx.Body.Hero);
            if (!light) return;
            float flight = e.Range * ctx.Units / Mathf.Max(1f, e.Speed * ctx.Units) * 1000f;
            emitter = ClericLightFx.Create("ArcherArrowHost", pos, null, flight + 500f, dim);
            if (dim >= 0.6f)
            {
                trailRoot = new GameObject("ArcherArrowTrail").transform;
                trailRoot.SetParent(emitter.transform, false); // a map change (ClericLightFx.ClearAll) takes the trail too
                trailRoot.position = new Vector3(pos.x, pos.y, 0f);
                float w = idx == ArcherLightSkills.PiercingArrow ? 5.5f : big ? 4.5f : 2.6f;
                TrailRenderer t = ClericAttackLightFx.MakeTrail(trailRoot, w * (dim >= 0.99f ? 1f : 0.7f), big ? 0.2f : 0.14f, dim >= 0.99f ? 1f : 0.6f, lp);
                t.Clear();
            }
        }

        public void Tick(Vector2 pos, Vector2 dir)
        {
            rig.Aim(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            float now = Time.time;
            rig.Tick(pos, now - last);
            last = now;
            if (!light || emitter == null) return;
            if (trailRoot != null) trailRoot.position = new Vector3(pos.x, pos.y, 0f);
            travelled += (pos - lastPos).magnitude;
            lastPos = pos;
            if (ctx.Def.Index != ArcherLightSkills.PiercingArrow || dim < 0.6f) return;
            // wind spiral: two motes on a helix round the flight line, left behind (they hang in the air and fade)
            spin += GameClock.Dt * 28f;
            var side = new Vector2(-dir.y, dir.x);
            for (int k = 0; k < 2; k++)
            {
                float s = Mathf.Sin(spin + k * Mathf.PI);
                if (emitter.Count(1) > 0)
                    emitter.Emit(k == 0, pos - dir * 6f + side * s * 4.5f, -dir * 10f + side * s * 6f, 1.5f + 0.6f * Mathf.Abs(Mathf.Cos(spin)), k == 0 ? lp.Light : ArcherLightLook.Spark, 0.32f);
            }
            if (travelled < nextRing) return;
            nextRing += RingStep;
            WindRing(pos, dir);
        }

        /// <summary>A ring of wind standing across the flight line (the arrow just went through it), widening and fading in 240 ms.</summary>
        void WindRing(Vector2 at, Vector2 dir)
        {
            float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            ClericLightFx.Create("ArcherWindRing", at, null, 260f, dim)
                .Upright(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Wind), ClericLightFx.Band.Over, false, 0f, 240f, 3f, 6f, 9f, 20f, 0.75f, Vector2.zero, -2).Turn(a)
                .Upright(ArcherLightLook.Line(ArcherLightLook.WindArc, ArcherLightLook.Leaf), ClericLightFx.Band.Over, false, 0f, 200f, 5f, 8f, 9f, 15f, 0.6f, -dir * 4f, -1).Turn(a + 180f).Kind(LayerKind.Detail);
        }

        public void Hit(Vector2 at)
        {
            if (!light) { ArrowBoltRig.Impact(at, big, pal); return; }
            if (ctx.Def.Index == ArcherLightSkills.PiercingArrow) PierceBurst(at);
            // other arrows stop at their hit: End draws it
        }

        public void End(Vector2 pos, bool hit, bool blast)
        {
            if (!light) { if (hit || blast) ArrowBoltRig.Impact(pos, true, pal); }
            else if (hit)
            {
                int idx = ctx.Def.Index;
                if (idx == ArcherLightSkills.BindingShot) { LeafBurst(pos, 1.25f, ArcherLightLook.Vine); ArcherBindingLightFx.Spawn(ctx, e, pos); }
                else if (idx != ArcherLightSkills.PiercingArrow) LeafBurst(pos, big ? 1.1f : 0.8f, ArcherLightLook.Leaf);
            }
            else if (ctx.Def.Index == ArcherLightSkills.PiercingArrow) WindFade(pos);
            rig.End();
            if (trailRoot != null) Object.Destroy(trailRoot.gameObject, 0.3f); // the trail fades out behind the last point
            if (host != null) Object.Destroy(host);
        }

        /// <summary>Piercing Arrow passing through a monster: leaf star + gold flash, wind gusts thrown forward, ground ring, sparks forward.</summary>
        void PierceBurst(Vector2 at)
        {
            Vector2 dir = ctx.Dir;
            float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector2 down = new Vector2(0f, -8f);
            var fx = ClericLightFx.Create("ArcherPierce", at, null, 460f, dim);
            fx.Ground(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Leaf), 0f, 320f, 8f, 34f, 0.85f, ClericLightFx.Squash, 0f, 8, down)
              .Ground(ArcherLightLook.Pool(ArcherLightLook.Leaf), 0f, 360f, 14f, 30f, 1f, ClericLightFx.Squash, 0f, 4, down).Kind(LayerKind.Glow)
              .Upright(ArcherLightLook.Glyph(ArcherLightLook.Star, ArcherLightLook.Leaf), ClericLightFx.Band.Over, false, 0f, 300f, 12f, 28f, 12f, 28f, 0.95f, Vector2.zero, 1).Pop(120f)
              .Upright(ArcherLightLook.Flash(ArcherLightLook.Gold), ClericLightFx.Band.Over, false, 0f, 170f, 8f, 22f, 8f, 22f, 0.8f, Vector2.zero, 2)
              .Upright(ArcherLightLook.Line(ArcherLightLook.WindArc, ArcherLightLook.Wind), ClericLightFx.Band.Over, false, 0f, 280f, 10f, 18f, 14f, 26f, 0.85f, dir * 4f, 3).Turn(a).Move(dir * 18f)
              .Upright(ArcherLightLook.Line(ArcherLightLook.WindArc, ArcherLightLook.Leaf), ClericLightFx.Band.Over, false, 40f, 300f, 8f, 14f, 10f, 20f, 0.7f, dir * 2f, 3).Turn(a).Move(dir * 26f).Kind(LayerKind.Detail);
            Color spark = ArcherLightLook.Spark, glint = ArcherLightLook.Glint;
            fx.At(0f, f =>
            {
                int m = f.Count(10);
                for (int i = 0; i < m; i++)
                {
                    Vector2 v = Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f)) * (dir * Random.Range(45f, 85f));
                    f.Emit(i % 3 != 0, at, v, Random.Range(1.5f, 2.5f), i % 3 == 0 ? glint : spark, 0.34f);
                }
            });
            ArcherLightSkills.Punch(ctx, 0.07f, 0f);
        }

        /// <summary>Arrow stopped by a monster: leaf-star pop, flash, ring, a few leaves and sparks (`k` = size).</summary>
        void LeafBurst(Vector2 at, float k, ClericLightLook.Pal p)
        {
            Vector2 down = new Vector2(0f, -8f);
            var fx = ClericLightFx.Create("ArcherArrowHit", at, null, 420f, dim);
            fx.Ground(ArcherLightLook.Line(ArcherLightLook.Band, p), 0f, 300f, 6f * k, 28f * k, 0.8f, ClericLightFx.Squash, 0f, 8, down)
              .Upright(ArcherLightLook.Glyph(ArcherLightLook.Star, p), ClericLightFx.Band.Over, false, 0f, 260f, 9f * k, 22f * k, 9f * k, 22f * k, 0.92f, Vector2.zero, 1).Pop(110f)
              .Upright(ArcherLightLook.Flash(p), ClericLightFx.Band.Over, false, 0f, 150f, 6f * k, 16f * k, 6f * k, 16f * k, 0.75f, Vector2.zero, 2);
            for (int i = 0; i < 3; i++)
            {
                float ang = 40f + i * 50f + Random.Range(-10f, 10f), rad = ang * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                fx.Upright(ArcherLightLook.Glyph(ArcherLightLook.LeafMask, p), ClericLightFx.Band.Over, false, 20f, 380f, 4f * k, 5f * k, 4f * k, 5f * k, 0.9f, d * 3f, 3).Turn(ang - 90f).Move(d * 14f * k + new Vector2(0f, -4f)).Kind(i == 1 ? LayerKind.Main : LayerKind.Detail);
            }
            Color spark = p.Light;
            fx.At(0f, f => f.Ring(true, at, Mathf.RoundToInt(7 * k), 38f * k, 1.5f, 2.4f, spark, 0.3f));
        }

        /// <summary>Piercing Arrow out of range: the wind spiral unwinds (a last ring + motes).</summary>
        void WindFade(Vector2 at)
        {
            Color c = ArcherLightLook.Wind.Light;
            ClericLightFx.Create("ArcherWindFade", at, null, 300f, dim)
                .Upright(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Wind), ClericLightFx.Band.Over, false, 0f, 260f, 6f, 16f, 6f, 16f, 0.5f, Vector2.zero)
                .At(0f, f => f.Puff(false, at, 4, 4f, 8f, 1.4f, 2.2f, c, 0.35f));
        }
    }
}
