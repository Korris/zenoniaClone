using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What the Auto does this frame, decided once per frame (every InputAdapter reader of the frame sees the same answer): potion first, then the
    /// target (re-picked every 0.4 s), a skill when one is ready, loot within 3 tiles, else back to the spot it was switched on. Numbers from the
    /// ChatGPT V1 table (docs/reference/auto-battle-midgard-senki-chatgpt.md).
    /// </summary>
    public static partial class AutoBattle
    {
        const float Tile = 16f, PatrolRadius = 7f * Tile, ChaseExtra = 2f * Tile, LootRadius = 3f * Tile, HomeSlack = Tile;
        const float RetargetSeconds = 0.4f, SkillGapSeconds = 0.35f, PotionGapSeconds = 2f, StuckSeconds = 1.5f, SkipSeconds = 5f;
        const float HpPotionAt = 0.55f, MpPotionAt = 0.35f, BigSkillCooldownMs = 20000f, LowHpFraction = 0.4f;

        static int thoughtFrame = -1;
        static Vector2 move;
        static bool attack;
        static GameKey tap = GameKey.AutoToggle; // AutoToggle = nothing to tap
        static Monster target;
        static float retargetAt, skillAt, hpPotionAt, mpPotionAt, movedAt;
        static Vector2 lastPos;
        static readonly TileNav reach = new TileNav(), walk = new TileNav(); // reach: steps from the hero to anything; walk: the way to the current goal
        const int MaxSteps = 14; // a target further than this many walking tiles (around cliffs) is not worth the trip
        static readonly Dictionary<Monster, float> skipUntil = new Dictionary<Monster, float>(); // unreachable targets, ignored for a while

        static void ResetBrain() { thoughtFrame = -1; target = null; retargetAt = 0f; movedAt = Time.time; skipUntil.Clear(); }

        static void Think()
        {
            if (thoughtFrame == Time.frameCount) return;
            thoughtFrame = Time.frameCount;
            move = Vector2.zero; attack = false; tap = GameKey.AutoToggle;
            HeroController hero = LocalPlayer.Hero;
            if (hero == null || hero.IsDead) return;
            float now = Time.time;
            if (hero.Hp < hero.MaxHp * HpPotionAt && now >= hpPotionAt) { hpPotionAt = now + PotionGapSeconds; tap = GameKey.HpPotion; }
            else if (hero.Mp < hero.MaxMp * MpPotionAt && now >= mpPotionAt) { mpPotionAt = now + PotionGapSeconds; tap = GameKey.MpPotion; }

            if ((hero.Position - lastPos).sqrMagnitude > 1f) { lastPos = hero.Position; movedAt = now; }
            if (target != null && (target.IsDying || !InPatrol(target.Center, ChaseExtra))) target = null;
            if (target == null || now >= retargetAt) { target = PickTarget(hero, now); retargetAt = now + RetargetSeconds; }
            if (target != null)
            {
                bool close = (target.Center - hero.Position).sqrMagnitude < 3f * Tile * 3f * Tile && LineOfSight(hero.Position, target.Center);
                bool inRange = close ? SteerToAttack(hero, target.Center) : WalkTo(hero, target.Feet);
                if (hero.LocksTarget) inRange = hero.InLockReach(target, 1f); // Warrior: the slash needs sword contact with the body edge (LockTargetRules); keep walking until then
                if (inRange) { attack = true; movedAt = now; }
                else if (now - movedAt > StuckSeconds) { skipUntil[target] = now + SkipSeconds; target = null; movedAt = now; return; } // walled off: try another
                if (inRange && tap == GameKey.AutoToggle && now >= skillAt)
                {
                    int skill = PickSkill(hero, target);
                    if (skill >= 0) { tap = GameKey.Skill1 + skill; skillAt = now + SkillGapSeconds; }
                }
                return;
            }
            LootDrop loot = PickLoot(hero);
            if (loot != null) { WalkTo(hero, loot.transform.position); return; }
            if ((hero.Position - origin).magnitude > HomeSlack) WalkTo(hero, origin);
        }

        static bool InPatrol(Vector2 p, float extra) { return (p - origin).sqrMagnitude <= (PatrolRadius + extra) * (PatrolRadius + extra); }

        /// <summary>The boss the player was fighting, else the nearest reachable non-boss monster inside the patrol circle.</summary>
        static Monster PickTarget(HeroController hero, float now)
        {
            if (allowedBoss != null && !allowedBoss.IsDying) return allowedBoss;
            Monster best = null;
            float bestD = float.MaxValue, skip;
            foreach (Monster m in Monster.All)
            {
                if (m.IsDying || m.IsBoss || m.IsWanted || !InPatrol(m.Center, 0f)) continue;
                if (skipUntil.TryGetValue(m, out skip) && now < skip) continue;
                int steps = reach.Steps(hero.Position, m.Feet); // field around the hero: one BFS answers every monster
                if (steps < 0 || steps > MaxSteps) continue;
                float d = steps + (m.Center - hero.Position).magnitude / 1000f; // ties: the straight-line nearer one
                if (d < bestD) { bestD = d; best = m; }
            }
            return best;
        }

        /// <summary>Lines up on one axis first so the swing faces the target (as the capture fight bot does); true once in reach.</summary>
        static bool SteerToAttack(HeroController hero, Vector2 at)
        {
            Vector2 diff = at - hero.Position - new Vector2(0f, 4f);
            float range = hero.Class.Ranged ? 80f : 24f;
            bool horizontal = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
            float along = horizontal ? diff.x : diff.y, across = horizontal ? diff.y : diff.x;
            if (Mathf.Abs(across) > 5f) { if (horizontal) move.y = Mathf.Sign(across); else move.x = Mathf.Sign(across); return false; }
            if (horizontal) move.x = Mathf.Sign(along); else move.y = Mathf.Sign(along); // keep facing it while swinging
            return Mathf.Abs(along) <= range;
        }

        /// <summary>One tile step along the BFS path (around cliffs, over bridges and stairs); straight in when on the goal's tile. Always false (not in reach).</summary>
        static bool WalkTo(HeroController hero, Vector2 to)
        {
            Vector2 step = walk.StepToward(hero.Position, to);
            if (step == Vector2.zero)
            {
                Vector2 d = to - hero.Position;
                if (Mathf.Abs(d.x) > 3f) step.x = Mathf.Sign(d.x);
                if (Mathf.Abs(d.y) > 3f) step.y = Mathf.Sign(d.y);
            }
            move = step;
            return false;
        }

        static LootDrop PickLoot(HeroController hero)
        {
            LootDrop best = null;
            float bestD = LootRadius * LootRadius;
            foreach (LootDrop l in LootDrop.All)
            {
                if (l == null || !l.CanTake(hero) || !InPatrol(l.transform.position, 0f)) continue;
                float d = ((Vector2)l.transform.position - hero.Position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = l; }
            }
            return best;
        }

        /// <summary>
        /// A ready, affordable magazine skill: self buffs first, heals only under half HP, long-cooldown skills (20 s+) not wasted on a
        /// weak monster that is nearly dead and alone; among the rest the longest cooldown (the big hits) goes first. -1 = none.
        /// </summary>
        static int PickSkill(HeroController hero, Monster foe)
        {
            HeroClassDef cls = hero.Class;
            int best = -1;
            float bestScore = float.MinValue;
            bool weakAlone = !foe.IsBoss && !foe.IsWanted && foe.Hp < foe.MaxHp * LowHpFraction && CountNear(foe.Center, 3f * Tile) < 3;
            for (int page = 0; page < SkillMagazine.PageCount(cls); page++)
                for (int s = 0; s < SkillMagazine.SlotCount; s++)
                {
                    SkillSlot slot = SkillMagazine.Slot(cls, page, s);
                    if (!slot.IsSkill) continue;
                    int i = slot.SkillIndex;
                    SkillDef def = SkillTable.Get(hero.Rpg.ClassId, i);
                    int rank = hero.Rpg.Skills[i];
                    if (def == null || rank <= 0 || hero.SkillCdLeftMs(i) > 0f) continue;
                    if (!SkillCooldowns.DevFreeMp && hero.Mp < def.MpAt(rank)) continue;
                    bool heal = HasEffect(def, EffectKind.Heal);
                    if (heal && hero.Hp >= hero.MaxHp * 0.5f) continue;
                    if (def.BaseCooldownMs >= BigSkillCooldownMs && weakAlone && def.Targeting != SkillTargeting.Self) continue;
                    float score = def.Targeting == SkillTargeting.Self || heal ? 1e6f : def.BaseCooldownMs;
                    if (score > bestScore) { bestScore = score; best = i; }
                }
            return best;
        }

        static bool HasEffect(SkillDef def, EffectKind kind)
        {
            foreach (SkillEffect e in def.Effects) if (e.Kind == kind) return true;
            return false;
        }

        static int CountNear(Vector2 at, float radius)
        {
            int n = 0;
            foreach (Monster m in Monster.All) if (!m.IsDying && (m.Center - at).sqrMagnitude <= radius * radius) n++;
            return n;
        }

        static bool LineOfSight(Vector2 a, Vector2 b)
        {
            IGameMap map = MapRegistry.Current;
            if (map == null) return true;
            int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 6f);
            for (int i = 1; i < n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)n);
                if (map.RectBlocked(p.x - 3f, p.y, 6f, 6f)) return false;
            }
            return true;
        }
    }
}
