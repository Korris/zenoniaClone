using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto.EditorTools
{
    /// <summary>Smoke checks of the skill executor (FrostSmokeTest, per class): every skill casts without throwing, a step-0 strike hurts a dummy, a shield buff lands.</summary>
    public static class SkillExecutorCheck
    {
        public static void RunWorld(Action<bool, string> expect, HeroController hero, GameObject root)
        {
            string cls = hero.Rpg.ClassId;
            Monster dummy = Monster.Create(root.transform, MonsterDef.FrostSlime, hero.Position + new Vector2(14f, 0f), -1);
            try
            {
                int cast = 0;
                for (int i = 0; i < SkillDef.PerClass; i++)
                {
                    try { SkillExecutor.Cast(hero, i, 3); cast++; }
                    catch (Exception e) { expect(false, cls + ": skill " + i + " throws " + e.Message); }
                }
                expect(cast == SkillDef.PerClass, cls + ": all 8 skills cast without exceptions");
                // Step 0 of an around-self strike lands on the cast frame, so the slime beside the hero is hurt even without a running scene.
                Monster hurt = Monster.Create(root.transform, MonsterDef.FrostSlime, hero.Position + new Vector2(0f, 10f), -1);
                float hp = hurt.Hp;
                int strike = cls == "warrior" ? 1 : cls == "cleric" ? 6 : cls == "wizard" ? 5 : cls == "assassin" ? 2 : 6; // Whirlwind, Purification, Frost Nova, Blade Dance, EMP Burst
                SkillExecutor.Cast(hero, strike, 3);
                expect(hurt.Hp < hp || hurt.IsDying, cls + ": around-self skill strike damages a monster beside the hero (" + hp + " -> " + hurt.Hp + ")");
                UnityEngine.Object.DestroyImmediate(hurt.gameObject);
                Zone.Main.Monsters.Remove(hurt);
                if (cls == "wizard")
                {
                    SkillExecutor.Cast(hero, 4, 2);
                    expect(HeroBuffs.For(hero).ShieldLeft > 0f, "wizard: Mana Shield adds an absorbing shield (" + HeroBuffs.For(hero).ShieldLeft + " HP)");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(dummy.gameObject); if (Zone.Main != null) Zone.Main.Monsters.Remove(dummy); }
        }
    }
}
