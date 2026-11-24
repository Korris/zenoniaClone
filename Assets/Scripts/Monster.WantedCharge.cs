using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Authority-only CHARGE of a skill boss (WantedSkillDef.ChargeMs &gt; 0, first: Brambleback Matriarch's Bramble Charge). Monster.Ai.cs's Telegraph state calls
    /// BeginCharge instead of FireWantedSkill when the picked skill is a charge: the boss then dashes along the direction LOCKED when the warning began (LockWantedShapes),
    /// straight through heroes (no soft body collision while charging, Monster.cs Step), and every hero its body reaches inside the warned rectangle takes the hit once
    /// (WantedChargeRules). It ends at the end of the line or at a wall, then the usual Execute recovery. The hit is decided here on the zone, never by a client.
    /// </summary>
    public partial class Monster
    {
        /// <summary>How far ahead of the feet point the dashing body's front edge sits (half its hit box length).</summary>
        float ChargeReach { get { return def.HitW * 0.5f; } }

        WantedSkillDef chargeSkill;
        WantedShapeInstance chargeShape;
        float chargeElapsedMs, chargeSpeed;
        readonly HashSet<HeroController> chargeHit = new HashSet<HeroController>();

        /// <summary>True while the boss is dashing (Monster.cs skips the hero body clip).</summary>
        bool Charging { get { return chargeSkill != null; } }

        /// <summary>Telegraph ran out for a charge skill: start the dash along the locked shape (Execute lasts the dash + the usual recovery).</summary>
        void BeginCharge()
        {
            WantedSkillDef skill = pendingSkill;
            pendingSkill = null;
            if (skill == null || pendingShapes.Count == 0) { Enter(State.Execute, ExecuteMs); return; }
            chargeSkill = skill;
            chargeShape = pendingShapes[0];
            chargeElapsedMs = 0f;
            chargeSpeed = WantedChargeRules.SpeedUnitsPerSec(skill);
            chargeHit.Clear();
            Enter(State.Execute, skill.ChargeMs + ExecuteMs);
        }

        /// <summary>One Execute tick of a dash: the velocity along the locked direction, and the hits on every hero the body just reached.</summary>
        Vector2 ChargeStep(float dtMs)
        {
            chargeElapsedMs += dtMs;
            float travelled = WantedChargeRules.Travelled(chargeSkill, chargeElapsedMs);
            IReadOnlyList<HeroController> all = homeZone != null ? homeZone.Heroes.All : null;
            for (int i = 0; all != null && i < all.Count; i++)
            {
                HeroController h = all[i];
                if (h == null || h.IsDead || chargeHit.Contains(h)) continue;
                Vector2 p = h.Position;
                if (!WantedChargeRules.Reached(chargeSkill, chargeShape, travelled, ChargeReach, p.x, p.y)) continue;
                chargeHit.Add(h);
                h.TakeWantedSkillHit(Feet, def.Atk, chargeSkill);
            }
            if (chargeElapsedMs >= chargeSkill.ChargeMs) { chargeSkill = null; return Vector2.zero; }
            float rad = chargeShape.DirDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * chargeSpeed;
        }
    }
}
