using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Binding Shot's bind on the foe it stopped on (SkillTable.Archer.cs: Stun 1200 ms), RO3 Soft Fantasy V1 (2026-11-26, ArcherLightLook Vine):
    ///  - a vine ring drawing itself in on the ground under the foe (archer-vine-ring, ~2.2 x its hit box, turning slowly) over a soft green pool;
    ///  - two vine rings round the body (low, chest) turning opposite ways, leaves popping out, a gold glint when it takes hold;
    ///  - held for the stun time, then the vines dissolve; the stun ending early (boss: stun turns into a slow; dead) snaps them: leaf burst.
    /// Render only: the stun itself is the sim's (authority); the picture only reads the monster's status to end early.
    /// </summary>
    public sealed class ArcherBindingLightFx : MonoBehaviour
    {
        const float GraceMs = 400f; // a client sees the stun in the next snapshot
        Monster target;
        ClericLightFx fx;
        float age, holdMs;
        bool snapped;

        public static void Spawn(SkillContext ctx, SkillEffect e, Vector2 hitAt)
        {
            Monster m = ArcherLightSkills.NearestMonster(hitAt, 24f);
            if (m == null) return;
            float hold = Mathf.Max(300f, e.Status.Ms);
            Rect box = m.Box;
            float d = Mathf.Max(36f, box.width * 2.6f), bw = Mathf.Max(22f, box.width * 1.7f), hh = Mathf.Max(12f, box.height);
            float dim = ArcherLightLook.Dim(ctx.Body.Hero);
            ClericLightLook.Pal vine = ArcherLightLook.Vine;
            var fx = ClericLightFx.Create("ArcherBinding", m.Feet, null, hold + 300f, dim);
            fx.Ground(ArcherLightLook.Line(ArcherLightLook.VineRing, vine), 0f, hold + 250f, d * 0.75f, d, 0.95f, ClericLightFx.Squash, 14f, 6).Reveal(180f).Env(40f, 260f).Phases(0f, 260f, 1f, 0.75f)
              .Ground(ArcherLightLook.Line(ArcherLightLook.VineRing, ArcherLightLook.Leaf), 60f, hold + 200f, d * 0.5f, d * 0.68f, 0.8f, ClericLightFx.Squash, -22f, 6).Reveal(220f).Env(40f, 260f).Kind(LayerKind.Detail)
              .Ground(ArcherLightLook.Pool(vine), 0f, hold + 200f, d * 0.5f, d * 0.9f, 1f).Env(80f, 260f).Kind(LayerKind.Glow)
              .Ground(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Leaf), 0f, 260f, d * 1.4f, d * 0.7f, 0.8f, ClericLightFx.Squash, 0f, 7)        // closing in on the foe
              .Upright(ArcherLightLook.Line(ArcherLightLook.VineRing, vine), ClericLightFx.Band.Over, false, 40f, hold + 200f, bw * 1.3f, bw, bw * 0.42f, bw * 0.34f, 0.95f, new Vector2(0f, hh * 0.3f), 2).Env(60f, 240f).Floor(hh * 0.3f)
              .Upright(ArcherLightLook.Line(ArcherLightLook.VineRing, vine), ClericLightFx.Band.Over, false, 90f, hold + 160f, bw * 1.2f, bw * 0.9f, bw * 0.38f, bw * 0.3f, 0.9f, new Vector2(0f, hh * 0.68f), 3).Env(60f, 240f).Floor(hh * 0.68f).Kind(LayerKind.Detail)
              .Upright(ArcherLightLook.Flash(ArcherLightLook.Gold), ClericLightFx.Band.Over, false, 40f, 220f, 6f, 18f, 6f, 18f, 0.75f, new Vector2(0f, hh * 0.5f), 4);
            for (int i = 0; i < 4; i++)                                        // leaves popping out of the vines
            {
                float ang = 25f + i * 43f, rad = ang * Mathf.Deg2Rad, t0 = 60f + i * 35f;
                var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                fx.Upright(ArcherLightLook.Glyph(ArcherLightLook.LeafMask, ArcherLightLook.Leaf), ClericLightFx.Band.Over, false, t0, t0 + 520f, 3f, 6f, 3f, 6f, 0.9f, new Vector2(0f, hh * 0.45f) + dir * bw * 0.35f, 5)
                  .Turn(ang - 90f).Move(dir * 6f).Pop(140f).Kind(i % 2 == 0 ? LayerKind.Main : LayerKind.Detail);
            }
            Vector2 chest = m.Feet + new Vector2(0f, hh * 0.5f);
            Color mote = ArcherLightLook.Mote;
            fx.At(40f, f => f.Puff(false, chest, 8, bw * 0.4f, 10f, 1.5f, 2.3f, mote, 0.6f));
            var b = fx.gameObject.AddComponent<ArcherBindingLightFx>();
            b.target = m; b.fx = fx; b.holdMs = hold;
        }

        void Update()
        {
            if (fx == null || snapped) return;
            age += GameClock.Dt * 1000f;
            if (age >= holdMs) return;
            bool gone = target == null || target.IsDying, unbound = age > GraceMs && target != null && !target.Status.Has(StatusKind.Stun);
            if (gone || unbound) Snap();
        }

        /// <summary>The stun ended early: the vines snap (leaf burst + small ring) and the bind is removed.</summary>
        void Snap()
        {
            snapped = true;
            Vector2 at = fx.Anchor;
            Color leaf = ArcherLightLook.Leaf.Light;
            ClericLightFx.Create("ArcherBindingSnap", at, null, 320f, fx.Dim)
                .Ground(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Vine), 0f, 260f, 14f, 34f, 0.7f)
                .At(0f, f => f.Ring(false, at + new Vector2(0f, 8f), 10, 34f, 1.6f, 2.4f, leaf, 0.4f));
            Destroy(fx.gameObject);
        }
    }
}
