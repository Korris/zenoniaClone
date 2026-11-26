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
        public Vector2? Ground;          // Ground skills: the chosen centre (range-clamped on the authority); null for every other targeting
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

        /// <param name="ground">Ground skills: the point the player picked (null = auto pick). The authority clamps it to SkillDef.CastRange; a
        /// replaying client takes it as it came (the server's own point).</param>
        public static SkillContext Resolve(SkillDef def, int rank, ISkillBody body, int salt = 0, Vector2? ground = null)
        {
            if (def == null || body == null || body.IsDead) return null;
            Vector2 feet = body.Feet;
            var ctx = new SkillContext
            {
                Body = body, Def = def, Rank = Mathf.Max(1, rank), CastFrom = feet, Dir = HeroSkillBody.DirOf(body.Facing),
                Authority = NetMode.IsAuthority, View = NetMode.RendersFx,
            };
            ctx.Seed = SkillScatter.Seed(body.NetId, def.Index, 0f, 0f, salt); // position free: the proxy a client sees stands a hair off the server hero
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
                if (ctx.Ally == null) return null; // only a body without a hero (never a Cleric): nothing to heal
            }
            else if (def.Targeting == SkillTargeting.Ground)
            {
                ctx.Ground = SkillGroundAim.Point(def, body, ground, ctx.Authority);
                Vector2 to = ctx.Ground.Value - feet;
                if (to.sqrMagnitude > 1f) ctx.Dir = to.normalized;
                if (ctx.Authority) body.Face(ctx.Dir); // turn toward the chosen spot (the cast pose and a leap read the facing)
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

        static readonly System.Collections.Generic.List<HeroController> AllyPool = new System.Collections.Generic.List<HeroController>(8);
        static readonly System.Collections.Generic.List<float> FracPool = new System.Collections.Generic.List<float>(8);

        /// <summary>Hurt ally (the caster included) with the lowest HP fraction within reach, else the caster itself (LowestAllyHealRule: the heal always
        /// reaches the caster, 2026-11-20). Null only for a body without a hero.</summary>
        static HeroController LowestAlly(ISkillBody body, float reach)
        {
            HeroController self = body.Hero;
            if (self == null) return null;
            AllyPool.Clear(); FracPool.Clear();
            int caster = -1;
            foreach (HeroController h in Heroes(body))
            {
                if (h == null || h.IsDead || !PartyAllies.Are(self, h) || (h.Position - body.Feet).sqrMagnitude > reach * reach) continue;
                if (h == self) caster = AllyPool.Count;
                AllyPool.Add(h);
                FracPool.Add(h.Hp / Mathf.Max(1f, h.MaxHp));
            }
            if (caster < 0) { caster = AllyPool.Count; AllyPool.Add(self); FracPool.Add(self.Hp / Mathf.Max(1f, self.MaxHp)); }
            return AllyPool[LowestAllyHealRule.PickTarget(FracPool, caster)];
        }

        /// <summary>Every hero of the zone (ally effects check PartyAllies on each); just the caster outside a zone.</summary>
        public static System.Collections.Generic.IEnumerable<HeroController> Heroes(ISkillBody body)
        {
            if (Zone.Main != null && Zone.Main.Heroes.Count > 0) return Zone.Main.Heroes.All;
            return new[] { body.Hero };
        }

        /// <summary>Centre of a Strike / Heal placed `e.Range` ahead: the chosen point of a Ground skill, else along the aim from `from`, stopped by walls.</summary>
        public static Vector2 PlacedCentre(SkillContext ctx, SkillEffect e, Vector2 from)
        {
            if (ctx.Ground.HasValue && e.Range > 0f) return ctx.Ground.Value;
            return e.Range > 0f ? ClampToWalls(from, from + ctx.Dir * e.Range * ctx.Units) : from;
        }

        /// <summary>Centre of a Rain's scatter disc: the chosen point of a Ground skill, else `Range` ahead of where the cast began (stopped by walls).</summary>
        public static Vector2 RainCentre(SkillContext ctx, SkillEffect e)
        {
            if (ctx.Ground.HasValue) return ctx.Ground.Value;
            return ClampToWalls(ctx.CastFrom, ctx.CastFrom + ctx.Dir * e.Range * ctx.Units);
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
