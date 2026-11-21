using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shader pictures of the Mechanic's Deploy Turret (2026-11-21, RO3 Soft Fantasy V1): the turret body stays its sprite (DeployedTurret), only its
    /// FX change. Shot = muzzle flare + honey bullet with a short cyan trail flying to the monster (it follows a walking monster, as the damage does)
    /// + honey pop on arrival; Landed = cyan tech ring drawing itself in under the base + dust puff + brass sparks as it touches down; Gone = the ring
    /// folding away. Timing stays DeployedTurret / TurretConfig's (shot speed, deploy, fade).
    /// </summary>
    public static class MechanicTurretLightFx
    {
        static float Dim(ushort owner) { HeroController me = LocalPlayer.Hero; return me == null || me.NetId == owner ? 1f : 0.35f; }

        /// <summary>One shot from `from` to `foe` (or the fixed `aim` when the monster is unknown / gone) over `ms`; `near` = sounds on.</summary>
        public static ClericLightFx Shot(Vector2 from, Monster foe, Vector2 aim, float ms, ushort owner, bool near)
        {
            if (!MechanicLightLook.Active) return null;
            float dim = Dim(owner);
            Vector2 to0 = aim - from;
            MechanicLightBursts.Muzzle(from, Mathf.Atan2(to0.y, to0.x) * Mathf.Rad2Deg, 18f, dim, MechanicLightLook.Honey, 5);
            var host = MechanicLightBursts.Host("TurretShot", from, ms, dim);
            Vector2 last = aim;
            MechanicLightBolt.Fire(host, u =>
                {
                    if (foe != null && !foe.IsDying) last = foe.Center;
                    return Vector2.Lerp(from, last, u);
                }, ms, MechanicLightLook.Glyph(MechanicLightLook.Core, MechanicLightLook.Honey), new Vector2(13f, 5f), MechanicLightLook.Tech, 3.2f, 0.1f)
                .OnEnd((at, hit) =>
                {
                    MechanicLightBursts.Pop(at, 18f, dim, MechanicLightLook.Honey, MechanicLightLook.Ember);
                    MechanicLightBursts.SplitPulses("mechanic.deploy-turret", at, 10f, dim, MechanicLightLook.Honey);
                    if (near) ClassSfx.BoltImpact(HeroClass.Mechanic, false);
                });
            return host;
        }

        /// <summary>The turret touching down at `feet`, `ms` before it lands (DeployMs).</summary>
        public static void Landed(Vector2 feet, ushort owner, float ms)
        {
            if (!MechanicLightLook.Active) return;
            float land = Mathf.Max(0f, ms), d = MechanicLightLook.RingQuad(11f);
            var f = MechanicLightBursts.Host("TurretLand", feet, land + 520f, Dim(owner));
            f.Ground(MechanicLightLook.Line(MechanicLightLook.Ring, MechanicLightLook.Tech), 0f, land + 500f, d * 0.85f, d, 0.85f, ClericLightFx.Squash, 30f, 6).Reveal(land + 60f).Env(40f, 200f)
             .Ground(MechanicLightLook.Pool(MechanicLightLook.Tech), 0f, land + 420f, 10f, 22f, 0.8f).Kind(LayerKind.Glow)
             .Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, land, land + 420f, 6f, 13f, 4f, 8f, 0.55f, new Vector2(-5f, 2f), -2).Rise(3f).Dissolve()
             .Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, land + 20f, land + 440f, 5f, 12f, 4f, 7f, 0.5f, new Vector2(5f, 1f), -2).Rise(3f).Dissolve()
             .At(land, h => MechanicLightBursts.Sparks(h, h.Anchor + new Vector2(0f, 2f), 6, 55f, 90f, 160f, MechanicLightLook.Ember, 0.22f, 1.2f));
        }

        /// <summary>Time over / replaced: the ring flashes once and folds in.</summary>
        public static void Gone(Vector2 feet, ushort owner)
        {
            if (!MechanicLightLook.Active) return;
            float d = MechanicLightLook.RingQuad(11f);
            MechanicLightBursts.Host("TurretGone", feet, 260f, Dim(owner))
                .Ground(MechanicLightLook.Line(MechanicLightLook.Ring, MechanicLightLook.Tech), 0f, 240f, d, d * 0.4f, 0.8f, ClericLightFx.Squash, -90f, 6)
                .At(0f, h => MechanicLightBursts.Sparks(h, h.Anchor + new Vector2(0f, 6f), 4, 40f, 90f, 360f, MechanicLightLook.Crackle, 0.2f, 1.1f));
        }
    }
}
