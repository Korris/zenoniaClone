using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Radial skill damage (web performRadialHit): every live monster whose box touches the circle takes one hit.</summary>
    public static class SkillDamage
    {
        static readonly List<Monster> Hits = new List<Monster>(24);

        /// <summary>Hits all monsters within radius of center; knockback pushes away from the centre; `src` gets the kill credit. Returns the reused list of monsters hit.</summary>
        public static List<Monster> Radial(Vector2 center, float radius, float atk, float mult, float knockback, bool big, ClassPalette sparks = null, float critChance = 0f, HeroController src = null)
        {
            Hits.Clear();
            ClassPalette hero = ClassPalette.Current;
            ClassPalette.Current = sparks ?? ClassPalette.Neutral; // skills keep their element colour for the impact sparks
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
                bool crit = critChance > 0f && Random.value < critChance;
                m.TakeHit(CombatMath.Damage(atk, mult * (crit ? 1.6f : 1f), m.Def), knock * (crit ? 2f : 1f), crit, big, false, src);
                Hits.Add(m);
            }
            ClassPalette.Current = hero;
            return Hits;
        }
    }
}
