using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Everything one cast needs, resolved once at cast time: who, which skill and rank, where it points, which target it locked.</summary>
    public sealed class SkillContext
    {
        public ISkillBody Body;
        public SkillDef Def;
        public int Rank;
        public Vector2 Dir;              // unit aim direction (changes only when a Blink turns the caster towards its target)
        public Vector2 CastFrom;         // caster feet when the cast began
        public Vector2? LineFrom;        // start of the last Dash / Leap: a Line strike right after it sweeps the travelled path
        public Monster Target;           // NearestEnemy
        public HeroController Ally;      // LowestAlly (may be the caster)
        public int Seed;                 // deterministic scatter (rain)
        public bool Authority, View;     // damage / heal / status run on the authority, art + sound where RendersFx
        public SkillDamageCtx Dmg;

        public float Units { get { return GameConfig.TileUnits; } }
        public float Pct(SkillEffect e) { return Def.PctAt(e, Rank); }
        public Vector2 Feet { get { return Body.Feet; } }
        public ClassPalette Palette { get { return Body.Palette; } }
    }

    /// <summary>Turns "slot pressed" into a SkillContext on the authority (target search, aim clamped by walls). Null = refused, costs nothing.</summary>
    public static class SkillAim
    {
        static readonly Vector2 HeroCenter = new Vector2(0f, 6f);

        public static SkillContext Resolve(SkillDef def, int rank, ISkillBody body, int salt = 0)
        {
            if (def == null || body == null || body.IsDead) return null;
            Vector2 feet = body.Feet;
            var ctx = new SkillContext
            {
                Body = body, Def = def, Rank = Mathf.Max(1, rank), CastFrom = feet, Dir = HeroSkillBody.DirOf(body.Facing),
                Authority = NetMode.IsAuthority, View = NetMode.RendersFx,
            };
            ctx.Seed = SkillScatter.Seed(body.NetId, def.Index, feet.x, feet.y, salt);
            float reach = def.TargetRange * GameConfig.TileUnits;
            if (def.Targeting == SkillTargeting.NearestEnemy)
            {
                ctx.Target = NearestMonster(feet, reach);
                if (ctx.Target == null) return null;
                Vector2 to = ctx.Target.Center - feet;
                if (to.sqrMagnitude > 0.01f) ctx.Dir = to.normalized;
            }
            else if (def.Targeting == SkillTargeting.LowestAlly)
            {
                ctx.Ally = LowestAlly(body, reach);
                if (ctx.Ally == null) return null; // nobody hurt in range: no cost
            }
            ctx.Dmg = SkillDamageCtx.For(body.Hero, def.DamageMul(ctx.Rank), body.Palette);
            ctx.Dmg.Atk = body.Atk;
            return ctx;
        }

        /// <summary>Closest live monster whose centre is within `reach` of `from` (null when none).</summary>
        public static Monster NearestMonster(Vector2 from, float reach)
        {
            Monster best = null;
            float bestSq = reach * reach;
            System.Collections.Generic.IReadOnlyList<Monster> all = Monster.All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m.IsDying) continue;
                float sq = (m.Center - from).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = m; }
            }
            return best;
        }

        /// <summary>Hurt hero (the caster included) with the lowest HP fraction within reach; null when everybody is at full HP.</summary>
        static HeroController LowestAlly(ISkillBody body, float reach)
        {
            HeroController best = null;
            float bestFrac = 0.999f;
            foreach (HeroController h in Heroes(body))
            {
                if (h == null || h.IsDead || (h.Position - body.Feet).sqrMagnitude > reach * reach) continue;
                float frac = h.Hp / Mathf.Max(1f, h.MaxHp);
                if (frac < bestFrac) { bestFrac = frac; best = h; }
            }
            return best;
        }

        /// <summary>Every hero of the zone (any hero counts as an ally until a party system exists); just the caster outside a zone.</summary>
        public static System.Collections.Generic.IEnumerable<HeroController> Heroes(ISkillBody body)
        {
            if (Zone.Main != null && Zone.Main.Heroes.Count > 0) return Zone.Main.Heroes.All;
            return new[] { body.Hero };
        }

        /// <summary>Walks from `from` towards `to` and returns the last point the hero box fits (so aimed areas never land inside walls).</summary>
        public static Vector2 ClampToWalls(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            float len = d.magnitude;
            if (len < 1f) return to;
            Vector2 last = from, step = d / len * 4f;
            for (float t = 4f; t < len + 4f; t += 4f)
            {
                Vector2 p = t >= len ? to : from + step * (t / 4f);
                if (MapRegistry.Current.RectBlocked(p.x - 2f, p.y, 4f, 4f)) return last;
                last = p;
            }
            return to;
        }
    }
}
