using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Wizard ultimate ICE STORM (web blizzard / IceStorm entity, rank 3: radius 44, 140% ATK, 4 waves). Timeline (ms, same as the web):
    /// magic circle cast 0-650, then 12 ice spears fall in 4 waves of 3 (impacts at 650 + 400 * wave, 560 ms fall), each wave damages
    /// the whole circle and chills the targets; the circle fades out ~850 ms after the last wave.
    /// Unity extras: big frozen circle at the target area, additive cyan streaks behind the spears, frost rings, ice-shard GPU particles,
    /// shake + hit-stop + a cyan screen tint per wave.
    /// </summary>
    public class IceStormUltimate : MonoBehaviour
    {
        public const float Radius = 44f, Mult = 1.4f, Range = 26f, CastMs = 650f, FallMs = 560f, WaveMs = 400f, TailMs = 850f, ChillMs = 2500f;
        const int Spears = 12, Waves = 4;
        const string Sheet = "Sprites/fx/wizard/ice-storm";
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        static readonly Color Cyan = new Color(0.45f, 0.85f, 1f);
        static readonly float[] Anchor = { 0.8f, 0.92f, 0.84f, 0.5f }; // web per-row anchor (fraction from the top of the 192px cell)
        static readonly Vector2 FallVec = new Vector2(-32f, 82f);      // spear start offset from its impact point (web, y flipped)

        sealed class Spear
        {
            public Vector2 Impact; public float ImpactMs, Size; public bool Landed;
            public SpriteRenderer Body, BodyGlow, Streak, Hit, HitGlow, Fade;
        }

        HeroController hero;
        Vector2 caster, center;
        float age, atk;
        int wave;
        readonly Spear[] spears = new Spear[Spears];
        Sprite[][] cast, meteor, impact, fade;
        SpriteRenderer circle, circleGlow, castSmall;

        public static void Cast(HeroController owner)
        {
            var go = new GameObject("IceStorm");
            go.transform.SetParent(owner.transform.parent, false);
            go.AddComponent<IceStormUltimate>().Begin(owner);
        }

        static Sprite[][] Rows(int row, float ppu)
        {
            Sprite[][] g = SpriteSheet.Grid(Sheet, 192, 192, ppu, new Vector2(0.5f, 1f - Anchor[row]));
            return g;
        }

        void Begin(HeroController owner)
        {
            hero = owner;
            caster = owner.Position;
            center = caster + DirVec[owner.Facing] * Range + new Vector2(0f, -6f);
            atk = owner.Class.Atk;
            const float spearPpu = 192f / 64f, hitPpu = 192f / 34f, circlePpu = 192f / (Radius * 2.5f);
            cast = Rows(0, circlePpu); meteor = Rows(1, spearPpu); impact = Rows(2, hitPpu); fade = Rows(3, hitPpu);
            int g = UltimateFx.GroundFxOrder;
            circle = UltimateFx.Layer(transform, "Circle", out circleGlow);
            circle.sortingOrder = g + 2; circleGlow.sortingOrder = g + 3;
            circle.transform.position = center; circleGlow.transform.localPosition = Vector3.zero;
            castSmall = ProceduralSprites.Child(transform, "CastSmall", null);
            castSmall.sortingOrder = g + 4;
            castSmall.transform.position = caster;
            castSmall.transform.localScale = Vector3.one * 0.38f;
            for (int i = 0; i < Spears; i++) spears[i] = MakeSpear(i);
            Apply();
        }

        Spear MakeSpear(int i)
        {
            // golden-angle spiral over the circle, 3 per wave (web IceStorm.meteors)
            float angle = i * 2.3999632f, dist = Radius * Mathf.Sqrt((i + 0.5f) / Spears) * 0.88f;
            var s = new Spear
            {
                Impact = center + new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist * 0.72f),
                ImpactMs = CastMs + (i / 3) * WaveMs, Size = 25f + (i % 3) * 4f,
            };
            int o = FeelConfig.FxOrder - 20 + i;
            s.Body = UltimateFx.Layer(transform, "Spear" + i, out s.BodyGlow); s.Body.sortingOrder = o; s.BodyGlow.sortingOrder = o + 40;
            s.Streak = ProceduralSprites.Child(transform, "Streak" + i, FxSprites.Dot());
            s.Streak.sharedMaterial = FeelMaterials.Additive; s.Streak.sortingOrder = o - 30;
            s.Hit = UltimateFx.Layer(transform, "Hit" + i, out s.HitGlow); s.Hit.sortingOrder = o + 60; s.HitGlow.sortingOrder = o + 100;
            s.Hit.transform.position = s.Impact;
            s.Fade = ProceduralSprites.Child(transform, "Fade" + i, null); s.Fade.sortingOrder = o + 61;
            s.Fade.transform.position = s.Impact;
            return s;
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= CastMs + 3f * WaveMs + TailMs) { Destroy(gameObject); return; }
            age += GameClock.Dt * 1000f;
            while (wave < Waves && age >= CastMs + wave * WaveMs) Strike(wave++);
            Apply();
        }

        void Strike(int w)
        {
            List<Monster> hit = SkillDamage.Radial(center, Radius, atk, Mult, 90f, w == Waves - 1);
            for (int i = 0; i < hit.Count; i++) hit[i].Chill(ChillMs);
            CameraFollow.AddTrauma(w == Waves - 1 ? 0.55f : 0.3f);
            GameClock.HitStop(w == Waves - 1 ? 60f : 30f);
            ScreenFlash.Trigger(new Color(0.2f, 0.55f, 1f, w == Waves - 1 ? 0.12f : 0.07f), 0.22f);
            ShockRing.Spawn(center, Radius * 1.05f, 0.4f, new Color(0.4f, 0.85f, 1f, 0.9f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.3f);
            if (w == Waves - 1) CameraFollow.Punch(0.6f);
            for (int i = w * 3; i < w * 3 + 3; i++)
            {
                Spear s = spears[i];
                ShockRing.Spawn(s.Impact, 13f, 0.3f, new Color(0.55f, 0.9f, 1f, 1f), UltimateFx.GroundFxOrder + 7, 0.72f, 0.2f);
                UltimateFx.Shards(s.Impact + new Vector2(0f, 3f), 14, 85f, new Color(0.6f, 0.92f, 1f, 1f));
                UltimateFx.Dust(s.Impact, 2, 5f);
            }
        }

        static void Show(SpriteRenderer sr, SpriteRenderer glow, Sprite[][] rows, int row, float t, float alpha, float glowAlpha)
        {
            bool on = rows != null && t >= 0f && t < 1f && alpha > 0f;
            sr.enabled = on;
            if (glow != null) glow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            Sprite sp = rows[row][Mathf.Min(5, Mathf.FloorToInt(t * 6f))];
            sr.sprite = sp;
            sr.color = new Color(1f, 1f, 1f, alpha);
            if (glow == null) return;
            glow.sprite = sp;
            glow.color = new Color(Cyan.r, Cyan.g, Cyan.b, glowAlpha * alpha);
        }

        void Apply()
        {
            // frozen circle: builds up over the cast, then holds on its full frame and shimmers until the tail fades it
            float castT = age / CastMs;
            float endFade = Mathf.Clamp01((CastMs + 3f * WaveMs + TailMs - age) / 600f);
            if (castT < 1f) Show(circle, circleGlow, cast, 0, castT, 1f, 0.3f);
            else
            {
                circle.enabled = endFade > 0f; circleGlow.enabled = circle.enabled && FeelConfig.FxOn;
                Sprite hold = cast != null ? cast[0][2 + (Mathf.FloorToInt(age / 140f) % 2)] : null;
                circle.sprite = hold; circleGlow.sprite = hold;
                circle.color = new Color(1f, 1f, 1f, endFade);
                circleGlow.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.28f * endFade * (0.7f + 0.3f * Mathf.Sin(age * 0.02f)));
            }
            Show(castSmall, null, cast, 0, castT, 1f, 0f);
            for (int i = 0; i < Spears; i++)
            {
                Spear s = spears[i];
                float f = (age - (s.ImpactMs - FallMs)) / FallMs;
                bool falling = f >= 0f && f < 1f;
                Vector2 pos = s.Impact + FallVec * (1f - f);
                s.Body.transform.position = pos;
                Show(s.Body, s.BodyGlow, meteor, 1, falling ? f : -1f, 1f, 0.4f);
                s.Streak.enabled = falling && FeelConfig.FxOn;
                if (falling)
                {
                    Vector2 d = -FallVec.normalized, mid = pos + d * 16f; // streak trails back up the fall line
                    s.Streak.transform.position = mid;
                    s.Streak.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    s.Streak.transform.localScale = new Vector3(46f * Mathf.Min(1f, f * 2.5f), 5f, 1f);
                    s.Streak.color = new Color(0.5f, 0.88f, 1f, 0.55f * f);
                }
                float hs = s.Size * 1.1f / 34f;
                s.Hit.transform.localScale = Vector3.one * hs;
                Show(s.Hit, s.HitGlow, impact, 2, (age - s.ImpactMs) / 440f, 1f, 0.35f);
                float ft = (age - s.ImpactMs - 360f) / 490f;
                s.Fade.transform.localScale = Vector3.one * hs * 1.15f;
                Show(s.Fade, null, fade, 3, ft, Mathf.Max(0f, 1f - ft), 0f);
            }
        }
    }
}
