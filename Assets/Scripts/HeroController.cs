using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Playable hero (warrior / wizard / mechanic): movement on the Frost Hollow collision grid (8-dir input, 4-dir facing),
    /// class combo (melee swing or ranged bolts), dash, hurt / death / respawn at the entrance.
    /// HP / MP and the combat stats live in the RPG PlayerState (level + equipment); RpgSession owns it and saves it.
    /// Everything advances with GameClock.Dt in Update (vsync-paced, camera follows in LateUpdate).
    /// World units == web world pixels; y points up (web y points down).
    /// </summary>
    public partial class HeroController : MonoBehaviour
    {
        /// <summary>The hero this process plays (offline hero, host / client own hero), or null on a dedicated server. Lives in LocalPlayer.</summary>
        public static HeroController Instance { get { return LocalPlayer.Hero; } }
        /// <summary>Scripted input: debug party members and network-driven heroes. Null = the keyboard / touch controls.</summary>
        public System.Func<GameKey, bool> AiHeld, AiPressed;
        bool Held(GameKey k) { return AiHeld != null ? AiHeld(k) : InputAdapter.Held(k); }
        bool Pressed(GameKey k) { return AiPressed != null ? AiPressed(k) : InputAdapter.Pressed(k); }
        /// <summary>Screenshot tooling only (--god): this hero ignores all incoming damage.</summary>
        public bool God;
        /// <summary>Per-zone entity id (assigned when the hero joins a Zone; 0 = not in a zone).</summary>
        public ushort NetId;
        /// <summary>RPG progress owner (EXP, pickups, potions, death penalty); null for debug party members.</summary>
        public PlayerRpg Owner;

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
        HeroRenderer view;                       // null on a headless server: no sprites, Render is a no-op
        HeroClassDef cls;
        const float StabGapMs = 90f;
        float stabLeft;
        ComboHit stabHit;
        int stabStep;
        float x, y;
        PlayerState rpg;
        float hp { get { return rpg.Hp; } set { rpg.Hp = value; } }
        int facing;
        float invuln, hurt, dashLeft, dashCooldown, animTimer, idleTimer, deathTimer;
        Vector2 dashDir;
        bool dead, moving;
        string gearLabel = "";

        public HeroClassDef Class { get { return cls; } }
        public HeroRenderer View { get { return view; } }
        public float Hp { get { return hp; } }
        public float MaxHp { get { return rpg.MaxHp; } }
        public float AtkStat { get { return rpg.Atk; } }
        public float DefStat { get { return rpg.Def; } }
        public float CritBonus { get { return rpg.CritBonus; } }
        public PlayerState Rpg { get { return rpg; } }
        public Vector2 Position { get { return new Vector2(x, y); } }
        public int Facing { get { return facing; } }
        public bool IsDead { get { return dead; } }
        public string GearLabel { get { return gearLabel; } }

        /// <summary>party = an extra AI-driven hero: it never becomes the LocalPlayer (the HUD, camera and touch controls keep following the player).</summary>
        public void Init(HeroRenderer renderer, PlayerState state, bool party = false)
        {
            if (!party) LocalPlayer.Set(this);
            view = renderer;
            Setup(renderer.Class, state);
            RefreshGearLabel();
            Render();
        }

        /// <summary>Dedicated-server hero: same simulation, no HeroRenderer, no sprites; Render and the gear label are skipped.</summary>
        public void InitHeadless(HeroClassDef def, PlayerState state)
        {
            view = null;
            Setup(def, state);
        }

        void Setup(HeroClassDef def, PlayerState state, ushort serverId = 0)
        {
            cls = def;
            rpg = state ?? PlayerState.NewGame(RpgSession.ClassId(cls.Id));
            combo = new ComboSystem(cls.Hits, cls.ChainMs);
            ResetSkillState();
            x = GameConfig.HeroStart.x;
            y = GameConfig.HeroStart.y;
            transform.position = new Vector3(x, y, 0f);
            if (Zone.Main != null) Zone.Main.Heroes.Add(this, serverId);
        }

        /// <summary>Takes this hero out of play right away (class switch): leaves the zone and drops its RPG owner so nothing can still target or pay it.</summary>
        public void Retire()
        {
            Owner = null;
            LocalPlayer.Clear(this);
            if (Zone.Main != null) Zone.Main.Heroes.Remove(this);
        }

        void OnDestroy()
        {
            LocalPlayer.Clear(this);
            if (Zone.Main != null) Zone.Main.Heroes.Remove(this);
        }

        /// <summary>Debug / screenshot helper: put the hero on a tile (feet) and face a direction.</summary>
        public void Teleport(Vector2 feet, int dir) { x = feet.x; y = feet.y; facing = dir; transform.position = new Vector3(x, y, 0f); }

        void Update()
        {
            float dt = GameClock.Dt;
            float dtMs = dt * 1000f;
            if (proxy) { ProxyTick(dtMs); return; }
            if (GameClock.Paused) { Render(); return; } // inventory open: frozen, no input
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
            if (TickSkill(dt, dtMs)) return; // rooted by a skill cast

            Vector2 input = ReadMove();
            bool dashing = dashLeft > 0f;
            if (!combo.IsAttacking && !dashing && input != Vector2.zero) UpdateFacing(input);

            if (!dashing && Pressed(GameKey.Skill) && TryCastSkill()) return;
            if (!dashing && Pressed(GameKey.Attack)) combo.PressAttack();
            if (!dashing && !combo.IsAttacking && dashCooldown <= 0f && Pressed(GameKey.Dash))
            {
                dashDir = input != Vector2.zero ? input.normalized : DirVec[facing];
                dashLeft = DashMs;
                dashCooldown = DashCooldownMs;
                animTimer = 0f;
                dashing = true;
            }

            UpdateRangedCastFx();
            events.Clear();
            combo.Update(dtMs, events);
            for (int i = 0; i < events.Count; i++) if (events[i].Type == ComboEventType.Hit) DoHit(events[i].Step);
            if (stabLeft > 0f && (stabLeft -= dtMs) <= 0f) MeleeStrike(stabHit, stabStep, true);

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
                vel = input * rpg.Speed;
            }
            moving = !combo.IsAttacking && vel != Vector2.zero;
            if (moving && !dashing) animTimer += dtMs;
            else if (!dashing) animTimer = 0f;

            if (vel == Vector2.zero) return;
            bool bx, by;
            Vector2 p = FrostHollowMap.Move(new Vector2(x, y), vel.x * dt, vel.y * dt, BoxW, BoxH, out bx, out by);
            x = p.x; y = p.y;
        }

        Vector2 ReadMove()
        {
            float ix = (Held(GameKey.Right) ? 1f : 0f) - (Held(GameKey.Left) ? 1f : 0f);
            float iy = (Held(GameKey.Up) ? 1f : 0f) - (Held(GameKey.Down) ? 1f : 0f);
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
            ResetSkillState();
            x = GameConfig.HeroStart.x;
            y = GameConfig.HeroStart.y;
            facing = 0;
            invuln = 1500f;
            hurt = 0f;
            dashLeft = 0f;
            combo.Reset();
            // -10% of the current-level EXP, full HP / MP. Last, so the revive event (toast + save) sees the respawn position.
            if (Owner != null) Owner.Revive(); else rpg.Revive();
            NetEventBus.HeroRespawned(this);
        }

        void HandleGearKeys()
        {
            if (view == null || AiPressed != null) return; // headless / scripted heroes ignore the debug look keys
            bool changed = false;
            int tier = view.Tier;
            bool enh = view.Enhanced;
            if (InputAdapter.Pressed(GameKey.TierUp)) { tier++; changed = true; }
            if (InputAdapter.Pressed(GameKey.TierDown)) { tier--; changed = true; }
            if (InputAdapter.Pressed(GameKey.Enhance)) { enh = !enh; changed = true; }
            if (cls.Id == HeroClass.Mechanic && InputAdapter.Pressed(GameKey.FxColor)) { ClassPalette.MechanicBlue = !ClassPalette.MechanicBlue; ClassPalette.Current = cls.ActivePalette; }
            if (!changed) return;
            view.SetGear(tier, enh); // debug look override until the equipment changes again
            RefreshGearLabel();
        }

        /// <summary>HUD gear line: the equipped weapon (+ offhand) names, or the debug look picked with Q / E / R.</summary>
        public void RefreshGearLabel()
        {
            if (view == null) return;
            if (view.DebugLook)
            {
                gearLabel = "Debug look: " + view.TierName + (view.Enhanced ? " (enhanced)" : "") + "   [Q/E tier, R enhance]";
                return;
            }
            Item w = rpg.GetEquipped(Slots.Weapon), o = rpg.GetEquipped(Slots.Offhand);
            gearLabel = (w != null ? EnhanceStats.DisplayName(w) : "No weapon") + (o != null ? " + " + EnhanceStats.DisplayName(o) : "") + "   [B] inventory   [Q/E/R] debug look";
        }
    }
}
