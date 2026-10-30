using System;
using System.Reflection;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Smoke checks of HeroController.TryCastSkill (FrostSmokeTest, per class): with a Lv 60 character that learned every skill, each of the 8 skills
    /// casts on three dummies, spends its MP, starts its cooldown, roots the hero and refuses a second cast; skills whose first step is a strike /
    /// heal / buff show that effect at once. Also the server-side rules: out-of-range index refused, illegal learn / assign / respec requests refused.
    /// </summary>
    public static class SkillHeroCheck
    {
        static readonly MethodInfo Tick = typeof(HeroController).GetMethod("TickSkill", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void RunWorld(Action<bool, string> expect, HeroController hero, GameObject root)
        {
            PlayerState p = hero.Rpg;
            string cls = p.ClassId;
            p.Level = 60;
            p.RecomputeStats();
            foreach (int cap in new[] { 5, 10 }) for (int pass = 0; pass < 12; pass++) for (int i = 0; i < SkillDef.PerClass; i++) if (p.Skills[i] < cap) p.LearnSkill(i); // spread to rank 5 first (prerequisites), then deeper
            bool all = true;
            for (int i = 0; i < SkillDef.PerClass; i++) all &= p.Skills[i] >= 1;
            expect(all && p.SkillPointsFree >= 0, cls + ": Lv 60 character learned all 8 skills (SP left " + p.SkillPointsFree + ")");

            Vector2 home = hero.Position;
            for (int i = 0; i < SkillDef.PerClass; i++) CastOne(expect, hero, root, p, SkillTable.Get(cls, i), home);

            hero.Teleport(home, 0);
            Drain(hero);
            expect(!hero.TryCastSkill(99) && !hero.TryCastSkill(-1), cls + ": out-of-range skill index refused");
            CheckServerRules(expect, cls);
        }

        /// <summary>Full MP / HP and a long tick: clears the cooldowns and the cast root (the smoke test has no running Update).</summary>
        static void Drain(HeroController hero)
        {
            hero.Rpg.Mp = hero.Rpg.MaxMp;
            hero.Rpg.Hp = hero.Rpg.MaxHp;
            Tick.Invoke(hero, new object[] { 20f, 60000f });
        }

        static void CastOne(Action<bool, string> expect, HeroController hero, GameObject root, PlayerState p, SkillDef def, Vector2 home)
        {
            string name = p.ClassId + "/" + def.Name;
            hero.Teleport(home, 3);
            Drain(hero);
            p.Hp = p.MaxHp * 0.5f; // a hurt hero gives Heal / LowestAlly something to do
            Monster[] dummies = new Monster[3];
            Vector2[] at = { new Vector2(14f, 0f), new Vector2(26f, 6f), new Vector2(20f, -8f) };
            for (int k = 0; k < dummies.Length; k++) dummies[k] = Monster.Create(root.transform, MonsterDef.FrostSlime, home + at[k], -1);
            try
            {
                float mp = hero.Mp, hp = hero.Hp, dummyHp = TotalHp(dummies);
                bool ok = hero.TryCastSkill(def.Index);
                expect(ok, name + ": cast accepted");
                if (!ok) return;
                expect(Mathf.Approximately(mp - hero.Mp, def.MpAt(p.Skills[def.Index])), name + ": MP spent (" + (mp - hero.Mp) + ")");
                expect(Mathf.Abs(hero.SkillCdLeftMs(def.Index) - def.CooldownMs) < 1f && hero.IsCasting, name + ": cooldown " + hero.SkillCdLeftMs(def.Index) + " ms running, hero rooted");
                expect(!hero.TryCastSkill(def.Index), name + ": second cast refused (cooldown / rooted)");
                SkillEffect first = def.Effects.Length > 0 ? def.Effects[0] : null;
                if (first == null || first.AtMs != 0) return;
                if (first.Kind == EffectKind.Strike && first.Pct > 0f && first.Shape != AreaShape.Line) expect(TotalHp(dummies) < dummyHp, name + ": opening strike hurt the dummies");
                if (first.Kind == EffectKind.Heal) expect(hero.Hp > hp, name + ": heal restored HP " + hp + " -> " + hero.Hp);
                if (first.Kind == EffectKind.Buff) expect(HeroBuffs.For(hero).Any, name + ": buff applied");
            }
            finally
            {
                foreach (Monster m in dummies) { if (Zone.Main != null) Zone.Main.Monsters.Remove(m); if (m != null) UnityEngine.Object.DestroyImmediate(m.gameObject); }
                HeroBuffs.For(hero).Clear();
            }
        }

        static float TotalHp(Monster[] ms) { float s = 0f; foreach (Monster m in ms) s += m.IsDying ? 0f : m.Hp; return s; }

        /// <summary>The checks the server applies to InvOp.SkillLearn / SkillAssign / SkillRespec.</summary>
        static void CheckServerRules(Action<bool, string> expect, string cls)
        {
            var rpg = new PlayerRpg(PlayerState.NewGame(cls));
            PlayerState s = rpg.State;
            expect(s.Skills[0] == 1 && s.Skills[7] == 0, cls + ": new character starts with skill 1 only");
            expect(!rpg.ApplyInvAction(InvOp.SkillLearn, "", 7).Ok && s.Skills[7] == 0, cls + ": learn without SP / level refused");
            expect(!rpg.ApplyInvAction(InvOp.SkillLearn, "", 200).Ok, cls + ": learn of a non-existent skill refused");
            expect(!rpg.ApplyInvAction(InvOp.SkillAssign, "0", 7).Ok, cls + ": assign of an unlearned skill refused");
            expect(!rpg.ApplyInvAction(InvOp.SkillAssign, "9", 0).Ok && !rpg.ApplyInvAction(InvOp.SkillAssign, "x", 0).Ok, cls + ": assign to a bad slot refused");
            s.Level = 10;
            expect(rpg.ApplyInvAction(InvOp.SkillLearn, "", 0).Ok && s.Skills[0] == 2, cls + ": learn with SP ranks the skill up");
            expect(rpg.ApplyInvAction(InvOp.SkillAssign, "5", 0).Ok && s.Loadout[5] == 0, cls + ": assign places the skill");
            expect(rpg.ApplyInvAction(InvOp.SkillAssign, "5", PlayerRpg.NoSkill).Ok && s.Loadout[5] == -1, cls + ": assign 255 clears the slot");
            s.Gold = 0;
            expect(!rpg.ApplyInvAction(InvOp.SkillRespec, "", 0).Ok && s.Skills[0] == 2, cls + ": respec without gold refused");
            s.Gold = 100000;
            expect(rpg.ApplyInvAction(InvOp.SkillRespec, "", 0).Ok && s.Skills.Sum == 0, cls + ": respec with gold resets the ranks");
        }
    }
}
