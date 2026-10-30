using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ranged combo shot (web fireComboBolt + projectile-hit-system): wizard magic bolts / arcane orb and mechanic bullets / plasma
    /// cannon. Hits the first monster it touches; a bolt with ExplodeRadius bursts and damages everything inside the radius.
    /// Game feel: hit-stop, camera trauma, impact sparks + white flash (via Monster.TakeHit), additive glow copy, blast FX.
    /// </summary>
    public partial class ComboProjectile : MonoBehaviour
    {
        const float CritChance = 0.08f, StepUnits = 4f, VisualLift = 6f;
        static readonly List<ComboProjectile> Pool = new List<ComboProjectile>();
        static Transform root;

        BoltDef def;
        Vector2 pos, dir;
        string fxFolder;
        ClassPalette pal;
        Color? tint { get { return pal.Tint; } } // palette hue for the painted-art styles
        float atk, mult, knockback, life, critBonus;
        bool finisher;
        HeroController src;     // who fired it (kill credit, damage table)
        SpriteRenderer sr, glow;
        ArcaneBoltRig arcane;   // wizard layered missile (null until first needed)
        StreakBoltRig streak;   // cleric light arrow / mechanic plasma bolt
        VfxSet vfx;             // painted projectile sheet (null = procedural rig / sprite fallback)
        float animT;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); }

        /// <summary>Launches a bolt. On a headless server it is a plain moving hitbox (no renderers); `src` is credited for every hit.</summary>
        public static void Fire(Vector2 pos, Vector2 dir, float angleDeg, BoltDef bolt, float atk, float mult, float knockback, bool finisher, string fxFolder, ClassPalette palette, float critBonus = 0f, HeroController src = null)
        {
            bool fx = NetMode.RendersFx;
            Sprite art = fx && bolt.Style == BoltStyle.Sprite ? BurstFx.Art(fxFolder, bolt.Sprite, palette.Tint) : null;
            if (root == null || (fx && art == null && bolt.Style == BoltStyle.Sprite)) return;
            ComboProjectile p = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { p = Pool[i]; break; }
            if (p == null)
            {
                var go = new GameObject("Bolt");
                go.transform.SetParent(root, false);
                p = go.AddComponent<ComboProjectile>();
                if (fx)
                {
                    p.sr = go.AddComponent<SpriteRenderer>();
                    p.glow = ProceduralSprites.Child(go.transform, "Glow", null);
                    p.glow.sharedMaterial = FeelMaterials.Additive;
                }
                Pool.Add(p);
            }
            p.def = bolt; p.pos = pos; p.dir = dir; p.atk = atk; p.mult = mult; p.knockback = knockback; p.finisher = finisher; p.fxFolder = fxFolder; p.pal = palette; p.critBonus = critBonus; p.src = src;
            p.life = bolt.Range / bolt.Speed;
            if (!fx) { p.vfx = null; p.gameObject.SetActive(true); return; } // headless: hitbox only
            p.sr.sprite = art; p.glow.sprite = art;
            p.sr.enabled = p.glow.enabled = bolt.Style == BoltStyle.Sprite;
            if (p.arcane != null) p.arcane.End();
            if (p.streak != null) p.streak.End();
            p.vfx = bolt.Style == BoltStyle.Sprite ? null : VfxSprites.For(bolt.Style);
            if (p.vfx != null) { p.BeginPainted(angleDeg, finisher); return; }
            if (bolt.Style != BoltStyle.Sprite)
            {
                p.transform.localScale = Vector3.one; // the rigs size themselves
                p.transform.position = Vector3.zero;
                p.transform.rotation = Quaternion.identity;
                p.gameObject.SetActive(true);
                var at = new Vector2(pos.x, pos.y + VisualLift);
                if (bolt.Style == BoltStyle.Arcane)
                {
                    if (p.arcane == null) p.arcane = new ArcaneBoltRig(p.transform);
                    p.arcane.Begin(at, angleDeg, bolt.Scale, palette);
                }
                else
                {
                    if (p.streak == null) p.streak = new StreakBoltRig(p.transform);
                    p.streak.Begin(at, angleDeg, bolt.ExplodeRadius > 0f ? 1.25f : 1f, bolt.Style, palette);
                }
                return;
            }
            p.sr.sortingOrder = FeelConfig.FxOrder - 10;
            p.glow.sortingOrder = FeelConfig.FxOrder - 9;
            p.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg); // art is painted flying to screen-right
            p.transform.localScale = new Vector3(bolt.Scale, bolt.Scale, 1f);
            p.transform.position = new Vector3(pos.x, pos.y + VisualLift, 0f);
            p.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (arcane != null) arcane.End();
            if (streak != null) streak.End();
        }

        void Update()
        {
            float dt = GameClock.Dt;
            if (dt > 0f)
            {
                float dist = def.Speed * dt;
                int steps = Mathf.Max(1, Mathf.CeilToInt(dist / StepUnits)); // sub-step so a fast bullet cannot tunnel through a monster
                for (int i = 0; i < steps; i++)
                {
                    pos += dir * (dist / steps);
                    if (HitSomething()) { gameObject.SetActive(false); return; }
                }
                life -= dt;
                if (life <= 0f) { gameObject.SetActive(false); return; }
            }
            if (!NetMode.RendersFx) return;
            if (vfx != null)
            {
                transform.position = new Vector3(pos.x, pos.y + VisualLift, 0f);
                Animate();
                return;
            }
            if (def.Style != BoltStyle.Sprite)
            {
                var at = new Vector2(pos.x, pos.y + VisualLift);
                if (def.Style == BoltStyle.Arcane) arcane.Tick(at, dt); else streak.Tick(at, dt);
                return;
            }
            transform.position = new Vector3(pos.x, pos.y + VisualLift, 0f);
            glow.enabled = FeelConfig.FxOn;
            glow.color = tint.HasValue ? new Color(tint.Value.r, tint.Value.g, tint.Value.b, 0.5f) : new Color(1f, 0.95f, 0.85f, 0.75f);
        }

        bool HitSomething()
        {
            var box = new Rect(pos.x - def.W * 0.5f, pos.y - def.H * 0.5f, def.W, def.H);
            IReadOnlyList<Monster> monsters = Monster.All;
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                if (m.IsDying || !box.Overlaps(m.Box)) continue;
                if (def.ExplodeRadius > 0f) Explode(); else Strike(m, dir * knockback, false);
                return true;
            }
            return false;
        }

        static readonly string[] PlasmaBlastSfx = { "mechanic/explosion-a", "mechanic/explosion-b" };

        /// <summary>Impact visuals per style: wizard starburst + crystal shards, cleric gold pillar, mechanic starburst + sparks + dust.</summary>
        void HitFx(bool blast)
        {
            if (!NetMode.RendersFx) return;
            var at = pos + new Vector2(0f, VisualLift);
            if (def.Style == BoltStyle.Plasma) // mechanic: plasma zap on a bullet hit, explosion for the cannon blast
            {
                if (blast) GameSfx.PlayAny(PlasmaBlastSfx); else GameSfx.Play("mechanic/hit-a", 0.7f, 0.08f);
            }
            if (vfx != null) { PaintedHitFx(at, blast); return; }
            switch (def.Style)
            {
                case BoltStyle.Arcane: if (blast) RuneFx.FinisherBurst(at, def.ExplodeRadius, pal); else RuneFx.Impact(at, pal); break;
                case BoltStyle.HolyArrow: HolySmite.Impact(pos, blast); break;
                case BoltStyle.Plasma: MechanicFx.Impact(at, blast, pal); break;
            }
        }

        /// <summary>One crit-rolled hit; the damage roll happens on impact like the web.</summary>
        void Strike(Monster m, Vector2 knock, bool blast)
        {
            bool crit = Random.value < CritChance + critBonus;
            int dmg = CombatMath.Damage(atk, mult * (crit ? 1.6f : 1f), m.Def);
            m.TakeHit(dmg, knock * (crit ? 2f : 1f), crit, blast, vfx != null, src);
            if (!blast) HitFx(false);
            GameClock.HitStop(FeelConfig.BoltHitStopMs(blast));
            if (!blast) CameraFollow.AddTrauma(crit ? FeelConfig.TraumaCrit : FeelConfig.TraumaBolt); // a blast shakes once in Explode
        }

        /// <summary>Burst at the projectile position: every live monster within the radius is hit once.</summary>
        void Explode()
        {
            IReadOnlyList<Monster> monsters = Monster.All;
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                if (m.IsDying) continue;
                Vector2 d = m.Center - pos;
                float dist = d.magnitude;
                if (dist > def.ExplodeRadius) continue;
                Vector2 away = dist > 0.01f ? d / dist : dir;
                Strike(m, away * knockback, true);
            }
            HitFx(true);
            if (vfx == null && def.ExplodeFx != null && (def.Style == BoltStyle.Sprite || def.Style == BoltStyle.Arcane))
                BurstFx.Spawn(fxFolder, def.ExplodeFx, pos + new Vector2(0f, VisualLift), Random.value * 360f, 2f * def.ExplodeRadius / 40f, 0.32f, true, tint); // burst art spans 40 units
            CameraFollow.AddTrauma(FeelConfig.TraumaBlast);
        }
    }
}
