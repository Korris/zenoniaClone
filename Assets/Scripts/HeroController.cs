using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Playable hero (warrior / wizard / mechanic): movement on the Frost Hollow collision grid (8-dir input, 4-dir facing),
    /// class combo (melee swing or ranged bolts), dash, hurt / death / respawn at the entrance.
    /// Everything advances with GameClock.Dt in Update (vsync-paced, camera follows in LateUpdate).
    /// World units == web world pixels; y points up (web y points down).
    /// </summary>
    public partial class HeroController : MonoBehaviour
    {
        public static HeroController Instance { get; private set; }
        /// <summary>Screenshot tooling only (--god): ignore all incoming damage.</summary>
        public static bool DebugGod;

        const float DashMs = 165f, DashSpeed = 220f, DashCooldownMs = 600f;
        const float HurtMs = 220f, InvulnMs = 500f, RespawnMs = 1400f;
        const float HpRegenPerSecond = 0.6f;
        const float CritChance = 0.08f;
        const float BoxW = 10f, BoxH = 8f;       // web HERO_HITBOX, feet-anchored

        // Direction index == sheet row: 0 down, 1 up, 2 left, 3 right.
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        static readonly float[] DirAngle = { -90f, 90f, 180f, 0f };

        ComboSystem combo;
        readonly List<ComboEvent> events = new List<ComboEvent>(4);
        HeroRenderer view;
        HeroClassDef cls;
        float x, y, hp;
        int facing;
        float invuln, hurt, dashLeft, dashCooldown, animTimer, idleTimer, deathTimer;
        Vector2 dashDir;
        bool dead, moving;
        string gearLabel = "";

        public HeroClassDef Class { get { return cls; } }
        public HeroRenderer View { get { return view; } }
        public float Hp { get { return hp; } }
        public float MaxHp { get { return cls.MaxHp; } }
        public float DefStat { get { return cls.Def; } }
        public Vector2 Position { get { return new Vector2(x, y); } }
        public int Facing { get { return facing; } }
        public bool IsDead { get { return dead; } }
        public string GearLabel { get { return gearLabel; } }

        public void Init(HeroRenderer renderer)
        {
            Instance = this;
            view = renderer;
            cls = renderer.Class;
            combo = new ComboSystem(cls.Hits, cls.ChainMs);
            hp = cls.MaxHp;
            ResetSkillState();
            x = GameConfig.HeroStart.x;
            y = GameConfig.HeroStart.y;
            transform.position = new Vector3(x, y, 0f);
            UpdateGearLabel();
            Render();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Debug / screenshot helper: put the hero on a tile (feet) and face a direction.</summary>
        public void Teleport(Vector2 feet, int dir) { x = feet.x; y = feet.y; facing = dir; transform.position = new Vector3(x, y, 0f); }

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
            hp = Mathf.Min(cls.MaxHp, hp + HpRegenPerSecond * dt);
            if (TickSkill(dt, dtMs)) return; // rooted by a skill cast

            Vector2 input = ReadMove();
            bool dashing = dashLeft > 0f;
            if (!combo.IsAttacking && !dashing && input != Vector2.zero) UpdateFacing(input);

            if (!dashing && InputAdapter.Pressed(GameKey.Skill) && TryCastSkill()) return;
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
                // Lunge forward during windup + active only (web stepPx; 0 for the ranged classes).
                if (hit != null && hit.StepPx > 0f && combo.Phase != ComboPhase.Recover) vel = DirVec[facing] * (hit.StepPx / ((hit.WindupMs + hit.ActiveMs) / 1000f));
            }
            else
            {
                vel = input * cls.Speed;
            }
            moving = !combo.IsAttacking && vel != Vector2.zero;
            if (moving && !dashing) animTimer += dtMs;
            else if (!dashing) animTimer = 0f;

            if (vel == Vector2.zero) return;
            bool bx, by;
            Vector2 p = FrostHollowMap.Move(new Vector2(x, y), vel.x * dt, vel.y * dt, BoxW, BoxH, out bx, out by);
            x = p.x; y = p.y;
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

        void Respawn()
        {
            dead = false;
            hp = cls.MaxHp;
            ResetSkillState();
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
            string what = cls.Id == HeroClass.Warrior ? "Sword + shield" : cls.Id == HeroClass.Wizard ? "Staff" : "Gun + robot";
            gearLabel = what + ": " + view.TierName + (view.Enhanced ? " (enhanced)" : "") + "   [Q/E tier, R enhance]";
        }
    }
}
