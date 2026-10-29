using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Melee chaser: idle -> chase (aggro range) -> windup (telegraph) -> strike -> cooldown. Takes knockback + hit flash,
    /// fades out on death and respawns at its home point once the hero is away.
    /// </summary>
    public class Monster : MonoBehaviour
    {
        enum State { Idle, Chase, Windup, Cooldown, Dying }

        const float WalkFrameMs = 160f, HitFlashMs = 100f, KnockbackMs = 120f;
        const float RecoveryMs = 240f;            // attack follow-through frames after the strike (web HD_MONSTER_ATTACK)
        const float RespawnDelayMs = 6000f, FadeMs = 700f;
        static readonly List<Monster> AllList = new List<Monster>();

        public static IReadOnlyList<Monster> All { get { return AllList; } }
        public static int Kills { get; private set; }
        public static void ResetStatics() { AllList.Clear(); Kills = 0; }

        MonsterDef def;
        Vector2 home;
        float x, y, hp, timer, animTimer, flash, knockLeft, deathTimer;
        Vector2 knock;
        int facing;
        State state = State.Idle;
        Sprite[][] walk, attack;
        SpriteRenderer body, shadow, barBack, barFill;

        public float Def { get { return def.Def; } }
        public bool IsDying { get { return state == State.Dying; } }
        public Rect Box { get { return new Rect(x - def.HitW * 0.5f, y, def.HitW, def.HitH); } }

        public static Monster Create(Transform parent, MonsterDef definition, Vector2 pos)
        {
            var go = new GameObject("Monster_" + definition.Sheet);
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<Monster>();
            m.Init(definition, pos);
            AllList.Add(m);
            return m;
        }

        void Init(MonsterDef definition, Vector2 pos)
        {
            def = definition;
            home = pos;
            walk = SpriteSheet.Grid("Sprites/monsters/" + def.Sheet, 192, 192, GameConfig.FramePpu, new Vector2(0.5f, 0f));
            // Attack cells are 256px with the feet 224px down => pivot 32px above the bottom.
            attack = SpriteSheet.Grid("Sprites/monsters/" + def.Sheet + "-attack", 256, 256, GameConfig.FramePpu, new Vector2(0.5f, 32f / 256f));
            shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            shadow.color = new Color(1f, 1f, 1f, 0.3f);
            float sw = def.HitW + 2f;
            shadow.transform.localScale = new Vector3(sw, sw, 1f);
            body = ProceduralSprites.Child(transform, "Body", null);
            barBack = ProceduralSprites.Child(transform, "BarBack", ProceduralSprites.White(true));
            barFill = ProceduralSprites.Child(transform, "BarFill", ProceduralSprites.White(true));
            barBack.color = new Color(0f, 0f, 0f, 0.7f);
            barFill.color = new Color(0.9f, 0.2f, 0.25f, 1f);
            Respawn();
        }

        void Respawn()
        {
            x = home.x + Random.Range(-6f, 6f);
            y = home.y + Random.Range(-6f, 6f);
            hp = def.Hp;
            state = State.Idle;
            timer = 0f; flash = 0f; knockLeft = 0f;
            body.enabled = true; shadow.enabled = true;
            animTimer = Random.value * 1000f;
        }

        void Update()
        {
            float dt = GameClock.Dt, dtMs = dt * 1000f;
            animTimer += dtMs;
            flash = Mathf.Max(0f, flash - dtMs);
            WarriorController hero = WarriorController.Instance;
            bool heroAlive = hero != null && !hero.IsDead;
            Vector2 toHero = heroAlive ? hero.Position - new Vector2(x, y) : Vector2.zero;
            float dist = toHero.magnitude;

            if (state == State.Dying)
            {
                deathTimer += dtMs;
                bool heroFar = !heroAlive || Vector2.Distance(hero.Position, home) > 90f;
                if (deathTimer >= RespawnDelayMs && heroFar) Respawn();
            }
            else
            {
                if (knockLeft > 0f) { x += knock.x * dt; y += knock.y * dt; knockLeft -= dtMs; }
                Think(dt, dtMs, heroAlive, toHero, dist, hero);
                x = Mathf.Clamp(x, 8f, GameConfig.MapW - 8f);
                y = Mathf.Clamp(y, 4f, GameConfig.MapH - 4f);
            }
            transform.position = new Vector3(x, y, 0f);
            Render();
        }

        void Think(float dt, float dtMs, bool heroAlive, Vector2 toHero, float dist, WarriorController hero)
        {
            switch (state)
            {
                case State.Idle:
                    if (heroAlive && dist <= def.AggroRange) state = State.Chase;
                    break;
                case State.Chase:
                    if (!heroAlive || dist > def.LeashRange) { state = State.Idle; break; }
                    FaceTowards(toHero);
                    if (dist <= def.AttackRange) { state = State.Windup; timer = def.WindupMs; break; }
                    if (knockLeft <= 0f && dist > 0.001f)
                    {
                        Vector2 step = toHero / dist * (def.Speed * dt);
                        x += step.x; y += step.y;
                    }
                    Separate(dt);
                    break;
                case State.Windup:
                    timer -= dtMs;
                    if (heroAlive) FaceTowards(toHero);
                    if (timer <= 0f)
                    {
                        if (heroAlive && dist <= def.AttackRange + 10f) hero.TakeDamage(CombatMath.Damage(def.Atk, 1f, WarriorController.Def));
                        state = State.Cooldown;
                        timer = def.CooldownMs;
                    }
                    break;
                case State.Cooldown:
                    timer -= dtMs;
                    if (timer <= 0f) state = State.Chase;
                    break;
            }
        }

        void FaceTowards(Vector2 d)
        {
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) facing = d.x > 0f ? 3 : 2;
            else facing = d.y > 0f ? 1 : 0;
        }

        /// <summary>Gentle push so monsters do not stack on one pixel.</summary>
        void Separate(float dt)
        {
            for (int i = 0; i < AllList.Count; i++)
            {
                Monster o = AllList[i];
                if (o == this || o.IsDying) continue;
                Vector2 d = new Vector2(x - o.x, y - o.y);
                float m = d.magnitude;
                if (m < 10f && m > 0.001f) { x += d.x / m * 30f * dt; y += d.y / m * 30f * dt; }
            }
        }

        public void TakeHit(int dmg, Vector2 knockVel, bool crit, bool finisher)
        {
            if (IsDying) return;
            hp -= dmg;
            flash = HitFlashMs;
            if (knockVel != Vector2.zero) { knock = knockVel; knockLeft = KnockbackMs; }
            if (state == State.Idle) state = State.Chase;
            Color c = crit ? new Color(1f, 0.85f, 0.2f) : finisher ? new Color(1f, 0.69f, 0.23f) : Color.white;
            DamageNumber.Spawn(x, y + def.BodyHeightUnits + 4f, dmg.ToString(), c, crit || finisher);
            if (hp <= 0f)
            {
                hp = 0f;
                state = State.Dying;
                deathTimer = 0f;
                Kills++;
            }
        }

        /// <summary>Attack frame index (0..3) like the web renderer, or -1 when not striking.</summary>
        int AttackIndex()
        {
            if (state == State.Windup) return timer > def.WindupMs * 0.5f ? 0 : 1;
            if (state == State.Cooldown)
            {
                float elapsed = def.CooldownMs - timer;
                if (elapsed >= 0f && elapsed < RecoveryMs) return 2 + Mathf.FloorToInt(elapsed / (RecoveryMs * 0.5f));
            }
            return -1;
        }

        void Render()
        {
            int order = GameConfig.OrderBase(y);
            float alpha = 1f;
            if (state == State.Dying)
            {
                alpha = 1f - Mathf.Clamp01(deathTimer / FadeMs);
                if (alpha <= 0f) { body.enabled = false; shadow.enabled = false; barBack.enabled = false; barFill.enabled = false; return; }
            }
            Sprite[] attackRow = SpriteSheet.Row(attack, facing);
            Sprite[] walkRow = SpriteSheet.Row(walk, facing);
            int ai = AttackIndex();
            Sprite s = null;
            if (flash <= 0f && ai >= 0 && attackRow != null && ai < attackRow.Length) s = attackRow[ai];
            else if (walkRow != null && walkRow.Length > 0)
            {
                bool still = state == State.Idle || state == State.Cooldown;
                s = walkRow[still ? 0 : Mathf.FloorToInt(animTimer / WalkFrameMs) % walkRow.Length];
            }
            body.sprite = s;
            body.color = flash > 0f ? new Color(1f, 0.55f, 0.55f, alpha) : new Color(1f, 1f, 1f, alpha);
            body.sortingOrder = order + 3;
            shadow.sortingOrder = order;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);

            bool showBar = state != State.Dying && hp < def.Hp;
            barBack.enabled = showBar; barFill.enabled = showBar;
            if (showBar)
            {
                float top = def.BodyHeightUnits + 2f;
                barBack.transform.localPosition = new Vector3(-7f, top, 0f);
                barBack.transform.localScale = new Vector3(14f, 2f, 1f);
                barFill.transform.localPosition = new Vector3(-7f, top, 0f);
                barFill.transform.localScale = new Vector3(14f * hp / def.Hp, 1.4f, 1f);
                barBack.sortingOrder = order + 6;
                barFill.sortingOrder = order + 7;
            }
        }
    }
}
