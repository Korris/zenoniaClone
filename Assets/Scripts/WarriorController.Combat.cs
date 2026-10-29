using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Warrior combat: combo hit resolution and incoming damage.</summary>
    public partial class WarriorController
    {
        void DoHit(int step)
        {
            ComboHit h = combo.CurrentHit;
            if (h == null) return;
            Vector2 v = DirVec[facing];
            float offset = h.Reach * 0.5f + 4f;
            float cx = x + v.x * offset, cy = y + 6f + v.y * offset;
            float w = v.x != 0f ? h.Reach : h.Width, ht = v.y != 0f ? h.Reach : h.Width;
            var box = new Rect(cx - w * 0.5f, cy - ht * 0.5f, w, ht);
            bool finisher = combo.IsFinisher;
            SlashEffect.Spawn(cx, cy, DirAngle[facing], h.FxScale, finisher, step == 2, FeelConfig.FxOrder);
            SwordTrail.Spawn(x, y + 6f, DirAngle[facing], h.Reach, finisher, step == 2);

            float mult = combo.MultiplierFor(step, 1);
            int landed = 0;
            bool anyCrit = false;
            IReadOnlyList<Monster> monsters = Monster.All;
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                if (m.IsDying || !box.Overlaps(m.Box)) continue;
                bool crit = Random.value < CritChance;
                int dmg = CombatMath.Damage(Atk, mult * (crit ? 1.6f : 1f), m.Def);
                m.TakeHit(dmg, v * h.Knockback * (crit ? 2f : 1f), crit, finisher);
                landed++;
                anyCrit |= crit;
            }
            if (landed > 0)
            {
                GameClock.HitStop(FeelConfig.HitStopMs(finisher));
                CameraFollow.AddTrauma(finisher ? FeelConfig.TraumaFinisher : anyCrit ? FeelConfig.TraumaCrit : FeelConfig.TraumaNormal);
            }
        }

        /// <summary>Called by monsters when a strike connects.</summary>
        public void TakeDamage(int dmg)
        {
            if (dead || invuln > 0f || dashLeft > 0f) return;
            hp -= dmg;
            invuln = InvulnMs;
            hurt = HurtMs;
            DamageNumber.Spawn(x, y + 26f, dmg.ToString(), new Color(1f, 0.35f, 0.35f), false);
            CameraFollow.AddTrauma(FeelConfig.TraumaHurt);
            if (hp <= 0f)
            {
                hp = 0f;
                dead = true;
                deathTimer = 0f;
                dashLeft = 0f;
                combo.Reset();
            }
        }
    }
}
