using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ranged combo shot (web fireComboBolt + projectile-hit-system): wizard magic bolts / arcane orb and mechanic bullets / plasma
    /// cannon. Hits the first monster it touches; a bolt with ExplodeRadius bursts and damages everything inside the radius.
    /// Game feel: hit-stop, camera trauma, impact sparks + white flash (via Monster.TakeHit), additive glow copy, blast FX.
    /// </summary>
    public class ComboProjectile : MonoBehaviour
    {
        const float CritChance = 0.08f, StepUnits = 4f, VisualLift = 6f;
        static readonly List<ComboProjectile> Pool = new List<ComboProjectile>();
        static Transform root;

        BoltDef def;
        Vector2 pos, dir;
        string fxFolder;
        float atk, mult, knockback, life;
        bool finisher;
        SpriteRenderer sr, glow;

        public static void Init(Transform parent) { root = parent; Pool.Clear(); }

        public static void Fire(Vector2 pos, Vector2 dir, float angleDeg, BoltDef bolt, float atk, float mult, float knockback, bool finisher, string fxFolder)
        {
            Sprite art = BurstFx.Art(fxFolder, bolt.Sprite);
            if (art == null || root == null) return;
            ComboProjectile p = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { p = Pool[i]; break; }
            if (p == null)
            {
                var go = new GameObject("Bolt");
                go.transform.SetParent(root, false);
                p = go.AddComponent<ComboProjectile>();
                p.sr = go.AddComponent<SpriteRenderer>();
                p.glow = ProceduralSprites.Child(go.transform, "Glow", null);
                p.glow.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(p);
            }
            p.def = bolt; p.pos = pos; p.dir = dir; p.atk = atk; p.mult = mult; p.knockback = knockback; p.finisher = finisher; p.fxFolder = fxFolder;
            p.life = bolt.Range / bolt.Speed;
            p.sr.sprite = art; p.glow.sprite = art;
            p.sr.sortingOrder = FeelConfig.FxOrder - 10;
            p.glow.sortingOrder = FeelConfig.FxOrder - 9;
            p.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg); // art is painted flying to screen-right
            p.transform.localScale = new Vector3(bolt.Scale, bolt.Scale, 1f);
            p.transform.position = new Vector3(pos.x, pos.y + VisualLift, 0f);
            p.gameObject.SetActive(true);
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
            transform.position = new Vector3(pos.x, pos.y + VisualLift, 0f);
            glow.enabled = FeelConfig.FxOn;
            glow.color = new Color(1f, 0.95f, 0.85f, 0.75f);
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

        /// <summary>One crit-rolled hit; the damage roll happens on impact like the web.</summary>
        void Strike(Monster m, Vector2 knock, bool blast)
        {
            bool crit = Random.value < CritChance;
            int dmg = CombatMath.Damage(atk, mult * (crit ? 1.6f : 1f), m.Def);
            m.TakeHit(dmg, knock * (crit ? 2f : 1f), crit, blast);
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
            if (def.ExplodeFx != null)
                BurstFx.Spawn(fxFolder, def.ExplodeFx, pos + new Vector2(0f, VisualLift), Random.value * 360f, 2f * def.ExplodeRadius / 40f, 0.32f, true); // burst art spans 40 units
            CameraFollow.AddTrauma(FeelConfig.TraumaBlast);
        }
    }
}
