using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Radial skill damage (web performRadialHit): every live monster whose box touches the circle takes one hit.</summary>
    public static class SkillDamage
    {
        static readonly List<Monster> Hits = new List<Monster>(24);

        /// <summary>Hits all monsters within radius of center; knockback pushes away from the centre. Returns the reused list of monsters hit.</summary>
        public static List<Monster> Radial(Vector2 center, float radius, float atk, float mult, float knockback, bool big)
        {
            Hits.Clear();
            IReadOnlyList<Monster> all = Monster.All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m.IsDying) continue;
                Rect b = m.Box;
                var nearest = new Vector2(Mathf.Clamp(center.x, b.xMin, b.xMax), Mathf.Clamp(center.y, b.yMin, b.yMax));
                if ((nearest - center).sqrMagnitude > radius * radius) continue;
                Vector2 away = m.Center - center;
                Vector2 knock = knockback > 0f && away.sqrMagnitude > 0.01f ? away.normalized * knockback : Vector2.zero;
                m.TakeHit(CombatMath.Damage(atk, mult, m.Def), knock, false, big);
                Hits.Add(m);
            }
            return Hits;
        }
    }
}
