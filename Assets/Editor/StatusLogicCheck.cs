using System;
using System.Reflection;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto.EditorTools
{
    /// <summary>Smoke checks for monster status effects + shape-based area damage on live monsters (FrostSmokeTest, once per run).</summary>
    public static class StatusLogicCheck
    {
        public static void RunWorld(Action<bool, string> expect, HeroController hero, GameObject root)
        {
            // Own dummies (the start map may be the village without monsters): a slime and a boss beside the hero.
            Vector2 at0 = hero.Position + new Vector2(0f, 40f);
            Monster slime = Monster.Create(root.transform, MonsterDef.FrostSlime, at0, -1);
            Monster boss = Monster.Create(root.transform, MonsterDef.FrostfangKing, at0 + new Vector2(40f, 0f), -1);
            try { Run(expect, hero, slime, boss); }
            finally { UnityEngine.Object.DestroyImmediate(slime.gameObject); UnityEngine.Object.DestroyImmediate(boss.gameObject); }
        }

        static void Run(Action<bool, string> expect, HeroController hero, Monster slime, Monster boss)
        {
            expect(slime.ApplyStatus(new StatusSpec(StatusKind.Freeze, 0f, 2000), hero) && slime.Status.Disabled && slime.Status.SpeedFactor == 0f, "status: freeze disables a monster");
            expect(boss.ApplyStatus(new StatusSpec(StatusKind.Stun, 0f, 2000), hero) && !boss.Status.Disabled && Mathf.Abs(boss.Status.SpeedFactor - 0.5f) < 0.01f, "status: boss stun becomes a 50% slow");
            slime.Status.Clear(); boss.Status.Clear();
            slime.Chill(1000f);
            expect(slime.Status.Has(StatusKind.Chill) && Mathf.Abs(slime.Status.SpeedFactor - 0.5f) < 0.01f, "status: Ice Storm chill halves the speed (legacy Chill API)");
            slime.Status.Clear();
            expect(slime.Launch() && slime.IsAirborne && !boss.Launch(), "status: Earth Break launch still works, boss still immune");

            slime.ApplyStatus(new StatusSpec(StatusKind.Mark, 50f, 5000), hero);
            expect(SkillHitHooks.Modify(hero, slime, 100) == 150, "status: mark adds 50% damage through the hit hook");
            slime.Status.Clear();

            // Circle strike on the slime with a burn: damages, credits the hero, burn ticks credit the hero too.
            var strike = new SkillEffect { Kind = EffectKind.Strike, Shape = AreaShape.Circle, Radius = 1f, Pct = 20f, Status = new StatusSpec(StatusKind.Burn, 40f, 3000) };
            SkillDamageCtx ctx = SkillDamageCtx.For(hero, 1f, null);
            float hp0 = slime.Hp;
            var hit = SkillDamage.Area(ctx, strike, slime.Center, Vector2.right);
            expect(hit.Count >= 1 && hit.Contains(slime) && slime.Hp < hp0 && slime.Status.Has(StatusKind.Burn), "area: circle strike damages and burns the monster");
            float hp1 = slime.Hp;
            typeof(Monster).GetMethod("TickStatus", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(slime, new object[] { 500f });
            expect(slime.Hp < hp1 || slime.IsDying, "status: burn ticks after 500 ms");
            expect(slime.DamageBy.ContainsKey(hero.NetId), "status: burn damage is credited to the caster");
            slime.Status.Clear();

            // A cone facing away from the monster hits nothing; facing it hits.
            var cone = new SkillEffect { Kind = EffectKind.Strike, Shape = AreaShape.Cone, Radius = 4f, AngleDeg = 90f, Pct = 10f };
            Vector2 at = slime.Center + Vector2.left * 20f;
            expect(!SkillDamage.Area(ctx, cone, at, Vector2.left).Contains(slime), "area: cone facing away misses");
            if (!slime.IsDying) expect(SkillDamage.Area(ctx, cone, at, Vector2.right).Contains(slime), "area: cone facing the monster hits");
        }
    }
}
