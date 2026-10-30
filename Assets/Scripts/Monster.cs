using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Hollow monster, AI ported from the web monster-ai-system: idle -> wander -> chase (aggro range) -> windup (telegraph) ->
    /// attack -> cooldown -> chase | return (leash). Walks on the tile collision grid, takes knockback + white flash, fades out on
    /// death and tells the spawner so the zone can schedule a respawn. See Monster.Ai.cs (brain) and Monster.Visuals.cs (sprites).
    /// </summary>
    public partial class Monster : MonoBehaviour
    {
        enum State { Idle, Wander, Chase, Windup, Cooldown, Return, Dying }

        const float WalkFrameMs = 160f, HitFlashMs = 100f, KnockbackMs = 120f;
        const float RecoveryMs = 240f;            // attack follow-through frames after the strike (web HD_MONSTER_ATTACK)
        const float FadeMs = 700f;
        static readonly List<Monster> NoMonsters = new List<Monster>();

        // The monster list, kill counter and boss live in the Zone; these getters keep the old call sites working.
        public static IReadOnlyList<Monster> All { get { return Zone.Main != null ? Zone.Main.Monsters : NoMonsters; } }
        public static int Kills { get { return Zone.Main != null ? Zone.Main.Kills : 0; } }
        /// <summary>The living Frostfang King (HUD boss bar), or null.</summary>
        public static Monster Boss { get { return Zone.Main != null ? Zone.Main.Boss : null; } }
        public static void ResetStatics() { Zone.ResetMain(); }

        MonsterDef def;
        Vector2 home, wanderDir, knock;
        int zone;                                  // spawn zone index, -1 = boss (never respawns)
        float x, y, hp, timer, animTimer, flash, knockLeft, knockTotal, deathTimer;
        int facing;                                // 0 down, 1 up, 2 left, 3 right
        State state = State.Idle;
        Sprite[][] walk, attack;
        SpriteRenderer body, shadow, barBack, barFill;
        bool headless;                             // dedicated server: simulation only, no sprites / renderers
        HeroController target;                     // sticky aggro target (Monster.Ai.cs)

        public float Def { get { return def.Def; } }
        /// <summary>Web monster type id (frostSlime, iceGoblin ...), the key of the RPG loot table.</summary>
        public string Type { get { return def.Sheet; } }
        public Vector2 Feet { get { return new Vector2(x, y); } }
        public string DisplayName { get { return def.Name; } }
        public float Hp { get { return hp; } }
        public float MaxHp { get { return def.Hp; } }
        public bool IsDying { get { return state == State.Dying; } }
        public Rect Box { get { return new Rect(x - def.HitW * 0.5f, y, def.HitW, def.HitH); } }
        /// <summary>Body centre used for AoE distance (web: feet - 6).</summary>
        public Vector2 Center { get { return new Vector2(x, y + 6f); } }

        public static Monster Create(Transform parent, MonsterDef definition, Vector2 feet, int zoneIndex)
        {
            var go = new GameObject("Monster_" + definition.Sheet);
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<Monster>();
            m.Init(definition, feet, zoneIndex);
            Zone zone = Zone.Main;
            if (zone != null)
            {
                zone.Monsters.Add(m);
                if (definition.IsBoss) zone.Boss = m;
            }
            return m;
        }

        void Init(MonsterDef definition, Vector2 feet, int zoneIndex)
        {
            def = definition;
            home = feet; x = feet.x; y = feet.y; zone = zoneIndex;
            hp = def.Hp;
            timer = 600f;
            animTimer = Random.value * 1000f;
            headless = NetMode.IsHeadless;
            transform.position = new Vector3(x, y, 0f);
            if (headless) return; // server: no sprite sheets, shadow, body or HP bar
            walk = SpriteSheet.Grid("Sprites/monsters/" + def.Sheet, 192, 192, GameConfig.FramePpu, new Vector2(0.5f, 0f));
            // Attack cells are 256px with the feet 224px down => pivot 32px above the bottom.
            attack = SpriteSheet.Grid("Sprites/monsters/" + def.Sheet + "-attack", 256, 256, GameConfig.FramePpu, new Vector2(0.5f, 32f / 256f));
            shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            float sw = def.HitW + 2f;
            shadow.transform.localScale = new Vector3(sw, sw, 1f);
            body = ProceduralSprites.Child(transform, "Body", null);
            body.sharedMaterial = FeelMaterials.SpriteFlash; // white-flash capable sprite shader
            body.transform.localScale = new Vector3(def.RenderScale, def.RenderScale, 1f);
            barBack = ProceduralSprites.Child(transform, "BarBack", ProceduralSprites.White(true));
            barFill = ProceduralSprites.Child(transform, "BarFill", ProceduralSprites.White(true));
            barBack.color = new Color(0f, 0f, 0f, 0.7f);
            barFill.color = new Color(0.9f, 0.2f, 0.25f, 1f);
            transform.position = new Vector3(x, y, 0f);
            Render();
        }

        void OnDestroy()
        {
            Zone zone = Zone.Main;
            if (zone == null) return;
            zone.Monsters.Remove(this);
            if (zone.Boss == this) zone.Boss = null;
        }

        void Update()
        {
            // A script recompile during Play mode wipes runtime-built references (body, HP bar):
            // drop the stale monster instead of throwing a NullReferenceException every frame.
            if (!headless && (body == null || barBack == null || barFill == null)) { Destroy(gameObject); return; }
            float dt = GameClock.Dt, dtMs = dt * 1000f;
            animTimer += dtMs;
            flash = Mathf.Max(0f, flash - dtMs);
            if (state == State.Dying)
            {
                deathTimer += dtMs;
                if (deathTimer >= FadeMs) { Destroy(gameObject); return; }
            }
            else Step(dt, dtMs);
            transform.position = new Vector3(x, y, 0f);
            Render();
        }

        /// <summary>
        /// One simulation step: knockback slide, or the AI intent. The AI only runs while a living hero is within ActiveRadius (dormancy: far
        /// monsters freeze in place and resume on wake, which also matches the old "frozen while the hero is dead").
        /// </summary>
        void Step(float dt, float dtMs)
        {
            float dx = 0f, dy = 0f;
            if (TickStatus(dtMs)) return;
            if (knockLeft > 0f)
            {
                // Fx on: ease-out slide (fast start, soft stop) with the same average speed; off: constant velocity.
                float k = FeelConfig.FxOn ? 2f * knockLeft / knockTotal : 1f;
                dx = knock.x * k * dt; dy = knock.y * k * dt; knockLeft -= dtMs;
            }
            else
            {
                HeroController hero = AcquireTarget();
                if (hero == null) return;
                bool strike;
                Vector2 move = Think(dtMs, hero, out strike);
                dx = move.x * dt; dy = move.y * dt;
                if (strike) Strike(hero);
            }
            if (dx == 0f && dy == 0f) return;
            if (state == State.Chase) { Vector2 sep = Separation(); dx += sep.x * dt; dy += sep.y * dt; }
            bool bx, by;
            Vector2 p = FrostHollowMap.Move(new Vector2(x, y), dx, dy, def.HitW, def.HitH, out bx, out by);
            x = p.x; y = p.y;
            if ((bx || by) && state == State.Wander) timer = 0f; // wandering into a wall picks a new direction
        }

        void Strike(HeroController hero)
        {
            if (Vector2.Distance(hero.Position, new Vector2(x, y)) <= def.AttackRange + 10f)
                hero.TakeDamage(CombatMath.Damage(def.Atk, 1f, hero.DefStat));
            if (def.IsBoss) CameraFollow.AddTrauma(0.5f);
        }

        /// <summary>Gentle push so monsters do not stack on one pixel.</summary>
        Vector2 Separation()
        {
            Vector2 push = Vector2.zero;
            IReadOnlyList<Monster> all = All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster o = all[i];
                if (o == this || o.IsDying) continue;
                Vector2 d = new Vector2(x - o.x, y - o.y);
                float m = d.magnitude;
                if (m < 10f && m > 0.001f) push += d / m * 30f;
            }
            return push;
        }

        /// <summary>
        /// Applies a hit. `src` is the attacking hero (kill credit, damage table, aggro); null credits the local player so single-player
        /// call sites without an attacker keep working.
        /// </summary>
        public void TakeHit(int dmg, Vector2 knockVel, bool crit, bool finisher, bool quietFx = false, HeroController src = null)
        {
            if (IsDying) return;
            if (src == null) src = LocalPlayer.Hero;
            Credit(src, Mathf.Min(dmg, Mathf.Max(1, Mathf.CeilToInt(hp)))); // overkill does not count towards the damage share
            if (src != null && !src.IsDead) target = src; // whoever hurts it becomes the sticky target
            hp -= dmg;
            bool fx = FeelConfig.FxOn;
            flash = fx ? FeelConfig.FlashMs : HitFlashMs;
            if (knockVel != Vector2.zero && !def.IsBoss) // the boss is too heavy to be shoved around
            {
                knock = fx ? knockVel * FeelConfig.KnockbackMultiplier : knockVel;
                knockTotal = knockLeft = fx ? FeelConfig.KnockbackMs : KnockbackMs;
            }
            if (state == State.Idle || state == State.Wander || state == State.Return) { state = State.Chase; timer = 0f; }
            float bodyH = def.BodyHeightUnits * def.RenderScale;
            Color c = crit ? new Color(1f, 0.85f, 0.2f) : finisher ? new Color(1f, 0.69f, 0.23f) : Color.white;
            DamageNumber.Spawn(x, y + bodyH + 4f, crit && fx ? dmg + "!" : dmg.ToString(), c, crit || finisher, crit);
            ImpactFx.Spawn(x, y + bodyH * 0.5f, knockVel, crit, finisher, null, quietFx);
            if (hp > 0f) return;
            hp = 0f;
            state = State.Dying;
            deathTimer = 0f;
            target = null;
            if (Zone.Main != null) Zone.Main.OnMonsterDied(this, zone);
        }
    }
}
