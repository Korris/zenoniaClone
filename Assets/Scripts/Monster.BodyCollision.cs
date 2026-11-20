using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Soft unit collision, monster side (Rpg/BodyCollisionRules.cs): a monster's own move stops at a hero's feet and slides around it, so a boss
    /// walking into a crowd stands still instead of shoving 40 players. Authority only (proxies never Step). Heroes never push monsters either.
    /// </summary>
    public partial class Monster
    {
        /// <summary>Foot core radius (target ring size, collision), from MonsterDef numbers only.</summary>
        public float FootRadius { get { return BodyCollisionRules.FootRadius(def.HitW, def.BodyHeightUnits * def.RenderScale, def.IsBoss, def.IsWanted); } }

        /// <summary>Ground distance under which a hero overlaps this monster (hard for bosses, soft for normal monsters).</summary>
        public float HeroContact { get { return BodyCollisionRules.ContactDistance(FootRadius, def.IsBoss); } }


        /// <summary>Removes the part of this step's move (dx, dy) that would walk into a living hero of this zone.</summary>
        void ClipAgainstHeroes(ref float dx, ref float dy)
        {
            if (homeZone == null) return;
            IReadOnlyList<HeroController> heroes = homeZone.Heroes.All;
            float c = HeroContact;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroController h = heroes[i];
                if (h == null || h.IsDead) continue;
                Vector2 hp = h.SimPosition;
                BodyCollisionRules.ClipMove(x - hp.x, y - hp.y, c, ref dx, ref dy);
            }
        }

        /// <summary>True when the hero touches this monster's feet: it may strike although AttackRange is shorter than the blocked approach.</summary>
        bool TouchesHero(HeroController hero, float slack)
        {
            Vector2 hp = hero.SimPosition;
            if (BodyCollisionRules.TouchReach(hp.x - x, hp.y - y, HeroContact, slack)) return true;
            if (!def.IsBoss || def.Ranged != null) return false;
            Rect b = MeleeBody; // a boss: the hero is in sword contact with its painted body (symmetric melee, no stepping into the hero)
            Vector2 chest = hero.ChestPoint;
            float r = BodyCollisionRules.BossContactReach + slack;
            return LockTargetRules.BoxDistSq(chest.x, chest.y, b.xMin, b.yMin, b.xMax, b.yMax) <= r * r;
        }
    }
}
