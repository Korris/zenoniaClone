using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Red-haired warrior: movement (8-dir input, 4-dir facing), 4-hit combo, dash, hurt / death / respawn.
    /// Everything advances with GameClock.Dt in Update (vsync-paced, camera follows in LateUpdate).
    /// World units == web world pixels; y points up (web y points down).
    /// </summary>
    public class WarriorController : MonoBehaviour
    {
        public static WarriorController Instance { get; private set; }

        public const float MaxHp = 100f, Atk = 12f, Def = 4f, Speed = 80f;
        const float DashMs = 165f, DashSpeed = 220f, DashCooldownMs = 600f;
        const float HurtMs = 220f, InvulnMs = 500f, RespawnMs = 1400f;
        const float HpRegenPerSecond = 0.6f;
        const float CritChance = 0.08f;

        // Direction index == sheet row: 0 down, 1 up, 2 left, 3 right.
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        static readonly float[] DirAngle = { -90f, 90f, 180f, 0f };

        readonly ComboSystem combo = new ComboSystem(ComboSystem.WarriorHits);
        readonly List<ComboEvent> events = new List<ComboEvent>(4);
        WarriorRenderer view;
        float x, y, hp = MaxHp;
        int facing;
        float invuln, hurt, dashLeft, dashCooldown, animTimer, idleTimer, deathTimer;
        Vector2 dashDir;
        bool dead, moving;
        string gearLabel = "";

        public float Hp { get { return hp; } }
        public Vector2 Position { get { return new Vector2(x, y); } }
        public bool IsDead { get { return dead; } }
        public string GearLabel { get { return gearLabel; } }

        public void Init(WarriorRenderer renderer)
        {
            Instance = this;
            view = renderer;
            x = GameConfig.HeroStart.x;
            y = GameConfig.HeroStart.y;
            transform.position = new Vector3(x, y, 0f);
            UpdateGearLabel();
            Render();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            float dt = GameClock.Dt;
            float dtMs = dt * 1000f;
            HandleGearKeys();
            if (dead)
            {
                deathTimer += dtMs;
                if (deathTimer >= RespawnMs) Respawn();
            }
            else
            {
                Simulate(dt, dtMs);
            }
            transform.position = new Vector3(x, y, 0f);
            Render();
        }

        void Simulate(float dt, float dtMs)
        {
            invuln = Mathf.Max(0f, invuln - dtMs);
            hurt = Mathf.Max(0f, hurt - dtMs);
            dashCooldown = Mathf.Max(0f, dashCooldown - dtMs);
            idleTimer += dtMs;
            hp = Mathf.Min(MaxHp, hp + HpRegenPerSecond * dt);

            Vector2 input = ReadMove();
            bool dashing = dashLeft > 0f;
            if (!combo.IsAttacking && !dashing && input != Vector2.zero) UpdateFacing(input);

            if (!dashing && InputAdapter.Pressed(GameKey.Attack)) combo.PressAttack();
            if (!dashing && !combo.IsAttacking && dashCooldown <= 0f && InputAdapter.Pressed(GameKey.Dash))
            {
                dashDir = input != Vector2.zero ? input.normalized : DirVec[facing];
                dashLeft = DashMs;
                dashCooldown = DashCooldownMs;
                animTimer = 0f;
                dashing = true;
            }

            events.Clear();
            combo.Update(dtMs, events);
            for (int i = 0; i < events.Count; i++) if (events[i].Type == ComboEventType.Hit) DoHit(events[i].Step);

            Vector2 vel = Vector2.zero;
            if (dashing)
            {
                vel = dashDir * DashSpeed;
                dashLeft -= dtMs;
                animTimer += dtMs;
            }
            else if (combo.IsAttacking)
            {
                ComboHit hit = combo.CurrentHit;
                // Lunge forward during windup + active only (web stepPx).
                if (hit != null && combo.Phase != ComboPhase.Recover) vel = DirVec[facing] * (hit.StepPx / ((hit.WindupMs + hit.ActiveMs) / 1000f));
            }
            else
            {
                vel = input * Speed;
            }
            moving = !combo.IsAttacking && vel != Vector2.zero;
            if (moving && !dashing) animTimer += dtMs;
            else if (!dashing) animTimer = 0f;

            x = Mathf.Clamp(x + vel.x * dt, 10f, GameConfig.MapW - 10f);
            y = Mathf.Clamp(y + vel.y * dt, 6f, GameConfig.MapH - 6f);
        }

        static Vector2 ReadMove()
        {
            float ix = (InputAdapter.Held(GameKey.Right) ? 1f : 0f) - (InputAdapter.Held(GameKey.Left) ? 1f : 0f);
            float iy = (InputAdapter.Held(GameKey.Up) ? 1f : 0f) - (InputAdapter.Held(GameKey.Down) ? 1f : 0f);
            var v = new Vector2(ix, iy);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        void UpdateFacing(Vector2 m)
        {
            bool h = m.x != 0f, v = m.y != 0f;
            int hf = m.x > 0f ? 3 : 2, vf = m.y > 0f ? 1 : 0;
            if (h && !v) facing = hf;
            else if (v && !h) facing = vf;
            else if (h && v && facing != hf && facing != vf) facing = hf; // diagonal: keep facing if it matches a pressed axis
        }

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
            SlashEffect.Spawn(cx, cy, DirAngle[facing], h.FxScale, finisher, step == 2, GameConfig.OrderBase(y) + 7);

            float mult = combo.MultiplierFor(step, 1);
            int landed = 0;
            IReadOnlyList<Monster> monsters = Monster.All;
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                if (m.IsDying || !box.Overlaps(m.Box)) continue;
                bool crit = Random.value < CritChance;
                int dmg = CombatMath.Damage(Atk, mult * (crit ? 1.6f : 1f), m.Def);
                m.TakeHit(dmg, v * h.Knockback * (crit ? 2f : 1f), crit, finisher);
                landed++;
            }
            if (landed > 0)
            {
                GameClock.HitStop(finisher ? 60f : 25f);
                if (finisher) CameraFollow.Shake(2.5f, 140f);
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
            CameraFollow.Shake(1.5f, 100f);
            if (hp <= 0f)
            {
                hp = 0f;
                dead = true;
                deathTimer = 0f;
                dashLeft = 0f;
                combo.Reset();
            }
        }

        void Respawn()
        {
            dead = false;
            hp = MaxHp;
            x = GameConfig.HeroStart.x;
            y = GameConfig.HeroStart.y;
            facing = 0;
            invuln = 1500f;
            hurt = 0f;
            dashLeft = 0f;
            combo.Reset();
        }

        void HandleGearKeys()
        {
            bool changed = false;
            int tier = view.Tier;
            bool enh = view.Enhanced;
            if (InputAdapter.Pressed(GameKey.TierUp)) { tier++; changed = true; }
            if (InputAdapter.Pressed(GameKey.TierDown)) { tier--; changed = true; }
            if (InputAdapter.Pressed(GameKey.Enhance)) { enh = !enh; changed = true; }
            if (!changed) return;
            view.SetGear(tier, enh);
            UpdateGearLabel();
        }

        void UpdateGearLabel()
        {
            gearLabel = "Gear: " + view.TierName + (view.Enhanced ? " (enhanced)" : "") + "   [Q/E tier, R enhance]";
        }

        static int ByProgress(int n, float p) { return Mathf.Min(n - 1, Mathf.FloorToInt(Mathf.Clamp01(p) * n)); }

        void Render()
        {
            string anim;
            int frame;
            if (dead)
            {
                anim = "death";
                int n = Mathf.Max(1, view.FrameCount(anim, facing));
                frame = ByProgress(n, deathTimer / (120f * n));
            }
            else if (hurt > 0f)
            {
                anim = "hurt";
                frame = ByProgress(Mathf.Max(1, view.FrameCount(anim, facing)), 1f - hurt / HurtMs);
            }
            else if (dashLeft > 0f)
            {
                anim = "dash";
                frame = Mathf.FloorToInt(animTimer / 55f) % Mathf.Max(1, view.FrameCount(anim, facing));
            }
            else if (combo.IsAttacking)
            {
                anim = combo.IsFinisher ? "attack3" : (((combo.Step - 1) % 2) + 1 == 1 ? "attack1" : "attack2");
                frame = ByProgress(Mathf.Max(1, view.FrameCount(anim, facing)), combo.PhaseProgress);
            }
            else if (moving)
            {
                anim = "walk";
                int n = Mathf.Max(1, view.FrameCount(anim, facing));
                frame = Mathf.FloorToInt(animTimer / (560f / n)) % n; // one full step cycle = 560 ms regardless of frame count
            }
            else
            {
                anim = "idle";
                frame = Mathf.FloorToInt(idleTimer / 200f) % Mathf.Max(1, view.FrameCount(anim, facing));
            }
            view.Show(anim, facing, frame, GameConfig.OrderBase(y), 1f);
        }
    }
}
