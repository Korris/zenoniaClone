using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Pictures of a Pet Companion attack or skill (RendersFx only), art from ChatGPT (Resources/Sprites/fx/pets/, Tools/build-pet-art.py fx). Play() is the entry for
    /// the server's PetSkill event: Shield / Heal show a badge over the owner with a thin ring at the feet; attacks hand over to HeroPet.Attack, and the pet calls Strike()
    /// when its swing starts: a ranged pet throws its element's projectile (flies PetConfig.FlightMs, then the burst), a melee pet shows the burst (element art, or the shared
    /// sparks for a plain Strike) on its hit frame (PetConfig.HitFrameMs). `basic` draws everything at 60 %. Normal alpha blending: additive washes out on snow
    /// (sprite-rules Sổ lỗi 2026-11-16). The damage itself arrives as a MonsterHit with HitFlags.Pet.
    /// </summary>
    public sealed class PetSkillFx : MonoBehaviour
    {
        const float ShotUnits = 10f, HitUnits = 13f, BadgeUnits = 8f, Lift = 8f, HitMs = 260f, BadgeMs = 650f;
        static readonly Dictionary<string, Sprite> Art = new Dictionary<string, Sprite>();

        enum Phase { Fly, Wait, Hit, Badge }

        SpriteRenderer sr;
        Vector2 from, lastTarget;
        Monster target;
        HeroController owner;
        PetSkillKind kind;
        Phase phase;
        float ageMs, scale = 1f, waitMs;

        static Sprite Load(string name)
        {
            Sprite s;
            if (Art.TryGetValue(name, out s)) return s;
            var tex = Resources.Load<Texture2D>("Sprites/fx/pets/" + name);
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect) : null;
            if (s == null) Debug.LogWarning("[ZenoniaProto] no pet FX art Sprites/fx/pets/" + name);
            Art[name] = s;
            return s;
        }

        static string Element(PetSkillKind k)
        {
            switch (k)
            {
                case PetSkillKind.Ice: return "ice";
                case PetSkillKind.Poison: return "poison";
                case PetSkillKind.Lightning: return "lightning";
                case PetSkillKind.Shield: return "shield";
                case PetSkillKind.Heal: return "heal";
                default: return "fire";
            }
        }

        /// <summary>The server's event: a buff badge now, or the pet's attack cycle (HeroPet runs to the target and calls Strike when it swings).</summary>
        /// <summary>support = a support pet's skill: no body, the projectile leaves the owner at 80 %.</summary>
        public static void Play(HeroController owner, Monster target, PetSkillKind kind, bool basic = false, bool support = false)
        {
            if (!NetMode.RendersFx || owner == null || !FeelConfig.FxOn || !PetViewPrefs.Shows(owner)) return; // hidden pets show no skill pictures either
            if (PetConfig.IsAttack(kind)) { if (support) Strike(owner, target, kind, false, owner.Position + new Vector2(0f, 14f), false, 0.8f); else HeroPet.Attack(owner, target, kind, basic); return; }
            Color c = PetArt.SkillColor(kind);
            ShockRing.Spawn(owner.Position, 14f, 0.6f, new Color(c.r, c.g, c.b, 0.7f), FeelConfig.FxOrder - 20, 0.62f, 0.5f); // a thin buff ring at the feet
            PetSkillFx fx = Make(owner, null, kind, 1f);
            if (fx == null) return;
            fx.phase = Phase.Badge;
            fx.sr.sprite = Load(Element(kind));
            fx.Step(0f);
        }

        /// <summary>The pet's swing began at `from`: ranged = the projectile, melee = the burst on the hit frame.</summary>
        public static void Strike(HeroController owner, Monster target, PetSkillKind kind, bool basic, Vector2 from, bool melee, float size = 0f)
        {
            if (!NetMode.RendersFx || owner == null || target == null || !FeelConfig.FxOn || !PetViewPrefs.Shows(owner)) return;
            PetSkillFx fx = Make(owner, target, kind, size > 0f ? size : basic ? 0.6f : 1f);
            if (fx == null) return;
            fx.from = from;
            fx.lastTarget = target.Center + new Vector2(0f, Lift * 0.5f);
            if (melee) { fx.phase = Phase.Wait; fx.waitMs = PetConfig.HitFrameMs; }
            else { fx.phase = Phase.Fly; fx.sr.sprite = Load(Element(kind)); }
            fx.Step(0f);
        }

        static PetSkillFx Make(HeroController owner, Monster target, PetSkillKind kind, float scale)
        {
            var go = new GameObject("PetSkillFx");
            go.transform.SetParent(owner.transform.parent, false);
            PetSkillFx fx = go.AddComponent<PetSkillFx>();
            fx.owner = owner; fx.target = target; fx.kind = kind; fx.scale = scale;
            fx.sr = go.AddComponent<SpriteRenderer>();
            fx.sr.sortingOrder = FeelConfig.FxOrder - 14; // the combo bolts' band (ArcaneBoltRig): above the y-sorted world, under the hit FX
            return fx;
        }

        void Update() { Step(GameClock.Dt * 1000f); }

        void Step(float dtMs)
        {
            ageMs += dtMs;
            if (target != null && !target.IsDying) lastTarget = target.Center + new Vector2(0f, Lift * 0.5f);
            if (phase == Phase.Wait) { if (ageMs < waitMs) return; BeginHit(); }
            if (phase == Phase.Fly)
            {
                float t = Mathf.Clamp01(ageMs / PetConfig.FlightMs);
                Vector2 p = Vector2.Lerp(from, lastTarget, t) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 6f); // a small arc
                Vector2 d = lastTarget - from + new Vector2(0f, Mathf.Cos(t * Mathf.PI) * 6f * Mathf.PI);
                Place(p, ShotUnits * scale, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 1f);
                if (t < 1f) return;
                BeginHit();
            }
            if (phase == Phase.Hit)
            {
                float t = Mathf.Clamp01(ageMs / HitMs);
                Place(lastTarget, HitUnits * scale * (0.6f + 0.5f * Mathf.Sqrt(t)), 0f, 1f - t * t);
                if (t >= 1f) Destroy(gameObject);
                return;
            }
            float b = Mathf.Clamp01(ageMs / BadgeMs); // Badge: pops in over the owner's head, rises a little, fades
            Vector2 head = (owner != null ? owner.Position : from) + new Vector2(0f, 30f + b * 6f);
            float pop = b < 0.2f ? Mathf.Lerp(0.5f, 1.15f, b / 0.2f) : Mathf.Lerp(1.15f, 1f, (b - 0.2f) / 0.8f);
            Place(head, BadgeUnits * pop, 0f, b < 0.6f ? 1f : 1f - (b - 0.6f) / 0.4f);
            if (b >= 1f) Destroy(gameObject);
        }

        /// <summary>The hit lands: a plain Strike shows the shared sparks and ends, an element shows its burst.</summary>
        void BeginHit()
        {
            ageMs = 0f;
            if (target == null || kind == PetSkillKind.Strike)
            {
                if (target != null) ImpactFx.Spawn(lastTarget.x, lastTarget.y, (lastTarget - from).normalized, scale >= 1f, false, null, scale < 1f);
                Destroy(gameObject);
                phase = Phase.Hit; ageMs = HitMs; // nothing more to draw this frame
                return;
            }
            phase = Phase.Hit;
            sr.sprite = Load(Element(kind) + "-hit");
            sr.sortingOrder = FeelConfig.FxOrder + 1;
            if (sr.sprite == null) Destroy(gameObject);
        }

        void Place(Vector2 at, float units, float angleDeg, float alpha)
        {
            if (sr.sprite == null) return;
            transform.position = new Vector3(at.x, at.y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            float k = units / Mathf.Max(0.001f, Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y));
            transform.localScale = new Vector3(k, k, 1f);
            sr.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
